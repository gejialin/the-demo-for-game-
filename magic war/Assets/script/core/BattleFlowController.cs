using UnityEngine;
using UnityEngine.UI;

public class BattleFlowController : MonoBehaviour
{
    public enum BattlePhase
    {
        Idle,
        Preparing,
        Countdown,
        Active,
        Resolved
    }

    [SerializeField] private CharacterSelectionManager selectionManager;
    [SerializeField] private Transform enemySpawnPoint;
    [SerializeField] private GameObject mageEnemyPrefab;
    [SerializeField] private GameObject warlockEnemyPrefab;
    [SerializeField] private GameObject cultistEnemyPrefab;
    [SerializeField] private GameObject agentEnemyPrefab;
    [SerializeField] private bool spawnEnemyOnStart = true;
    [SerializeField] private bool showResultOverlay = true;
    [SerializeField] private bool hideTrainingDummyDuringBattle = true;

    private GameObject currentEnemy;
    private CharacterClassType currentEnemyClass;
    private HealthComponent playerHealth;
    private HealthComponent enemyHealth;
    private Text resultText;
    private GameObject resultOverlay;
    private Button restartButton;
    private Button backToSelectionButton;
    private GameObject trainingDummy;
    private BattleFlowData flowConfig;
    private ShrinkingArenaField arenaField;
    private BattlePhase currentPhase;
    private float phaseEndTime;
    private float matchEndTime;
    private bool battleResolved;

    public BattlePhase CurrentPhase => currentPhase;
    public bool IsPreparing => currentPhase == BattlePhase.Preparing;
    public bool IsCountdownActive => currentPhase == BattlePhase.Countdown;
    public bool IsBattleActive => currentPhase == BattlePhase.Active;
    public bool IsAwaitingBattleConfirmation => currentPhase == BattlePhase.Preparing && !battleResolved;
    public float CountdownRemaining => currentPhase == BattlePhase.Countdown ? Mathf.Max(0f, phaseEndTime - Time.time) : 0f;
    public float MatchTimeRemaining => currentPhase == BattlePhase.Active ? Mathf.Max(0f, matchEndTime - Time.time) : 0f;
    public float MatchDuration => flowConfig != null ? Mathf.Max(0f, flowConfig.matchDuration) : 0f;
    public string PhaseLabel => GetPhaseLabel();

    private void Awake()
    {
        flowConfig = BattleFlowDatabase.Load();

        if (selectionManager == null)
            selectionManager = FindObjectOfType<CharacterSelectionManager>();

        if (selectionManager != null)
        {
            selectionManager.PlayerSpawned += HandlePlayerSpawned;
            selectionManager.CurrentPlayerChanged += HandleCurrentPlayerChanged;
            selectionManager.SelectionVisibilityChanged += HandleSelectionVisibilityChanged;
        }
    }

    private void OnDestroy()
    {
        if (selectionManager != null)
        {
            selectionManager.PlayerSpawned -= HandlePlayerSpawned;
            selectionManager.CurrentPlayerChanged -= HandleCurrentPlayerChanged;
            selectionManager.SelectionVisibilityChanged -= HandleSelectionVisibilityChanged;
        }
    }

    private void Start()
    {
        EnsureResultOverlay();
        ResolveTrainingDummy();
        ResolveArenaField();
        ResetBattleRuntime();

        if (selectionManager != null && selectionManager.CurrentPlayer != null)
            HandlePlayerSpawned(selectionManager.CurrentPlayer, selectionManager.CurrentSelectedCharacter);
    }

    private void Update()
    {
        TrySynchronizeBattleState();

        if (battleResolved)
            return;

        UpdateBattleTimers();

        if (currentPhase != BattlePhase.Active)
            return;

        if (playerHealth != null && playerHealth.IsDead)
            ResolveBattle(GetDefeatLabel());
        else if (enemyHealth != null && enemyHealth.IsDead)
            ResolveBattle(GetVictoryLabel());
    }

