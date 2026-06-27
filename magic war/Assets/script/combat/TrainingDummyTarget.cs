using UnityEngine;

public class TrainingDummyTarget : MonoBehaviour
{
    [SerializeField] private Transform resetPoint;
    [SerializeField] private float respawnDelay = 1.75f;
    [SerializeField] private bool faceResetPointOnRespawn = true;
    [SerializeField] private float fallRespawnHeight = -8f;

    private HealthComponent health;
    private KnockbackController3D knockbackController;
    private StatusEffectController statusEffectController;
    private CharacterStateController stateController;
    private Rigidbody rb;

    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private bool respawnQueued;
    private float respawnTime;

    private void Awake()
    {
        health = GetComponent<HealthComponent>();
        knockbackController = GetComponent<KnockbackController3D>();
        statusEffectController = GetComponent<StatusEffectController>();
        stateController = GetComponent<CharacterStateController>();
        rb = GetComponent<Rigidbody>();

        initialPosition = transform.position;
        initialRotation = transform.rotation;

        if (health != null)
            health.SetDeactivateOnDeath(false);
    }

    private void Update()
    {
        if (health == null)
            return;

        if (!respawnQueued && health.IsDead)
        {
            respawnQueued = true;
            respawnTime = Time.time + respawnDelay;
        }

        if (!respawnQueued && transform.position.y <= fallRespawnHeight)
        {
            respawnQueued = true;
            respawnTime = Time.time + 0.25f;
        }

        if (respawnQueued && Time.time >= respawnTime)
            RespawnNow();
    }

    public void RespawnNow()
    {
        respawnQueued = false;

        Vector3 targetPosition = resetPoint != null ? resetPoint.position : initialPosition;
        Quaternion targetRotation = resetPoint != null ? resetPoint.rotation : initialRotation;

        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        transform.position = targetPosition;
        transform.rotation = faceResetPointOnRespawn ? targetRotation : initialRotation;

        if (statusEffectController != null)
            statusEffectController.ClearEffects();

        if (knockbackController != null)
            knockbackController.CancelKnockback();

        if (health != null)
            health.Revive();

        if (stateController != null)
            stateController.SetState(CharacterState.Idle);
    }
}
