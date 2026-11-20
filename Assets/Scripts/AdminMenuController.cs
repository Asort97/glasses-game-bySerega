using UnityEngine;
using UnityEngine.UI;

public sealed class AdminMenuController : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Button openButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button refillSkipsButton;
    [SerializeField] private LensSkipController[] skipControllers;
    [SerializeField] private LensMinigameManager leftManager;
    [SerializeField] private LensMinigameManager rightManager;
    [SerializeField] private Dropdown leftPicker;
    [SerializeField] private Dropdown rightPicker;
    [SerializeField] private Toggle skipInitialSequenceToggle;
    [SerializeField] private Toggle infiniteLivesToggle;
    [SerializeField] private Toggle globalTimerOffToggle;
    [SerializeField] private Dropdown debuffPicker;
    [SerializeField] private DebuffSpawner debuffSpawner;
    [SerializeField] private KeyCode toggleInitialSequenceHotkey = KeyCode.F8;

    private void Awake()
    {
        if (panel != null)
            panel.SetActive(false);

        if (leftManager && rightManager && leftPicker && rightPicker)
        {
            // Both lenses may be off at once: that pauses the run.
            new AdminLensPicker(leftManager, leftPicker, "Left");
            new AdminLensPicker(rightManager, rightPicker, "Right");
        }
        else Debug.LogError("Admin lens selectors or minigame managers are not assigned.", this);

        if (skipInitialSequenceToggle != null)
        {
            skipInitialSequenceToggle.SetIsOnWithoutNotify(
                GameAdminSettings.SkipInitialIntroAndTutorial);
        }
        if (infiniteLivesToggle) infiniteLivesToggle.SetIsOnWithoutNotify(GameAdminSettings.InfiniteLives);
        if (globalTimerOffToggle) globalTimerOffToggle.SetIsOnWithoutNotify(GameAdminSettings.GlobalTimerDisabled);
        SetupDebuffPicker();
    }

    private readonly System.Collections.Generic.List<string> _debuffValues = new System.Collections.Generic.List<string>();

    private void SetupDebuffPicker()
    {
        if (!debuffPicker) return;
        _debuffValues.Clear();
        var labels = new System.Collections.Generic.List<string>();
        _debuffValues.Add(""); labels.Add("Debuffs: Random");
        _debuffValues.Add(GameAdminSettings.DebuffsOff); labels.Add("Debuffs: Off");
        foreach (DebuffId id in System.Enum.GetValues(typeof(DebuffId)))
        {
            if (debuffSpawner && System.Array.FindIndex(debuffSpawner.Definitions ?? new DebuffDefinition[0],
                    d => d != null && d.Id == id) < 0) continue;
            _debuffValues.Add(id.ToString());
            labels.Add("Debuff: " + id);
        }
        debuffPicker.ClearOptions();
        debuffPicker.AddOptions(labels);
        int index = _debuffValues.IndexOf(GameAdminSettings.DebuffSelection);
        debuffPicker.SetValueWithoutNotify(Mathf.Max(0, index));
        debuffPicker.RefreshShownValue();
    }

    private void SelectDebuff(int index)
    {
        if (index < 0 || index >= _debuffValues.Count) return;
        GameAdminSettings.DebuffSelection = _debuffValues[index];
        if (_debuffValues[index] == GameAdminSettings.DebuffsOff && debuffSpawner)
            debuffSpawner.ClearWindows();
    }

    private static void SetInfiniteLives(bool value) => GameAdminSettings.InfiniteLives = value;
    private static void SetGlobalTimerOff(bool value) => GameAdminSettings.GlobalTimerDisabled = value;

    private void OnEnable()
    {
        if (refillSkipsButton) refillSkipsButton.onClick.AddListener(RefillSkips);
        if (openButton != null)
            openButton.onClick.AddListener(TogglePanel);
        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePanel);
        if (skipInitialSequenceToggle != null)
            skipInitialSequenceToggle.onValueChanged.AddListener(SetSkipInitialSequence);
        if (infiniteLivesToggle) infiniteLivesToggle.onValueChanged.AddListener(SetInfiniteLives);
        if (globalTimerOffToggle) globalTimerOffToggle.onValueChanged.AddListener(SetGlobalTimerOff);
        if (debuffPicker) debuffPicker.onValueChanged.AddListener(SelectDebuff);
    }

    private void OnDisable()
    {
        if (refillSkipsButton) refillSkipsButton.onClick.RemoveListener(RefillSkips);
        if (openButton != null)
            openButton.onClick.RemoveListener(TogglePanel);
        if (closeButton != null)
            closeButton.onClick.RemoveListener(ClosePanel);
        if (skipInitialSequenceToggle != null)
            skipInitialSequenceToggle.onValueChanged.RemoveListener(SetSkipInitialSequence);
        if (infiniteLivesToggle) infiniteLivesToggle.onValueChanged.RemoveListener(SetInfiniteLives);
        if (globalTimerOffToggle) globalTimerOffToggle.onValueChanged.RemoveListener(SetGlobalTimerOff);
        if (debuffPicker) debuffPicker.onValueChanged.RemoveListener(SelectDebuff);
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleInitialSequenceHotkey))
            ToggleInitialSequence();
    }

    private void TogglePanel()
    {
        if (panel != null)
            panel.SetActive(!panel.activeSelf);
    }

    private void RefillSkips()
    {
        foreach (var controller in skipControllers)
            if (controller) controller.RefillCharges();
    }

    private void ClosePanel()
    {
        if (panel != null)
            panel.SetActive(false);
    }

    private static void SetSkipInitialSequence(bool skip)
    {
        GameAdminSettings.SkipInitialIntroAndTutorial = skip;
    }

    private void ToggleInitialSequence()
    {
        bool skip = !GameAdminSettings.SkipInitialIntroAndTutorial;
        GameAdminSettings.SkipInitialIntroAndTutorial = skip;
        skipInitialSequenceToggle.SetIsOnWithoutNotify(skip);
    }
}
