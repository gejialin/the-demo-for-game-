using System;
using UnityEngine;

public static class BattleFlowDatabase
{
    private const string ResourcePath = "BattleFlowConfig";

    private static BattleFlowData cachedData;
    private static bool hasLoaded;

    public static BattleFlowData Load()
    {
        if (hasLoaded)
            return cachedData;

        hasLoaded = true;
        TextAsset asset = Resources.Load<TextAsset>(ResourcePath);
        if (asset == null)
        {
            Debug.LogWarning("Missing battle flow config at Resources/" + ResourcePath + ".json.");
            return null;
        }

        cachedData = JsonUtility.FromJson<BattleFlowData>(asset.text);
        return cachedData;
    }
}

[Serializable]
public class BattleFlowData
{
    public float startCountdownDuration;
    public float matchDuration;
    public float timeoutHealthDifferenceThreshold;
    public bool allowDrawOnTimeout;
    public string countdownLabel;
    public string activeLabel;
    public string timeUpLabel;
    public string drawLabel;
    public BattleMatchupData[] counterRules;
}

[Serializable]
public class BattleMatchupData
{
    public string playerClass;
    public string enemyClass;
}

public enum BattleTimeoutOutcome
{
    Victory,
    Defeat,
    Draw
}

public static class BattleFlowRules
{
    public static bool TryGetConfiguredCounterClass(
        BattleFlowData flowData,
        CharacterClassType playerClass,
        out CharacterClassType enemyClass)
    {
        enemyClass = default;

        if (flowData == null || flowData.counterRules == null)
            return false;

        string playerClassName = playerClass.ToString();
        for (int i = 0; i < flowData.counterRules.Length; i++)
        {
            BattleMatchupData rule = flowData.counterRules[i];
            if (rule == null || string.IsNullOrEmpty(rule.playerClass) || string.IsNullOrEmpty(rule.enemyClass))
                continue;

            if (!string.Equals(rule.playerClass, playerClassName, StringComparison.OrdinalIgnoreCase))
                continue;

            if (Enum.TryParse(rule.enemyClass, true, out enemyClass))
                return true;
        }

        return false;
    }

    public static bool ShouldStartBattleFromCountdown(float now, float phaseEndTime)
    {
        return now >= phaseEndTime;
    }

    public static BattleTimeoutOutcome ResolveTimeoutOutcome(
        float playerNormalizedHealth,
        float enemyNormalizedHealth,
        float threshold,
        bool allowDrawOnTimeout)
    {
        float difference = playerNormalizedHealth - enemyNormalizedHealth;
        float clampedThreshold = Mathf.Max(0f, threshold);

        if (Mathf.Abs(difference) <= clampedThreshold && allowDrawOnTimeout)
            return BattleTimeoutOutcome.Draw;

        return difference >= 0f ? BattleTimeoutOutcome.Victory : BattleTimeoutOutcome.Defeat;
    }

    public static string BuildPhaseLabel(
        BattleFlowController.BattlePhase phase,
        bool battleResolved,
        bool timedOut,
        float countdownRemaining,
        string readyLabel,
        string countdownLabel,
        string activeLabel,
        string resolvedLabel,
        string timeUpLabel)
    {
        if (battleResolved)
            return timedOut && !string.IsNullOrEmpty(timeUpLabel) ? timeUpLabel : resolvedLabel;

        switch (phase)
        {
            case BattleFlowController.BattlePhase.Countdown:
                return countdownLabel + "  " + Mathf.CeilToInt(Mathf.Max(0f, countdownRemaining));
            case BattleFlowController.BattlePhase.Active:
                return activeLabel;
            default:
                return readyLabel;
        }
    }
}
