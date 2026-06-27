using UnityEngine;

public class HealthComponent : MonoBehaviour
{
    [SerializeField] private bool deactivateOnDeath = true;

    public float maxHealth = 100f;
    public float currentHealth;

    private CharacterStateController stateController;
    private CharacterStats stats;
    private StatusEffectController statusEffectController;
    private KnockbackController3D knockbackController;

    public float NormalizedHealth => maxHealth <= 0f ? 0f : currentHealth / maxHealth;
    public bool IsDead => stateController != null
        ? stateController.currentState == CharacterState.Dead
        : currentHealth <= 0f;

    private void Awake()
    {
        stats = GetComponent<CharacterStats>();
        if (stats != null)
            maxHealth = stats.maxHealth;

        currentHealth = maxHealth;
        stateController = GetComponent<CharacterStateController>();
        statusEffectController = GetComponent<StatusEffectController>();
        knockbackController = GetComponent<KnockbackController3D>();
    }

    public void TakeDirectDamage(float amount)
    {
        if (IsDead)
            return;

        currentHealth -= amount;
        currentHealth = Mathf.Max(currentHealth, 0f);

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        if (stateController != null)
            stateController.currentState = CharacterState.Dead;

        if (statusEffectController != null)
            statusEffectController.ClearEffects();

        if (knockbackController != null)
            knockbackController.CancelKnockback();

        if (deactivateOnDeath)
            gameObject.SetActive(false);
    }

    public void SetDeactivateOnDeath(bool shouldDeactivate)
    {
        deactivateOnDeath = shouldDeactivate;
    }

    public void Revive(float restoredHealth = -1f)
    {
        float targetHealth = restoredHealth > 0f ? restoredHealth : maxHealth;
        currentHealth = Mathf.Clamp(targetHealth, 0f, maxHealth);

        if (statusEffectController != null)
            statusEffectController.ClearEffects();

        if (knockbackController != null)
            knockbackController.CancelKnockback();

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        if (stateController != null && currentHealth > 0f)
            stateController.SetState(CharacterState.Idle);
    }

    public void ResetRuntimeState()
    {
        if (stats != null)
            maxHealth = stats.maxHealth;

        Revive(maxHealth);
    }
}
