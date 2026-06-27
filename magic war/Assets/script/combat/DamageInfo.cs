using UnityEngine;

[System.Serializable]
public struct DamageInfo
{
    public float damage;
    public DamageType damageType;
    public GameObject source;
    public Vector3 hitPoint;
    public Vector3 knockbackDirection;
    public float knockbackForce;
    public float knockbackDuration;
    public float slowPercent;
    public float slowDuration;
    public float burnDamagePerTick;
    public float burnTickInterval;
    public float burnDuration;

    public DamageInfo(float damage, DamageType damageType, GameObject source, Vector3 knockbackDirection, float knockbackForce, float knockbackDuration)
    {
        this.damage = damage;
        this.damageType = damageType;
        this.source = source;
        hitPoint = Vector3.zero;
        this.knockbackDirection = knockbackDirection;
        this.knockbackForce = knockbackForce;
        this.knockbackDuration = knockbackDuration;
        slowPercent = 0f;
        slowDuration = 0f;
        burnDamagePerTick = 0f;
        burnTickInterval = 0.5f;
        burnDuration = 0f;
    }
}
