using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BattleRuntimeHUD : MonoBehaviour
{
    private const float SceneReferenceRefreshInterval = 1f;

    [SerializeField] private Text heroText;
    [SerializeField] private Image healthFillImage;
    [SerializeField] private Text healthValueText;
    [SerializeField] private Text controlModeText;
    [SerializeField] private Text actionModeText;
    [SerializeField] private Text characterStateText;
    [SerializeField] private Text battlePhaseText;
    [SerializeField] private Text matchTimerText;
    [SerializeField] private Text hintText;
    [SerializeField] private Text collapseText;
    [SerializeField] private Text edgeDangerText;
    [SerializeField] private Text statusText;
    [SerializeField] private Image edgeDangerOverlayImage;
    [SerializeField] private Text primarySkillText;
    [SerializeField] private Image primaryCooldownFillImage;
    [SerializeField] private Text primaryCooldownText;
    [SerializeField] private Text meleeSkillText;
    [SerializeField] private Image meleeCooldownFillImage;
    [SerializeField] private Text meleeCooldownText;

    private PlayerController3D playerController;
    private SkillCaster3D skillCaster;
    private CharacterStats characterStats;
    private HealthComponent healthComponent;
    private CharacterStateController stateController;
    private ShrinkingArenaField arenaField;
    private BattleFlowController battleFlow;
    private StatusEffectController statusEffectController;
    private LavaContactHandler3D lavaContactHandler;
    private float collapseTextRefreshCooldown;
    private float sceneReferenceRefreshCooldown;

    private void Awake()
    {
        TryResolveRuntimeTexts();
    }

    private void Update()
    {
        RefreshSceneReferences();

        if (heroText != null)
            heroText.text = LocalizedUiTextBridge.ComposeLabelValue("hud.hero_label", GetHeroName());

        if (healthFillImage != null)
            healthFillImage.fillAmount = healthComponent != null ? healthComponent.NormalizedHealth : 0f;

        if (healthValueText != null)
        {
            if (healthComponent != null)
                healthValueText.text = Mathf.CeilToInt(healthComponent.currentHealth) + " / " + Mathf.CeilToInt(healthComponent.maxHealth);
            else
                healthValueText.text = "-- / --";
        }

        if (controlModeText != null)
            controlModeText.text = LocalizedUiTextBridge.ComposeLabelValue("hud.control_label", GetControlModeLabel());

        if (actionModeText != null)
            actionModeText.text = LocalizedUiTextBridge.ComposeLabelValue("hud.aim_label", GetActionModeLabel());

        if (characterStateText != null)
            characterStateText.text = LocalizedUiTextBridge.ComposeLabelValue("hud.body_label", GetCharacterStateLabel());

        if (battlePhaseText != null)
            battlePhaseText.text = LocalizedUiTextBridge.ComposeLabelValue("hud.phase_label", GetBattlePhaseLabel());

        if (matchTimerText != null)
            matchTimerText.text = LocalizedUiTextBridge.ComposeLabelValue("hud.time_label", GetMatchTimerLabel());

        if (hintText != null)
            hintText.text = GetHintText();

        if (collapseText != null)
            collapseText.text = GetCollapseLabel();
        else if (collapseTextRefreshCooldown <= 0f)
        {
            collapseTextRefreshCooldown = 1f;
            TryResolveRuntimeTexts();
        }

        if (edgeDangerText != null)
            edgeDangerText.text = GetEdgeDangerLabel();

        if (statusText != null)
            statusText.text = GetStatusLabel();

        UpdateEdgeDangerOverlay();

        collapseTextRefreshCooldown = Mathf.Max(0f, collapseTextRefreshCooldown - Time.deltaTime);

        if (primarySkillText != null)
            primarySkillText.text = LocalizedUiTextBridge.ComposeLabelValue("skill.primary_label", GetPrimarySkillName());

        if (primaryCooldownFillImage != null)
            primaryCooldownFillImage.fillAmount = skillCaster != null
                ? GetCooldownRatio(skillCaster.GetPrimaryCooldownRemaining(), skillCaster.primarySkill.cooldown)
                : 0f;

        if (primaryCooldownText != null)
            primaryCooldownText.text = skillCaster != null
                ? GetCooldownLabel(skillCaster.GetPrimaryCooldownRemaining())
                : "--";

        if (meleeSkillText != null)
            meleeSkillText.text = LocalizedUiTextBridge.ComposeLabelValue("skill.melee_label", GetMeleeSkillName());

        if (meleeCooldownFillImage != null)
            meleeCooldownFillImage.fillAmount = skillCaster != null
                ? GetCooldownRatio(skillCaster.GetMeleeCooldownRemaining(), skillCaster.meleeSkill.cooldown)
                : 0f;

        if (meleeCooldownText != null)
            meleeCooldownText.text = skillCaster != null
                ? GetCooldownLabel(skillCaster.GetMeleeCooldownRemaining())
                : "--";
    }

    public void BindPlayer(PlayerController3D controller)
    {
        if (ReferenceEquals(playerController, controller))
            return;

        playerController = controller;
        skillCaster = controller != null ? controller.GetComponent<SkillCaster3D>() : null;
        characterStats = controller != null ? controller.GetComponent<CharacterStats>() : null;
        healthComponent = controller != null ? controller.GetComponent<HealthComponent>() : null;
        stateController = controller != null ? controller.GetComponent<CharacterStateController>() : null;
        statusEffectController = controller != null ? controller.GetComponent<StatusEffectController>() : null;
        lavaContactHandler = controller != null ? controller.GetComponent<LavaContactHandler3D>() : null;
    }

    public bool HasBoundPlayer()
    {
        return playerController != null;
    }

    public void SetReferences(
        Text hero,
        Image healthFill,
        Text healthValue,
        Text controlMode,
        Text actionMode,
        Text characterState,
        Text battlePhase,
        Text matchTimer,
        Text hint,
        Text collapse,
        Text edgeDanger,
        Text status,
        Image edgeOverlay,
        Text primarySkill,
        Image primaryCooldownFill,
        Text primaryCooldown,
        Text meleeSkill,
        Image meleeCooldownFill,
        Text meleeCooldown)
    {
        heroText = hero;
        healthFillImage = healthFill;
        healthValueText = healthValue;
        controlModeText = controlMode;
        actionModeText = actionMode;
        characterStateText = characterState;
        battlePhaseText = battlePhase;
        matchTimerText = matchTimer;
        hintText = hint;
        collapseText = collapse;
        edgeDangerText = edgeDanger;
        statusText = status;
        edgeDangerOverlayImage = edgeOverlay;
        primarySkillText = primarySkill;
        primaryCooldownFillImage = primaryCooldownFill;
        primaryCooldownText = primaryCooldown;
        meleeSkillText = meleeSkill;
        meleeCooldownFillImage = meleeCooldownFill;
        meleeCooldownText = meleeCooldown;
    }

    private void TryResolveRuntimeTexts()
    {
        if (battlePhaseText != null && matchTimerText != null && collapseText != null && edgeDangerText != null && statusText != null)
            return;

        Text[] texts = GetComponentsInChildren<Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] == null)
                continue;

            if (texts[i].name == "CollapseText")
                collapseText = texts[i];
            else if (texts[i].name == "BattlePhaseText")
                battlePhaseText = texts[i];
            else if (texts[i].name == "MatchTimerText")
                matchTimerText = texts[i];
            else if (texts[i].name == "EdgeDangerText")
                edgeDangerText = texts[i];
            else if (texts[i].name == "StatusText")
                statusText = texts[i];

            if (battlePhaseText != null && matchTimerText != null && collapseText != null && edgeDangerText != null && statusText != null)
            {
                return;
            }
        }
    }

    private void RefreshSceneReferences()
    {
        if (arenaField != null && battleFlow != null)
            return;

        sceneReferenceRefreshCooldown -= Time.deltaTime;
        if (sceneReferenceRefreshCooldown > 0f)
            return;

        sceneReferenceRefreshCooldown = SceneReferenceRefreshInterval;

        if (arenaField == null)
            arenaField = FindObjectOfType<ShrinkingArenaField>();

        if (battleFlow == null)
            battleFlow = FindObjectOfType<BattleFlowController>();
    }

    private void OnDisable()
    {
        if (edgeDangerOverlayImage != null)
            edgeDangerOverlayImage.enabled = false;
    }

    private string GetHeroName()
    {
        if (characterStats == null)
            return LocalizedUiTextBridge.Get("common.unknown");

        return LocalizedUiTextBridge.GetClassName(characterStats.classType);
    }

    private string GetControlModeLabel()
    {
        return playerController != null ? playerController.GetControlModeLabel() : "--";
    }

    private string GetActionModeLabel()
    {
        return playerController != null ? playerController.GetActionModeLabel() : "--";
    }

    private string GetHintText()
    {
        return playerController != null ? playerController.GetHintText() : string.Empty;
    }

    private string GetPrimarySkillName()
    {
        return skillCaster != null ? skillCaster.GetPrimarySkillDisplayName() : "--";
    }

    private string GetMeleeSkillName()
    {
        return skillCaster != null ? skillCaster.GetMeleeSkillDisplayName() : "--";
    }

    private float GetCooldownRatio(float remaining, float cooldown)
    {
        if (cooldown <= 0f)
            return 0f;

        return Mathf.Clamp01(remaining / cooldown);
    }

    private string GetCooldownLabel(float remaining)
    {
        return remaining > 0f
            ? LocalizedUiTextBridge.FormatDuration(remaining)
            : LocalizedUiTextBridge.Get("common.ready");
    }

    private string GetCharacterStateLabel()
    {
        if (stateController == null)
            return LocalizedUiTextBridge.Get("common.unknown");

        return LocalizedUiTextBridge.GetCharacterStateLabel(stateController.currentState);
    }

    private string GetCollapseLabel()
    {
        if (arenaField == null)
            return LocalizedUiTextBridge.ComposeLabelValue("hud.collapse_label", "--");

        if (!arenaField.IsCollapseActive && arenaField.TimeUntilNextCollapse <= 0f)
            return LocalizedUiTextBridge.ComposeLabelValue("hud.collapse_label", LocalizedUiTextBridge.Get("hud.collapse_stable"));

        if (arenaField.NextCollapseTime < 0f)
            return LocalizedUiTextBridge.ComposeLabelValue("hud.collapse_label", LocalizedUiTextBridge.Get("hud.collapse_final"));

        string stateLabel = arenaField.IsCollapseWarningActive
            ? LocalizedUiTextBridge.Get("hud.collapse_warning")
            : LocalizedUiTextBridge.Get("hud.collapse_next");
        return LocalizedUiTextBridge.ComposeLabelValue(
            "hud.collapse_label",
            stateLabel + "  " + LocalizedUiTextBridge.FormatDuration(arenaField.TimeUntilNextCollapse));
    }

    private string GetBattlePhaseLabel()
    {
        if (battleFlow == null)
            return LocalizedUiTextBridge.Get("common.ready");

        return LocalizedUiTextBridge.LocalizeBattlePhaseLabel(battleFlow.PhaseLabel);
    }

    private string GetMatchTimerLabel()
    {
        if (battleFlow == null)
            return "--";

        if (battleFlow.IsCountdownActive)
            return LocalizedUiTextBridge.FormatDuration(battleFlow.CountdownRemaining);

        if (battleFlow.IsBattleActive)
            return LocalizedUiTextBridge.FormatDuration(battleFlow.MatchTimeRemaining);

        if (battleFlow.IsPreparing)
            return LocalizedUiTextBridge.Get("common.ready");

        if (battleFlow.CurrentPhase == BattleFlowController.BattlePhase.Resolved)
            return LocalizedUiTextBridge.Get("common.end");

        return LocalizedUiTextBridge.Get("common.ready");
    }

    private string GetEdgeDangerLabel()
    {
        if (arenaField == null)
            return LocalizedUiTextBridge.ComposeLabelValue("hud.edge_label", "--");

        if (arenaField.IsPlayerOnCollapseDangerZone)
            return LocalizedUiTextBridge.ComposeLabelValue(
                "hud.edge_label",
                LocalizedUiTextBridge.Get("hud.edge_collapse") + "  " + arenaField.PlayerCollapseDangerDistance.ToString("0.0"));

        if (arenaField.IsPlayerOutsideSafeZone)
            return LocalizedUiTextBridge.ComposeLabelValue(
                "hud.edge_label",
                LocalizedUiTextBridge.Get("hud.edge_out") + "  " + Mathf.Abs(arenaField.PlayerEdgeDistance).ToString("0.0"));

        if (arenaField.EdgeDangerNormalized > 0f)
            return LocalizedUiTextBridge.ComposeLabelValue(
                "hud.edge_label",
                LocalizedUiTextBridge.Get("hud.edge_warning") + "  " + Mathf.Max(0f, arenaField.PlayerEdgeDistance).ToString("0.0"));

        return LocalizedUiTextBridge.ComposeLabelValue(
            "hud.edge_label",
            LocalizedUiTextBridge.Get("hud.edge_safe") + "  " + Mathf.Max(0f, arenaField.PlayerEdgeDistance).ToString("0.0"));
    }

    private string GetStatusLabel()
    {
        if (healthComponent == null)
            return LocalizedUiTextBridge.ComposeLabelValue("hud.status_label", "--");

        bool inLava = lavaContactHandler != null && lavaContactHandler.IsRecentlyTouchingLava();
        bool burning = statusEffectController != null && statusEffectController.IsBurning;

        if (inLava && burning)
            return LocalizedUiTextBridge.ComposeLabelValue("hud.status_label", LocalizedUiTextBridge.Get("hud.status_lava_burn"));

        if (inLava)
            return LocalizedUiTextBridge.ComposeLabelValue("hud.status_label", LocalizedUiTextBridge.Get("hud.status_lava"));

        if (burning)
            return LocalizedUiTextBridge.ComposeLabelValue(
                "hud.status_label",
                LocalizedUiTextBridge.Get("hud.status_burn") + "  " + LocalizedUiTextBridge.FormatDuration(statusEffectController.BurnTimeRemaining));

        return LocalizedUiTextBridge.ComposeLabelValue("hud.status_label", LocalizedUiTextBridge.Get("hud.status_normal"));
    }

    private void UpdateEdgeDangerOverlay()
    {
        if (edgeDangerOverlayImage == null || arenaField == null)
            return;

        ArenaFieldConfig config = arenaField.GetConfig();
        if (config == null)
            return;

        bool inLava = lavaContactHandler != null && lavaContactHandler.IsRecentlyTouchingLava();
        bool burning = statusEffectController != null && statusEffectController.IsBurning;
        Color color;
        float alpha;

        if (inLava)
        {
            color = config.lavaOverlayColor;
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * Mathf.Max(0.01f, config.lavaOverlayPulseSpeed) * Mathf.PI * 2f);
            alpha = Mathf.Lerp(config.lavaOverlayAlpha * 0.55f, config.lavaOverlayAlpha, pulse);
        }
        else if (burning)
        {
            color = config.burnOverlayColor;
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * Mathf.Max(0.01f, config.burnOverlayPulseSpeed) * Mathf.PI * 2f);
            alpha = Mathf.Lerp(config.burnOverlayAlpha * 0.55f, config.burnOverlayAlpha, pulse);
        }
        else if (arenaField.IsPlayerOnCollapseDangerZone)
        {
            color = Color.Lerp(config.edgeDangerOverlayColor, config.collapseWarningColor, 0.55f);
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * Mathf.Max(0.01f, config.collapseWarningPulseSpeed) * Mathf.PI * 2f);
            float zoneBlend = Mathf.Clamp01(arenaField.PlayerCollapseDangerDistance / Mathf.Max(0.01f, config.collapseStepSize));
            alpha = Mathf.Lerp(0.08f, config.edgeDangerOverlayMaxAlpha * 1.1f, zoneBlend) * Mathf.Lerp(0.7f, 1f, pulse);
        }
        else if (arenaField.IsPlayerOutsideSafeZone)
        {
            color = config.edgeOutsideOverlayColor;
            alpha = config.edgeOutsideOverlayAlpha;
        }
        else
        {
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * Mathf.Max(0.01f, config.edgeDangerOverlayPulseSpeed) * Mathf.PI * 2f);
            alpha = arenaField.EdgeDangerNormalized * Mathf.Lerp(0.2f, 1f, pulse) * config.edgeDangerOverlayMaxAlpha;
            color = config.edgeDangerOverlayColor;
        }

        color.a = Mathf.Clamp01(alpha);
        edgeDangerOverlayImage.color = color;
        edgeDangerOverlayImage.enabled = color.a > 0.001f;
    }
}

