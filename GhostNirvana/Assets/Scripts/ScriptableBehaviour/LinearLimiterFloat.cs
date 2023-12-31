using UnityEngine;
using NaughtyAttributes;

namespace ScriptableBehaviour {

[CreateAssetMenu(fileName = "Data",
                 menuName = "~/Stats/LinearLimitedFloat", order = 1)]
public class LinearLimiterFloat : LinearFloat, ISerializationCallbackReceiver, ILimited<float> {
    /* [System.NonSerialized] */
    [ReadOnly]
    public float Limiter;

    float ILimited<float>.Value { get => Value; set => Value = value; }

    public override void OnAfterDeserialize() {
        base.OnAfterDeserialize();
        Value = 0;
    }

    /// Need to be called every time either additive or multiplicative scale is changed.
    public override void Recompute() {
        Limiter = (BaseValue + AdditiveScale) * MultiplicativeScale;
    }
    public void CheckAndCorrectLimit() => Value = Mathf.Clamp(Value, 0, Limiter);
}

}
