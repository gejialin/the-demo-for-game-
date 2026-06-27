using UnityEngine;

[RequireComponent(typeof(Collider))]
public class LavaZone3D : MonoBehaviour
{
    [SerializeField] private ArenaFieldConfig config;

    public float ContactDamagePerTick => config != null ? config.contactDamagePerTick : 0f;
    public float TickInterval => config != null ? config.tickInterval : 1f;
    public bool ApplyBurn => config != null && config.applyBurn;
    public float BurnDamagePerTick => config != null ? config.burnDamagePerTick : 0f;
    public float BurnTickInterval => config != null ? config.burnTickInterval : 1f;
    public float BurnDuration => config != null ? config.burnDuration : 0f;

    public void SetConfig(ArenaFieldConfig fieldConfig)
    {
        config = fieldConfig;
        if (config != null)
            config.Normalize();
    }

    private void Awake()
    {
        if (config != null)
            config.Normalize();
    }

    public bool BlocksProjectiles()
    {
        return config != null && config.lavaBlocksProjectiles;
    }

    private void OnTriggerEnter(Collider other)
    {
        ApplyLavaContact(other);
    }

    private void OnTriggerStay(Collider other)
    {
        ApplyLavaContact(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision == null)
            return;

        ApplyLavaContact(collision.collider);
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision == null)
            return;

        ApplyLavaContact(collision.collider);
    }

    private void ApplyLavaContact(Collider other)
    {
        if (!TryResolveHandler(other, out LavaContactHandler3D handler))
            return;

        handler.TouchLava(this);
    }

    private bool TryResolveHandler(Collider other, out LavaContactHandler3D handler)
    {
        handler = null;
        if (config == null || other == null || !isActiveAndEnabled)
            return false;

        handler = other.GetComponentInParent<LavaContactHandler3D>();
        if (handler != null)
            return true;

        Rigidbody attachedBody = other.attachedRigidbody;
        if (attachedBody != null)
        {
            handler = attachedBody.GetComponent<LavaContactHandler3D>();
            if (handler != null)
                return true;

            handler = attachedBody.GetComponentInParent<LavaContactHandler3D>();
            if (handler != null)
                return true;
        }

        Transform root = other.transform.root;
        if (root != null)
            handler = root.GetComponent<LavaContactHandler3D>();

        return handler != null;
    }
}