internal static class LocalizedUiTextBridge
{
    private static readonly Dictionary<string, string> FallbackTexts = new Dictionary<string, string>
    {
        { "common.unknown", "未知" },
        { "common.ready", "就绪" },
        { "common.end", "结束" },
        { "hud.hero_label", "英雄" },
        { "hud.health_label", "生命" },
        { "hud.control_label", "操作" },
        { "hud.aim_label", "瞄准" },
        { "hud.body_label", "状态" },
        { "hud.phase_label", "阶段" },
        { "hud.time_label", "时间" },
        { "hud.collapse_label", "塌陷" },
        { "hud.collapse_stable", "稳定" },
        { "hud.collapse_warning", "警告" },
        { "hud.collapse_next", "下一次" },
        { "hud.collapse_final", "最终阶段" },
        { "hud.edge_label", "边缘" },
        { "hud.edge_collapse", "塌陷" },
        { "hud.edge_out", "出圈" },
        { "hud.edge_warning", "警告" },
        { "hud.edge_safe", "安全" },
        { "hud.status_label", "状态" },
        { "hud.status_lava", "熔浆" },
        { "hud.status_burn", "燃烧" },
        { "hud.status_lava_burn", "熔浆 + 燃烧" },
        { "hud.status_normal", "正常" },
        { "skill.primary_label", "远程" },
        { "skill.melee_label", "近战" },
        { "skill.primary_name", "远程" },
        { "skill.melee_name", "近战" },
        { "skill.generic_name", "技能" },
        { "control.wasd_mouse", "键鼠移动" },
        { "control.click_move", "点地移动" },
        { "action.normal", "普通" },
        { "action.aim_primary", "瞄准远程" },
        { "action.aim_melee", "瞄准近战" },
        { "hint.wasd", "移动: WASD  施法: 左键/右键  暂停: Esc  切换: Tab" },
        { "hint.aim_primary", "确认: 左键  取消: 右键 或 Esc" },
        { "hint.aim_melee", "确认: 左键  取消: 右键 或 Esc" },
        { "hint.click_move", "移动: 右键  远程瞄准: Q  近战瞄准: E  暂停: Esc  切换: Tab" },
        { "class.mage", "法师" },
        { "class.warlock", "术士" },
        { "class.cultist", "异教徒" },
        { "class.agent", "代理人" },
        { "state.idle", "待机" },
        { "state.moving", "移动" },
        { "state.casting", "施法" },
        { "state.recovery", "后摇" },
        { "state.knocked", "受击" },
        { "state.dead", "死亡" },
        { "battle.phase.countdown", "战斗开始" },
        { "battle.phase.active", "战斗中" },
        { "battle.phase.timeout", "时间到" },
        { "battle.phase.resolved", "已结算" },
        { "battle.phase.draw", "平局" },
        { "battle.phase.victory", "胜利" },
        { "battle.phase.defeat", "失败" }
    };

