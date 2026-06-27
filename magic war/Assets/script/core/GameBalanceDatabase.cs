using System;
using UnityEngine;

public static class GameBalanceDatabase
{
    private const string ResourcePath = "GameBalance";

    private static GameBalanceData cachedData;
    private static bool hasLoaded;

    public static bool TryGetClassBalance(CharacterClassType classType, out CharacterClassBalance balance)
    {
        balance = null;
        GameBalanceData data = Load();

        if (data == null || data.classes == null)
            return false;

        string className = classType.ToString();
        for (int i = 0; i < data.classes.Length; i++)
        {
            CharacterClassBalance candidate = data.classes[i];
            if (candidate != null && string.Equals(candidate.classType, className, StringComparison.OrdinalIgnoreCase))
            {
                balance = candidate;
                return true;
            }
        }

        return false;
    }

    private static GameBalanceData Load()
    {
        if (hasLoaded)
            return cachedData;

        hasLoaded = true;
        TextAsset asset = Resources.Load<TextAsset>(ResourcePath);
        if (asset == null)
        {
            Debug.LogWarning("Missing balance file at Resources/" + ResourcePath + ".json.");
            return null;
        }

        cachedData = JsonUtility.FromJson<GameBalanceData>(asset.text);
        return cachedData;
    }
}

[Serializable]
public class GameBalanceData
{
    public CharacterClassBalance[] classes;
}

[Serializable]
public class CharacterClassBalance
{
    public string classType;

    public float maxHealth;
    public float moveSpeed;
    public float outgoingDamageMultiplier;
    public float incomingDamageMultiplier;
    public float knockbackTakenMultiplier;
    public float knockbackPowerMultiplier;
    public float slowResistance;
    public float burnResistance;

    public SkillBalanceData primarySkill;
    public SkillBalanceData meleeSkill;
    public AiBalanceData ai;

    public void ApplyStatsTo(CharacterStats stats)
    {
        if (stats == null)
            return;

        CharacterClassType parsedClass;
        if (Enum.TryParse(classType, true, out parsedClass))
            stats.classType = parsedClass;

        stats.maxHealth = maxHealth;
        stats.moveSpeed = moveSpeed;
        stats.outgoingDamageMultiplier = outgoingDamageMultiplier;
        stats.incomingDamageMultiplier = incomingDamageMultiplier;
        stats.knockbackTakenMultiplier = knockbackTakenMultiplier;
        stats.knockbackPowerMultiplier = knockbackPowerMultiplier;
        stats.slowResistance = slowResistance;
        stats.burnResistance = burnResistance;
    }
}

[Serializable]
public class AiBalanceData
{
    public float chaseDistance;
    public float retreatDistance;
    public float strafeDistance;
    public float strafeRefreshInterval;
    public float primaryCastDistance;
    public float meleeCastDistance;

    public void ApplyTo(EnemyHeroController3D target)
    {
        if (target == null)
            return;

        target.SetBehaviorTuning(
            chaseDistance,
            retreatDistance,
            strafeDistance,
            strafeRefreshInterval,
            primaryCastDistance,
            meleeCastDistance);
    }
}

[Serializable]
public class SkillBalanceData
{
    public string skillName;
    public string damageType;
    public float damage;
    public float windup;
    public float recovery;
    public float missRecovery;
    public float cooldown;
    public float knockbackForce;
    public float knockbackDuration;
    public float range;
    public float radius;
    public float projectileSpeed;
    public float projectileLifetime;
    public float slowPercent;
    public float slowDuration;
    public float burnDamagePerTick;
    public float burnTickInterval;
    public float burnDuration;
    public float selfDamageOnCast;

    public void CopyTo(SkillCaster3D.SkillCastConfig target)
    {
        if (target == null)
            return;

        target.skillName = skillName;

        DamageType parsedDamageType;
        if (Enum.TryParse(damageType, true, out parsedDamageType))
            target.damageType = parsedDamageType;

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
