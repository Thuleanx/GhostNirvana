using Base;
using UnityEngine;
using CombatSystem;
using ScriptableBehaviour;
using Optimization;
using Utils;

namespace GhostNirvana {

public class SpawnOnHit : MonoBehaviour {
    [SerializeField] GameObject prefab;
    [SerializeField] ScriptableFloat chanceToSpawnOnHit;
    Miyu miyu;

    void Awake() {
        miyu = GetComponentInParent<Miyu>();
    }

    protected void OnEnable() {
        miyu.OnHitEvent?.AddListener(OnHit);
    }
    
    protected void OnDisable() {
        miyu.OnHitEvent?.RemoveListener(OnHit);
    }

    void OnHit(Hit hit) {
        float spawnChance = chanceToSpawnOnHit ? chanceToSpawnOnHit.Value : 0;
        if (spawnChance <= 0) return;
        spawnChance = Mathf.Min(spawnChance, 1);

        bool shouldSpawn = Mathx.RandomRange(0.0f, 1.0f) < spawnChance;
        if (!shouldSpawn) return;

        Vector3 spawnPosition = (hit.Hurtbox as MonoBehaviour)?.transform?.position ?? hit.Position;
        spawnPosition.y = 0;

        ObjectPoolManager.Instance.Borrow(
            gameObject.scene, prefab.transform,
            spawnPosition, Quaternion.identity
        );
    }
}

}