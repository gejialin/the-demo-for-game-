using UnityEngine;

[CreateAssetMenu(fileName = "ArenaFieldConfig", menuName = "Magic War/Arena Field Config")]
public class ArenaFieldConfig : ScriptableObject
{
    [Header("Layout")]
    public Vector3 center;
    public float initialHalfSize;
    public float finalHalfSize;
    public float floorThickness;
    public float floorTileSize;
    public float floorTileGap;
    public float lavaPadding;
    public float lavaSurfaceOffset;
    public float lavaInnerOverlap;

    [Header("Collapse")]
    public float collapseStartDelay;
    public float collapseStepInterval;
    public float collapseStepSize;
    public float collapseFallDuration;
    public float collapseFallDistance;
    public float collapseTileCascadeDuration;
    public float collapseTileNoiseDelay;
    public float collapseTilePreDropLift;
    public float collapseTilePreDropTilt;
    public float collapseTileSideDrift;

    [Header("Collapse Feedback")]
    public float collapseWarningLeadTime;
    public float collapseWarningPulseSpeed;
    public float collapseWarningEmission;
    public Color collapseWarningColor;
    public float collapseImpactDuration;
    public float collapseImpactEmission;
    public Color collapseImpactColor;
    public float collapsePreviewRingThickness;
    public float collapsePreviewRingHeight;
    public float collapsePreviewRingEmission;
    public Color collapsePreviewRingColor;
    public float collapseDangerTileFlashSpeed;
    public float collapseDangerTileEmission;
    public Color collapseDangerTileColor;
    public float collapseDangerTileJitterHeight;
    public float collapseDangerTileJitterSpeed;
    public float collapseDangerTileBandingStrength;
    public float edgeDangerWarningDistance;
    public float edgeDangerCriticalDistance;
    public float edgeDangerPulseSpeed;
    public float edgeDangerEmission;
    public Color edgeDangerColor;
    public float edgeOutsideEmission;
    public Color edgeOutsideColor;
    public float edgeDangerOverlayMaxAlpha;
    public float edgeDangerOverlayPulseSpeed;
    public float edgeOutsideOverlayAlpha;
    public Color edgeDangerOverlayColor;
    public Color edgeOutsideOverlayColor;
    public float lavaOverlayAlpha;
    public float lavaOverlayPulseSpeed;
    public Color lavaOverlayColor;
    public float burnOverlayAlpha;
    public float burnOverlayPulseSpeed;
    public Color burnOverlayColor;

    [Header("Lava Damage")]
    public float contactDamagePerTick;
    public float tickInterval;
    public bool applyBurn;
    public float burnDamagePerTick;
    public float burnTickInterval;
    public float burnDuration;
    public bool lavaBlocksProjectiles;

    [Header("Visuals")]
    public string arenaDisplayName = "黑曜塌陷场";
    [TextArea(2, 4)] public string preparationRuleSummary = "地板会按阶段塌陷，熔浆将填补危险区域。";
    public Color arenaColor;
    public Color lavaColor;
    public Color boundaryColor;
    public float boundaryHeight;
    public float boundaryThickness;
    public bool boundaryBlocksProjectiles = true;

    [Header("Generated Object Names")]
    public string arenaRootName;
    public string safeFloorName;
    public string lavaRootName;
    public string boundaryRootName;

    private void OnValidate()
    {
        Normalize();
    }

