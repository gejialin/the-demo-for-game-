using UnityEngine;

[CreateAssetMenu(fileName = "CharacterClassConfig", menuName = "Magic War/Character Class Config")]
public class CharacterClassConfig : ScriptableObject
{
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
    [Range(0f, 1f)] public float slowResistance;
    [Range(0f, 1f)] public float burnResistance;

    [Header("Skills")]
    public SkillCastData primarySkill;
    public SkillCastData meleeSkill;

    [Header("AI")]
    public AiBehaviorConfig ai;
}

[System.Serializable]
public class AiBehaviorConfig
{
    public float chaseDistance = 10f;
    public float retreatDistance = 2.2f;
    public float strafeDistance = 4.6f;
    public float strafeRefreshInterval = 1.1f;
    public float primaryCastDistance = 8.5f;
    public float meleeCastDistance = 2.3f;

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
