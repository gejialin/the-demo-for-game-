using System;
using UnityEngine;

public static class UiTextDatabase
{
    private const string ResourcePath = "UiTextConfig";
    private const string DefaultLanguage = "zh-CN";

    private static UiTextConfigData cachedData;
    private static bool hasLoaded;

    public static string Get(string key, string fallback = "")
    {
        if (string.IsNullOrEmpty(key))
            return fallback;

        UiTextConfigData data = Load();
        if (data == null || data.entries == null)
            return fallback;

        for (int i = 0; i < data.entries.Length; i++)
        {
            UiTextEntry entry = data.entries[i];
            if (entry == null || !string.Equals(entry.key, key, StringComparison.OrdinalIgnoreCase))
                continue;

            string localized = entry.GetValue(GetDefaultLanguage(data));
            if (!string.IsNullOrEmpty(localized))
                return localized;

            return !string.IsNullOrEmpty(entry.zhCN) ? entry.zhCN : fallback;
        }

        return fallback;
    }

    public static string GetCharacterLabel(CharacterClassType classType)
    {
        return Get("character.class." + classType, classType.ToString());
    }

    public static string GetCharacterStateLabel(CharacterState state)
    {
        return Get("character.state." + state, state.ToString());
    }

    private static UiTextConfigData Load()
    {
        if (hasLoaded)
            return cachedData;

        hasLoaded = true;
        TextAsset asset = Resources.Load<TextAsset>(ResourcePath);
        if (asset == null)
        {
            Debug.LogWarning("Missing UI text config at Resources/" + ResourcePath + ".json.");
            return null;
        }

        cachedData = JsonUtility.FromJson<UiTextConfigData>(asset.text);
        return cachedData;
    }

    private static string GetDefaultLanguage(UiTextConfigData data)
    {
        if (data == null || string.IsNullOrEmpty(data.defaultLanguage))
            return DefaultLanguage;

        return data.defaultLanguage;
    }
}

[Serializable]
public class UiTextConfigData
{
    public string defaultLanguage;
    public UiTextEntry[] entries;
}

[Serializable]
public class UiTextEntry
{
    public string key;
    public string zhCN;
    public string enUS;

    public string GetValue(string languageCode)
    {
        if (string.Equals(languageCode, "en-US", StringComparison.OrdinalIgnoreCase))
            return enUS;

        return zhCN;
    }
}