    public static string Get(string key)
    {
        return Get(key, null);
    }

    public static string Get(string key, string fallback)
    {
        string localizedValue = UiTextDatabase.Get(key);
        if (!string.IsNullOrEmpty(localizedValue))
            return localizedValue;

        string mappedValue;
        if (FallbackTexts.TryGetValue(key, out mappedValue))
            return mappedValue;

        if (!string.IsNullOrEmpty(fallback))
            return fallback;

        return key;
    }

    public static string ComposeLabelValue(string key, string value)
    {
        return Get(key) + "  " + value;
    }

    public static string FormatDuration(float seconds)
    {
        return seconds.ToString("0.0") + "秒";
    }

    public static string GetClassName(CharacterClassType classType)
    {
        string localizedValue = UiTextDatabase.GetCharacterLabel(classType);
        if (!string.IsNullOrEmpty(localizedValue) && !string.Equals(localizedValue, classType.ToString(), StringComparison.Ordinal))
            return localizedValue;

        switch (classType)
        {
            case CharacterClassType.Mage:
                return Get("class.mage");
            case CharacterClassType.Warlock:
                return Get("class.warlock");
            case CharacterClassType.Cultist:
                return Get("class.cultist");
            case CharacterClassType.Agent:
                return Get("class.agent");
            default:
                return classType.ToString();
        }
    }

