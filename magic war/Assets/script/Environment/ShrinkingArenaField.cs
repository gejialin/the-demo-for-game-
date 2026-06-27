using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class ShrinkingArenaField : MonoBehaviour
{
    private enum FloorTileState
    {
        Active,
        Collapsing,
        Fallen
    }

    private sealed class FloorTile
    {
        public Transform transform;
        public Renderer renderer;
        public Collider collider;
        public GroundPlane groundPlane;
        public Transform lavaReplacementTransform;
        public Renderer lavaReplacementRenderer;
        public Collider lavaReplacementCollider;
        public Vector3 baseLocalPosition;
        public Quaternion baseLocalRotation;
        public float edgeDistance;
        public float warningNoise;
        public FloorTileState state;
        public float collapseStartTime;
        public float collapseDelay;
        public float collapseDirectionSign;
    }

    [SerializeField] private ArenaFieldConfig config;
    [SerializeField] private Transform safeFloor;
    [SerializeField] private Transform lavaRoot;
    [SerializeField] private Transform boundaryRoot;
    [SerializeField] private Transform collapsePreviewRoot;

    private readonly List<FloorTile> floorTiles = new List<FloorTile>();

    private Transform collapsedLavaTileRoot;
    private float startTime;
    private Material arenaMaterial;
    private Material lavaMaterial;
    private Material boundaryMaterial;
    private Material collapsePreviewMaterial;
    private MaterialPropertyBlock tilePropertyBlock;
    private float lastAppliedHalfSize = -1f;
    private float lastCollapseTime = -999f;
    private Transform trackedPlayer;
    private Color currentArenaTint;
    private float currentArenaEmission;
    private bool runtimeInitialized;

    public float CurrentHalfSize { get; private set; }
    public float NextCollapseTime { get; private set; }
    public float TimeUntilNextCollapse => Mathf.Max(0f, NextCollapseTime - (Time.time - startTime));
    public bool IsCollapseWarningActive { get; private set; }
    public bool IsCollapseActive => config != null && CurrentHalfSize > config.finalHalfSize + 0.001f;
    public float CollapseWarningNormalized { get; private set; }
    public float NextCollapseHalfSize { get; private set; }
    public float PlayerEdgeDistance { get; private set; }
    public bool IsPlayerOutsideSafeZone { get; private set; }
    public float EdgeDangerNormalized { get; private set; }
    public bool IsPlayerOnCollapseDangerZone { get; private set; }
    public float PlayerCollapseDangerDistance { get; private set; }

    public ArenaFieldConfig GetConfig()
    {
        return config;
    }

    public float GetSignedEdgeDistance(Vector3 worldPosition)
    {
        if (config == null)
            return 0f;

        Vector3 localOffset = worldPosition - config.center;
        float halfExtent = Mathf.Max(Mathf.Abs(localOffset.x), Mathf.Abs(localOffset.z));
        return CurrentHalfSize - halfExtent;
    }

    public bool IsWorldPositionInsideSafeZone(Vector3 worldPosition, float inset = 0f)
    {
        return GetSignedEdgeDistance(worldPosition) >= Mathf.Max(0f, inset);
    }

    public Vector3 ClampWorldPositionToSafeZone(Vector3 worldPosition, float inset = 0f)
    {
        if (config == null)
            return worldPosition;

        float clampedHalfSize = Mathf.Max(0f, CurrentHalfSize - Mathf.Max(0f, inset));
        Vector3 offset = worldPosition - config.center;
        offset.x = Mathf.Clamp(offset.x, -clampedHalfSize, clampedHalfSize);
        offset.z = Mathf.Clamp(offset.z, -clampedHalfSize, clampedHalfSize);
        worldPosition.x = config.center.x + offset.x;
        worldPosition.z = config.center.z + offset.z;
        return worldPosition;
    }

    private void Awake()
    {
        if (config != null)
            config.Normalize();

        InitializeRuntimeField();
    }

    private void OnEnable()
    {
#if UNITY_EDITOR
        EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
#endif
    }

    private void OnDisable()
    {
#if UNITY_EDITOR
        EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
#endif
    }

    private void OnDestroy()
    {
        ReleaseRuntimeMaterials();
    }

    private void OnValidate()
    {
        if (Application.isPlaying || config == null)
            return;

        config.Normalize();
    }

    private void Update()
    {
        ApplyShrink();
        UpdateTrackedPlayerState();
        UpdateVisualFeedback();
    }

    public void SetConfig(ArenaFieldConfig fieldConfig)
    {
        config = fieldConfig;
        if (config != null)
            config.Normalize();
    }

    public void BuildField()
    {
        if (config == null)
            return;

        config.Normalize();
        ReleaseRuntimeMaterials();
        floorTiles.Clear();
        trackedPlayer = null;
        lastAppliedHalfSize = -1f;
        lastCollapseTime = -999f;
        NextCollapseTime = -1f;
        NextCollapseHalfSize = 0f;
        CollapseWarningNormalized = 0f;
        IsCollapseWarningActive = false;
        PlayerEdgeDistance = 0f;
        IsPlayerOutsideSafeZone = false;
        EdgeDangerNormalized = 0f;
        IsPlayerOnCollapseDangerZone = false;
        PlayerCollapseDangerDistance = 0f;

        CreateMaterials();
        CreateRoots();
        collapsedLavaTileRoot = null;
        ClearChildren(lavaRoot);
        ClearChildren(boundaryRoot);
        ClearChildren(collapsePreviewRoot);
        CreateLavaZones();
        CreateSafeFloor();
        CreateBoundaries();
        CreateCollapsePreview();
        ApplyHalfSize(config.initialHalfSize);
        UpdateSafeFloorTiles();
    }

    public void ResetRuntimeField()
    {
        if (config == null)
            return;

        runtimeInitialized = true;
        BuildField();
        startTime = Time.time;
    }

    private void InitializeRuntimeField()
    {
        if (!Application.isPlaying || config == null || runtimeInitialized)
            return;

        ResetRuntimeField();
    }

#if UNITY_EDITOR
    private void HandlePlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
            InitializeRuntimeField();
    }

    [ContextMenu("Rebuild Field In Editor")]
    private void RebuildFieldInEditor()
    {
        if (Application.isPlaying || config == null)
            return;

        BuildField();
    }
