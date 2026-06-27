using UnityEngine;

public class EnemyHeroController3D : MonoBehaviour
{
    [SerializeField] private float chaseDistance = 10f;
    [SerializeField] private float retreatDistance = 2.2f;
    [SerializeField] private float strafeDistance = 4.6f;
    [SerializeField] private float strafeRefreshInterval = 1.1f;
    [SerializeField] private float primaryCastDistance = 8.5f;
    [SerializeField] private float meleeCastDistance = 2.3f;

    private CharacterMotor3D motor;
    private SkillCaster3D skillCaster;
    private CharacterFacing3D facing;
    private CharacterStateController stateController;
    private TeamTag teamTag;
    private ShrinkingArenaField arenaField;

    private Transform target;
    private float nextStrafeRefreshTime;
    private Vector3 currentMoveTarget;

    private void Awake()
    {
        motor = GetComponent<CharacterMotor3D>();
        skillCaster = GetComponent<SkillCaster3D>();
        facing = GetComponent<CharacterFacing3D>();
        stateController = GetComponent<CharacterStateController>();
        teamTag = GetComponent<TeamTag>();
        arenaField = FindObjectOfType<ShrinkingArenaField>();
        ApplyBalanceTuning();
    }

    private void Update()
    {
        if (stateController != null && stateController.currentState == CharacterState.Dead)
        {
            if (motor != null)
                motor.StopMovement();
            return;
        }

        ResolveTarget();
        if (target == null)
        {
            if (motor != null)
                motor.StopMovement();
            return;
        }

        Vector3 toTarget = target.position - transform.position;
        toTarget.y = 0f;
        float distance = toTarget.magnitude;

        if (toTarget.sqrMagnitude > 0.001f && facing != null)
            facing.FaceDirection(toTarget.normalized);

        if (stateController != null && !stateController.CanCast)
            return;

        if (distance <= meleeCastDistance && skillCaster != null && skillCaster.TryCastMelee(toTarget.normalized))
            return;

        if (distance <= primaryCastDistance && skillCaster != null && skillCaster.TryCastPrimary(toTarget.normalized))
            return;

        UpdateMovement(distance, toTarget);
    }

    private void ResolveTarget()
    {
        if (target != null && target.gameObject.activeInHierarchy)
        {
            TeamTag targetTeam = target.GetComponent<TeamTag>();
            if (teamTag == null || !teamTag.IsSameTeam(targetTeam))
                return;
        }

        PlayerController3D player = PlayerController3D.ActivePlayer;
        if (player == null)
            player = FindObjectOfType<PlayerController3D>();
        if (player == null)
        {
            target = null;
            return;
        }

        TeamTag playerTeam = player.GetComponent<TeamTag>();
        if (teamTag != null && teamTag.IsSameTeam(playerTeam))
        {
            target = null;
            return;
        }

        target = player.transform;
    }

    private void UpdateMovement(float distance, Vector3 toTarget)
    {
        if (motor == null || target == null)
            return;

        if (distance > chaseDistance)
        {
            currentMoveTarget = GetSafeMoveTarget(target.position, 0.25f);
            motor.SetMoveTarget(currentMoveTarget);
            return;
        }

        if (distance < retreatDistance)
        {
            Vector3 retreatTarget = transform.position - (toTarget.normalized * Mathf.Max(1.2f, retreatDistance - distance + 1f));
            retreatTarget.y = transform.position.y;
            retreatTarget = GetSafeMoveTarget(retreatTarget, 0.35f);
            motor.SetMoveTarget(retreatTarget);
            currentMoveTarget = retreatTarget;
            return;
        }

        if (Time.time >= nextStrafeRefreshTime || !motor.HasMoveTarget())
        {
            nextStrafeRefreshTime = Time.time + strafeRefreshInterval;
            Vector3 lateral = Vector3.Cross(Vector3.up, toTarget.normalized);
            float side = Mathf.Sin(Time.time * 2.13f) >= 0f ? 1f : -1f;
            Vector3 strafeTarget = target.position - (toTarget.normalized * strafeDistance) + (lateral * side * 1.8f);
            strafeTarget.y = transform.position.y;
            strafeTarget = GetSafeMoveTarget(strafeTarget, 0.35f);
            currentMoveTarget = strafeTarget;
            motor.SetMoveTarget(currentMoveTarget);
        }
    }

    private Vector3 GetSafeMoveTarget(Vector3 desiredTarget, float inset)
    {
        if (arenaField == null)
            arenaField = FindObjectOfType<ShrinkingArenaField>();

        if (arenaField == null)
            return desiredTarget;

        Vector3 currentPosition = transform.position;
        if (!arenaField.IsWorldPositionInsideSafeZone(desiredTarget, inset))
            desiredTarget = arenaField.ClampWorldPositionToSafeZone(desiredTarget, inset);

        float currentEdgeDistance = arenaField.GetSignedEdgeDistance(currentPosition);
        if (currentEdgeDistance < inset)
        {
            Vector3 recentered = arenaField.ClampWorldPositionToSafeZone(currentPosition, inset);
            recentered.y = desiredTarget.y;
            return recentered;
        }

        desiredTarget.y = transform.position.y;
        return desiredTarget;
    }

    public void SetBehaviorTuning(
        float newChaseDistance,
        float newRetreatDistance,
        float newStrafeDistance,
        float newStrafeRefreshInterval,
        float newPrimaryCastDistance,
        float newMeleeCastDistance)
    {
        chaseDistance = Mathf.Max(0.5f, newChaseDistance);
        retreatDistance = Mathf.Max(0.2f, newRetreatDistance);
        strafeDistance = Mathf.Max(0.2f, newStrafeDistance);
        strafeRefreshInterval = Mathf.Max(0.05f, newStrafeRefreshInterval);
        primaryCastDistance = Mathf.Max(0.2f, newPrimaryCastDistance);
        meleeCastDistance = Mathf.Max(0.2f, newMeleeCastDistance);
    }

    public void ResetRuntimeState()
    {
        target = null;
        nextStrafeRefreshTime = 0f;
        currentMoveTarget = Vector3.zero;

        if (motor != null)
            motor.StopMovement();
    }

    private void ApplyBalanceTuning()
    {
        CharacterStats stats = GetComponent<CharacterStats>();
        if (stats == null)
            return;

        CharacterClassBalance balance;
        if (GameBalanceDatabase.TryGetClassBalance(stats.classType, out balance) && balance != null && balance.ai != null)
        {
            balance.ai.ApplyTo(this);
            return;
        }

        if (stats.ClassConfig != null && stats.ClassConfig.ai != null)
            stats.ClassConfig.ai.ApplyTo(this);
    }
}
