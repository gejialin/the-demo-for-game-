using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private bool pauseOnEscape = true;

    public bool IsPaused { get; private set; }

    private CharacterSelectionManager selectionManager;
    private BattleFlowController battleFlow;
    private GameObject pauseOverlay;
    private Button resumeButton;
    private Button backToSelectionButton;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Time.timeScale = 1f;
            Instance = null;
        }
    }

    private void Update()
    {
        if (IsPaused && IsSelectionScreenVisible())
        {
            SetPaused(false);
            return;
        }

        if (!pauseOnEscape || !Input.GetKeyDown(KeyCode.Escape))
            return;

        if (!IsPaused)
        {
            if (IsSelectionScreenVisible())
                return;

            PlayerController3D playerController = PlayerController3D.ActivePlayer;
            if (playerController == null)
                playerController = FindObjectOfType<PlayerController3D>();
            if (playerController != null && playerController.TryCancelAimMode())
                return;
        }

        SetPaused(!IsPaused);
    }

    public void SetPaused(bool paused)
    {
        if (paused && IsSelectionScreenVisible())
            return;

        IsPaused = paused;
        Time.timeScale = paused ? 0f : 1f;
        ApplyPauseState(paused);
    }

    private void ApplyPauseState(bool paused)
    {
        EnsurePauseOverlay();

        if (pauseOverlay != null)
            pauseOverlay.SetActive(paused);

        ToggleBehaviour<PlayerController3D>(!paused);
        ToggleBehaviour<EnemyHeroController3D>(!paused);
    }

    private void ResumeFromPause()
    {
        SetPaused(false);
    }

    private void ReturnToSelectionFromPause()
    {
        SetPaused(false);
        EnsureRuntimeReferences();

        if (battleFlow != null)
        {
            battleFlow.ReturnToSelection();
            return;
        }

        if (selectionManager != null)
            selectionManager.ShowSelectionScreen(true);
    }

    private void EnsureRuntimeReferences()
    {
        if (selectionManager == null)
            selectionManager = FindObjectOfType<CharacterSelectionManager>();

        if (battleFlow == null)
            battleFlow = FindObjectOfType<BattleFlowController>();
    }

    private bool IsSelectionScreenVisible()
    {
        EnsureRuntimeReferences();
        return selectionManager != null
            && (selectionManager.IsSelectionVisible || selectionManager.IsPreparationVisible);
    }

    private void EnsurePauseOverlay()
    {
        if (pauseOverlay != null)
            return;

        EnsureEventSystem();

        GameObject canvasObject = new GameObject("PauseOverlayCanvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 160;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject overlay = CreatePanel("PauseOverlay", canvasObject.transform, new Color(0.03f, 0.04f, 0.05f, 0.86f));
        RectTransform overlayRect = overlay.GetComponent<RectTransform>();
        SetFullStretch(overlayRect);

        GameObject dialog = CreatePanel("PauseDialog", overlay.transform, new Color(0.08f, 0.09f, 0.11f, 0.97f));
        RectTransform dialogRect = dialog.GetComponent<RectTransform>();
        dialogRect.anchorMin = new Vector2(0.5f, 0.5f);
        dialogRect.anchorMax = new Vector2(0.5f, 0.5f);
        dialogRect.pivot = new Vector2(0.5f, 0.5f);
        dialogRect.anchoredPosition = Vector2.zero;
        dialogRect.sizeDelta = new Vector2(420f, 260f);

        GameObject accent = CreatePanel("Accent", dialog.transform, new Color(0.82f, 0.34f, 0.12f, 1f));
        RectTransform accentRect = accent.GetComponent<RectTransform>();
        accentRect.anchorMin = new Vector2(0f, 1f);
        accentRect.anchorMax = new Vector2(1f, 1f);
        accentRect.pivot = new Vector2(0.5f, 1f);
        accentRect.anchoredPosition = Vector2.zero;
        accentRect.sizeDelta = new Vector2(0f, 8f);

        Text title = CreateText(
            "Title",
            dialog.transform,
            UiTextDatabase.Get("pause.title", "已暂停"),
            44,
            FontStyle.Bold,
            TextAnchor.MiddleCenter,
            Color.white);
        RectTransform titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -58f);
        titleRect.sizeDelta = new Vector2(320f, 60f);

        Text subtitle = CreateText(
            "Subtitle",
            dialog.transform,
            UiTextDatabase.Get("pause.subtitle", "当前对局已暂停"),
            22,
            FontStyle.Normal,
            TextAnchor.MiddleCenter,
            new Color(0.80f, 0.84f, 0.88f, 1f));
        RectTransform subtitleRect = subtitle.GetComponent<RectTransform>();
        subtitleRect.anchorMin = new Vector2(0.5f, 1f);
        subtitleRect.anchorMax = new Vector2(0.5f, 1f);
        subtitleRect.pivot = new Vector2(0.5f, 1f);
        subtitleRect.anchoredPosition = new Vector2(0f, -114f);
        subtitleRect.sizeDelta = new Vector2(340f, 34f);

        resumeButton = CreateButton(
            dialog.transform,
            "ResumeButton",
            UiTextDatabase.Get("pause.resume", "继续战斗"),
            new Vector2(0f, -165f),
            new Color(0.83f, 0.42f, 0.16f, 0.96f));
        resumeButton.onClick.AddListener(ResumeFromPause);

        backToSelectionButton = CreateButton(
            dialog.transform,
            "BackToSelectionButton",
            UiTextDatabase.Get("pause.backToSelection", "返回选人"),
            new Vector2(0f, -220f),
            new Color(0.18f, 0.30f, 0.46f, 0.96f));
        backToSelectionButton.onClick.AddListener(ReturnToSelectionFromPause);

        overlay.SetActive(false);
        pauseOverlay = overlay;
    }

    private static void ToggleBehaviour<T>(bool enabled) where T : Behaviour
    {
        T[] behaviours = FindObjectsOfType<T>();
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] != null)
                behaviours[i].enabled = enabled;
        }
    }

    private void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null)
            return;

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
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

    private Button CreateButton(Transform parent, string name, string label, Vector2 anchoredPosition, Color color)
    {
        GameObject buttonObject = CreatePanel(name, parent, color);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(260f, 42f);

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = buttonObject.GetComponent<Image>();

        Text text = CreateText("Text", buttonObject.transform, label, 22, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        RectTransform textRect = text.GetComponent<RectTransform>();
        SetFullStretch(textRect);
        return button;
    }

    private void SetFullStretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
    }
}
