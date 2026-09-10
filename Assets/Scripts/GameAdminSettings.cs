using UnityEngine;

public static class GameAdminSettings
{
    private const string SkipInitialSequenceKey = "Admin.SkipInitialIntroAndTutorial";

    public static bool SkipInitialIntroAndTutorial
    {
        get => PlayerPrefs.GetInt(SkipInitialSequenceKey, 0) == 1;
        set
        {
            PlayerPrefs.SetInt(SkipInitialSequenceKey, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