#endif

    private void ApplyShrink()
    {
        if (config == null)
            return;

        float elapsed = Time.time - startTime;
        UpdateCollapseSchedule(elapsed);
        float halfSize = GetCollapsedHalfSize(elapsed);
        ApplyHalfSize(halfSize);
    }

    private void UpdateCollapseSchedule(float elapsed)
    {
        NextCollapseTime = -1f;
        IsCollapseWarningActive = false;
        CollapseWarningNormalized = 0f;
        NextCollapseHalfSize = CurrentHalfSize;

        float stepSize = Mathf.Max(0.01f, config.collapseStepSize);
        if (elapsed < config.collapseStartDelay)
        {
            NextCollapseTime = config.collapseStartDelay;
            NextCollapseHalfSize = Mathf.Max(config.finalHalfSize, config.initialHalfSize - stepSize);
        }
        else if (config.collapseStepInterval > 0f && CurrentHalfSize > config.finalHalfSize + 0.001f)
        {
            int completedSteps = Mathf.Max(0, Mathf.FloorToInt((elapsed - config.collapseStartDelay) / config.collapseStepInterval) + 1);
            float nextTime = config.collapseStartDelay + (completedSteps * config.collapseStepInterval);
            float projectedHalfSize = config.initialHalfSize - (completedSteps * stepSize);
            if (projectedHalfSize > config.finalHalfSize + 0.001f)
            {
                NextCollapseTime = nextTime;
                NextCollapseHalfSize = Mathf.Max(config.finalHalfSize, projectedHalfSize - stepSize);
            }
        }

        if (NextCollapseTime < 0f)
            return;

        float leadTime = Mathf.Max(0f, config.collapseWarningLeadTime);
        float warningStart = Mathf.Max(0f, NextCollapseTime - leadTime);
        if (elapsed < warningStart || elapsed > NextCollapseTime)
            return;

        IsCollapseWarningActive = true;
        CollapseWarningNormalized = leadTime > 0.001f
            ? Mathf.Clamp01((elapsed - warningStart) / leadTime)
            : 1f;
    }

    private float GetCollapsedHalfSize(float elapsed)
    {
        float minHalfSize = Mathf.Max(0f, config.finalHalfSize);
        float currentHalfSize = Mathf.Max(minHalfSize, config.initialHalfSize);
        float stepSize = Mathf.Max(0.01f, config.collapseStepSize);

        if (elapsed < config.collapseStartDelay)
            return currentHalfSize;

        if (config.collapseStepInterval <= 0f)
            return minHalfSize;

        int steps = Mathf.FloorToInt((elapsed - config.collapseStartDelay) / config.collapseStepInterval) + 1;
        if (steps <= 0)
            return currentHalfSize;

        float collapsedSize = currentHalfSize - (steps * stepSize);
        return Mathf.Max(minHalfSize, collapsedSize);
    }

    private void ApplyHalfSize(float halfSize)
    {
        float previousHalfSize = lastAppliedHalfSize < 0f ? Mathf.Max(0f, halfSize) : lastAppliedHalfSize;
        CurrentHalfSize = Mathf.Max(0f, halfSize);

        bool didCollapse = lastAppliedHalfSize >= 0f && CurrentHalfSize < lastAppliedHalfSize - 0.001f;
        if (didCollapse)
        {
            lastCollapseTime = Time.time;
            StartCollapsingTiles(previousHalfSize, CurrentHalfSize);
        }

        lastAppliedHalfSize = CurrentHalfSize;

        if (safeFloor != null)
            safeFloor.position = config.center;

        UpdateSafeFloorTiles();
        UpdateLavaZoneTransforms();
        UpdateBoundaryTransforms();
    }

    private void StartCollapsingTiles(float previousHalfSize, float currentHalfSize)
    {
        for (int i = 0; i < floorTiles.Count; i++)
        {
            FloorTile tile = floorTiles[i];
            if (tile == null || tile.state != FloorTileState.Active)
                continue;

            if (IsTileInsideHalf(tile, previousHalfSize) && !IsTileInsideHalf(tile, currentHalfSize))
            {
                tile.state = FloorTileState.Collapsing;
                tile.collapseStartTime = Time.time;
                tile.collapseDelay = GetCollapseTileDelay(tile, previousHalfSize, currentHalfSize);
                tile.collapseDirectionSign = GetCollapseDirectionSign(tile);
            }
        }
    }

    private void UpdateVisualFeedback()
    {
        if (config == null)
            return;

        if (arenaMaterial != null)
        {
            EvaluateMaterialFeedback(config.arenaColor, out currentArenaTint, out currentArenaEmission);
            ApplyMaterialState(arenaMaterial, currentArenaTint, currentArenaEmission);
        }

        if (boundaryMaterial != null)
        {
            EvaluateMaterialFeedback(config.boundaryColor, out Color boundaryTint, out float boundaryEmission);
            ApplyMaterialState(boundaryMaterial, boundaryTint, boundaryEmission);
        }

        UpdateSafeFloorTiles();
        UpdateCollapsePreview();
    }

    private void EvaluateMaterialFeedback(Color baseColor, out Color tint, out float emission)
    {
        tint = baseColor;
        emission = 0f;

        if (IsCollapseWarningActive)
        {
            float pulseSpeed = Mathf.Max(0.01f, config.collapseWarningPulseSpeed);
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * pulseSpeed * Mathf.PI * 2f);
            float blend = Mathf.Lerp(0.15f, 0.95f, CollapseWarningNormalized) * pulse;
            tint = Color.Lerp(baseColor, config.collapseWarningColor, blend);
            emission = Mathf.Lerp(0f, config.collapseWarningEmission, blend);
        }

        if (trackedPlayer != null)
        {
            if (IsPlayerOutsideSafeZone)
            {
                tint = Color.Lerp(tint, config.edgeOutsideColor, 0.9f);
                emission = Mathf.Max(emission, config.edgeOutsideEmission);
            }
            else if (EdgeDangerNormalized > 0f)
            {
                float pulse = 0.45f + 0.55f * Mathf.Sin(Time.time * Mathf.Max(0.01f, config.edgeDangerPulseSpeed) * Mathf.PI * 2f);
                float dangerBlend = EdgeDangerNormalized * pulse;
                tint = Color.Lerp(tint, config.edgeDangerColor, dangerBlend);
                emission = Mathf.Max(emission, config.edgeDangerEmission * dangerBlend);
            }
        }

        float impactDuration = Mathf.Max(0f, config.collapseImpactDuration);
        if (impactDuration > 0.001f)
        {
            float impactT = 1f - Mathf.Clamp01((Time.time - lastCollapseTime) / impactDuration);
            if (impactT > 0f)
            {
                tint = Color.Lerp(tint, config.collapseImpactColor, impactT);
                emission = Mathf.Max(emission, config.collapseImpactEmission * impactT);
            }
        }
    }

    private void ApplyMaterialState(Material material, Color tint, float emission)
    {
        if (material == null)
            return;

        material.color = tint;
        material.SetColor("_Color", tint);
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", tint * emission);
    }

    private void UpdateTrackedPlayerState()
    {
        if (trackedPlayer != null && !trackedPlayer.gameObject.activeInHierarchy)
            trackedPlayer = null;

        if (trackedPlayer == null)
        {
            PlayerController3D controller = FindObjectOfType<PlayerController3D>();
            if (controller != null)
                trackedPlayer = controller.transform;
        }

        if (trackedPlayer == null || config == null)
        {
            PlayerEdgeDistance = 0f;
            IsPlayerOutsideSafeZone = false;
            EdgeDangerNormalized = 0f;
            IsPlayerOnCollapseDangerZone = false;
            PlayerCollapseDangerDistance = 0f;
            return;
        }

        Vector3 localOffset = trackedPlayer.position - config.center;
        float playerHalfExtent = Mathf.Max(Mathf.Abs(localOffset.x), Mathf.Abs(localOffset.z));
        float distanceToEdge = CurrentHalfSize - playerHalfExtent;
        PlayerEdgeDistance = distanceToEdge;
        IsPlayerOutsideSafeZone = distanceToEdge < 0f;

        float collapseDistance = Mathf.Max(0f, playerHalfExtent - NextCollapseHalfSize);
        bool hasPendingCollapse = IsCollapseActive && NextCollapseTime >= 0f && NextCollapseHalfSize < CurrentHalfSize - 0.001f;
        IsPlayerOnCollapseDangerZone = hasPendingCollapse && !IsPlayerOutsideSafeZone && playerHalfExtent > NextCollapseHalfSize + 0.001f;
        PlayerCollapseDangerDistance = IsPlayerOnCollapseDangerZone ? collapseDistance : 0f;

        if (IsPlayerOutsideSafeZone)
        {
            EdgeDangerNormalized = 1f;
            return;
        }

        float warningDistance = Mathf.Max(0.01f, config.edgeDangerWarningDistance);
        float criticalDistance = Mathf.Clamp(config.edgeDangerCriticalDistance, 0f, warningDistance);
        if (distanceToEdge >= warningDistance)
        {
            EdgeDangerNormalized = 0f;
            return;
        }

        if (distanceToEdge <= criticalDistance)
        {
            EdgeDangerNormalized = 1f;
            return;
        }

        EdgeDangerNormalized = 1f - Mathf.InverseLerp(criticalDistance, warningDistance, distanceToEdge);
    }

    private void CreateMaterials()
    {
        if (tilePropertyBlock == null)
            tilePropertyBlock = new MaterialPropertyBlock();

        arenaMaterial = CreateRuntimeMaterial(config.arenaColor);
        lavaMaterial = CreateRuntimeMaterial(config.lavaColor, enableEmission: true, emissionColor: config.lavaColor);
        boundaryMaterial = CreateRuntimeMaterial(config.boundaryColor);
        collapsePreviewMaterial = CreateRuntimeMaterial(
            config.collapsePreviewRingColor,
            enableEmission: true,
            emissionColor: config.collapsePreviewRingColor * config.collapsePreviewRingEmission,
            transparent: true);
    }

    private void CreateRoots()
    {
        if (lavaRoot == null)
            lavaRoot = FindOrCreateChildRoot(config.lavaRootName);

        if (boundaryRoot == null)
            boundaryRoot = FindOrCreateChildRoot(config.boundaryRootName);

        if (collapsePreviewRoot == null)
            collapsePreviewRoot = FindOrCreateChildRoot("CollapsePreview");
    }

    private Transform CreateChildRoot(string objectName)
    {
        GameObject root = new GameObject(objectName);
        root.transform.SetParent(transform, false);
        root.transform.localPosition = Vector3.zero;
        return root.transform;
    }

    private Transform FindOrCreateChildRoot(string objectName)
    {
        Transform existing = transform.Find(objectName);
        return existing != null ? existing : CreateChildRoot(objectName);
    }

    private Transform FindOrCreateChildRoot(Transform parent, string objectName)
    {
        if (parent == null)
            return null;

        Transform existing = parent.Find(objectName);
        if (existing != null)
            return existing;

        GameObject root = new GameObject(objectName);
        root.transform.SetParent(parent, false);
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;
        return root.transform;
    }

    private void CreateSafeFloor()
    {
        if (safeFloor == null)
        {
            Transform existingRoot = transform.Find(config.safeFloorName);
            safeFloor = existingRoot != null ? existingRoot : CreateChildRoot(config.safeFloorName);
        }

        PrepareSafeFloorRoot();
        safeFloor.position = config.center;
        ClearChildren(safeFloor);
        floorTiles.Clear();

        collapsedLavaTileRoot = FindOrCreateChildRoot(lavaRoot, "CollapsedLavaTiles");
        AlignCollapsedLavaTileRoot();
        ClearChildren(collapsedLavaTileRoot);

        float tileSize = Mathf.Max(0.2f, config.floorTileSize);
        float tileGap = Mathf.Max(0f, config.floorTileGap);
        float step = tileSize + tileGap;
        float tileHalf = tileSize * 0.5f;
        int tileCountPerAxis = Mathf.Max(1, Mathf.FloorToInt(((config.initialHalfSize * 2f) + tileGap) / step));
        float origin = -((tileCountPerAxis - 1) * step) * 0.5f;

        for (int x = 0; x < tileCountPerAxis; x++)
        {
            for (int z = 0; z < tileCountPerAxis; z++)
            {
                Vector3 localPosition = new Vector3(origin + (x * step), 0f, origin + (z * step));
                float edgeDistance = Mathf.Max(Mathf.Abs(localPosition.x) + tileHalf, Mathf.Abs(localPosition.z) + tileHalf);
                if (edgeDistance > config.initialHalfSize + 0.001f)
                    continue;

                GameObject tileObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tileObject.name = $"Tile_{x}_{z}";
                tileObject.transform.SetParent(safeFloor, false);
                tileObject.transform.localPosition = localPosition;
                tileObject.transform.localScale = new Vector3(tileSize, config.floorThickness, tileSize);

                Renderer renderer = tileObject.GetComponent<Renderer>();
                if (renderer != null)
                    renderer.sharedMaterial = arenaMaterial;

                GameObject lavaReplacementObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                lavaReplacementObject.name = $"LavaTile_{x}_{z}";
                lavaReplacementObject.transform.SetParent(collapsedLavaTileRoot, false);
                lavaReplacementObject.transform.localPosition = localPosition;
                lavaReplacementObject.transform.localRotation = Quaternion.identity;
                lavaReplacementObject.transform.localScale = new Vector3(tileSize, config.floorThickness, tileSize);

                Renderer lavaRenderer = lavaReplacementObject.GetComponent<Renderer>();
                if (lavaRenderer != null)
                {
                    lavaRenderer.sharedMaterial = lavaMaterial;
                    lavaRenderer.enabled = false;
                }

                Collider lavaCollider = lavaReplacementObject.GetComponent<Collider>();
                if (lavaCollider != null)
                    lavaCollider.enabled = false;

                EnsureGroundPlane(lavaReplacementObject);

                LavaZone3D lavaZone = lavaReplacementObject.GetComponent<LavaZone3D>();
                if (lavaZone == null)
                    lavaZone = lavaReplacementObject.AddComponent<LavaZone3D>();

                lavaZone.SetConfig(config);

                GroundPlane ground = tileObject.GetComponent<GroundPlane>();
                if (ground == null)
                    ground = tileObject.AddComponent<GroundPlane>();

                floorTiles.Add(new FloorTile
                {
                    transform = tileObject.transform,
                    renderer = renderer,
                    collider = tileObject.GetComponent<Collider>(),
                    groundPlane = ground,
                    lavaReplacementTransform = lavaReplacementObject.transform,
                    lavaReplacementRenderer = lavaRenderer,
                    lavaReplacementCollider = lavaCollider,
                    baseLocalPosition = localPosition,
                    baseLocalRotation = tileObject.transform.localRotation,
                    edgeDistance = edgeDistance,
                    warningNoise = Mathf.Abs(Mathf.Sin((x * 0.79f) + (z * 1.13f))),
                    state = FloorTileState.Active,
                    collapseStartTime = -1f,
                    collapseDelay = 0f,
                    collapseDirectionSign = localPosition.x >= 0f ? 1f : -1f
                });
            }
        }
    }

    private void PrepareSafeFloorRoot()
    {
        if (safeFloor == null)
            return;

        Transform originalRoot = safeFloor;
        if (originalRoot.name == "TileRoot" && originalRoot.parent != null && originalRoot.parent.name == config.safeFloorName)
            originalRoot = originalRoot.parent;

        bool hasRenderableRoot =
            originalRoot.GetComponent<Renderer>() != null
            || originalRoot.GetComponent<Collider>() != null
            || originalRoot.GetComponent<GroundPlane>() != null
            || originalRoot.GetComponent<MeshFilter>() != null;

        if (!hasRenderableRoot)
        {
            safeFloor = originalRoot.Find("TileRoot") != null ? originalRoot.Find("TileRoot") : originalRoot;
            originalRoot.localScale = Vector3.one;
            return;
        }

        Transform nestedRoot = originalRoot.Find("TileRoot");
        if (nestedRoot == null)
        {
            GameObject nested = new GameObject("TileRoot");
            nested.transform.SetParent(originalRoot, false);
            nested.transform.localPosition = Vector3.zero;
            nestedRoot = nested.transform;
        }

        DisableComponent(originalRoot.GetComponent<Renderer>());
        DisableComponent(originalRoot.GetComponent<Collider>());
        DisableComponent(originalRoot.GetComponent<MeshFilter>());
        DisableComponent(originalRoot.GetComponent<GroundPlane>());
        DisableComponent(originalRoot.GetComponent<MeshRenderer>());
        originalRoot.localScale = Vector3.one;
        safeFloor = nestedRoot;
    }

    private void DisableComponent(Component component)
    {
        if (component == null)
            return;

        if (component is Renderer renderer)
        {
            renderer.enabled = false;
            return;
        }

        if (component is Collider collider)
        {
            collider.enabled = false;
            return;
        }

        if (component is Behaviour behaviour)
        {
            behaviour.enabled = false;
            return;
        }

        if (component is MeshFilter meshFilter)
            return;
    }

    private void ClearChildren(Transform root)
    {
        if (root == null)
            return;

        for (int i = root.childCount - 1; i >= 0; i--)
        {
            Transform child = root.GetChild(i);
            if (child == null)
                continue;

            if (Application.isPlaying)
            {
                child.gameObject.SetActive(false);
                child.SetParent(null, false);
                Destroy(child.gameObject);
            }
            else
                DestroyImmediate(child.gameObject);
        }
    }

    private void UpdateSafeFloorTiles()
    {
        if (safeFloor == null || config == null)
            return;

        float fallDuration = Mathf.Max(0.01f, config.collapseFallDuration);
        float fallDistance = Mathf.Max(0.01f, config.collapseFallDistance);
        float warningPulse = 0.5f + 0.5f * Mathf.Sin(Time.time * Mathf.Max(0.01f, config.collapseWarningPulseSpeed) * Mathf.PI * 2f);

        for (int i = 0; i < floorTiles.Count; i++)
        {
            FloorTile tile = floorTiles[i];
            if (tile == null || tile.transform == null)
                continue;

            switch (tile.state)
            {
                case FloorTileState.Active:
                    tile.transform.localPosition = tile.baseLocalPosition;
                    tile.transform.localRotation = tile.baseLocalRotation;
                    if (tile.collider != null)
                        tile.collider.enabled = IsTileInsideHalf(tile, CurrentHalfSize);
                    if (tile.groundPlane != null)
                        tile.groundPlane.enabled = IsTileInsideHalf(tile, CurrentHalfSize);
                    if (tile.renderer != null)
                    {
                        tile.renderer.enabled = IsTileInsideHalf(tile, CurrentHalfSize);
                        ApplyTileVisual(tile, warningPulse);
                    }
                    SetLavaReplacementActive(tile, false);
                    break;

                case FloorTileState.Collapsing:
                    float collapseElapsed = Time.time - tile.collapseStartTime - tile.collapseDelay;
                    bool hasStartedFalling = collapseElapsed > 0f;

                    if (tile.collider != null)
                        tile.collider.enabled = !hasStartedFalling;
                    if (tile.groundPlane != null)
                        tile.groundPlane.enabled = !hasStartedFalling;
                    SetLavaReplacementActive(tile, hasStartedFalling);

                    if (!hasStartedFalling)
                    {
                        tile.transform.localPosition = tile.baseLocalPosition;
                        tile.transform.localRotation = tile.baseLocalRotation;
                        if (tile.renderer != null)
                        {
                            tile.renderer.enabled = true;
                            ApplyCollapseQueuedTileVisual(tile);
                        }
                        break;
                    }

                    float t = Mathf.Clamp01(collapseElapsed / fallDuration);
                    ApplyCollapsingTileMotion(tile, t, fallDistance);

                    if (tile.renderer != null)
                    {
                        tile.renderer.enabled = true;
                        ApplyCollapsingTileVisual(tile, t);
                    }

                    if (t >= 1f)
                    {
                        tile.state = FloorTileState.Fallen;
                        if (tile.renderer != null)
                        {
                            tile.renderer.SetPropertyBlock(null);
                            tile.renderer.enabled = false;
                        }
                    }
                    break;

                case FloorTileState.Fallen:
                    tile.transform.localPosition = tile.baseLocalPosition + Vector3.down * fallDistance;
                    tile.transform.localRotation = tile.baseLocalRotation;
                    if (tile.collider != null)
                        tile.collider.enabled = false;
                    if (tile.groundPlane != null)
                        tile.groundPlane.enabled = false;
                    SetLavaReplacementActive(tile, true);
                    if (tile.renderer != null)
                    {
                        tile.renderer.SetPropertyBlock(null);
                        tile.renderer.enabled = false;
                    }
                    break;
            }
        }
    }

    private void ApplyTileVisual(FloorTile tile, float warningPulse)
    {
        if (tile.renderer == null || tilePropertyBlock == null)
            return;

        bool isPreviewTile = IsCollapseWarningActive
            && NextCollapseTime >= 0f
            && IsTileInsideHalf(tile, CurrentHalfSize)
            && !IsTileInsideHalf(tile, NextCollapseHalfSize);

        if (!isPreviewTile)
        {
            tile.transform.localPosition = tile.baseLocalPosition;
            tile.renderer.SetPropertyBlock(null);
            return;
        }

        float flashSpeed = Mathf.Max(0.01f, config.collapseDangerTileFlashSpeed);
        float flashPulse = 0.5f + 0.5f * Mathf.Sin((Time.time * flashSpeed * Mathf.PI * 2f) + (tile.warningNoise * Mathf.PI * 2f));
        float banding = Mathf.Lerp(0.35f, 1f, tile.warningNoise) * Mathf.Clamp01(config.collapseDangerTileBandingStrength);
        float blend = Mathf.Lerp(0.28f, 1f, CollapseWarningNormalized) * Mathf.Lerp(0.55f, 1f, warningPulse) * Mathf.Lerp(0.7f, 1f, flashPulse);
        Color warningColor = Color.Lerp(config.collapseWarningColor, config.collapseDangerTileColor, Mathf.Lerp(0.15f, banding, flashPulse));
        Color tint = Color.Lerp(currentArenaTint, warningColor, blend);
        float emission = Mathf.Max(currentArenaEmission, Mathf.Lerp(config.collapseWarningEmission, config.collapseDangerTileEmission, flashPulse) * blend);

        float jitterHeight = Mathf.Max(0f, config.collapseDangerTileJitterHeight);
        if (jitterHeight > 0f)
        {
            float jitterSpeed = Mathf.Max(0.01f, config.collapseDangerTileJitterSpeed);
            float jitterPulse = Mathf.Sin((Time.time * jitterSpeed * Mathf.PI * 2f) + (tile.warningNoise * Mathf.PI * 4f));
            float jitter = jitterPulse * jitterHeight * Mathf.Lerp(0.2f, 1f, CollapseWarningNormalized) * Mathf.Lerp(0.3f, 1f, flashPulse);
            tile.transform.localPosition = tile.baseLocalPosition + (Vector3.up * jitter);
        }
        else
        {
            tile.transform.localPosition = tile.baseLocalPosition;
        }

        tilePropertyBlock.Clear();
        tilePropertyBlock.SetColor("_Color", tint);
        tilePropertyBlock.SetColor("_EmissionColor", tint * emission);
        tile.renderer.SetPropertyBlock(tilePropertyBlock);
    }

    private void ApplyCollapseQueuedTileVisual(FloorTile tile)
    {
        if (tile.renderer == null || tilePropertyBlock == null)
            return;

        float queuePulse = 0.5f + 0.5f * Mathf.Sin((Time.time - tile.collapseStartTime) * Mathf.PI * 4f + (tile.warningNoise * Mathf.PI * 2f));
        float blend = Mathf.Lerp(0.35f, 0.85f, queuePulse);
        Color tint = Color.Lerp(currentArenaTint, config.collapseDangerTileColor, blend);
        float emission = Mathf.Max(currentArenaEmission, Mathf.Lerp(config.collapseWarningEmission, config.collapseDangerTileEmission, blend));

        tilePropertyBlock.Clear();
        tilePropertyBlock.SetColor("_Color", tint);
        tilePropertyBlock.SetColor("_EmissionColor", tint * emission);
        tile.renderer.SetPropertyBlock(tilePropertyBlock);
    }

    private void ApplyCollapsingTileVisual(FloorTile tile, float collapseT)
    {
        if (tile.renderer == null || tilePropertyBlock == null)
            return;

        float pulse = 0.5f + 0.5f * Mathf.Sin((Time.time - tile.collapseStartTime) * Mathf.PI * 6f);
        float blend = Mathf.Lerp(0.6f, 1f, pulse) * (1f - (collapseT * 0.25f));
        Color tint = Color.Lerp(currentArenaTint, config.collapseImpactColor, blend);
        float emission = Mathf.Max(currentArenaEmission, config.collapseImpactEmission * (1f - collapseT * 0.4f));

        tilePropertyBlock.Clear();
        tilePropertyBlock.SetColor("_Color", tint);
        tilePropertyBlock.SetColor("_EmissionColor", tint * emission);
        tile.renderer.SetPropertyBlock(tilePropertyBlock);
    }

    private void ApplyCollapsingTileMotion(FloorTile tile, float collapseT, float fallDistance)
    {
        if (tile == null || tile.transform == null)
            return;

        float liftHeight = Mathf.Max(0f, config.collapseTilePreDropLift);
        float tiltAngle = Mathf.Max(0f, config.collapseTilePreDropTilt);
        float sideDrift = Mathf.Max(0f, config.collapseTileSideDrift);

        float anticipationT = Mathf.Clamp01(collapseT / 0.22f);
        float fallT = collapseT <= 0.22f ? 0f : Mathf.Clamp01((collapseT - 0.22f) / 0.78f);

        float lift = liftHeight * Mathf.Sin(anticipationT * Mathf.PI) * (1f - fallT);
        float drop = Mathf.SmoothStep(0f, fallDistance, fallT);
        float drift = sideDrift * tile.collapseDirectionSign * fallT * Mathf.Lerp(0.2f, 1f, tile.warningNoise);
        float tiltX = Mathf.Lerp(0f, tiltAngle, anticipationT) * (1f - fallT * 0.35f);
        float tiltZ = Mathf.Lerp(0f, tiltAngle * 0.7f * tile.collapseDirectionSign, anticipationT);

        tile.transform.localPosition = tile.baseLocalPosition + new Vector3(drift, lift - drop, 0f);
        tile.transform.localRotation = tile.baseLocalRotation * Quaternion.Euler(tiltX, 0f, tiltZ);
    }

    private float GetCollapseTileDelay(FloorTile tile, float previousHalfSize, float currentHalfSize)
    {
        if (tile == null || config == null)
            return 0f;

        float cascadeDuration = Mathf.Max(0f, config.collapseTileCascadeDuration);
        float noiseDelay = Mathf.Max(0f, config.collapseTileNoiseDelay);
        if (cascadeDuration <= 0.001f && noiseDelay <= 0.001f)
            return 0f;

        float ringWidth = Mathf.Max(0.01f, previousHalfSize - currentHalfSize);
        float bandDepth = Mathf.Clamp01((previousHalfSize - tile.edgeDistance) / ringWidth);
        float edgeBias = 1f - bandDepth;
        return (edgeBias * cascadeDuration) + (tile.warningNoise * noiseDelay);
    }

    private float GetCollapseDirectionSign(FloorTile tile)
    {
        if (tile == null)
            return 1f;

        float axis = Mathf.Abs(tile.baseLocalPosition.x) >= Mathf.Abs(tile.baseLocalPosition.z)
            ? tile.baseLocalPosition.x
            : tile.baseLocalPosition.z;

        if (Mathf.Abs(axis) <= 0.001f)
            return tile.warningNoise >= 0.5f ? 1f : -1f;

        return Mathf.Sign(axis);
    }

    private bool IsTileInsideHalf(FloorTile tile, float halfSize)
    {
        return tile != null && tile.edgeDistance <= halfSize + 0.001f;
    }

    private void SetLavaReplacementActive(FloorTile tile, bool active)
    {
        if (tile == null)
            return;

        if (tile.lavaReplacementTransform != null)
        {
            tile.lavaReplacementTransform.localPosition = tile.baseLocalPosition;
            tile.lavaReplacementTransform.localRotation = Quaternion.identity;
        }

        if (tile.lavaReplacementRenderer != null)
            tile.lavaReplacementRenderer.enabled = active;

        if (tile.lavaReplacementCollider != null)
            tile.lavaReplacementCollider.enabled = active;
    }

    private void CreateLavaZones()
    {
        EnsureLavaZone("LavaNorth");
        EnsureLavaZone("LavaSouth");
        EnsureLavaZone("LavaEast");
        EnsureLavaZone("LavaWest");
    }

    private Transform EnsureLavaZone(string zoneName)
    {
        Transform existing = lavaRoot.Find(zoneName);
        if (existing != null)
        {
            ConfigureLavaZone(existing);
            return existing;
        }

        GameObject zone = GameObject.CreatePrimitive(PrimitiveType.Cube);
        zone.name = zoneName;
        zone.transform.SetParent(lavaRoot, false);
        ConfigureLavaZone(zone.transform);
        return zone.transform;
    }

    private void ConfigureLavaZone(Transform zoneTransform)
    {
        if (zoneTransform == null)
            return;

        Collider collider = zoneTransform.GetComponent<Collider>();
        if (collider != null)
            collider.isTrigger = false;

        EnsureGroundPlane(zoneTransform.gameObject);

        Renderer renderer = zoneTransform.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = lavaMaterial;

        LavaZone3D lavaZone = zoneTransform.GetComponent<LavaZone3D>();
        if (lavaZone == null)
            lavaZone = zoneTransform.gameObject.AddComponent<LavaZone3D>();

        lavaZone.SetConfig(config);
    }

    private void CreateBoundaries()
    {
        EnsureBoundary("BoundaryNorth");
        EnsureBoundary("BoundarySouth");
        EnsureBoundary("BoundaryEast");
        EnsureBoundary("BoundaryWest");
    }

    private void CreateCollapsePreview()
    {
        EnsureCollapsePreviewEdge("PreviewNorth");
        EnsureCollapsePreviewEdge("PreviewSouth");
        EnsureCollapsePreviewEdge("PreviewEast");
        EnsureCollapsePreviewEdge("PreviewWest");
        SetCollapsePreviewVisible(false);
    }

    private Transform EnsureBoundary(string boundaryName)
    {
        Transform existing = boundaryRoot.Find(boundaryName);
        if (existing != null)
            return existing;

        GameObject boundary = GameObject.CreatePrimitive(PrimitiveType.Cube);
        boundary.name = boundaryName;
        boundary.transform.SetParent(boundaryRoot, false);

        Collider collider = boundary.GetComponent<Collider>();
        if (collider != null)
            collider.isTrigger = true;

        Renderer renderer = boundary.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = boundaryMaterial;

        ProjectileBlocker3D projectileBlocker = boundary.GetComponent<ProjectileBlocker3D>();
        if (projectileBlocker == null)
            projectileBlocker = boundary.gameObject.AddComponent<ProjectileBlocker3D>();

        if (config != null)
            projectileBlocker.SetBlocksProjectiles(config.boundaryBlocksProjectiles);

        return boundary.transform;
    }

    private Transform EnsureCollapsePreviewEdge(string edgeName)
    {
        Transform existing = collapsePreviewRoot.Find(edgeName);
        if (existing != null)
            return existing;

        GameObject edge = GameObject.CreatePrimitive(PrimitiveType.Cube);
        edge.name = edgeName;
        edge.transform.SetParent(collapsePreviewRoot, false);

        Collider collider = edge.GetComponent<Collider>();
        if (collider != null)
        {
            if (Application.isPlaying)
                Destroy(collider);
            else
                DestroyImmediate(collider);
        }

        Renderer renderer = edge.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = collapsePreviewMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        return edge.transform;
    }

    private void UpdateLavaZoneTransforms()
    {
        if (lavaRoot == null || config == null)
            return;

        AlignCollapsedLavaTileRoot();

        float halfSize = CurrentHalfSize;
        float outerHalfSize = config.initialHalfSize + config.lavaPadding;
        float collapsedRingInnerHalfSize = Mathf.Max(0f, halfSize - Mathf.Max(0f, config.lavaInnerOverlap));
        float lavaThickness = Mathf.Max(0f, outerHalfSize - collapsedRingInnerHalfSize);
        float surfaceY = config.center.y;
        float totalWidth = outerHalfSize * 2f;
        float innerWidth = collapsedRingInnerHalfSize * 2f;

        SetZone("LavaNorth", config.center + new Vector3(0f, surfaceY - config.center.y, collapsedRingInnerHalfSize + lavaThickness * 0.5f), new Vector3(totalWidth, config.floorThickness, lavaThickness));
        SetZone("LavaSouth", config.center + new Vector3(0f, surfaceY - config.center.y, -collapsedRingInnerHalfSize - lavaThickness * 0.5f), new Vector3(totalWidth, config.floorThickness, lavaThickness));
        SetZone("LavaEast", config.center + new Vector3(collapsedRingInnerHalfSize + lavaThickness * 0.5f, surfaceY - config.center.y, 0f), new Vector3(lavaThickness, config.floorThickness, innerWidth));
        SetZone("LavaWest", config.center + new Vector3(-collapsedRingInnerHalfSize - lavaThickness * 0.5f, surfaceY - config.center.y, 0f), new Vector3(lavaThickness, config.floorThickness, innerWidth));
    }

    private void SetZone(string zoneName, Vector3 position, Vector3 scale)
    {
        Transform zone = lavaRoot.Find(zoneName);
        if (zone == null)
            return;

        zone.position = position;
        zone.localScale = scale;
    }

    private void UpdateBoundaryTransforms()
    {
        if (boundaryRoot == null || config == null)
            return;

        float halfSize = CurrentHalfSize;
        float y = config.center.y + config.boundaryHeight * 0.5f;
        float length = halfSize * 2f + config.boundaryThickness * 2f;

        SetBoundary("BoundaryNorth", config.center + new Vector3(0f, y - config.center.y, halfSize), new Vector3(length, config.boundaryHeight, config.boundaryThickness));
        SetBoundary("BoundarySouth", config.center + new Vector3(0f, y - config.center.y, -halfSize), new Vector3(length, config.boundaryHeight, config.boundaryThickness));
        SetBoundary("BoundaryEast", config.center + new Vector3(halfSize, y - config.center.y, 0f), new Vector3(config.boundaryThickness, config.boundaryHeight, length));
        SetBoundary("BoundaryWest", config.center + new Vector3(-halfSize, y - config.center.y, 0f), new Vector3(config.boundaryThickness, config.boundaryHeight, length));
    }

    private void SetBoundary(string boundaryName, Vector3 position, Vector3 scale)
    {
        Transform boundary = boundaryRoot.Find(boundaryName);
        if (boundary == null)
            return;

        boundary.position = position;
        boundary.localScale = scale;
    }

    private void UpdateCollapsePreview()
    {
        if (collapsePreviewRoot == null || config == null)
            return;

        bool visible = IsCollapseWarningActive && NextCollapseTime >= 0f && NextCollapseHalfSize < CurrentHalfSize - 0.001f;
        SetCollapsePreviewVisible(visible);
        if (!visible)
            return;

        float previewHalfSize = Mathf.Max(config.finalHalfSize, NextCollapseHalfSize);
        float y = config.center.y + config.collapsePreviewRingHeight;
        float thickness = Mathf.Max(0.05f, config.collapsePreviewRingThickness);
        float lineLength = previewHalfSize * 2f + thickness;
        float pulse = 0.35f + (CollapseWarningNormalized * 0.65f);

        if (collapsePreviewMaterial != null)
        {
            Color color = Color.Lerp(config.arenaColor, config.collapsePreviewRingColor, pulse);
            collapsePreviewMaterial.color = color;
            collapsePreviewMaterial.SetColor("_Color", color);
            collapsePreviewMaterial.SetColor("_EmissionColor", color * Mathf.Lerp(0f, config.collapsePreviewRingEmission, pulse));
        }

        SetPreviewEdge("PreviewNorth", config.center + new Vector3(0f, y - config.center.y, previewHalfSize), new Vector3(lineLength, thickness, thickness));
        SetPreviewEdge("PreviewSouth", config.center + new Vector3(0f, y - config.center.y, -previewHalfSize), new Vector3(lineLength, thickness, thickness));
        SetPreviewEdge("PreviewEast", config.center + new Vector3(previewHalfSize, y - config.center.y, 0f), new Vector3(thickness, thickness, lineLength));
        SetPreviewEdge("PreviewWest", config.center + new Vector3(-previewHalfSize, y - config.center.y, 0f), new Vector3(thickness, thickness, lineLength));
    }

    private void SetCollapsePreviewVisible(bool visible)
    {
        if (collapsePreviewRoot == null)
            return;

        for (int i = 0; i < collapsePreviewRoot.childCount; i++)
        {
            Transform child = collapsePreviewRoot.GetChild(i);
            if (child != null)
                child.gameObject.SetActive(visible);
        }
    }

    private void SetPreviewEdge(string edgeName, Vector3 position, Vector3 scale)
    {
        Transform edge = collapsePreviewRoot.Find(edgeName);
        if (edge == null)
            return;

        edge.position = position;
        edge.localScale = scale;
    }

    private void AlignCollapsedLavaTileRoot()
    {
        if (collapsedLavaTileRoot == null || config == null)
            return;

        collapsedLavaTileRoot.position = config.center;
        collapsedLavaTileRoot.rotation = Quaternion.identity;
        collapsedLavaTileRoot.localScale = Vector3.one;
    }

    private void EnsureGroundPlane(GameObject targetObject)
    {
        if (targetObject == null)
            return;

        GroundPlane groundPlane = targetObject.GetComponent<GroundPlane>();
        if (groundPlane == null)
            targetObject.AddComponent<GroundPlane>();
    }

    private Material CreateRuntimeMaterial(Color color, bool enableEmission = false, Color? emissionColor = null, bool transparent = false)
    {
        Shader shader = Shader.Find("Standard");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");
        if (shader == null)
            return null;

        Material material = new Material(shader)
        {
            hideFlags = HideFlags.DontSave,
            color = color
        };

        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);

        if (enableEmission && emissionColor.HasValue)
        {
            material.EnableKeyword("_EMISSION");
            if (material.HasProperty("_EmissionColor"))
                material.SetColor("_EmissionColor", emissionColor.Value);
        }

        if (transparent)
        {
            material.SetFloat("_Mode", 3f);
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = 3000;
        }

        return material;
    }

    private void ReleaseRuntimeMaterials()
    {
        DestroyRuntimeMaterial(ref arenaMaterial);
        DestroyRuntimeMaterial(ref lavaMaterial);
        DestroyRuntimeMaterial(ref boundaryMaterial);
        DestroyRuntimeMaterial(ref collapsePreviewMaterial);
    }

    private void DestroyRuntimeMaterial(ref Material material)
    {
        if (material == null)
            return;

        if (Application.isPlaying)
            Destroy(material);
        else
            DestroyImmediate(material);

        material = null;
    }
}
