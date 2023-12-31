using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using NaughtyAttributes;
using ScriptableBehaviour;
using Utils;
using System.Collections;

namespace GhostNirvana.Upgrade {

public class UpgradeSystem : MonoBehaviour {
    [SerializeField] ScriptableInt currentHealth; // use to check for death

    [SerializeField] int numAffordableOptionEveryLevel = 3;
    [SerializeField] LinearLimiterFloat experience;
    [SerializeField] LinearInt applianceCollectionAmount;
    [SerializeField] Bank bank;
    [SerializeField, Required] RectTransform levelUpOptionPanel;
    [SerializeField] Buff levelUpBuff;
    [SerializeField] UpgradeOptionDetails upgradeDetails;
    [SerializeField] UpgradeMoneyDisplay upgradeMoneyDetails;
    [SerializeField] int wage;
    [SerializeField] float delayAfterUpgradeSelected;
    bool levelUpSequenceRunning;
    List<UpgradeOption> upgradeOptions = new List<UpgradeOption>();

    [SerializeField] BuffList buffOptions;
    [SerializeField] UnityEvent OnLevelUp;
    [SerializeField] UnityEvent OnOptionSelected;

    Dictionary<Buff, int> buffsTaken = new Dictionary<Buff, int>();
    public List<Buff> BuffsTakenInSequence = new List<Buff>();

    ApplianceCollector collector;
    int level;

    public static UpgradeSystem Instance;

    void Awake() {
        Instance = this;
        collector = GetComponentInChildren<ApplianceCollector>();

        upgradeOptions.AddRange(GetComponentsInChildren<UpgradeOption>());
        levelUpOptionPanel.gameObject.SetActive(false);
    }

    void Update() {
        bool shouldLevelUp = !levelUpSequenceRunning && experience.Value >= experience.Limiter && currentHealth.Value > 0 && !Miyu.Instance.IsDead;
        if (shouldLevelUp) StartLevelUpSequence();
    }

    [Button] void DepositMoney() {
        bank.Deposit(5000);
    }

    [Button] void StartLevelUpSequence() {
        level++;

        OnLevelUp?.Invoke();

        int amountCollected;
        int moneyEarned;
        (amountCollected, moneyEarned) = collector.Collect((int) applianceCollectionAmount.Value);
        moneyEarned += wage;
        upgradeMoneyDetails.SetPaymentDescription(GetRank(), amountCollected, moneyEarned);
        bank.Deposit(wage);

        levelUpOptionPanel.gameObject.SetActive(true);
        levelUpSequenceRunning = true;

        int upgradeOptionsCount = upgradeOptions.Count;

        IEnumerator<Buff> randomBuffs = GetRandomBuffs();

        foreach (UpgradeOption upgradeOption in upgradeOptions) {
            Buff buffChosen = randomBuffs.MoveNext() ?
                randomBuffs.Current : buffOptions.All[Mathx.RandomRange(0, buffOptions.All.Count)];
            upgradeOption.Initialize(buffChosen);
        }

        experience.Value -= Mathf.Max(experience.Limiter, experience.Value);

        Time.timeScale = 0;
    }

    IEnumerator<Buff> GetRandomBuffs() {
        for (int excludeIndex = 0; excludeIndex < buffOptions.All.Count; excludeIndex++) {
            int numAvailable = 0;
            float totalWeight = 0;

            for (int i = excludeIndex; i < buffOptions.All.Count; i++) {
                float weight = ComputeWeight(buffOptions.All[i],
                    chooseOnlyAffordable: excludeIndex < numAffordableOptionEveryLevel);
                totalWeight += weight;
                numAvailable += weight > 0 ? 1 : 0;
            }

            if (numAvailable == 0) yield break;

            float value = Mathx.RandomRange(0, totalWeight);

            int lastIndex = 0;
            bool buffChosen = false;
            for (int i = excludeIndex; i < buffOptions.All.Count; i++) {
                float weight = ComputeWeight(buffOptions.All[i],
                    chooseOnlyAffordable: excludeIndex < numAffordableOptionEveryLevel);
                if (weight == 0) continue;
                lastIndex = i;
                if (value <= weight) {
                    buffChosen = true;
                    yield return buffOptions.All[i];
                    if (i != excludeIndex)
                        (buffOptions.All[i], buffOptions.All[excludeIndex]) = (buffOptions.All[excludeIndex], buffOptions.All[i]);
                    break;
                }
                value -= weight;
            }

            if (!buffChosen) {
                yield return buffOptions.All[lastIndex];
                if (lastIndex != excludeIndex)
                    (buffOptions.All[lastIndex], buffOptions.All[excludeIndex]) = (buffOptions.All[excludeIndex], buffOptions.All[lastIndex]);
            }
        }
    }

    float ComputeWeight(Buff buff, bool chooseOnlyAffordable) {
        buffsTaken.TryGetValue(buff, out int numberOfTimesThisBuffTaken);

        bool takenEnoughTimes = buff.purchaseLimit > 0 && numberOfTimesThisBuffTaken >= buff.purchaseLimit;
        if (takenEnoughTimes) return 0;

        bool hasAllPrereqs = true;
        foreach (Buff prereq in buff.Prerequisites) {
            buffsTaken.TryGetValue(prereq, out int numberOfTimesPrereqTaken);
            hasAllPrereqs &= numberOfTimesPrereqTaken > 0;
        }

        if (!hasAllPrereqs) return 0;

        bool canAffordBuff = bank.Value >= buff.Cost;
        if (chooseOnlyAffordable && !canAffordBuff) return 0;

        return buff.Weight;
    }

    public void EndLevelUpSequence(Buff chosenBuff) {
        StartCoroutine(_EndLevelUpSequence(chosenBuff));
    }

    IEnumerator _EndLevelUpSequence(Buff chosenBuff) {
        buffsTaken.TryGetValue(chosenBuff, out int numberOfTimesBuffTaken);
        buffsTaken[chosenBuff] = numberOfTimesBuffTaken + 1;
        BuffsTakenInSequence.Add(chosenBuff);

        levelUpBuff?.Apply();
        bank.Withraw(chosenBuff.Cost);

        OnOptionSelected?.Invoke();
        yield return new WaitForSecondsRealtime(delayAfterUpgradeSelected);

        levelUpSequenceRunning = false;
        levelUpOptionPanel.gameObject.SetActive(false);
        Time.timeScale = 1;
    }

    public string GetRank() {
        int levelUnaccounted = level;
        string suffix = "";
        int maxLoop = 9;
        while (levelUnaccounted > 0 && maxLoop-->0) {
            int digit = Mathf.Min(levelUnaccounted, 9);
            suffix += digit;
            levelUnaccounted -= digit;
        }
        if (suffix.Length == 0)
            suffix = "0";
        return "0." + suffix;
    }
}

}
