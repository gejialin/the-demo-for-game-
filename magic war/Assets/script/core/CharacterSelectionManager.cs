using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CharacterSelectionManager : MonoBehaviour
{
    private const int SelectionCanvasSortingOrder = 1000;
    private const int PreparationCanvasSortingOrder = 1010;
    private const string PlayerPrefsKey = "SelectedCharacterClass";
    private const string FallbackSelectionTitle = "选择英雄";
    private const string FallbackSelectionSubtitle = "选择一个角色开始战斗";
    private const string FallbackSelectAction = "选择";

    [SerializeField] private CharacterClassType defaultCharacter = CharacterClassType.Warlock;
    [SerializeField] private bool showSelectionOnStart = true;
    [SerializeField] private GameObject selectionRoot;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private HUDController hudController;
    [SerializeField] private GameObject magePrefab;
    [SerializeField] private GameObject warlockPrefab;
    [SerializeField] private GameObject cultistPrefab;
    [SerializeField] private GameObject agentPrefab;

    private PreparationScreenUI preparationScreen;
    private BattleFlowController battleFlow;
    private CharacterClassType pendingSelection;
    private bool hasPendingSelection;

    public GameObject CurrentPlayer { get; private set; }
    public CharacterClassType CurrentSelectedCharacter { get; private set; }
    public GameObject MagePrefab => magePrefab;
    public GameObject WarlockPrefab => warlockPrefab;
    public GameObject CultistPrefab => cultistPrefab;
    public GameObject AgentPrefab => agentPrefab;
    public GameObject SelectionRoot => selectionRoot;
    public bool IsSelectionVisible => selectionRoot != null && selectionRoot.activeSelf;
    public bool IsPreparationVisible => preparationScreen != null && preparationScreen.gameObject.activeSelf;

    public event System.Action<GameObject, CharacterClassType> PlayerSpawned;
    public event System.Action<GameObject> CurrentPlayerChanged;
    public event System.Action<bool> SelectionVisibilityChanged;

    private void Start()
    {
        CleanupScenePlayers();
        ResolveRuntimeReferences();

        if (showSelectionOnStart && selectionRoot == null)
            selectionRoot = CreateSelectionUi();

        EnsureSelectionCanvas();

        EnsurePreparationScreen();

        if (selectionRoot != null)
            SetSelectionRootActive(showSelectionOnStart);

        if (preparationScreen != null)
            preparationScreen.Hide();

        if (!showSelectionOnStart)
        {
            ConfirmSelection(LoadSelectedCharacter());
            StartConfirmedBattleFlow();
        }
    }

    public void SpawnSelectedCharacter()
    {
        ConfirmSelection(LoadSelectedCharacter());
        StartConfirmedBattleFlow();
    }

    public void SelectMage()
    {
        SelectCharacter(CharacterClassType.Mage);
    }

    public void SelectWarlock()
    {
        SelectCharacter(CharacterClassType.Warlock);
    }

    public void SelectCultist()
    {
        SelectCharacter(CharacterClassType.Cultist);
    }

    public void SelectAgent()
    {
        SelectCharacter(CharacterClassType.Agent);
    }

    public void SelectCharacter(CharacterClassType characterClass)
    {
        SaveSelectedCharacter(characterClass);
        pendingSelection = characterClass;
        hasPendingSelection = true;
        ShowPreparationScreen(characterClass);
    }

    public void RespawnCurrentSelection()
    {
        CharacterClassType selectedClass = CurrentSelectedCharacter != default
            ? CurrentSelectedCharacter
            : LoadSelectedCharacter();

        ConfirmSelection(selectedClass);
        StartConfirmedBattleFlow();
    }

    public void ShowSelectionScreen(bool destroyCurrentPlayer = true)
    {
        EnsureSelectionCanvas();

        EnsurePreparationScreen();

        if (destroyCurrentPlayer)
            DestroyCurrentPlayer(true);

        hasPendingSelection = false;
        if (preparationScreen != null)
            preparationScreen.Hide();

        SetSelectionRootActive(true);
    }

    public void HideSelectionScreen()
    {
        SetSelectionRootActive(false);
    }

    private void ShowPreparationScreen(CharacterClassType selectedClass)
    {
        EnsurePreparationScreen();
        HideSelectionScreen();

        if (preparationScreen == null)
        {
            ConfirmSelection(selectedClass);
            StartConfirmedBattleFlow();
            return;
        }

        CharacterClassType enemyClass = GetPreviewEnemyClass(selectedClass);
        preparationScreen.ShowPreparation(selectedClass, enemyClass);
    }

    private void ConfirmPendingSelection()
    {
        CharacterClassType selectedClass = hasPendingSelection
            ? pendingSelection
            : LoadSelectedCharacter();

        ConfirmSelection(selectedClass);
        StartConfirmedBattleFlow();
    }

    private void ConfirmSelection(CharacterClassType selectedClass)
    {
        hasPendingSelection = false;

        if (preparationScreen != null)
            preparationScreen.Hide();

        HideSelectionScreen();
        SpawnCharacter(selectedClass);
    }

    private void ReturnToSelectionFromPreparation()
    {
        hasPendingSelection = false;

        if (preparationScreen != null)
            preparationScreen.Hide();

        SetSelectionRootActive(true);
    }

    private CharacterClassType GetPreviewEnemyClass(CharacterClassType playerClass)
    {
        BattleFlowData flowData = BattleFlowDatabase.Load();
        CharacterClassType enemyClass;
        if (BattleFlowRules.TryGetConfiguredCounterClass(flowData, playerClass, out enemyClass))
            return enemyClass;

        switch (playerClass)
        {
            case CharacterClassType.Mage:
                return CharacterClassType.Warlock;
            case CharacterClassType.Warlock:
                return CharacterClassType.Agent;
            case CharacterClassType.Agent:
                return CharacterClassType.Cultist;
            case CharacterClassType.Cultist:
                return CharacterClassType.Mage;
            default:
                return CharacterClassType.Warlock;
        }
    }

    private void SpawnCharacter(CharacterClassType characterClass)
    {
        GameObject prefab = GetPrefab(characterClass);
        if (prefab == null)
        {
            Debug.LogError("No character prefab assigned for " + characterClass + ".");
            return;
        }

        DestroyCurrentPlayer(true);

        Vector3 position = spawnPoint != null ? spawnPoint.position : transform.position;
        Quaternion rotation = spawnPoint != null ? spawnPoint.rotation : transform.rotation;

        CurrentPlayer = Instantiate(prefab, position, rotation);
        CurrentPlayer.name = prefab.name;
        CurrentPlayer.tag = "Player";
        CurrentSelectedCharacter = characterClass;

        TeamTag teamTag = CurrentPlayer.GetComponent<TeamTag>();
        if (teamTag != null)
            teamTag.teamId = 1;

        CharacterStats stats = CurrentPlayer.GetComponent<CharacterStats>();
        CharacterClassBalance balance;
        if (stats != null && GameBalanceDatabase.TryGetClassBalance(characterClass, out balance))
            balance.ApplyStatsTo(stats);

        ResolveRuntimeReferences();
        if (hudController != null)
            hudController.BindPlayer(CurrentPlayer);

        CurrentPlayerChanged?.Invoke(CurrentPlayer);
        PlayerSpawned?.Invoke(CurrentPlayer, characterClass);
    }

    private void CleanupScenePlayers()
    {
        PlayerController3D[] players = Resources.FindObjectsOfTypeAll<PlayerController3D>();
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] == null || !players[i].gameObject.scene.IsValid())
                continue;

            if (players[i].CompareTag("Player"))
            {
                players[i].gameObject.SetActive(false);
                Destroy(players[i].gameObject);
            }
        }

        CurrentPlayer = null;
    }

    public void DestroyCurrentPlayer()
    {
        DestroyCurrentPlayer(false);
    }

    public void DestroyCurrentPlayer(bool destroyAllScenePlayers)
    {
        if (destroyAllScenePlayers)
        {
            CleanupScenePlayers();
        }
        else if (CurrentPlayer != null)
        {
            CurrentPlayer.SetActive(false);
            Destroy(CurrentPlayer);
        }

        CurrentPlayer = null;

        ResolveRuntimeReferences();
        if (hudController != null)
            hudController.BindPlayer(null);

        CurrentPlayerChanged?.Invoke(null);
    }

    public static void SaveSelectedCharacter(CharacterClassType characterClass)
    {
        PlayerPrefs.SetString(PlayerPrefsKey, characterClass.ToString());
        PlayerPrefs.Save();
    }

    private CharacterClassType LoadSelectedCharacter()
    {
        string savedValue = PlayerPrefs.GetString(PlayerPrefsKey, defaultCharacter.ToString());
        CharacterClassType selectedClass;

        if (System.Enum.TryParse(savedValue, out selectedClass))
            return selectedClass;

        return defaultCharacter;
    }

    private GameObject GetPrefab(CharacterClassType characterClass)
    {
        switch (characterClass)
        {
            case CharacterClassType.Mage:
                return magePrefab;
            case CharacterClassType.Warlock:
                return warlockPrefab;
            case CharacterClassType.Cultist:
                return cultistPrefab;
            case CharacterClassType.Agent:
                return agentPrefab;
            default:
                return magePrefab != null ? magePrefab : warlockPrefab;
        }
    }

    private GameObject CreateSelectionUi()
    {
        EnsureEventSystem();

        GameObject canvasObject = new GameObject("HeroSelectionCanvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        ConfigureRuntimeCanvas(canvas, SelectionCanvasSortingOrder);

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject background = CreatePanel("Background", canvasObject.transform, new Color(0.055f, 0.065f, 0.075f, 0.96f));
        SetFullStretch(background.GetComponent<RectTransform>());

        Text title = CreateText(
            "Title",
            background.transform,
            UiTextDatabase.Get("selection.title", FallbackSelectionTitle),
            64,
            FontStyle.Bold,
            TextAnchor.MiddleCenter,
            Color.white);
        RectTransform titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -86f);
        titleRect.sizeDelta = new Vector2(720f, 96f);

        Text subtitle = CreateText(
            "Subtitle",
            background.transform,
            UiTextDatabase.Get("selection.subtitle", FallbackSelectionSubtitle),
            28,
            FontStyle.Normal,
            TextAnchor.MiddleCenter,
            new Color(0.78f, 0.82f, 0.86f, 1f));
        RectTransform subtitleRect = subtitle.GetComponent<RectTransform>();
        subtitleRect.anchorMin = new Vector2(0.5f, 1f);
        subtitleRect.anchorMax = new Vector2(0.5f, 1f);
        subtitleRect.pivot = new Vector2(0.5f, 1f);
        subtitleRect.anchoredPosition = new Vector2(0f, -165f);
        subtitleRect.sizeDelta = new Vector2(720f, 52f);

        GameObject cardsRoot = new GameObject("HeroCards");
        cardsRoot.transform.SetParent(background.transform, false);
        RectTransform cardsRect = cardsRoot.AddComponent<RectTransform>();
        cardsRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardsRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardsRect.pivot = new Vector2(0.5f, 0.5f);
        cardsRect.anchoredPosition = new Vector2(0f, -40f);
        cardsRect.sizeDelta = new Vector2(1500f, 500f);

        CreateHeroButton(
            cardsRoot.transform,
            "MageCard",
            UiTextDatabase.GetCharacterLabel(CharacterClassType.Mage),
            UiTextDatabase.Get("selection.mage.skills", "奥术飞弹 / 震击"),
            UiTextDatabase.Get("selection.mage.description", "远近手感均衡，适合稳扎稳打"),
            new Vector2(-495f, 0f),
            new Color(0.22f, 0.40f, 0.74f, 1f),
            SelectMage);
        CreateHeroButton(
            cardsRoot.transform,
            "WarlockCard",
            UiTextDatabase.GetCharacterLabel(CharacterClassType.Warlock),
            UiTextDatabase.Get("selection.warlock.skills", "火球术 / 重击"),
            UiTextDatabase.Get("selection.warlock.description", "高爆发高伤害，灼烧压制最强"),
            new Vector2(-165f, 0f),
            new Color(0.58f, 0.12f, 0.08f, 1f),
            SelectWarlock);
        CreateHeroButton(
            cardsRoot.transform,
            "AgentCard",
            UiTextDatabase.GetCharacterLabel(CharacterClassType.Agent),
            UiTextDatabase.Get("selection.agent.skills", "圣光飞弹 / 推击"),
            UiTextDatabase.Get("selection.agent.description", "高速弹道与高击退，善于控场"),
            new Vector2(165f, 0f),
            new Color(0.74f, 0.62f, 0.28f, 1f),
            SelectAgent);
        CreateHeroButton(
            cardsRoot.transform,
            "CultistCard",
            UiTextDatabase.GetCharacterLabel(CharacterClassType.Cultist),
            UiTextDatabase.Get("selection.cultist.skills", "邪能飞弹 / 冲锋"),
            UiTextDatabase.Get("selection.cultist.description", "减速压迫与自损爆发并存"),
            new Vector2(495f, 0f),
            new Color(0.20f, 0.48f, 0.30f, 1f),
            SelectCultist);

        return canvasObject;
    }

    private void EnsurePreparationScreen()
    {
        if (preparationScreen != null)
        {
            ConfigureRuntimeCanvas(preparationScreen.GetComponentInParent<Canvas>(true), PreparationCanvasSortingOrder);
            preparationScreen.Initialize(ConfirmPendingSelection, ReturnToSelectionFromPreparation);
            return;
        }

        PreparationScreenUI existingScreen = FindObjectOfType<PreparationScreenUI>(true);
        if (existingScreen != null)
        {
            preparationScreen = existingScreen;
            ConfigureRuntimeCanvas(existingScreen.GetComponentInParent<Canvas>(true), PreparationCanvasSortingOrder);
            preparationScreen.Initialize(ConfirmPendingSelection, ReturnToSelectionFromPreparation);
            preparationScreen.Hide();
            return;
        }

        EnsureEventSystem();

        GameObject canvasObject = new GameObject("PreparationCanvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        ConfigureRuntimeCanvas(canvas, PreparationCanvasSortingOrder);

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject root = new GameObject("PreparationRoot");
        root.transform.SetParent(canvasObject.transform, false);
        RectTransform rootRect = root.AddComponent<RectTransform>();
        rootRect.localScale = Vector3.one;
        SetFullStretch(rootRect);

        preparationScreen = root.AddComponent<PreparationScreenUI>();
        preparationScreen.Initialize(ConfirmPendingSelection, ReturnToSelectionFromPreparation);
        preparationScreen.Hide();
    }

    private void EnsureSelectionCanvas()
    {
        if (selectionRoot == null)
            selectionRoot = CreateSelectionUi();

        Canvas canvas = selectionRoot.GetComponent<Canvas>();
        if (canvas == null)
            canvas = selectionRoot.GetComponentInParent<Canvas>(true);

        ConfigureRuntimeCanvas(canvas, SelectionCanvasSortingOrder);
    }

    private void CreateHeroButton(Transform parent, string name, string heroName, string skillText, string description, Vector2 position, Color accentColor, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObject = CreatePanel(name, parent, new Color(0.13f, 0.145f, 0.16f, 1f));
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(300f, 430f);

        Button button = buttonObject.AddComponent<Button>();
        button.transition = Selectable.Transition.ColorTint;
        button.targetGraphic = buttonObject.GetComponent<Image>();
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.13f, 0.145f, 0.16f, 1f);
        colors.highlightedColor = new Color(0.18f, 0.20f, 0.22f, 1f);
        colors.pressedColor = new Color(0.09f, 0.10f, 0.11f, 1f);
        colors.selectedColor = new Color(0.18f, 0.20f, 0.22f, 1f);
        button.colors = colors;
        button.onClick.AddListener(onClick);

        GameObject accent = CreatePanel("Accent", buttonObject.transform, accentColor);
        RectTransform accentRect = accent.GetComponent<RectTransform>();
        accentRect.anchorMin = new Vector2(0f, 1f);
        accentRect.anchorMax = new Vector2(1f, 1f);
        accentRect.pivot = new Vector2(0.5f, 1f);
        accentRect.anchoredPosition = Vector2.zero;
        accentRect.sizeDelta = new Vector2(0f, 8f);

        Text nameText = CreateText("Name", buttonObject.transform, heroName, 40, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        RectTransform nameRect = nameText.GetComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0f, 1f);
        nameRect.anchorMax = new Vector2(1f, 1f);
        nameRect.pivot = new Vector2(0.5f, 1f);
        nameRect.anchoredPosition = new Vector2(0f, -54f);
        nameRect.sizeDelta = new Vector2(-36f, 70f);

        Text skill = CreateText("Skill", buttonObject.transform, skillText, 26, FontStyle.Bold, TextAnchor.MiddleCenter, accentColor);
        RectTransform skillRect = skill.GetComponent<RectTransform>();
        skillRect.anchorMin = new Vector2(0f, 1f);
        skillRect.anchorMax = new Vector2(1f, 1f);
        skillRect.pivot = new Vector2(0.5f, 1f);
        skillRect.anchoredPosition = new Vector2(0f, -145f);
        skillRect.sizeDelta = new Vector2(-40f, 60f);

        Text desc = CreateText("Description", buttonObject.transform, description, 22, FontStyle.Normal, TextAnchor.MiddleCenter, new Color(0.80f, 0.84f, 0.88f, 1f));
        RectTransform descRect = desc.GetComponent<RectTransform>();
        descRect.anchorMin = new Vector2(0f, 1f);
        descRect.anchorMax = new Vector2(1f, 1f);
        descRect.pivot = new Vector2(0.5f, 1f);
        descRect.anchoredPosition = new Vector2(0f, -220f);
        descRect.sizeDelta = new Vector2(-48f, 96f);

        Text action = CreateText(
            "Action",
            buttonObject.transform,
            UiTextDatabase.Get("selection.action", FallbackSelectAction),
            30,
            FontStyle.Bold,
            TextAnchor.MiddleCenter,
            Color.white);
        RectTransform actionRect = action.GetComponent<RectTransform>();
        actionRect.anchorMin = new Vector2(0.5f, 0f);
        actionRect.anchorMax = new Vector2(0.5f, 0f);
        actionRect.pivot = new Vector2(0.5f, 0f);
        actionRect.anchoredPosition = new Vector2(0f, 34f);
        actionRect.sizeDelta = new Vector2(180f, 58f);
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
        rect.sizeDelta = Vector2.zero;
    }

    private void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null)
            return;

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
    }

    private void ResolveRuntimeReferences()
    {
        if (hudController == null)
            hudController = FindObjectOfType<HUDController>();

        if (battleFlow == null)
            battleFlow = FindObjectOfType<BattleFlowController>();
    }

    private void StartConfirmedBattleFlow()
    {
        ResolveRuntimeReferences();
        if (battleFlow != null)
            battleFlow.ConfirmBattlePreparation();
    }

    private void SetSelectionRootActive(bool active)
    {
        if (selectionRoot == null)
            return;

        EnsureSelectionCanvas();

        if (active)
            selectionRoot.transform.SetAsLastSibling();

        bool changed = selectionRoot.activeSelf != active;
        selectionRoot.SetActive(active);

        if (changed)
            SelectionVisibilityChanged?.Invoke(active);
    }

    private void ConfigureRuntimeCanvas(Canvas canvas, int sortingOrder)
    {
        if (canvas == null)
            return;

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.worldCamera = null;

        canvas.overrideSorting = true;
        canvas.sortingOrder = sortingOrder;

        RectTransform rect = canvas.GetComponent<RectTransform>();
        if (rect != null)
            SetFullStretch(rect);
    }

}
