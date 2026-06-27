using UnityEngine;

public class CharacterStats : MonoBehaviour
{
    [SerializeField] private CharacterClassConfig classConfig;

    public CharacterClassType classType;

    [Header("Base Stats")]
    public float maxHealth = 100f;
    public float moveSpeed = 6f;

    [Header("Combat Modifiers")]
    public float outgoingDamageMultiplier = 1f;
    public float incomingDamageMultiplier = 1f;
    public float knockbackTakenMultiplier = 1f;
    public float knockbackPowerMultiplier = 1f;

    [Header("Status Modifiers")]
    [Range(0f, 1f)] public float slowResistance = 0f;
    [Range(0f, 1f)] public float burnResistance = 0f;

    [HideInInspector] public float currentMoveSpeed;

    public CharacterClassConfig ClassConfig => classConfig;

    private void Awake()
    {
        ApplyClassConfig();
        currentMoveSpeed = moveSpeed;
    }

    public void SetClassConfig(CharacterClassConfig config)
    {
        classConfig = config;
        ApplyClassConfig();
        currentMoveSpeed = moveSpeed;
    }

    public void ResetRuntimeState()
    {
        currentMoveSpeed = moveSpeed;
    }

    private void ApplyClassConfig()
    {
        CharacterClassBalance balance;
        if (GameBalanceDatabase.TryGetClassBalance(classType, out balance))
        {
            balance.ApplyStatsTo(this);
            return;
        }

        if (classConfig == null)
            return;

        classType = classConfig.classType;
        maxHealth = classConfig.maxHealth;
        moveSpeed = classConfig.moveSpeed;
        outgoingDamageMultiplier = classConfig.outgoingDamageMultiplier;
        incomingDamageMultiplier = classConfig.incomingDamageMultiplier;
        knockbackTakenMultiplier = classConfig.knockbackTakenMultiplier;
        knockbackPowerMultiplier = classConfig.knockbackPowerMultiplier;
        slowResistance = classConfig.slowResistance;
        burnResistance = classConfig.burnResistance;
    }
}
