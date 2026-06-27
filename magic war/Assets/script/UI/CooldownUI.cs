using UnityEngine;
using UnityEngine.UI;

public class CooldownUI : MonoBehaviour
{
    [SerializeField] private SkillCaster3D caster;
    [SerializeField] private Image primaryFill;
    [SerializeField] private Image meleeFill;
    [SerializeField] private Text primaryText;
    [SerializeField] private Text meleeText;

    private void Update()
    {
        if (caster == null)
            return;

        UpdateSlot(primaryFill, primaryText, caster.GetPrimaryCooldownRemaining(), caster.primarySkill.cooldown);
        UpdateSlot(meleeFill, meleeText, caster.GetMeleeCooldownRemaining(), caster.meleeSkill.cooldown);
    }

    public void SetCaster(SkillCaster3D newCaster)
    {
        caster = newCaster;
    }

    private void UpdateSlot(Image fill, Text label, float remaining, float cooldown)
    {
        float ratio = cooldown <= 0f ? 0f : remaining / cooldown;

        if (fill != null)
            fill.fillAmount = ratio;

        if (label != null)
            label.text = remaining > 0f ? remaining.ToString("0.0") : string.Empty;
    }
}