    private void TrySynchronizeBattleState()
    {
        if (selectionManager == null)
            return;

        if (selectionManager.IsSelectionVisible || selectionManager.IsPreparationVisible)
            return;

        GameObject player = selectionManager.CurrentPlayer;
        if (player == null)
            return;

        if (playerHealth == null || playerHealth.gameObject != player)
        {
            HandlePlayerSpawned(player, selectionManager.CurrentSelectedCharacter);
            return;
        }

        if (currentPhase == BattlePhase.Preparing)
            SetCombatantsMovementEnabled(false);
    }

    private void HandlePlayerSpawned(GameObject player, CharacterClassType playerClass)
    {
        ResetCombatantRuntime(player, true);
        playerHealth = player != null ? player.GetComponent<HealthComponent>() : null;
        battleResolved = false;
        EnterPreparationState();

        if (resultOverlay != null)
            resultOverlay.SetActive(false);

        SetTrainingDummyActive(!hideTrainingDummyDuringBattle);
    }

    private void HandleCurrentPlayerChanged(GameObject player)
    {
        if (player != null)
        {
            playerHealth = player.GetComponent<HealthComponent>();
            return;
        }

        playerHealth = null;
        if (!battleResolved)
            ResetBattleState();
    }

    private void HandleSelectionVisibilityChanged(bool isVisible)
    {
        if (!isVisible)
            return;

        CleanupBattleForSelection();
    }

    private void SpawnEnemyForPlayerClass(CharacterClassType playerClass)
    {
        CharacterClassType enemyClass = ResolveCounterClass(playerClass);
        GameObject prefab = GetEnemyPrefab(enemyClass);
        if (prefab == null)
            return;

        if (currentEnemy != null)
        {
            currentEnemy.SetActive(false);
            Destroy(currentEnemy);
        }

        Vector3 spawnPosition = enemySpawnPoint != null ? enemySpawnPoint.position : transform.position;
        Quaternion spawnRotation = enemySpawnPoint != null ? enemySpawnPoint.rotation : transform.rotation;

        currentEnemy = Instantiate(prefab, spawnPosition, spawnRotation);
        currentEnemy.name = prefab.name.Replace("Player", "Enemy");
        currentEnemyClass = enemyClass;

        PlayerController3D playerController = currentEnemy.GetComponent<PlayerController3D>();
        if (playerController != null)
        {
            playerController.enabled = false;
            Destroy(playerController);
        }

        EnemyHeroController3D enemyController = currentEnemy.GetComponent<EnemyHeroController3D>();
        if (enemyController == null)
            enemyController = currentEnemy.AddComponent<EnemyHeroController3D>();

        TeamTag teamTag = currentEnemy.GetComponent<TeamTag>();
        if (teamTag != null)
            teamTag.teamId = 2;

        enemyHealth = currentEnemy.GetComponent<HealthComponent>();
    }

    private CharacterClassType ResolveCounterClass(CharacterClassType playerClass)
    {
        CharacterClassType configuredEnemyClass;
        if (BattleFlowRules.TryGetConfiguredCounterClass(flowConfig, playerClass, out configuredEnemyClass))
            return configuredEnemyClass;

        return GetFallbackCounterClass(playerClass);
    }

    private void BeginCountdown()
    {
        float countdownDuration = flowConfig != null ? Mathf.Max(0f, flowConfig.startCountdownDuration) : 0f;
        currentPhase = countdownDuration > 0f ? BattlePhase.Countdown : BattlePhase.Active;
        phaseEndTime = Time.time + countdownDuration;
        matchEndTime = currentPhase == BattlePhase.Active
            ? Time.time + GetConfiguredMatchDuration()
            : 0f;

        SetCombatantsMovementEnabled(false);

        if (currentPhase == BattlePhase.Active)
            StartBattle();
    }

