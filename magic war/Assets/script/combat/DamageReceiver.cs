using UnityEngine;

public class DamageReceiver : MonoBehaviour
{
    private HealthComponent health;
    private CharacterStats stats;
    private KnockbackController3D knockbackController;
    private StatusEffectController statusEffects;

    private void Awake()
    {
        health = GetComponent<HealthComponent>();
        stats = GetComponent<CharacterStats>();
        knockbackController = GetComponent<KnockbackController3D>();
        statusEffects = GetComponent<StatusEffectController>();
    }

    public void ReceiveDamage(DamageInfo info)
    {
        float damageMultiplier = stats != null ? stats.incomingDamageMultiplier : 1f;
        float finalDamage = Mathf.Max(0f, info.damage * damageMultiplier);

        if (health != null && finalDamage > 0f)
            health.TakeDirectDamage(finalDamage);

        if (knockbackController != null && info.knockbackForce > 0f)
        {
            float knockbackMultiplier = stats != null ? stats.knockbackTakenMultiplier : 1f;
            knockbackController.ApplyKnockback(
                info.knockbackDirection,
                info.knockbackForce * knockbackMultiplier,
                info.knockbackDuration
            );
        }

        if (statusEffects != null)
        {
            if (info.slowPercent > 0f && info.slowDuration > 0f)
                statusEffects.ApplySlow(info.slowPercent, info.slowDuration);

            if (info.burnDamagePerTick > 0f && info.burnDuration > 0f)
                statusEffects.ApplyBurn(info.burnDamagePerTick, info.burnTickInterval, info.burnDuration);
        }
    }
}
