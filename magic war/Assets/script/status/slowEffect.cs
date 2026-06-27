using UnityEngine;

public class slowEffect : MonoBehaviour
{
    [Range(0f, 1f)] public float slowPercent = 0.35f;
    public float duration = 1.5f;

    public void ApplyTo(GameObject target)
    {
        if (target == null)
            return;

        StatusEffectController status = target.GetComponent<StatusEffectController>();
        if (status != null)
            status.ApplySlow(slowPercent, duration);
    }
}
