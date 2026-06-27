using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PreparationScreenUI : MonoBehaviour
{
    private const string FallbackTitle = "战术部署";
    private const string FallbackSubtitle = "确认出战英雄、战场规则与敌方情报，准备进入本局对抗。";
    private const string FallbackModeLabel = "模式";
    private const string FallbackArenaLabel = "场地";
    private const string FallbackObjectiveLabel = "胜利条件";
    private const string FallbackObjectiveValue = "击败敌方，或在时间结束前保持更高生命。";
    private const string FallbackPrepState = "准备阶段";
    private const string FallbackPrepHint = "确认后立即生成角色并进入倒计时。";
    private const string FallbackAllyTitle = "我方席位";
    private const string FallbackEnemyTitle = "敌方情报";
    private const string FallbackHeroFocusTitle = "当前出战";
    private const string FallbackHeroSkillsTitle = "核心能力";
    private const string FallbackRuleLabel = "作战规则";
    private const string FallbackRosterReserve = "后备席位";
    private const string FallbackRosterSelected = "已锁定";
    private const string FallbackRosterExpected = "预计对手";
    private const string FallbackRosterWaiting = "等待补充";
    private const string FallbackActionBack = "返回选人";
    private const string FallbackActionPrepare = "确认准备";
    private const string FallbackFooterTip = "当前为框架版准备界面，后续可接入立绘、队伍槽位、匹配信息与更完整倒计时表现。";

    private static readonly Color BackgroundColor = new Color(0.03f, 0.05f, 0.07f, 0.98f);
    private static readonly Color BoardColor = new Color(0.08f, 0.11f, 0.16f, 0.94f);
    private static readonly Color PanelColor = new Color(0.10f, 0.14f, 0.19f, 0.92f);
    private static readonly Color PanelAltColor = new Color(0.07f, 0.10f, 0.14f, 0.9f);
    private static readonly Color OutlineColor = new Color(0.32f, 0.52f, 0.72f, 0.28f);
    private static readonly Color LabelColor = new Color(0.72f, 0.82f, 0.92f, 1f);
    private static readonly Color ValueColor = Color.white;
    private static readonly Color MutedColor = new Color(0.58f, 0.69f, 0.79f, 1f);
    private static readonly Color PlaceholderColor = new Color(0.30f, 0.39f, 0.48f, 0.75f);

    private readonly List<HeroSlotView> allySlots = new List<HeroSlotView>();
    private readonly List<HeroSlotView> enemySlots = new List<HeroSlotView>();

    private Action onPrepare;
    private Action onBack;
    private bool isBuilt;

    private Text titleText;
    private Text subtitleText;
    private Text roomText;
    private Text roomStateText;
    private Text partyText;
    private Text roomOwnerText;
    private Text roomMembersText;
    private Text modeText;
    private Text arenaText;
    private Text ruleText;
    private Text objectiveText;
    private Text stateText;
    private Text tipText;
    private Text allyStatusText;
    private Text enemyStatusText;
    private Text connectionTitleText;
    private Text connectionValueText;
    private Text readyCheckTitleText;
    private Text readyCheckValueText;
    private Text mapRuleTitleText;
    private Text mapRuleHintText;
    private Text selectedHeroNameText;
    private Text selectedHeroRoleText;
    private Text selectedHeroSkillsText;
    private Text selectedHeroTraitText;
    private Text enemyHeroNameText;
    private Text enemyHeroRoleText;
    private Text enemyHeroTraitText;
    private Image selectedHeroAccent;
    private Image enemyHeroAccent;
    private Button prepareButton;
    private Button backButton;

    public Button PrepareButton => prepareButton;
    public Button BackButton => backButton;

    private void Awake()
    {
        EnsureBuilt();
        Hide();
    }

    private void OnEnable()
    {
        BindCallbacks();
    }

    public void Initialize(Action prepareAction, Action backAction)
    {
        EnsureBuilt();
        onPrepare = prepareAction;
        onBack = backAction;
        BindCallbacks();
    }

    public void ShowPreparation(CharacterClassType selectedClass, CharacterClassType enemyClass)
    {
        EnsureBuilt();
        RefreshTexts(selectedClass, enemyClass);
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
    }

    public void Hide()
    {
        if (gameObject.activeSelf)
            gameObject.SetActive(false);
    }

    private void EnsureBuilt()
    {
        if (isBuilt && HasRequiredReferences() && !LayoutNeedsRefresh())
            return;

        RebuildUi();
        BuildUi();
        BindCallbacks();
        isBuilt = true;
    }

    private bool HasRequiredReferences()
    {
        return titleText != null
            && subtitleText != null
            && roomText != null
            && roomStateText != null
            && partyText != null
            && roomOwnerText != null
            && roomMembersText != null
            && stateText != null
            && tipText != null
            && allyStatusText != null
            && enemyStatusText != null
            && readyCheckTitleText != null
            && readyCheckValueText != null
            && mapRuleTitleText != null
            && mapRuleHintText != null
            && connectionTitleText != null
            && connectionValueText != null
            && prepareButton != null
            && backButton != null;
    }

    private bool LayoutNeedsRefresh()
    {
        RectTransform allyColumn = transform.Find("Board/CenterSection/AllyColumn") as RectTransform;
        RectTransform enemyColumn = transform.Find("Board/CenterSection/EnemyColumn") as RectTransform;
        if (allyColumn == null || enemyColumn == null)
            return true;

        return allyColumn.anchoredPosition.sqrMagnitude > 0.01f
            || enemyColumn.anchoredPosition.sqrMagnitude > 0.01f;
    }

    private void RebuildUi()
    {
        allySlots.Clear();
        enemySlots.Clear();

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = transform.GetChild(i).gameObject;
            if (Application.isPlaying)
                DestroyImmediate(child);
            else
                DestroyImmediate(child);
        }

        titleText = null;
        subtitleText = null;
        roomText = null;
        roomStateText = null;
        partyText = null;
        roomOwnerText = null;
        roomMembersText = null;
        modeText = null;
        arenaText = null;
        ruleText = null;
        objectiveText = null;
        stateText = null;
        tipText = null;
        allyStatusText = null;
        enemyStatusText = null;
        connectionTitleText = null;
        connectionValueText = null;
        readyCheckTitleText = null;
        readyCheckValueText = null;
        mapRuleTitleText = null;
        mapRuleHintText = null;
        selectedHeroNameText = null;
        selectedHeroRoleText = null;
        selectedHeroSkillsText = null;
        selectedHeroTraitText = null;
        enemyHeroNameText = null;
        enemyHeroRoleText = null;
        enemyHeroTraitText = null;
        selectedHeroAccent = null;
        enemyHeroAccent = null;
        prepareButton = null;
        backButton = null;
    }

    private void BuildUi()
    {
        RectTransform rootRect = EnsureRootRect();
        SetFullStretch(rootRect);

        Image background = gameObject.GetComponent<Image>();
        if (background == null)
            background = gameObject.AddComponent<Image>();
        background.color = BackgroundColor;

        GameObject board = CreatePanel("Board", transform, BoardColor);
        RectTransform boardRect = board.GetComponent<RectTransform>();
        boardRect.anchorMin = new Vector2(0.5f, 0.5f);
        boardRect.anchorMax = new Vector2(0.5f, 0.5f);
        boardRect.pivot = new Vector2(0.5f, 0.5f);
        boardRect.sizeDelta = new Vector2(1700f, 900f);
        AddOutline(board.transform, OutlineColor, 2f);

        RectTransform topRect = CreateSectionRect("TopSection", board.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -26f), new Vector2(1640f, 182f));
        CreateTopSection(topRect);

        RectTransform centerRect = CreateSectionRect("CenterSection", board.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 2f), new Vector2(1640f, 546f));
        CreateCenterSection(centerRect);

        RectTransform bottomRect = CreateSectionRect("BottomSection", board.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 26f), new Vector2(1640f, 132f));
        CreateBottomSection(bottomRect);
    }

    private void CreateTopSection(RectTransform parent)
    {
        GameObject shell = CreatePanel("TopShell", parent, PanelColor);
        SetFullStretch(shell.GetComponent<RectTransform>());
        AddOutline(shell.transform, OutlineColor, 1f);

        RectTransform roomStrip = CreateSectionRect("RoomStrip", shell.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(36f, -14f), new Vector2(820f, 28f), new Vector2(0f, 1f));
        CreateRoomStrip(roomStrip);

        titleText = CreateText("Title", shell.transform, FallbackTitle, 54, FontStyle.Bold, TextAnchor.MiddleLeft, ValueColor);
        SetRect(titleText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(36f, -46f), new Vector2(600f, 64f), new Vector2(0f, 1f));

        subtitleText = CreateText("Subtitle", shell.transform, FallbackSubtitle, 23, FontStyle.Normal, TextAnchor.UpperLeft, LabelColor);
        SetRect(subtitleText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -108f), new Vector2(720f, 52f), new Vector2(0f, 1f));

        roomOwnerText = CreateText("RoomOwner", shell.transform, "--", 15, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.86f, 0.92f, 0.98f, 0.92f));
        SetRect(roomOwnerText.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(42f, 22f), new Vector2(210f, 22f), new Vector2(0f, 0f));

        roomMembersText = CreateText("RoomMembers", shell.transform, "--", 15, FontStyle.Normal, TextAnchor.MiddleLeft, LabelColor);
        SetRect(roomMembersText.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(274f, 22f), new Vector2(430f, 22f), new Vector2(0f, 0f));

        RectTransform rightInfo = CreateSectionRect("MetadataColumn", shell.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-28f, 2f), new Vector2(690f, 132f), new Vector2(1f, 0.5f));
        CreateMetadataRow(rightInfo);
    }

    private void CreateRoomStrip(RectTransform parent)
    {
        roomText = CreateInfoPill(parent, "RoomPill", new Vector2(0f, 0f), new Vector2(254f, 28f));
        roomStateText = CreateInfoPill(parent, "RoomStatePill", new Vector2(268f, 0f), new Vector2(224f, 28f));
        partyText = CreateInfoPill(parent, "PartyPill", new Vector2(506f, 0f), new Vector2(286f, 28f));
    }

    private void CreateMetadataRow(RectTransform parent)
    {
        float modeWidth = 156f;
        float arenaWidth = 156f;
        float objectiveWidth = 354f;
        float spacing = 12f;
        MetadataCard modeCard = CreateMetadataCard(parent, "ModeCard", 0f, modeWidth, 0f);
        MetadataCard arenaCard = CreateMetadataCard(parent, "ArenaCard", modeWidth + spacing, arenaWidth, 0f);
        MetadataCard objectiveCard = CreateMetadataCard(parent, "ObjectiveCard", modeWidth + arenaWidth + spacing * 2f, objectiveWidth, 0f);

        modeCard.label.text = GetText("prep.mode.label", FallbackModeLabel);
        arenaCard.label.text = GetText("prep.arena.label", FallbackArenaLabel);
        objectiveCard.label.text = GetText("prep.objective.label", FallbackObjectiveLabel);

        modeText = modeCard.value;
        arenaText = arenaCard.value;
        objectiveText = objectiveCard.value;

        RectTransform statusStrip = CreateSectionRect("StateStrip", parent, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 10f), new Vector2(0f, 28f), new Vector2(0.5f, 0f));
        GameObject strip = CreatePanel("Fill", statusStrip, new Color(0.12f, 0.18f, 0.25f, 0.95f));
        SetFullStretch(strip.GetComponent<RectTransform>());

        stateText = CreateText("StateText", strip.transform, FallbackPrepState, 20, FontStyle.Bold, TextAnchor.MiddleLeft, ValueColor);
        SetRect(stateText.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(18f, 0f), new Vector2(260f, 24f), new Vector2(0f, 0.5f));

        tipText = CreateText("TipText", strip.transform, FallbackPrepHint, 16, FontStyle.Normal, TextAnchor.MiddleRight, LabelColor);
        SetRect(tipText.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-18f, 0f), new Vector2(360f, 24f), new Vector2(1f, 0.5f));
    }

    private Text CreateInfoPill(RectTransform parent, string name, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        GameObject pill = CreatePanel(name, parent, new Color(0.12f, 0.17f, 0.23f, 0.96f));
        RectTransform rect = pill.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
        AddOutline(pill.transform, new Color(0.36f, 0.54f, 0.74f, 0.18f), 1f);

        Text text = CreateText("Value", pill.transform, "--", 16, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.86f, 0.92f, 0.98f, 0.96f));
        SetRect(text.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
        return text;
    }

    private MetadataCard CreateMetadataCard(RectTransform parent, string name, float xOffset, float width, float yOffset)
    {
        GameObject card = CreatePanel(name, parent, PanelAltColor);
        RectTransform rect = card.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(xOffset, yOffset);
        rect.sizeDelta = new Vector2(width, 82f);
        AddOutline(card.transform, OutlineColor, 1f);

        Text label = CreateText("Label", card.transform, string.Empty, 18, FontStyle.Bold, TextAnchor.UpperLeft, MutedColor);
        SetRect(label.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(14f, -12f), new Vector2(-28f, 24f), new Vector2(0f, 1f));

        Text value = CreateText("Value", card.transform, "--", 20, FontStyle.Bold, TextAnchor.UpperLeft, ValueColor);
        SetRect(value.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(14f, 38f), new Vector2(-28f, -46f), new Vector2(0f, 0f));
        value.verticalOverflow = VerticalWrapMode.Overflow;

        return new MetadataCard { label = label, value = value };
    }

    private void CreateCenterSection(RectTransform parent)
    {
        float sideWidth = 412f;
        float centerWidth = 756f;

        RectTransform leftRect = CreateSectionRect("AllyColumn", parent, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(sideWidth, 546f), new Vector2(0f, 0.5f));
        RectTransform centerRect = CreateSectionRect("HeroFocusColumn", parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(centerWidth, 546f), new Vector2(0.5f, 0.5f));
        RectTransform rightRect = CreateSectionRect("EnemyColumn", parent, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 0f), new Vector2(sideWidth, 546f), new Vector2(1f, 0.5f));

        centerRect.anchoredPosition = Vector2.zero;
        leftRect.anchoredPosition = Vector2.zero;
        rightRect.anchoredPosition = Vector2.zero;

        CreateRosterColumn(leftRect, true, FallbackAllyTitle, allySlots);
        CreateHeroFocus(centerRect);
        CreateRosterColumn(rightRect, false, FallbackEnemyTitle, enemySlots);
    }

    private void CreateRosterColumn(RectTransform parent, bool isAlly, string fallbackTitle, List<HeroSlotView> targetSlots)
    {
        GameObject shell = CreatePanel(isAlly ? "AllyShell" : "EnemyShell", parent, PanelColor);
        SetFullStretch(shell.GetComponent<RectTransform>());
        AddOutline(shell.transform, OutlineColor, 1f);

        Text columnTitle = CreateText("ColumnTitle", shell.transform, fallbackTitle, 28, FontStyle.Bold, TextAnchor.MiddleLeft, ValueColor);
        SetRect(columnTitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(26f, -24f), new Vector2(-52f, 34f), new Vector2(0f, 1f));
        columnTitle.text = GetText(isAlly ? "prep.ally.title" : "prep.enemy.title", fallbackTitle);

        Text teamText = CreateText(
            "TeamText",
            shell.transform,
            GetText(isAlly ? "prep.team.self" : "prep.team.enemy", isAlly ? "我方" : "敌方"),
            14,
            FontStyle.Bold,
            TextAnchor.MiddleCenter,
            ValueColor);
        SetRect(teamText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-98f, -22f), new Vector2(74f, 24f), new Vector2(1f, 1f));

        Text statusText = CreateText(
            "StatusText",
            shell.transform,
            "--",
            14,
            FontStyle.Bold,
            TextAnchor.MiddleCenter,
            new Color(0.84f, 0.90f, 0.97f, 0.96f));
        SetRect(statusText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-22f, -22f), new Vector2(68f, 24f), new Vector2(1f, 1f));
        if (isAlly)
            allyStatusText = statusText;
        else
            enemyStatusText = statusText;

        Text columnHint = CreateText(
            "ColumnHint",
            shell.transform,
            isAlly ? GetText("prep.ally.hint", "确认当前英雄与备选思路。") : GetText("prep.enemy.hint", "显示预计克制角色与后续补位。"),
            18,
            FontStyle.Normal,
            TextAnchor.UpperLeft,
            LabelColor);
        SetRect(columnHint.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(26f, -64f), new Vector2(-52f, 42f), new Vector2(0f, 1f));

        float slotTop = -128f;
        for (int i = 0; i < 3; i++)
        {
            HeroSlotView slot = CreateHeroSlot(shell.transform, (isAlly ? "Ally" : "Enemy") + "Slot" + i, new Vector2(26f, slotTop - i * 116f));
            slot.stateText.text = i == 0
                ? GetText(isAlly ? "prep.roster.selected" : "prep.roster.expected", isAlly ? FallbackRosterSelected : FallbackRosterExpected)
                : GetText("prep.roster.reserve", FallbackRosterReserve);
            slot.heroNameText.text = GetText("prep.roster.waiting", FallbackRosterWaiting);
            slot.roleText.text = "--";
            slot.traitText.text = "--";
            targetSlots.Add(slot);
        }
    }

    private HeroSlotView CreateHeroSlot(Transform parent, string name, Vector2 anchoredPosition)
    {
        GameObject shell = CreatePanel(name, parent, PanelAltColor);
        RectTransform rect = shell.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(-52f, 98f);
        AddOutline(shell.transform, OutlineColor, 1f);

        GameObject portrait = CreatePanel("Portrait", shell.transform, new Color(0.13f, 0.19f, 0.27f, 1f));
        RectTransform portraitRect = portrait.GetComponent<RectTransform>();
        portraitRect.anchorMin = new Vector2(0f, 0.5f);
        portraitRect.anchorMax = new Vector2(0f, 0.5f);
        portraitRect.pivot = new Vector2(0f, 0.5f);
        portraitRect.anchoredPosition = new Vector2(14f, 0f);
        portraitRect.sizeDelta = new Vector2(58f, 58f);

        Text portraitText = CreateText("PortraitText", portrait.transform, GetText("prep.roster.portrait", "头像"), 14, FontStyle.Bold, TextAnchor.MiddleCenter, LabelColor);
        SetRect(portraitText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));

        GameObject accent = CreatePanel("Accent", shell.transform, PlaceholderColor);
        RectTransform accentRect = accent.GetComponent<RectTransform>();
        accentRect.anchorMin = new Vector2(0f, 0f);
        accentRect.anchorMax = new Vector2(0f, 1f);
        accentRect.pivot = new Vector2(0f, 0.5f);
        accentRect.anchoredPosition = Vector2.zero;
        accentRect.sizeDelta = new Vector2(8f, 0f);

        Text state = CreateText("State", shell.transform, string.Empty, 16, FontStyle.Bold, TextAnchor.UpperLeft, MutedColor);
        SetRect(state.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(88f, -12f), new Vector2(-104f, 20f), new Vector2(0f, 1f));

        Text heroName = CreateText("HeroName", shell.transform, "--", 26, FontStyle.Bold, TextAnchor.MiddleLeft, ValueColor);
        SetRect(heroName.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(88f, -40f), new Vector2(-104f, 30f), new Vector2(0f, 1f));

        Text role = CreateText("Role", shell.transform, "--", 18, FontStyle.Bold, TextAnchor.MiddleLeft, LabelColor);
        SetRect(role.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(88f, 34f), new Vector2(-104f, 22f), new Vector2(0f, 0f));

        Text trait = CreateText("Trait", shell.transform, "--", 16, FontStyle.Normal, TextAnchor.MiddleLeft, MutedColor);
        SetRect(trait.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(88f, 12f), new Vector2(-104f, 18f), new Vector2(0f, 0f));

        return new HeroSlotView
        {
            accent = accent.GetComponent<Image>(),
            stateText = state,
            heroNameText = heroName,
            roleText = role,
            traitText = trait
        };
    }

    private void CreateHeroFocus(RectTransform parent)
    {
        GameObject shell = CreatePanel("HeroFocusShell", parent, PanelColor);
        SetFullStretch(shell.GetComponent<RectTransform>());
        AddOutline(shell.transform, OutlineColor, 1f);

        Text focusTitle = CreateText("FocusTitle", shell.transform, GetText("prep.center.title", FallbackHeroFocusTitle), 30, FontStyle.Bold, TextAnchor.MiddleLeft, ValueColor);
        SetRect(focusTitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(32f, -22f), new Vector2(-64f, 38f), new Vector2(0f, 1f));

        RectTransform heroCardRect = CreateSectionRect("HeroCard", shell.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(32f, -74f), new Vector2(-64f, 204f), new Vector2(0f, 1f));
        GameObject heroCard = CreatePanel("CardFill", heroCardRect, PanelAltColor);
        SetFullStretch(heroCard.GetComponent<RectTransform>());
        AddOutline(heroCard.transform, OutlineColor, 1f);

        GameObject heroArt = CreatePanel("HeroArtPlaceholder", heroCard.transform, new Color(0.11f, 0.18f, 0.25f, 1f));
        RectTransform heroArtRect = heroArt.GetComponent<RectTransform>();
        heroArtRect.anchorMin = new Vector2(0f, 0f);
        heroArtRect.anchorMax = new Vector2(0f, 1f);
        heroArtRect.pivot = new Vector2(0f, 0.5f);
        heroArtRect.anchoredPosition = new Vector2(0f, 0f);
        heroArtRect.sizeDelta = new Vector2(224f, 0f);

        selectedHeroAccent = CreatePanel("SelectedAccent", heroArt.transform, PlaceholderColor).GetComponent<Image>();
        RectTransform selectedAccentRect = selectedHeroAccent.GetComponent<RectTransform>();
        selectedAccentRect.anchorMin = new Vector2(0f, 0f);
        selectedAccentRect.anchorMax = new Vector2(1f, 1f);
        selectedAccentRect.offsetMin = new Vector2(14f, 14f);
        selectedAccentRect.offsetMax = new Vector2(-14f, -14f);

        Text artLabel = CreateText("ArtLabel", heroArt.transform, GetText("prep.hero.artPlaceholder", "立绘占位"), 22, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.86f, 0.92f, 0.98f, 0.9f));
        SetRect(artLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(180f, 40f), new Vector2(0.5f, 0.5f));

        selectedHeroNameText = CreateText("SelectedHeroName", heroCard.transform, "--", 44, FontStyle.Bold, TextAnchor.MiddleLeft, ValueColor);
        SetRect(selectedHeroNameText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(260f, -24f), new Vector2(-28f, 54f), new Vector2(0f, 1f));

        selectedHeroRoleText = CreateText("SelectedHeroRole", heroCard.transform, "--", 22, FontStyle.Bold, TextAnchor.MiddleLeft, LabelColor);
        SetRect(selectedHeroRoleText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(262f, -86f), new Vector2(-28f, 30f), new Vector2(0f, 1f));

        selectedHeroTraitText = CreateText("SelectedHeroTrait", heroCard.transform, "--", 19, FontStyle.Normal, TextAnchor.UpperLeft, MutedColor);
        SetRect(selectedHeroTraitText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(262f, -126f), new Vector2(-32f, 48f), new Vector2(0f, 1f));

        Text focusState = CreateText("FocusState", heroCard.transform, GetText("prep.center_hint", FallbackPrepHint), 16, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.85f, 0.90f, 0.96f, 0.96f));
        SetRect(focusState.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(262f, 24f), new Vector2(-24f, 22f), new Vector2(0f, 0f));

        RectTransform lowerGrid = CreateSectionRect("LowerGrid", shell.transform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(32f, 56f), new Vector2(-64f, 226f), new Vector2(0f, 0f));
        CreateFocusLowerGrid(lowerGrid);
    }

    private void CreateFocusLowerGrid(RectTransform parent)
    {
        GameObject leftCard = CreatePanel("SkillsCard", parent, PanelAltColor);
        RectTransform leftRect = leftCard.GetComponent<RectTransform>();
        leftRect.anchorMin = new Vector2(0f, 0f);
        leftRect.anchorMax = new Vector2(0.5f, 1f);
        leftRect.offsetMin = Vector2.zero;
        leftRect.offsetMax = new Vector2(-10f, 0f);
        AddOutline(leftCard.transform, OutlineColor, 1f);

        GameObject rightCard = CreatePanel("EnemyCard", parent, PanelAltColor);
        RectTransform rightRect = rightCard.GetComponent<RectTransform>();
        rightRect.anchorMin = new Vector2(0.5f, 0f);
        rightRect.anchorMax = new Vector2(1f, 1f);
        rightRect.offsetMin = new Vector2(10f, 0f);
        rightRect.offsetMax = Vector2.zero;
        AddOutline(rightCard.transform, OutlineColor, 1f);

        Text skillsTitle = CreateText("SkillsTitle", leftCard.transform, GetText("prep.center.skills", FallbackHeroSkillsTitle), 24, FontStyle.Bold, TextAnchor.MiddleLeft, ValueColor);
        SetRect(skillsTitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, -16f), new Vector2(-40f, 30f), new Vector2(0f, 1f));

        selectedHeroSkillsText = CreateText("SelectedHeroSkills", leftCard.transform, "--", 20, FontStyle.Bold, TextAnchor.UpperLeft, LabelColor);
        SetRect(selectedHeroSkillsText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, -56f), new Vector2(-40f, 34f), new Vector2(0f, 1f));

        Text ruleTitle = CreateText("RuleTitle", leftCard.transform, GetText("prep.center.ruleTitle", FallbackRuleLabel), 19, FontStyle.Bold, TextAnchor.MiddleLeft, ValueColor);
        SetRect(ruleTitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, -110f), new Vector2(-40f, 24f), new Vector2(0f, 1f));

        ruleText = CreateText("RuleText", leftCard.transform, "--", 17, FontStyle.Normal, TextAnchor.UpperLeft, MutedColor);
        SetRect(ruleText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 0f), new Vector2(20f, -142f), new Vector2(-40f, 52f), new Vector2(0f, 1f));

        Text enemyTitle = CreateText("EnemyTitle", rightCard.transform, GetText("prep.enemy.focusTitle", "对位情报"), 22, FontStyle.Bold, TextAnchor.MiddleLeft, ValueColor);
        SetRect(enemyTitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, -16f), new Vector2(-40f, 30f), new Vector2(0f, 1f));

        GameObject enemyBadge = CreatePanel("EnemyBadge", rightCard.transform, new Color(0.12f, 0.17f, 0.23f, 1f));
        RectTransform enemyBadgeRect = enemyBadge.GetComponent<RectTransform>();
        enemyBadgeRect.anchorMin = new Vector2(0f, 0f);
        enemyBadgeRect.anchorMax = new Vector2(0f, 1f);
        enemyBadgeRect.pivot = new Vector2(0f, 0.5f);
        enemyBadgeRect.anchoredPosition = new Vector2(20f, 0f);
        enemyBadgeRect.sizeDelta = new Vector2(132f, -76f);

        enemyHeroAccent = CreatePanel("EnemyAccent", enemyBadge.transform, PlaceholderColor).GetComponent<Image>();
        RectTransform enemyAccentRect = enemyHeroAccent.GetComponent<RectTransform>();
        enemyAccentRect.anchorMin = new Vector2(0f, 0f);
        enemyAccentRect.anchorMax = new Vector2(1f, 1f);
        enemyAccentRect.offsetMin = new Vector2(14f, 14f);
        enemyAccentRect.offsetMax = new Vector2(-14f, -14f);

        Text enemyBadgeText = CreateText("EnemyBadgeText", enemyBadge.transform, GetText("prep.enemy.placeholder", "敌方位"), 18, FontStyle.Bold, TextAnchor.MiddleCenter, ValueColor);
        SetRect(enemyBadgeText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(88f, 36f), new Vector2(0.5f, 0.5f));

        enemyHeroNameText = CreateText("EnemyHeroName", rightCard.transform, "--", 28, FontStyle.Bold, TextAnchor.MiddleLeft, ValueColor);
        SetRect(enemyHeroNameText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(170f, -58f), new Vector2(-30f, 36f), new Vector2(0f, 1f));

        enemyHeroRoleText = CreateText("EnemyHeroRole", rightCard.transform, "--", 18, FontStyle.Bold, TextAnchor.MiddleLeft, LabelColor);
        SetRect(enemyHeroRoleText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(170f, -102f), new Vector2(-30f, 24f), new Vector2(0f, 1f));

        enemyHeroTraitText = CreateText("EnemyHeroTrait", rightCard.transform, "--", 17, FontStyle.Normal, TextAnchor.UpperLeft, MutedColor);
        SetRect(enemyHeroTraitText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(170f, -134f), new Vector2(-30f, 46f), new Vector2(0f, 1f));

        GameObject mapPanel = CreatePanel("MapRulePanel", rightCard.transform, new Color(0.10f, 0.15f, 0.21f, 0.96f));
        RectTransform mapPanelRect = mapPanel.GetComponent<RectTransform>();
        mapPanelRect.anchorMin = new Vector2(0f, 0f);
        mapPanelRect.anchorMax = new Vector2(1f, 0f);
        mapPanelRect.offsetMin = new Vector2(20f, 16f);
        mapPanelRect.offsetMax = new Vector2(-20f, 84f);
        AddOutline(mapPanel.transform, new Color(0.30f, 0.47f, 0.67f, 0.18f), 1f);

        mapRuleTitleText = CreateText("MapRuleTitle", mapPanel.transform, GetText("prep.mapRule.title", "地图 / 规则"), 18, FontStyle.Bold, TextAnchor.MiddleLeft, ValueColor);
        SetRect(mapRuleTitleText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(16f, -12f), new Vector2(-32f, 22f), new Vector2(0f, 1f));

        mapRuleHintText = CreateText("MapRuleHint", mapPanel.transform, "--", 14, FontStyle.Normal, TextAnchor.UpperLeft, MutedColor);
        SetRect(mapRuleHintText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 0f), new Vector2(16f, -38f), new Vector2(-32f, 14f), new Vector2(0f, 1f));
    }

    private void CreateBottomSection(RectTransform parent)
    {
        GameObject shell = CreatePanel("BottomShell", parent, PanelColor);
        SetFullStretch(shell.GetComponent<RectTransform>());
        AddOutline(shell.transform, OutlineColor, 1f);

        Text footerTip = CreateText("FooterTip", shell.transform, GetText("prep.note", FallbackFooterTip), 16, FontStyle.Normal, TextAnchor.UpperLeft, LabelColor);
        SetRect(footerTip.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(26f, 8f), new Vector2(470f, 56f), new Vector2(0f, 0.5f));

        readyCheckTitleText = CreateText("ReadyCheckTitle", shell.transform, GetText("prep.readyCheck.title", "准备检查"), 18, FontStyle.Bold, TextAnchor.UpperLeft, ValueColor);
        SetRect(readyCheckTitleText.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(526f, 18f), new Vector2(128f, 24f), new Vector2(0f, 0.5f));

        readyCheckValueText = CreateText("ReadyCheckValue", shell.transform, GetText("prep.readyCheck.waiting", "等待我方确认"), 16, FontStyle.Normal, TextAnchor.UpperLeft, MutedColor);
        SetRect(readyCheckValueText.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(526f, -8f), new Vector2(210f, 48f), new Vector2(0f, 0.5f));

        connectionTitleText = CreateText("ConnectionTitle", shell.transform, GetText("prep.connection.title", "房间提示"), 18, FontStyle.Bold, TextAnchor.UpperLeft, ValueColor);
        SetRect(connectionTitleText.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(766f, 18f), new Vector2(180f, 24f), new Vector2(0f, 0.5f));

        connectionValueText = CreateText("ConnectionValue", shell.transform, GetText("prep.connection.value", "当前以单机预览驱动，布局已兼容后续房间数据接入。"), 16, FontStyle.Normal, TextAnchor.UpperLeft, MutedColor);
        SetRect(connectionValueText.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(766f, -8f), new Vector2(300f, 48f), new Vector2(0f, 0.5f));

        backButton = CreateActionButton(
            shell.transform,
            "BackButton",
            GetText("prep.back", FallbackActionBack),
            new Vector2(1f, 0.5f),
            new Vector2(-286f, 0f),
            new Color(0.19f, 0.28f, 0.38f, 1f));

        prepareButton = CreateActionButton(
            shell.transform,
            "PrepareButton",
            GetText("prep.prepare", FallbackActionPrepare),
            new Vector2(1f, 0.5f),
            new Vector2(-106f, 0f),
            new Color(0.97f, 0.56f, 0.16f, 1f));
    }

    private Button CreateActionButton(Transform parent, string name, string label, Vector2 anchor, Vector2 anchoredPosition, Color color)
    {
        GameObject buttonObject = CreatePanel(name, parent, color);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(164f, 62f);

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = buttonObject.GetComponent<Image>();
        button.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = button.colors;
        colors.normalColor = color;
        colors.highlightedColor = Color.Lerp(color, Color.white, 0.12f);
        colors.pressedColor = Color.Lerp(color, Color.black, 0.12f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        Text text = CreateText("Label", buttonObject.transform, label, 24, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        SetRect(text.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));

        return button;
    }

    private void BindCallbacks()
    {
        if (prepareButton != null)
        {
            prepareButton.onClick.RemoveAllListeners();
            prepareButton.onClick.AddListener(HandlePrepareClicked);
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(HandleBackClicked);
        }
    }

    private void HandlePrepareClicked()
    {
        onPrepare?.Invoke();
    }

    private void HandleBackClicked()
    {
        onBack?.Invoke();
    }

    private void RefreshTexts(CharacterClassType selectedClass, CharacterClassType enemyClass)
    {
        titleText.text = GetText("prep.title", FallbackTitle);
        subtitleText.text = GetText("prep.subtitle", FallbackSubtitle);
        roomText.text = ComposeCompactValue("prep.room.label", "prep.room.value", "房间", "本地对战房间");
        roomStateText.text = ComposeCompactValue("prep.room.stateLabel", "prep.room.stateValue", "房间状态", "等待确认");
        partyText.text = ComposeCompactValue("prep.party.label", "prep.party.value", "队伍结构", "当前房间 1 / 2 席位");
        roomOwnerText.text = GetText("prep.room.owner", "房主：本地玩家");
        roomMembersText.text = GetText("prep.room.members", "房间成员：1 / 2，等待另一席位");
        modeText.text = PreparationPresentationDatabase.GetModeLabel();
        arenaText.text = PreparationPresentationDatabase.GetArenaLabel();
        objectiveText.text = GetText("prep.objective.value", FallbackObjectiveValue);
        ruleText.text = PreparationPresentationDatabase.GetRuleSummary();
        stateText.text = GetText("prep.match.awaiting", FallbackPrepState);
        tipText.text = GetText("prep.match.starting", "确认准备后进入开局倒计时");
        allyStatusText.text = GetText("prep.status.ready", "已锁定");
        enemyStatusText.text = GetText("prep.status.waiting", "等待敌方确认");
        connectionTitleText.text = GetText("prep.connection.title", "房间提示");
        connectionValueText.text = GetText("prep.connection.value", "当前以单机预览驱动，布局已兼容后续房间数据接入。");
        readyCheckTitleText.text = GetText("prep.readyCheck.title", "准备检查");
        readyCheckValueText.text = GetText("prep.readyCheck.waiting", "等待我方确认");
        mapRuleTitleText.text = GetText("prep.mapRule.title", "地图 / 规则");
        mapRuleHintText.text = GetText("prep.room.rules", "房间规则") + "： " + PreparationPresentationDatabase.GetRuleSummary() + "\n" + GetText("prep.mapRule.hint", "为后续地图投票、房主规则和赛制说明预留。");

        HeroPresentation selectedPresentation = HeroPresentation.ForClass(selectedClass);
        HeroPresentation enemyPresentation = HeroPresentation.ForClass(enemyClass);

        selectedHeroNameText.text = UiTextDatabase.GetCharacterLabel(selectedClass);
        selectedHeroRoleText.text = GetRoleText(selectedClass, selectedPresentation.roleFallback);
        selectedHeroSkillsText.text = GetSkillSummaryText(selectedClass, selectedPresentation.skillFallback);
        selectedHeroTraitText.text = GetTraitText(selectedClass, selectedPresentation.traitFallback);
        selectedHeroAccent.color = selectedPresentation.accentColor;

        enemyHeroNameText.text = UiTextDatabase.GetCharacterLabel(enemyClass);
        enemyHeroRoleText.text = GetRoleText(enemyClass, enemyPresentation.roleFallback);
        enemyHeroTraitText.text = GetCounterHintText(enemyClass, enemyPresentation.counterFallback);
        enemyHeroAccent.color = enemyPresentation.accentColor;

        PopulateRoster(allySlots, selectedClass, true);
        PopulateRoster(enemySlots, enemyClass, false);
    }

    private void PopulateRoster(List<HeroSlotView> slots, CharacterClassType focusClass, bool isAlly)
    {
        if (slots.Count == 0)
            return;

        HeroPresentation focusPresentation = HeroPresentation.ForClass(focusClass);
        slots[0].stateText.text = isAlly
            ? GetText("prep.slot.locked", FallbackRosterSelected)
            : GetText("prep.status.selecting", FallbackRosterExpected);
        slots[0].heroNameText.text = UiTextDatabase.GetCharacterLabel(focusClass);
        slots[0].roleText.text = GetRoleText(focusClass, focusPresentation.roleFallback);
        slots[0].traitText.text = isAlly
            ? GetTraitText(focusClass, focusPresentation.traitFallback)
            : GetCounterHintText(focusClass, focusPresentation.counterFallback);
        slots[0].accent.color = focusPresentation.accentColor;

        for (int i = 1; i < slots.Count; i++)
        {
            slots[i].stateText.text = GetText("prep.roster.reserve", FallbackRosterReserve);
            slots[i].heroNameText.text = GetText("prep.roster.waiting", FallbackRosterWaiting);
            slots[i].roleText.text = GetText("prep.slot.open", "开放中");
            slots[i].traitText.text = isAlly
                ? GetText("prep.roster.futureHint", "可扩展为队友、替补或推荐阵容。")
                : GetText("prep.enemy.futureHint", "可扩展为敌方队列、禁选与情报。");
            slots[i].accent.color = PlaceholderColor;
        }
    }

    private RectTransform EnsureRootRect()
    {
        RectTransform rect = GetComponent<RectTransform>();
        if (rect == null)
            rect = gameObject.AddComponent<RectTransform>();
        rect.localScale = Vector3.one;
        return rect;
    }

    private RectTransform CreateSectionRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        return CreateSectionRect(name, parent, anchorMin, anchorMax, anchoredPosition, sizeDelta, new Vector2(0.5f, 0.5f));
    }

    private RectTransform CreateSectionRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta, Vector2 pivot)
    {
        GameObject section = new GameObject(name);
        section.transform.SetParent(parent, false);
        RectTransform rect = section.AddComponent<RectTransform>();
        rect.localScale = Vector3.one;
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
        return rect;
    }

    private GameObject CreatePanel(string name, Transform parent, Color color)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.localScale = Vector3.one;
        Image image = panel.AddComponent<Image>();
        image.color = color;
        return panel;
    }

    private Image AddOutline(Transform parent, Color color, float thickness)
    {
        GameObject outline = CreatePanel("Outline", parent, color);
        outline.transform.SetAsFirstSibling();
        RectTransform rect = outline.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(-thickness, -thickness);
        rect.offsetMax = new Vector2(thickness, thickness);
        return outline.GetComponent<Image>();
    }

    private Text CreateText(string name, Transform parent, string value, int fontSize, FontStyle style, TextAnchor alignment, Color color)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);
        RectTransform rect = textObject.AddComponent<RectTransform>();
        rect.localScale = Vector3.one;

        Text text = textObject.AddComponent<Text>();
        text.text = value;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        return text;
    }

    private void SetFullStretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
    }

    private void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta, Vector2 pivot)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
    }

    private string GetText(string key, string fallback)
    {
        return UiTextDatabase.Get(key, fallback);
    }

    private string ComposeCompactValue(string labelKey, string valueKey, string labelFallback, string valueFallback)
    {
        return GetText(labelKey, labelFallback) + "  " + GetText(valueKey, valueFallback);
    }

    private string GetRoleText(CharacterClassType classType, string fallback)
    {
        return UiTextDatabase.Get("prep.hero." + classType.ToString().ToLowerInvariant() + ".role", fallback);
    }

    private string GetTraitText(CharacterClassType classType, string fallback)
    {
        return UiTextDatabase.Get("prep.hero." + classType.ToString().ToLowerInvariant() + ".trait", fallback);
    }

    private string GetSkillSummaryText(CharacterClassType classType, string fallback)
    {
        return UiTextDatabase.Get("selection." + classType.ToString().ToLowerInvariant() + ".skills", fallback);
    }

    private string GetCounterHintText(CharacterClassType enemyClass, string fallback)
    {
        string key = "prep.hero." + enemyClass.ToString().ToLowerInvariant() + ".counterHint";
        return UiTextDatabase.Get(key, fallback);
    }

    private struct MetadataCard
    {
        public Text label;
        public Text value;
    }

    private sealed class HeroSlotView
    {
        public Image accent;
        public Text stateText;
        public Text heroNameText;
        public Text roleText;
        public Text traitText;
    }

    private struct HeroPresentation
    {
        public Color accentColor;
        public string roleFallback;
        public string skillFallback;
        public string traitFallback;
        public string counterFallback;

        public static HeroPresentation ForClass(CharacterClassType classType)
        {
            switch (classType)
            {
                case CharacterClassType.Mage:
                    return new HeroPresentation
                    {
                        accentColor = new Color(0.29f, 0.56f, 0.98f, 1f),
                        roleFallback = "奥术压制",
                        skillFallback = "奥术球 / 奥术震击",
                        traitFallback = "中距离稳定换血，节奏均衡。",
                        counterFallback = "中距离连段稳定，擅长拉开节奏。"
                    };
                case CharacterClassType.Warlock:
                    return new HeroPresentation
                    {
                        accentColor = new Color(0.98f, 0.42f, 0.20f, 1f),
                        roleFallback = "灼烧爆发",
                        skillFallback = "火球术 / 炎爆重击",
                        traitFallback = "爆发高，擅长近身压迫与追击。",
                        counterFallback = "爆发威胁高，注意保持安全距离。"
                    };
                case CharacterClassType.Cultist:
                    return new HeroPresentation
                    {
                        accentColor = new Color(0.34f, 0.74f, 0.44f, 1f),
                        roleFallback = "邪能压迫",
                        skillFallback = "邪能飞弹 / 邪蚀冲撞",
                        traitFallback = "减速与突进并存，擅长制造混乱。",
                        counterFallback = "持续骚扰强，注意塌陷边缘的位移风险。"
                    };
                case CharacterClassType.Agent:
                    return new HeroPresentation
                    {
                        accentColor = new Color(0.95f, 0.74f, 0.25f, 1f),
                        roleFallback = "圣能控场",
                        skillFallback = "圣能飞弹 / 圣裁推击",
                        traitFallback = "弹道快、击退强，适合边缘控制。",
                        counterFallback = "控场强势，留意被推出安全区。"
                    };
                default:
                    return new HeroPresentation
                    {
                        accentColor = PlaceholderColor,
                        roleFallback = "--",
                        skillFallback = "--",
                        traitFallback = "--",
                        counterFallback = "--"
                    };
            }
        }
    }
}