    public void Normalize()
    {
        initialHalfSize = Mathf.Max(0.5f, initialHalfSize);
        finalHalfSize = Mathf.Clamp(finalHalfSize, 0f, initialHalfSize);

        floorThickness = Mathf.Max(0.05f, floorThickness);
        floorTileSize = Mathf.Max(0.2f, floorTileSize);
        floorTileGap = Mathf.Max(0f, floorTileGap);
        lavaPadding = Mathf.Max(0f, lavaPadding);
        lavaInnerOverlap = Mathf.Max(0f, lavaInnerOverlap);

        collapseStartDelay = Mathf.Max(0f, collapseStartDelay);
        collapseStepInterval = Mathf.Max(0f, collapseStepInterval);
        collapseStepSize = Mathf.Max(0.01f, collapseStepSize);
        collapseFallDuration = Mathf.Max(0.01f, collapseFallDuration);
        collapseFallDistance = Mathf.Max(0.1f, collapseFallDistance);
        collapseTileCascadeDuration = Mathf.Max(0f, collapseTileCascadeDuration);
        collapseTileNoiseDelay = Mathf.Max(0f, collapseTileNoiseDelay);
        collapseTilePreDropLift = Mathf.Max(0f, collapseTilePreDropLift);
        collapseTilePreDropTilt = Mathf.Max(0f, collapseTilePreDropTilt);
        collapseTileSideDrift = Mathf.Max(0f, collapseTileSideDrift);

        collapseWarningLeadTime = Mathf.Max(0f, collapseWarningLeadTime);
        collapseWarningPulseSpeed = Mathf.Max(0f, collapseWarningPulseSpeed);
        collapseWarningEmission = Mathf.Max(0f, collapseWarningEmission);
        collapseImpactDuration = Mathf.Max(0f, collapseImpactDuration);
        collapseImpactEmission = Mathf.Max(0f, collapseImpactEmission);
        collapsePreviewRingThickness = Mathf.Max(0.05f, collapsePreviewRingThickness);
        collapsePreviewRingEmission = Mathf.Max(0f, collapsePreviewRingEmission);
        collapseDangerTileFlashSpeed = Mathf.Max(0f, collapseDangerTileFlashSpeed);
        collapseDangerTileEmission = Mathf.Max(0f, collapseDangerTileEmission);
        collapseDangerTileJitterHeight = Mathf.Max(0f, collapseDangerTileJitterHeight);
        collapseDangerTileJitterSpeed = Mathf.Max(0f, collapseDangerTileJitterSpeed);
        collapseDangerTileBandingStrength = Mathf.Clamp01(collapseDangerTileBandingStrength);
        edgeDangerWarningDistance = Mathf.Max(0.01f, edgeDangerWarningDistance);
        edgeDangerCriticalDistance = Mathf.Clamp(edgeDangerCriticalDistance, 0f, edgeDangerWarningDistance);
        edgeDangerPulseSpeed = Mathf.Max(0f, edgeDangerPulseSpeed);
        edgeDangerEmission = Mathf.Max(0f, edgeDangerEmission);
        edgeOutsideEmission = Mathf.Max(0f, edgeOutsideEmission);
        edgeDangerOverlayMaxAlpha = Mathf.Clamp01(edgeDangerOverlayMaxAlpha);
        edgeDangerOverlayPulseSpeed = Mathf.Max(0f, edgeDangerOverlayPulseSpeed);
        edgeOutsideOverlayAlpha = Mathf.Clamp01(edgeOutsideOverlayAlpha);
        lavaOverlayAlpha = Mathf.Clamp01(lavaOverlayAlpha);
        lavaOverlayPulseSpeed = Mathf.Max(0f, lavaOverlayPulseSpeed);
        burnOverlayAlpha = Mathf.Clamp01(burnOverlayAlpha);
        burnOverlayPulseSpeed = Mathf.Max(0f, burnOverlayPulseSpeed);

        contactDamagePerTick = Mathf.Max(0f, contactDamagePerTick);
        tickInterval = Mathf.Max(0.01f, tickInterval);
        burnDamagePerTick = Mathf.Max(0f, burnDamagePerTick);
        burnTickInterval = Mathf.Max(0.01f, burnTickInterval);
        burnDuration = Mathf.Max(0f, burnDuration);

        boundaryHeight = Mathf.Max(0.1f, boundaryHeight);
        boundaryThickness = Mathf.Max(0.05f, boundaryThickness);
        arenaDisplayName = CoalesceName(arenaDisplayName, "黑曜塌陷场");
        preparationRuleSummary = CoalesceName(preparationRuleSummary, "地板会按阶段塌陷，熔浆将填补危险区域。");

        arenaRootName = CoalesceName(arenaRootName, "ArenaField");
        safeFloorName = CoalesceName(safeFloorName, "SafeFloor");
        lavaRootName = CoalesceName(lavaRootName, "LavaRoot");
        boundaryRootName = CoalesceName(boundaryRootName, "BoundaryRoot");
    }

    private static string CoalesceName(string value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }
}
