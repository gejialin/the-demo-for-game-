using UnityEngine;
using UnityEngine.UI;

public class HUDController : MonoBehaviour
{
    private const float PlayerResolveInterval = 0.5f;
    private const float RuntimeUiResolveInterval = 1f;

    [SerializeField] private GameObject player;
    [SerializeField] private BattleRuntimeHUD battleRuntimeHUD;
    [SerializeField] private BattleAimIndicator battleAimIndicator;

    private PlayerController3D boundPlayerController;
    private BattleFlowController battleFlow;
    private float playerResolveCooldown;
    private float runtimeUiResolveCooldown;

    private void Awake()
    {
        EnsureRuntimeHud();
        RebindResolvedPlayer(true);
    }

    private void Start()
    {
        EnsureRuntimeHud();
        RebindResolvedPlayer(true);
    }

    private void Update()
    {
        MaintainRuntimeReferences();
        UpdateHudVisibility();
    }

    public void BindPlayer(GameObject newPlayer)
    {
        PlayerController3D controller = newPlayer != null ? newPlayer.GetComponent<PlayerController3D>() : null;
        ApplyPlayerBinding(controller);
    }

    private void MaintainRuntimeReferences()
    {
        runtimeUiResolveCooldown -= Time.deltaTime;
        if ((battleRuntimeHUD == null || battleAimIndicator == null) && runtimeUiResolveCooldown <= 0f)
        {
            runtimeUiResolveCooldown = RuntimeUiResolveInterval;
            EnsureRuntimeHud();
            ApplyPlayerBinding(boundPlayerController);
        }

        if (boundPlayerController != null && !boundPlayerController.gameObject.activeInHierarchy)
            ApplyPlayerBinding(null);

        playerResolveCooldown -= Time.deltaTime;
        if (playerResolveCooldown > 0f)
            return;

        if (boundPlayerController != null && !NeedsPlayerRebind())
            return;

        playerResolveCooldown = PlayerResolveInterval;
        RebindResolvedPlayer(false);
    }

    private void EnsureRuntimeHud()
    {
        if (battleRuntimeHUD == null)
        {
            BattleRuntimeHUD existingHud = FindObjectOfType<BattleRuntimeHUD>();
            if (existingHud != null)
            {
                battleRuntimeHUD = existingHud;
            }
        }

        if (battleAimIndicator == null)
        {
            BattleAimIndicator existingIndicator = FindObjectOfType<BattleAimIndicator>();
            if (existingIndicator != null)
                battleAimIndicator = existingIndicator;
        }

        if (battleRuntimeHUD != null && battleAimIndicator != null)
            return;

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        if (battleRuntimeHUD == null)
            battleRuntimeHUD = CreateRuntimeHud(font);

        if (battleAimIndicator == null)
            battleAimIndicator = CreateAimIndicator();
    }

    private void UpdateHudVisibility()
    {
        if (battleFlow == null)
            battleFlow = FindObjectOfType<BattleFlowController>();

        bool shouldShowHud = battleFlow != null
            && (battleFlow.IsCountdownActive || battleFlow.IsBattleActive);

        Canvas hudCanvas = battleRuntimeHUD != null ? battleRuntimeHUD.GetComponentInParent<Canvas>(true) : null;
        if (hudCanvas != null && hudCanvas.gameObject.activeSelf != shouldShowHud)
            hudCanvas.gameObject.SetActive(shouldShowHud);

        if (battleAimIndicator != null)
            battleAimIndicator.gameObject.SetActive(shouldShowHud);
    }

    private void RebindResolvedPlayer(bool force)
    {
        PlayerController3D controller = ResolvePlayerController();
        if (!force && ReferenceEquals(boundPlayerController, controller))
            return;

        ApplyPlayerBinding(controller);
    }

    private PlayerController3D ResolvePlayerController()
    {
        if (player != null)
        {
            PlayerController3D playerComponent = player.GetComponent<PlayerController3D>();
            if (playerComponent != null && player.activeInHierarchy)
                return playerComponent;
        }

        return FindObjectOfType<PlayerController3D>();
    }

    private void ApplyPlayerBinding(PlayerController3D controller)
    {
        boundPlayerController = controller;
        player = controller != null ? controller.gameObject : null;

        if (battleRuntimeHUD != null)
            battleRuntimeHUD.BindPlayer(controller);

        if (battleAimIndicator != null)
            battleAimIndicator.BindPlayer(controller);
    }

