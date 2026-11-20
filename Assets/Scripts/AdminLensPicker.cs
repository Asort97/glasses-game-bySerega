using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

internal sealed class AdminLensPicker
{
    private const int RandomPool = 0, Disabled = 1;
    private readonly List<MinigameBase> _games;
    private readonly Dropdown _dropdown;
    private readonly LensMinigameManager _manager;
    private readonly string _key;
    public bool IsOff => _dropdown.value == Disabled;
    public event Action TurnedOff;

    public AdminLensPicker(LensMinigameManager manager, Dropdown dropdown, string side)
    {
        _manager = manager;
        _key = "Admin.RegularPool." + side;
        _games = new List<MinigameBase>(manager.RegularPool);
        _games.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        _dropdown = dropdown;
        _dropdown.ClearOptions();
        var labels = new List<string> { side + ": Random", side + ": Off" };
        foreach (var game in _games)
            labels.Add(side + ": " + game.name.Replace("_minigame", ""));
        _dropdown.AddOptions(labels);

        string saved = PlayerPrefs.GetString(_key, "");
        int index = _games.FindIndex(game => game.name == saved);
        _dropdown.SetValueWithoutNotify(saved == "#off" ? Disabled : index < 0 ? RandomPool : index + 2);
        _dropdown.RefreshShownValue();
        Apply(_dropdown.value);
        _dropdown.onValueChanged.AddListener(Select);
    }

    public void UseRandom() => _dropdown.value = RandomPool;

    private void Select(int index)
    {
        if (index == Disabled) TurnedOff?.Invoke();
        Apply(index);
        PlayerPrefs.SetString(_key, index == Disabled ? "#off" : index == RandomPool ? "" : _games[index - 2].name);
        PlayerPrefs.Save();
    }

    private void Apply(int index) => _manager.SetAdminSelection(index == Disabled, index < 2 ? null : _games[index - 2]);
}
