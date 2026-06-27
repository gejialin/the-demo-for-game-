using UnityEngine;

public class BurnEffect : MonoBehaviour
{
    public float damagePerTick = 2f;
    public float tickInterval = 0.5f;
    public float duration = 2f;

    public void ApplyTo(GameObject target)
    {
        if (target == null)
            return;

        StatusEffectController status = target.GetComponent<StatusEffectController>();
        if (status != null)
            status.ApplyBurn(damagePerTick, tickInterval, duration);
    }
}