    public void ConfirmBattlePreparation()
    {
        if (selectionManager == null || selectionManager.IsSelectionVisible || selectionManager.IsPreparationVisible)
            return;

        GameObject currentPlayer = selectionManager.CurrentPlayer;
        if (currentPlayer != null && (playerHealth == null || playerHealth.gameObject != currentPlayer || currentPhase == BattlePhase.Idle))
            HandlePlayerSpawned(currentPlayer, selectionManager.CurrentSelectedCharacter);

        if (playerHealth == null || playerHealth.IsDead)
            return;

        if (battleResolved || currentPhase != BattlePhase.Preparing)
            return;

        if (currentEnemy == null || !currentEnemy.activeInHierarchy)
            SpawnEnemyForPlayerClass(selectionManager.CurrentSelectedCharacter);

        if (currentEnemy == null || enemyHealth == null || enemyHealth.IsDead)
            return;

        ResetCombatantRuntime(selectionManager.CurrentPlayer, true);
        ResetCombatantRuntime(currentEnemy, true);
        ClearRuntimeProjectiles();
        ActivateArenaForBattle();
        BeginCountdown();
    }

    private void StartBattle()
    {
        currentPhase = BattlePhase.Active;
        matchEndTime = Time.time + GetConfiguredMatchDuration();
        SetCombatantsMovementEnabled(true);
    }

    private void UpdateBattleTimers()
    {
        if (currentPhase == BattlePhase.Countdown && BattleFlowRules.ShouldStartBattleFromCountdown(Time.time, phaseEndTime))
        {
            StartBattle();
            return;
        }

        if (currentPhase == BattlePhase.Active && MatchDuration > 0f && Time.time >= matchEndTime)
            ResolveTimeout();
    }

    private void ResolveTimeout()
    {
        float playerRatio = playerHealth != null ? playerHealth.NormalizedHealth : 0f;
        float enemyRatio = enemyHealth != null ? enemyHealth.NormalizedHealth : 0f;
        float threshold = flowConfig != null ? Mathf.Max(0f, flowConfig.timeoutHealthDifferenceThreshold) : 0f;
        bool allowDrawOnTimeout = flowConfig != null && flowConfig.allowDrawOnTimeout;
        BattleTimeoutOutcome outcome = BattleFlowRules.ResolveTimeoutOutcome(playerRatio, enemyRatio, threshold, allowDrawOnTimeout);

        switch (outcome)
        {
            case BattleTimeoutOutcome.Draw:
                ResolveBattle(GetConfiguredDrawLabel());
                return;
            case BattleTimeoutOutcome.Victory:
                ResolveBattle(GetVictoryLabel());
                return;
            default:
                ResolveBattle(GetDefeatLabel());
                return;
        }
    }

    private void ResolveBattle(string resultLabel)
    {
        battleResolved = true;
        currentPhase = BattlePhase.Resolved;
        ClearRuntimeProjectiles();
        ResetCombatantRuntime(selectionManager != null ? selectionManager.CurrentPlayer : null, false);
        ResetCombatantRuntime(currentEnemy, false);
        SetTrainingDummyActive(true);
        SetCombatantsMovementEnabled(false);
        DeactivateArenaForPreparation();

        if (resultText != null)
        {
            string enemyLabel = UiTextDatabase.GetCharacterLabel(currentEnemyClass);
            string enemyPrefix = UiTextDatabase.Get("battle.result.enemy", "敌方");
            resultText.text = resultLabel + "\n" + enemyPrefix + "  " + enemyLabel;
        }

        if (resultOverlay != null && showResultOverlay)
            resultOverlay.SetActive(true);
    }

    private void RestartBattle()
    {
        if (selectionManager == null)
            return;

        ClearBattleActors();
        ResetBattleRuntime();
        selectionManager.RespawnCurrentSelection();
    }

    public void ReturnToSelection()
    {
        ClearBattleActors();
        ResetBattleRuntime();
        playerHealth = null;

        if (selectionManager != null)
            selectionManager.ShowSelectionScreen(true);
    }

