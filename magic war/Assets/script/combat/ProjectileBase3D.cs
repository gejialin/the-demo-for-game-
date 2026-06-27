using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ProjectileBase3D : MonoBehaviour
{
    [SerializeField] private bool destroyOnHit = true;
    [SerializeField] private bool destroyOnBlocked = true;

    private DamageInfo damageInfo;
    private Vector3 direction;
    private float speed;
    private float lifeEndTime;
    private GameObject owner;
    private TeamTag ownerTeam;
    private bool initialized;
    private Collider projectileCollider;
    private bool impactResolved;

    public void Initialize(DamageInfo info, Vector3 fireDirection, float projectileSpeed, float lifetime, GameObject projectileOwner, TeamTag projectileOwnerTeam)
    {
        damageInfo = info;
        direction = fireDirection;
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.001f)
        {
            direction = transform.forward;
            direction.y = 0f;
        }

        direction = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector3.forward;
        speed = projectileSpeed;
        lifeEndTime = Time.time + lifetime;
        owner = projectileOwner;
        ownerTeam = projectileOwnerTeam;
        initialized = true;
    }

    private void Awake()
    {
        projectileCollider = GetComponent<Collider>();
        if (projectileCollider != null)
            projectileCollider.isTrigger = true;
    }

    private void Update()
    {
        if (!initialized || impactResolved)
            return;

        Vector3 travel = direction * speed * Time.deltaTime;
        if (travel.sqrMagnitude > 0.000001f)
        {
            if (TryResolveSweepHit(travel, out RaycastHit sweepHit))
            {
                transform.position = sweepHit.point;
                HandleCollision(sweepHit.collider, sweepHit.point);
            }
            else
            {
                transform.position += travel;
            }
        }

        if (Time.time >= lifeEndTime)
            Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!initialized || impactResolved)
            return;

        HandleCollision(other, other != null ? other.ClosestPoint(transform.position) : transform.position);
    }

    private bool TryResolveSweepHit(Vector3 travel, out RaycastHit hit)
    {
        Vector3 origin = transform.position;
        float distance = travel.magnitude;
        float radius = GetSweepRadius();

        if (radius > 0.001f)
            return Physics.SphereCast(origin, radius, direction, out hit, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);

        return Physics.Raycast(origin, direction, out hit, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
    }

    private float GetSweepRadius()
    {
        if (projectileCollider == null)
            return 0f;

        SphereCollider sphere = projectileCollider as SphereCollider;
        if (sphere != null)
            return sphere.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.z);

        CapsuleCollider capsule = projectileCollider as CapsuleCollider;
        if (capsule != null)
            return capsule.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.z);

        Vector3 extents = projectileCollider.bounds.extents;
        return Mathf.Max(0f, Mathf.Min(extents.x, extents.y, extents.z));
    }

    private void HandleCollision(Collider other, Vector3 hitPoint)
    {
        if (other == null)
            return;

        if (owner != null && (other.gameObject == owner || other.transform.IsChildOf(owner.transform)))
            return;

        TeamTag otherTeam = other.GetComponentInParent<TeamTag>();
        if (ownerTeam != null && ownerTeam.IsSameTeam(otherTeam))
            return;

        Hurtbox hurtbox = other.GetComponentInParent<Hurtbox>();
        DamageReceiver receiver = hurtbox == null ? other.GetComponentInParent<DamageReceiver>() : null;
        if (hurtbox == null && receiver == null)
        {
            if (ShouldBlockProjectile(other))
                HandleProjectileBlocked();
            return;
        }

        DamageInfo hitInfo = damageInfo;
        hitInfo.hitPoint = hitPoint;
        hitInfo.knockbackDirection = direction;

        if (hurtbox != null)
            hurtbox.ReceiveDamage(hitInfo);
        else
            receiver.ReceiveDamage(hitInfo);

        impactResolved = true;

        if (destroyOnHit)
            Destroy(gameObject);
    }

    private bool ShouldBlockProjectile(Collider other)
    {
        if (other == null)
            return false;

        ProjectileBlocker3D blocker = other.GetComponentInParent<ProjectileBlocker3D>();
        if (blocker != null && blocker.BlocksProjectiles)
            return true;

        LavaZone3D lavaZone = other.GetComponentInParent<LavaZone3D>();
        if (lavaZone != null && lavaZone.BlocksProjectiles())
            return true;

        return false;
    }

    private void HandleProjectileBlocked()
    {
        impactResolved = true;
        initialized = false;

        if (destroyOnBlocked)
            Destroy(gameObject);
    }
}