    public static string GetCharacterStateLabel(CharacterState state)
    {
        string localizedValue = UiTextDatabase.GetCharacterStateLabel(state);
        if (!string.IsNullOrEmpty(localizedValue) && !string.Equals(localizedValue, state.ToString(), StringComparison.Ordinal))
            return localizedValue;

        switch (state)
        {
            case CharacterState.Idle:
                return Get("state.idle");
            case CharacterState.Moving:
                return Get("state.moving");
            case CharacterState.Casting:
                return Get("state.casting");
            case CharacterState.Recovery:
                return Get("state.recovery");
            case CharacterState.Knocked:
                return Get("state.knocked");
            case CharacterState.Dead:
                return Get("state.dead");
            default:
                return state.ToString();
        }
    }

    public static string GetControlModeLabel(InputControlMode controlMode)
    {
        switch (controlMode)
        {
            case InputControlMode.ClickToMove:
                return Get("control.click_move");
            default:
                return Get("control.wasd_mouse");
        }
    }

    public static string GetActionModeLabel(PlayerActionMode actionMode)
    {
        switch (actionMode)
        {
            case PlayerActionMode.AimingPrimary:
                return Get("action.aim_primary");
            case PlayerActionMode.AimingMelee:
                return Get("action.aim_melee");
            default:
                return Get("action.normal");
        }
    }

