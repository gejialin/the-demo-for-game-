using UnityEngine;

public class LavaContactHandler3D : MonoBehaviour
{
    private const float DefaultLavaGraceWindow = 0.2f;

    private HealthComponent health;
    private StatusEffectController status;
    private float nextTickTime = -1f;
    private float lastLavaTouchTime = -999f;

    public float LastLavaTouchTime => lastLavaTouchTime;
    public bool IsRecentlyTouchingLava(float graceWindow = DefaultLavaGraceWindow)
    {
        return graceWindow >= 0f && Time.time - lastLavaTouchTime <= graceWindow;
    }

    private void Awake()
    {
        health = GetComponent<HealthComponent>();
        status = GetComponent<StatusEffectController>();
    }

    private void OnDisable()
    {
        nextTickTime = -1f;
        lastLavaTouchTime = -999f;
    }

    public void TouchLava(LavaZone3D lava)
    {
        if (lava == null || health == null || !isActiveAndEnabled || health.IsDead)
            return;

        // Keep contact state fresh on every stay callback so HUD/overlay does not flicker
        // between "Lava" and "Burn" while damage is still waiting for its next tick.
        lastLavaTouchTime = Time.time;

        if (Time.time < nextTickTime)
            return;

        nextTickTime = Time.time + Mathf.Max(0.01f, lava.TickInterval);

        float contactDamage = Mathf.Max(0f, lava.ContactDamagePerTick);
        if (contactDamage > 0f)
            health.TakeDirectDamage(contactDamage);

        if (lava.ApplyBurn && status != null && !health.IsDead)
        {
            status.ApplyBurn(
                Mathf.Max(0f, lava.BurnDamagePerTick),
                Mathf.Max(0.01f, lava.BurnTickInterval),
                Mathf.Max(0f, lava.BurnDuration));
        }
    }
}