    private CharacterClassType GetFallbackCounterClass(CharacterClassType playerClass)
    {
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

    private GameObject GetEnemyPrefab(CharacterClassType enemyClass)
    {
        switch (enemyClass)
        {
            case CharacterClassType.Mage:
                return mageEnemyPrefab != null ? mageEnemyPrefab : mageEnemyPrefab = selectionManager != null ? selectionManager.MagePrefab : null;
            case CharacterClassType.Warlock:
                return warlockEnemyPrefab != null ? warlockEnemyPrefab : warlockEnemyPrefab = selectionManager != null ? selectionManager.WarlockPrefab : null;
            case CharacterClassType.Cultist:
                return cultistEnemyPrefab != null ? cultistEnemyPrefab : cultistEnemyPrefab = selectionManager != null ? selectionManager.CultistPrefab : null;
            case CharacterClassType.Agent:
                return agentEnemyPrefab != null ? agentEnemyPrefab : agentEnemyPrefab = selectionManager != null ? selectionManager.AgentPrefab : null;
            default:
                return warlockEnemyPrefab;
        }
    }

    private void EnsureResultOverlay()
    {
        if (resultOverlay != null && resultText != null && restartButton != null && backToSelectionButton != null)
            return;

        BattleRuntimeHUD battleHud = FindObjectOfType<BattleRuntimeHUD>();
        Canvas existingCanvas = battleHud != null ? battleHud.GetComponentInParent<Canvas>() : null;
        if (existingCanvas == null)
        {
            HUDController hudController = FindObjectOfType<HUDController>();
            existingCanvas = hudController != null ? hudController.GetComponentInChildren<Canvas>(true) : null;
        }

        if (existingCanvas == null)
            existingCanvas = FindObjectOfType<Canvas>();

        if (existingCanvas == null)
            return;

        Transform existing = existingCanvas.transform.Find("BattleResultOverlay");
        if (existing != null)
        {
            resultOverlay = existing.gameObject;
            resultText = existing.GetComponentInChildren<Text>(true);
            Button[] buttons = existing.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i].name == "RestartButton")
                    restartButton = buttons[i];
                else if (buttons[i].name == "BackToSelectionButton")
                    backToSelectionButton = buttons[i];
            }
            BindOverlayButtons();
            return;
        }

        GameObject overlay = new GameObject("BattleResultOverlay");
        overlay.transform.SetParent(existingCanvas.transform, false);
        RectTransform overlayRect = overlay.AddComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;
        Image overlayImage = overlay.AddComponent<Image>();
        overlayImage.color = new Color(0.03f, 0.04f, 0.05f, 0.82f);
        overlay.SetActive(false);

        GameObject textObject = new GameObject("ResultText");
        textObject.transform.SetParent(overlay.transform, false);
        RectTransform textRect = textObject.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = Vector2.zero;
        textRect.sizeDelta = new Vector2(520f, 180f);

        Text text = textObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 42;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;

        restartButton = CreateOverlayButton(
            overlay.transform,
            "RestartButton",
            UiTextDatabase.Get("battle.result.restart", "重新开始"),
            new Vector2(-140f, -140f),
            new Color(0.83f, 0.42f, 0.16f, 0.96f));

        backToSelectionButton = CreateOverlayButton(
            overlay.transform,
            "BackToSelectionButton",
            UiTextDatabase.Get("battle.result.backToSelection", "返回选人"),
            new Vector2(140f, -140f),
            new Color(0.18f, 0.30f, 0.46f, 0.96f));

        resultOverlay = overlay;
        resultText = text;
        BindOverlayButtons();
    }

    private void BindOverlayButtons()
    {
        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(RestartBattle);
        }

        if (backToSelectionButton != null)
        {
            backToSelectionButton.onClick.RemoveAllListeners();
            backToSelectionButton.onClick.AddListener(ReturnToSelection);
        }
    }

    private Button CreateOverlayButton(Transform parent, string objectName, string label, Vector2 anchoredPosition, Color color)
    {
        GameObject buttonObject = new GameObject(objectName);
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(220f, 56f);

        Image image = buttonObject.AddComponent<Image>();
        image.color = color;

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;

        GameObject textObject = new GameObject("Text");
        textObject.transform.SetParent(buttonObject.transform, false);
        RectTransform textRect = textObject.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Text text = textObject.AddComponent<Text>();
        text.text = label;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 24;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;

        return button;
    }

    private void ResolveTrainingDummy()
    {
        if (trainingDummy == null)
        {
            TrainingDummyTarget dummy = FindObjectOfType<TrainingDummyTarget>();
            trainingDummy = dummy != null ? dummy.gameObject : null;
        }
    }

    private void ResolveArenaField()
    {
        if (arenaField == null)
            arenaField = FindObjectOfType<ShrinkingArenaField>();
    }

    private void ResetBattleRuntime()
    {
        ResetBattleState();
        ResolveArenaField();
        ClearRuntimeProjectiles();

        if (resultOverlay != null)
            resultOverlay.SetActive(false);

        if (arenaField != null)
            arenaField.ResetRuntimeField();

        SetTrainingDummyActive(true);
    }

    private void EnterPreparationState()
    {
        ResetBattleState();
        currentPhase = BattlePhase.Preparing;
        SetCombatantsMovementEnabled(false);
        DeactivateArenaForPreparation();
    }

    private void ResetBattleState()
    {
        battleResolved = false;
        currentPhase = BattlePhase.Idle;
        phaseEndTime = 0f;
        matchEndTime = 0f;
        SetCombatantsMovementEnabled(false);
        DeactivateArenaForPreparation();

        if (resultOverlay != null)
            resultOverlay.SetActive(false);
    }

    private void CleanupBattleForSelection()
    {
        ClearBattleActors();
        ResetBattleRuntime();
        playerHealth = null;
    }

    private void ClearBattleActors()
    {
        SetCombatantsMovementEnabled(false);

        if (currentEnemy != null)
        {
            currentEnemy.SetActive(false);
            Destroy(currentEnemy);
        }

        currentEnemy = null;
        currentEnemyClass = default;
        enemyHealth = null;
    }

    private void ResetCombatantRuntime(GameObject actor, bool resetHealth)
    {
        if (actor == null)
            return;

        CharacterMotor3D motor = actor.GetComponent<CharacterMotor3D>();
        if (motor != null)
            motor.StopMovement();

        Rigidbody body = actor.GetComponent<Rigidbody>();
        if (body != null)
        {
            body.velocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }

        CharacterStats stats = actor.GetComponent<CharacterStats>();
        if (stats != null)
            stats.ResetRuntimeState();

        SkillCaster3D skillCaster = actor.GetComponent<SkillCaster3D>();
        if (skillCaster != null)
            skillCaster.ResetRuntimeState();

        PlayerController3D playerController = actor.GetComponent<PlayerController3D>();
        if (playerController != null)
            playerController.ResetRuntimeState();

        EnemyHeroController3D enemyController = actor.GetComponent<EnemyHeroController3D>();
        if (enemyController != null)
            enemyController.ResetRuntimeState();

        if (resetHealth)
        {
            HealthComponent health = actor.GetComponent<HealthComponent>();
            if (health != null)
                health.ResetRuntimeState();
            return;
        }

        StatusEffectController statusEffects = actor.GetComponent<StatusEffectController>();
        if (statusEffects != null)
            statusEffects.ClearEffects();

        KnockbackController3D knockback = actor.GetComponent<KnockbackController3D>();
        if (knockback != null)
            knockback.CancelKnockback();

        HealthComponent aliveHealth = actor.GetComponent<HealthComponent>();
        if (aliveHealth == null || !aliveHealth.IsDead)
        {
            CharacterStateController stateController = actor.GetComponent<CharacterStateController>();
            if (stateController != null)
                stateController.ResetRuntimeState();
        }
    }

    private void ClearRuntimeProjectiles()
    {
        ProjectileBase3D[] projectiles = Resources.FindObjectsOfTypeAll<ProjectileBase3D>();
        for (int i = 0; i < projectiles.Length; i++)
        {
            if (projectiles[i] == null || !projectiles[i].gameObject.scene.IsValid())
                continue;

            Destroy(projectiles[i].gameObject);
        }
    }

    private void ActivateArenaForBattle()
    {
        ResolveArenaField();
        if (arenaField == null)
            return;

        arenaField.enabled = true;
        arenaField.ResetRuntimeField();
    }

    private void DeactivateArenaForPreparation()
    {
        ResolveArenaField();
        if (arenaField == null)
            return;

        arenaField.ResetRuntimeField();
        arenaField.enabled = false;
    }

    private void SetTrainingDummyActive(bool active)
    {
        ResolveTrainingDummy();
        if (trainingDummy != null)
            trainingDummy.SetActive(active);
    }

    private void SetCombatantsMovementEnabled(bool enabled)
    {
        SetMovementEnabled(selectionManager != null ? selectionManager.CurrentPlayer : null, enabled);
        SetMovementEnabled(currentEnemy, enabled);
    }

    private void SetMovementEnabled(GameObject actor, bool enabled)
    {
        if (actor == null)
            return;

        CharacterMotor3D motor = actor.GetComponent<CharacterMotor3D>();
        if (motor != null)
        {
            motor.StopMovement();
            motor.enabled = enabled;
        }

        PlayerController3D playerController = actor.GetComponent<PlayerController3D>();
        if (playerController != null)
            playerController.enabled = enabled;

        EnemyHeroController3D enemyController = actor.GetComponent<EnemyHeroController3D>();
        if (enemyController != null)
            enemyController.enabled = enabled;
    }

    private float GetConfiguredMatchDuration()
    {
        return flowConfig != null ? Mathf.Max(0f, flowConfig.matchDuration) : 0f;
    }

    private string GetConfiguredDrawLabel()
    {
        if (flowConfig == null || string.IsNullOrEmpty(flowConfig.drawLabel))
            return UiTextDatabase.Get("battle.result.draw", "平局");

        return flowConfig.drawLabel;
    }

    private string GetPhaseLabel()
    {
        string readyLabel = UiTextDatabase.Get("battle.phase.ready", "就绪");
        string countdownLabel = flowConfig != null && !string.IsNullOrEmpty(flowConfig.countdownLabel)
            ? flowConfig.countdownLabel
            : UiTextDatabase.Get("battle.phase.countdown", "战斗开始");
        string activeLabel = flowConfig != null && !string.IsNullOrEmpty(flowConfig.activeLabel)
            ? flowConfig.activeLabel
            : UiTextDatabase.Get("battle.phase.active", "战斗中");
        string resolvedLabel = UiTextDatabase.Get("battle.phase.resolved", "已结算");
        string timeUpLabel = flowConfig != null && !string.IsNullOrEmpty(flowConfig.timeUpLabel)
            ? flowConfig.timeUpLabel
            : UiTextDatabase.Get("battle.phase.timeUp", "时间到");
        string preparingLabel = UiTextDatabase.Get("battle.phase.preparing", "战前准备");
        bool timedOut = MatchDuration > 0f && Time.time >= matchEndTime;
        string idleOrPreparingLabel = currentPhase == BattlePhase.Preparing ? preparingLabel : readyLabel;

        return BattleFlowRules.BuildPhaseLabel(
            currentPhase,
            battleResolved,
            timedOut,
            CountdownRemaining,
            idleOrPreparingLabel,
            countdownLabel,
            activeLabel,
            resolvedLabel,
            timeUpLabel);
    }

    private string GetVictoryLabel()
    {
        return UiTextDatabase.Get("battle.result.victory", "胜利");
    }

    private string GetDefeatLabel()
    {
        return UiTextDatabase.Get("battle.result.defeat", "失败");
    }
}
