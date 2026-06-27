using UnityEngine;

[System.Serializable]
public class SkillCastData
{
    public string skillName = "Skill";
    public DamageType damageType = DamageType.Physical;
    public float damage = 20f;
    public float windup = 0.2f;
    public float recovery = 0.25f;
    public float missRecovery = 0.25f;
    public float cooldown = 0.5f;
    public float knockbackForce = 8f;
    public float knockbackDuration = 0.18f;
    public float range = 2f;
    public float radius = 0.8f;
    public float projectileSpeed = 14f;
    public float projectileLifetime = 3f;
    public float slowPercent;
    public float slowDuration;
    public float burnDamagePerTick;
    public float burnTickInterval = 0.5f;
    public float burnDuration;
    public float selfDamageOnCast;

    public void CopyTo(SkillCaster3D.SkillCastConfig target)
    {
        if (target == null)
            return;

        target.skillName = skillName;
        target.damageType = damageType;
        target.damage = damage;
        target.windup = windup;
        target.recovery = recovery;
        target.missRecovery = missRecovery;
        target.cooldown = cooldown;
        target.knockbackForce = knockbackForce;
        target.knockbackDuration = knockbackDuration;
        target.range = range;
        target.radius = radius;
        target.projectileSpeed = projectileSpeed;
        target.projectileLifetime = projectileLifetime;
        target.slowPercent = slowPercent;
        target.slowDuration = slowDuration;
        target.burnDamagePerTick = burnDamagePerTick;
        target.burnTickInterval = burnTickInterval;
        target.burnDuration = burnDuration;
        target.selfDamageOnCast = selfDamageOnCast;
    }
}