    private bool NeedsPlayerRebind()
    {
        if (boundPlayerController == null)
            return true;

        if (battleRuntimeHUD == null || battleAimIndicator == null)
            return true;

        return !battleRuntimeHUD.HasBoundPlayer() || !battleAimIndicator.HasBoundPlayer();
    }

    private void CreateHudText(
        string objectName,
        Transform parent,
        Vector2 anchoredPosition,
        Vector2 size,
        int fontSize,
        FontStyle fontStyle,
        Font font,
        out Text text)
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        text = textObject.AddComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = TextAnchor.UpperLeft;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
    }

    private Image CreateBar(string objectName, Transform parent, Vector2 anchoredPosition, Vector2 size, Color fillColor)
    {
        GameObject frameObject = new GameObject(objectName);
        frameObject.transform.SetParent(parent, false);

        RectTransform frameRect = frameObject.AddComponent<RectTransform>();
        frameRect.anchorMin = new Vector2(0f, 1f);
        frameRect.anchorMax = new Vector2(0f, 1f);
        frameRect.pivot = new Vector2(0f, 1f);
        frameRect.anchoredPosition = anchoredPosition;
        frameRect.sizeDelta = size;

        Image frameImage = frameObject.AddComponent<Image>();
        frameImage.color = new Color(0.12f, 0.15f, 0.18f, 0.96f);

        GameObject fillObject = new GameObject("Fill");
        fillObject.transform.SetParent(frameObject.transform, false);

        RectTransform fillRect = fillObject.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(2f, 2f);
        fillRect.offsetMax = new Vector2(-2f, -2f);

        Image fillImage = fillObject.AddComponent<Image>();
        fillImage.color = fillColor;
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImage.fillAmount = 1f;
        return fillImage;
    }

    private BattleRuntimeHUD CreateRuntimeHud(Font font)
    {
        GameObject canvasObject = new GameObject("BattleHUDCanvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 80;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject overlayObject = new GameObject("EdgeDangerOverlay");
        overlayObject.transform.SetParent(canvasObject.transform, false);
        RectTransform overlayRect = overlayObject.AddComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;
        Image edgeDangerOverlay = overlayObject.AddComponent<Image>();
        edgeDangerOverlay.color = new Color(1f, 0f, 0f, 0f);
        edgeDangerOverlay.raycastTarget = false;
        edgeDangerOverlay.enabled = false;

        GameObject panelObject = new GameObject("BattleHUDPanel");
        panelObject.transform.SetParent(canvasObject.transform, false);
        RectTransform panelRect = panelObject.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = new Vector2(24f, -24f);
        panelRect.sizeDelta = new Vector2(460f, 430f);

        Image panelImage = panelObject.AddComponent<Image>();
        panelImage.color = new Color(0.05f, 0.06f, 0.08f, 0.84f);

        BattleRuntimeHUD runtimeHud = panelObject.AddComponent<BattleRuntimeHUD>();
        runtimeHud.hideFlags = HideFlags.None;

        CreateHudText("HeroText", panelObject.transform, new Vector2(20f, -18f), new Vector2(408f, 30f), 26, FontStyle.Bold, font, out Text heroText);
        CreateHudText("HealthLabelText", panelObject.transform, new Vector2(20f, -54f), new Vector2(120f, 22f), 16, FontStyle.Bold, font, out Text healthLabelText);
        healthLabelText.text = LocalizedUiTextBridge.Get("hud.health_label");
        CreateHudText("HealthValueText", panelObject.transform, new Vector2(310f, -54f), new Vector2(118f, 22f), 16, FontStyle.Bold, font, out Text healthValueText);
        healthValueText.alignment = TextAnchor.UpperRight;
        Image healthFillImage = CreateBar("HealthBar", panelObject.transform, new Vector2(20f, -82f), new Vector2(408f, 18f), new Color(0.82f, 0.18f, 0.20f, 1f));

        CreateHudText("ControlModeText", panelObject.transform, new Vector2(20f, -112f), new Vector2(408f, 24f), 20, FontStyle.Bold, font, out Text controlModeText);
        CreateHudText("ActionModeText", panelObject.transform, new Vector2(20f, -140f), new Vector2(198f, 24f), 18, FontStyle.Bold, font, out Text actionModeText);
        CreateHudText("CharacterStateText", panelObject.transform, new Vector2(230f, -140f), new Vector2(198f, 24f), 18, FontStyle.Bold, font, out Text characterStateText);
        characterStateText.alignment = TextAnchor.UpperRight;
        CreateHudText("BattlePhaseText", panelObject.transform, new Vector2(20f, -172f), new Vector2(220f, 24f), 18, FontStyle.Bold, font, out Text battlePhaseText);
        CreateHudText("MatchTimerText", panelObject.transform, new Vector2(248f, -172f), new Vector2(180f, 24f), 18, FontStyle.Bold, font, out Text matchTimerText);
        matchTimerText.alignment = TextAnchor.UpperRight;
        CreateHudText("HintText", panelObject.transform, new Vector2(20f, -202f), new Vector2(408f, 44f), 18, FontStyle.Normal, font, out Text hintText);
        CreateHudText("CollapseText", panelObject.transform, new Vector2(20f, -244f), new Vector2(408f, 22f), 18, FontStyle.Bold, font, out Text collapseText);
        CreateHudText("EdgeDangerText", panelObject.transform, new Vector2(20f, -268f), new Vector2(408f, 22f), 18, FontStyle.Bold, font, out Text edgeDangerText);
        CreateHudText("StatusText", panelObject.transform, new Vector2(20f, -292f), new Vector2(408f, 22f), 18, FontStyle.Bold, font, out Text statusText);

        CreateHudText("PrimarySkillText", panelObject.transform, new Vector2(20f, -328f), new Vector2(220f, 22f), 18, FontStyle.Bold, font, out Text primarySkillText);
        CreateHudText("PrimaryCooldownText", panelObject.transform, new Vector2(288f, -328f), new Vector2(140f, 22f), 16, FontStyle.Bold, font, out Text primaryCooldownText);
        primaryCooldownText.alignment = TextAnchor.UpperRight;
        Image primaryCooldownFillImage = CreateBar("PrimaryCooldownBar", panelObject.transform, new Vector2(20f, -354f), new Vector2(408f, 14f), new Color(0.94f, 0.50f, 0.16f, 1f));

        CreateHudText("MeleeSkillText", panelObject.transform, new Vector2(20f, -372f), new Vector2(220f, 22f), 18, FontStyle.Bold, font, out Text meleeSkillText);
        CreateHudText("MeleeCooldownText", panelObject.transform, new Vector2(288f, -372f), new Vector2(140f, 22f), 16, FontStyle.Bold, font, out Text meleeCooldownText);
        meleeCooldownText.alignment = TextAnchor.UpperRight;
        Image meleeCooldownFillImage = CreateBar("MeleeCooldownBar", panelObject.transform, new Vector2(20f, -398f), new Vector2(408f, 14f), new Color(0.34f, 0.66f, 0.98f, 1f));

        runtimeHud.SetReferences(
            heroText,
            healthFillImage,
            healthValueText,
            controlModeText,
            actionModeText,
            characterStateText,
            battlePhaseText,
            matchTimerText,
            hintText,
            collapseText,
            edgeDangerText,
            statusText,
            edgeDangerOverlay,
            primarySkillText,
            primaryCooldownFillImage,
            primaryCooldownText,
            meleeSkillText,
            meleeCooldownFillImage,
            meleeCooldownText
        );

        return runtimeHud;
    }

    private BattleAimIndicator CreateAimIndicator()
    {
        GameObject indicatorObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        indicatorObject.name = "BattleAimIndicator";
        indicatorObject.transform.position = new Vector3(0f, -100f, 0f);

        Collider collider = indicatorObject.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);

        indicatorObject.transform.localScale = new Vector3(0.6f, 0.01f, 0.6f);

        Renderer renderer = indicatorObject.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            Material material = new Material(Shader.Find("Standard"));
            material.color = new Color(0.92f, 0.92f, 0.92f, 0.72f);
            material.SetFloat("_Mode", 3f);
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = 3000;
            renderer.material = material;
            renderer.enabled = false;
        }

        return indicatorObject.AddComponent<BattleAimIndicator>();
    }
}