    public static string GetHintText(InputControlMode controlMode, PlayerActionMode actionMode)
    {
        if (controlMode == InputControlMode.WASDMouseAim)
            return Get("hint.wasd");

        switch (actionMode)
        {
            case PlayerActionMode.AimingPrimary:
                return Get("hint.aim_primary");
            case PlayerActionMode.AimingMelee:
                return Get("hint.aim_melee");
            default:
                return Get("hint.click_move");
        }
    }

    public static string LocalizeBattlePhaseLabel(string phaseLabel)
    {
        if (string.IsNullOrEmpty(phaseLabel))
            return Get("common.ready");

        switch (phaseLabel.Trim())
        {
            case "Battle Starts":
                return Get("battle.phase.countdown");
            case "Preparing":
                return Get("battle.phase.preparing", "战前准备");
            case "Fight":
                return Get("battle.phase.active");
            case "Time Up":
                return Get("battle.phase.timeout");
            case "Victory":
                return Get("battle.phase.victory");
            case "Defeat":
                return Get("battle.phase.defeat");
            case "Draw":
                return Get("battle.phase.draw");
            case "Resolved":
            case "End":
                return Get("battle.phase.resolved");
            default:
                return phaseLabel;
        }
    }

    public static string LocalizeSkillName(string skillName, string fallbackKey, string fallbackValue)
    {
        if (string.IsNullOrWhiteSpace(skillName))
            return Get(fallbackKey, fallbackValue);

        switch (skillName.Trim())
        {
            case "Primary":
                return Get("skill.primary_name");
            case "Melee":
                return Get("skill.melee_name");
            case "Skill":
                return Get("skill.generic_name");
            default:
                return skillName;
        }
    }
}