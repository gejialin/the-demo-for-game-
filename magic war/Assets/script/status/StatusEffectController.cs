using System.Collections;
using UnityEngine;

public class StatusEffectController : MonoBehaviour
{
    private CharacterStats stats;
    private HealthComponent health;

    private Coroutine slowRoutine;
    private Coroutine burnRoutine;
    private float burnEndTime;
    private float burnDamagePerTick;
    private float burnTickInterval;
    private float lastBurnAppliedTime = -999f;

    public bool IsBurning => burnRoutine != null && Time.time < burnEndTime;
    public float BurnTimeRemaining => Mathf.Max(0f, burnEndTime - Time.time);
    public float LastBurnAppliedTime => lastBurnAppliedTime;

    private void Awake()
    {
        stats = GetComponent<CharacterStats>();
        health = GetComponent<HealthComponent>();
    }

    public void ApplySlow(float slowPercent, float duration)
    {
        if (health == null || health.IsDead || stats == null || duration <= 0f)
            return;

        if (slowRoutine != null) StopCoroutine(slowRoutine);
        slowRoutine = StartCoroutine(SlowRoutine(slowPercent, duration));
    }

    private IEnumerator SlowRoutine(float slowPercent, float duration)
    {
        float resistance = stats != null ? stats.slowResistance : 0f;
        float realSlow = Mathf.Clamp01(slowPercent * (1f - resistance));
        stats.currentMoveSpeed = stats.moveSpeed * (1f - realSlow);

        yield return new WaitForSeconds(duration);

        stats.currentMoveSpeed = stats.moveSpeed;
        slowRoutine = null;
    }

    public void ApplyBurn(float damagePerTick, float tickInterval, float duration)
    {
        if (health == null || health.IsDead || damagePerTick <= 0f || tickInterval <= 0f || duration <= 0f)
            return;

        float resistance = stats != null ? stats.burnResistance : 0f;
        burnDamagePerTick = Mathf.Max(burnDamagePerTick, damagePerTick * (1f - resistance));
        burnTickInterval = tickInterval;
        burnEndTime = Mathf.Max(burnEndTime, Time.time + duration);
        lastBurnAppliedTime = Time.time;

        if (burnRoutine == null)
            burnRoutine = StartCoroutine(BurnRoutine());
    }

    private IEnumerator BurnRoutine()
    {
        while (Time.time < burnEndTime)
        {
            if (health == null || health.IsDead)
                break;

            health.TakeDirectDamage(burnDamagePerTick);
            yield return new WaitForSeconds(burnTickInterval);
        }

        burnDamagePerTick = 0f;
        burnTickInterval = 0f;
        burnEndTime = 0f;
        burnRoutine = null;
    }

    public void ClearEffects()
    {
        if (slowRoutine != null)
        {
            StopCoroutine(slowRoutine);
            slowRoutine = null;
        }

        if (burnRoutine != null)
        {
            StopCoroutine(burnRoutine);
            burnRoutine = null;
        }

        burnDamagePerTick = 0f;
        burnTickInterval = 0f;
        burnEndTime = 0f;
        lastBurnAppliedTime = -999f;

        if (stats != null)
            stats.currentMoveSpeed = stats.moveSpeed;
    }
}
