using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkillCaster3D : MonoBehaviour
{
    private const string DefaultProjectileSpawnPointName = "ProjectileSpawnPoint";

    [System.Serializable]
    public class SkillCastConfig
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
    }

    [Header("Skill Config")]
    public SkillCastConfig primarySkill = new SkillCastConfig
    {
        skillName = "远程",
        damageType = DamageType.Arcane,
        damage = 18f,
        windup = 0.18f,
        recovery = 0.2f,
        missRecovery = 0.2f,
        cooldown = 0.6f,
        knockbackForce = 8f,
        knockbackDuration = 0.16f,
        range = 16f,
        radius = 0.35f,
        projectileSpeed = 14f,
        projectileLifetime = 3f
    };

    public SkillCastConfig meleeSkill = new SkillCastConfig
    {
        skillName = "近战",
        damageType = DamageType.Physical,
        damage = 28f,
        windup = 0.25f,
        recovery = 0.35f,
        missRecovery = 0.35f,
        cooldown = 1f,
        knockbackForce = 12f,
        knockbackDuration = 0.2f,
        range = 2f,
        radius = 1f
    };

    [Header("References")]
    [SerializeField] private ProjectileBase3D projectilePrefab;
    [SerializeField] private Transform projectileSpawnPoint;
    [SerializeField] private LayerMask hittableMask = ~0;

    private CharacterMotor3D motor;
    private CharacterStats stats;
    private HealthComponent health;
    private CharacterStateController stateController;
    private TeamTag teamTag;
    private float nextPrimaryTime;
    private float nextMeleeTime;
    private bool isCasting;
    private Coroutine activeCastRoutine;

    private void Awake()
    {
        motor = GetComponent<CharacterMotor3D>();
        stats = GetComponent<CharacterStats>();
        health = GetComponent<HealthComponent>();
        stateController = GetComponent<CharacterStateController>();
        teamTag = GetComponent<TeamTag>();
        ResolveLayerMasks();

        ResolveProjectileSpawnPoint();

        ApplyClassConfig();
        NormalizeSkillDisplayNames();
    }

    public string GetPrimarySkillDisplayName()
    {
        return LocalizedUiTextBridge.LocalizeSkillName(primarySkill.skillName, "skill.primary_name", "远程");
    }

    public string GetMeleeSkillDisplayName()
    {
        return LocalizedUiTextBridge.LocalizeSkillName(meleeSkill.skillName, "skill.melee_name", "近战");
    }

    public bool TryCastPrimary(Vector3 direction)
    {
        if (!CanStartCast(nextPrimaryTime, direction))
            return false;

        nextPrimaryTime = Time.time + primarySkill.cooldown;
        activeCastRoutine = StartCoroutine(CastRoutine(primarySkill, direction, true));
        return true;
    }

    public bool TryCastMelee(Vector3 direction)
    {
        if (!CanStartCast(nextMeleeTime, direction))
            return false;

        nextMeleeTime = Time.time + meleeSkill.cooldown;
        activeCastRoutine = StartCoroutine(CastRoutine(meleeSkill, direction, false));
        return true;
    }

    public float GetPrimaryCooldownRemaining()
    {
        return Mathf.Max(0f, nextPrimaryTime - Time.time);
    }

    public float GetMeleeCooldownRemaining()
    {
        return Mathf.Max(0f, nextMeleeTime - Time.time);
    }

    public void ResetRuntimeState()
    {
        if (activeCastRoutine != null)
        {
            StopCoroutine(activeCastRoutine);
            activeCastRoutine = null;
        }

        isCasting = false;
        nextPrimaryTime = 0f;
        nextMeleeTime = 0f;

        if (motor != null)
            motor.StopMovement();

        if (stateController != null && stateController.currentState != CharacterState.Dead)
            stateController.ResetRuntimeState();
    }

    private bool CanStartCast(float nextReadyTime, Vector3 direction)
    {
        direction.y = 0f;

        if (isCasting || Time.time < nextReadyTime || direction.sqrMagnitude <= 0.001f)
            return false;

        return stateController == null || stateController.CanCast;
    }

    private IEnumerator CastRoutine(SkillCastConfig config, Vector3 direction, bool isPrimary)
    {
        isCasting = true;
        direction.y = 0f;
        direction.Normalize();

        if (motor != null)
            motor.StopMovement();

        if (stateController != null)
            stateController.SetState(CharacterState.Casting);

        if (config.selfDamageOnCast > 0f && health != null)
            health.TakeDirectDamage(config.selfDamageOnCast);

        yield return new WaitForSeconds(config.windup);

        float recoveryDuration = config.recovery;
        if (stateController == null || stateController.currentState != CharacterState.Dead)
        {
            bool hitSomething = true;
            if (isPrimary)
            {
                FireProjectile(config, direction);
            }
            else
            {
                hitSomething = PerformMeleeHit(config, direction);
            }

            if (!hitSomething)
                recoveryDuration = Mathf.Max(config.recovery, config.missRecovery);
        }

        if (stateController != null && stateController.currentState != CharacterState.Dead)
            stateController.SetState(CharacterState.Recovery);

        yield return new WaitForSeconds(recoveryDuration);

        if (stateController != null)
            stateController.ReturnToIdleIfNotDead();

        isCasting = false;
        activeCastRoutine = null;
    }

    private void FireProjectile(SkillCastConfig config, Vector3 direction)
    {
        if (projectilePrefab == null)
            return;

        Vector3 spawnPosition = GetProjectileSpawnPosition();
        ProjectileBase3D projectile = Instantiate(
            projectilePrefab,
            spawnPosition,
            Quaternion.LookRotation(direction, Vector3.up)
        );

        projectile.Initialize(CreateDamageInfo(config, direction, spawnPosition), direction, config.projectileSpeed, config.projectileLifetime, gameObject, teamTag);
    }

    private Vector3 GetProjectileSpawnPosition()
    {
        if (projectileSpawnPoint != null && projectileSpawnPoint != transform)
            return projectileSpawnPoint.position;

        Collider ownCollider = GetComponent<Collider>();
        if (ownCollider != null)
        {
            Vector3 center = ownCollider.bounds.center;
            center.y = Mathf.Max(transform.position.y + 0.35f, center.y);
            return center;
        }

        return transform.position + Vector3.up * 0.6f;
    }

    private void ResolveProjectileSpawnPoint()
    {
        if (projectileSpawnPoint != null && projectileSpawnPoint != transform)
            return;

        Transform childSpawnPoint = transform.Find(DefaultProjectileSpawnPointName);
        projectileSpawnPoint = childSpawnPoint != null ? childSpawnPoint : transform;
    }

    private bool PerformMeleeHit(SkillCastConfig config, Vector3 direction)
    {
        ResolveLayerMasks();
        Vector3 center = transform.position + direction.normalized * config.range;
        Collider[] hits = Physics.OverlapSphere(center, config.radius, hittableMask, QueryTriggerInteraction.Collide);
        HashSet<DamageReceiver> damagedReceivers = new HashSet<DamageReceiver>();

        for (int i = 0; i < hits.Length; i++)
        {
            Collider hit = hits[i];
            if (hit.transform == transform || hit.transform.IsChildOf(transform))
                continue;

            if (IsSameTeam(hit))
                continue;

            Hurtbox hurtbox = hit.GetComponentInParent<Hurtbox>();
            DamageReceiver receiver = hurtbox != null
                ? hurtbox.GetComponentInParent<DamageReceiver>()
                : hit.GetComponentInParent<DamageReceiver>();

            if (receiver != null && !damagedReceivers.Add(receiver))
                continue;

            DamageInfo info = CreateDamageInfo(config, direction, hit.ClosestPoint(transform.position));

            if (hurtbox != null)
                hurtbox.ReceiveDamage(info);
            else if (receiver != null)
                receiver.ReceiveDamage(info);
        }

        return damagedReceivers.Count > 0;
    }

    private DamageInfo CreateDamageInfo(SkillCastConfig config, Vector3 direction, Vector3 hitPoint)
    {
        float outgoingMultiplier = stats != null ? stats.outgoingDamageMultiplier : 1f;
        float knockbackMultiplier = stats != null ? stats.knockbackPowerMultiplier : 1f;

        DamageInfo info = new DamageInfo(
            config.damage * outgoingMultiplier,
            config.damageType,
            gameObject,
            direction,
            config.knockbackForce * knockbackMultiplier,
            config.knockbackDuration
        );

        info.hitPoint = hitPoint;
        info.slowPercent = config.slowPercent;
        info.slowDuration = config.slowDuration;
        info.burnDamagePerTick = config.burnDamagePerTick;
        info.burnTickInterval = config.burnTickInterval;
        info.burnDuration = config.burnDuration;
        return info;
    }

    private bool IsSameTeam(Collider hit)
    {
        if (teamTag == null)
            return false;

        TeamTag otherTeam = hit.GetComponentInParent<TeamTag>();
        return teamTag.IsSameTeam(otherTeam);
    }

    private void ApplyClassConfig()
    {
        CharacterClassBalance balance;
        if (stats != null && GameBalanceDatabase.TryGetClassBalance(stats.classType, out balance))
        {
            if (balance.primarySkill != null)
                balance.primarySkill.CopyTo(primarySkill);
            else if (stats.ClassConfig != null && stats.ClassConfig.primarySkill != null)
                stats.ClassConfig.primarySkill.CopyTo(primarySkill);

            if (balance.meleeSkill != null)
                balance.meleeSkill.CopyTo(meleeSkill);
            else if (stats.ClassConfig != null && stats.ClassConfig.meleeSkill != null)
                stats.ClassConfig.meleeSkill.CopyTo(meleeSkill);

            NormalizeSkillDisplayNames();
            return;
        }

        if (stats == null || stats.ClassConfig == null)
            return;

        if (stats.ClassConfig.primarySkill != null)
            stats.ClassConfig.primarySkill.CopyTo(primarySkill);

        if (stats.ClassConfig.meleeSkill != null)
            stats.ClassConfig.meleeSkill.CopyTo(meleeSkill);

        NormalizeSkillDisplayNames();
    }

    private void NormalizeSkillDisplayNames()
    {
        primarySkill.skillName = LocalizedUiTextBridge.LocalizeSkillName(primarySkill.skillName, "skill.primary_name", "远程");
        meleeSkill.skillName = LocalizedUiTextBridge.LocalizeSkillName(meleeSkill.skillName, "skill.melee_name", "近战");
    }

    private void ResolveLayerMasks()
    {
        if (hittableMask.value == 0)
            hittableMask = LayerMaskConfig.GetHittableMask();
    }
}
