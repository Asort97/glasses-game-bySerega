using UnityEngine;

public static class GameAdminSettings
{
    private const string SkipInitialSequenceKey = "Admin.SkipInitialIntroAndTutorial";
    private const string InfiniteLivesKey = "Admin.InfiniteLives";
    private const string GlobalTimerDisabledKey = "Admin.GlobalTimerDisabled";
    private const string DebuffSelectionKey = "Admin.DebuffSelection";

    /// <summary>Debuff selection value meaning no loading windows at all.</summary>
    public const string DebuffsOff = "#off";

    public static bool SkipInitialIntroAndTutorial
    {
        get => GetBool(SkipInitialSequenceKey);
        set => SetBool(SkipInitialSequenceKey, value);
    }

    public static bool InfiniteLives
    {
        get => GetBool(InfiniteLivesKey);
        set => SetBool(InfiniteLivesKey, value);
    }

    public static bool GlobalTimerDisabled
    {
        get => GetBool(GlobalTimerDisabledKey);
        set => SetBool(GlobalTimerDisabledKey, value);
    }

    /// <summary>Empty for random debuffs, <see cref="DebuffsOff"/>, or a <see cref="DebuffId"/> name.</summary>
    public static string DebuffSelection
    {
        get => PlayerPrefs.GetString(DebuffSelectionKey, "");
        set
        {
            PlayerPrefs.SetString(DebuffSelectionKey, value ?? "");
            PlayerPrefs.Save();
        }
    }

    private static bool GetBool(string key) => PlayerPrefs.GetInt(key, 0) == 1;

    private static void SetBool(string key, bool value)
    {
        PlayerPrefs.SetInt(key, value ? 1 : 0);
        PlayerPrefs.Save();
    }
}
