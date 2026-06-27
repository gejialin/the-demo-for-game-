using System.IO;
using UnityEngine;

public static class PreparationPresentationDatabase
{
    private const string ArenaConfigAssetPath = "Assets/Configs/ArenaFieldConfig.asset";
    private const string DefaultModeLabel = "1v1 生存对抗";

    private static ArenaFieldConfig cachedArenaConfig;
    private static bool hasResolvedArenaConfig;

    public static string GetModeLabel()
    {
        return UiTextDatabase.Get("prep.mode.value", DefaultModeLabel);
    }

    public static string GetArenaLabel()
    {
        ArenaFieldConfig config = LoadArenaConfig();
        if (config != null && !string.IsNullOrWhiteSpace(config.arenaDisplayName))
            return config.arenaDisplayName;

        return UiTextDatabase.Get("prep.arena.value", "黑曜塌陷场");
    }

    public static string GetRuleSummary()
    {
        ArenaFieldConfig config = LoadArenaConfig();
        if (config != null && !string.IsNullOrWhiteSpace(config.preparationRuleSummary))
            return config.preparationRuleSummary;

        return UiTextDatabase.Get("prep.rule.value", "地板会按阶段塌陷，熔浆将填补危险区域。");
    }

    private static ArenaFieldConfig LoadArenaConfig()
    {
        if (hasResolvedArenaConfig)
            return cachedArenaConfig;

        hasResolvedArenaConfig = true;

#if UNITY_EDITOR
        cachedArenaConfig = UnityEditor.AssetDatabase.LoadAssetAtPath<ArenaFieldConfig>(ArenaConfigAssetPath);
        if (cachedArenaConfig != null)
            return cachedArenaConfig;
#endif

        string resourcesPath = Path.GetFileNameWithoutExtension(ArenaConfigAssetPath);
        cachedArenaConfig = Resources.Load<ArenaFieldConfig>(resourcesPath);
        return cachedArenaConfig;
    }
}
