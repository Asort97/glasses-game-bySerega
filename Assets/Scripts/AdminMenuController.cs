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
    [SerializeField] private KeyCode toggleInitialSequenceHotkey = KeyCode.F8;

    private void Awake()
    {
        if (panel != null)
            panel.SetActive(false);

        if (leftManager && rightManager && leftPicker && rightPicker)
        {
            var left = new AdminLensPicker(leftManager, leftPicker, "Left");
            var right = new AdminLensPicker(rightManager, rightPicker, "Right");
            if (left.IsOff && right.IsOff) right.UseRandom();
            left.TurnedOff += () => { if (right.IsOff) right.UseRandom(); };
            right.TurnedOff += () => { if (left.IsOff) left.UseRandom(); };
        }
        else Debug.LogError("Admin lens selectors or minigame managers are not assigned.", this);

        if (skipInitialSequenceToggle != null)
        {
            skipInitialSequenceToggle.SetIsOnWithoutNotify(
                GameAdminSettings.SkipInitialIntroAndTutorial);
        }
    }

    private void OnEnable()
    {
        if (refillSkipsButton) refillSkipsButton.onClick.AddListener(RefillSkips);
        if (openButton != null)
            openButton.onClick.AddListener(TogglePanel);
        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePanel);
        if (skipInitialSequenceToggle != null)
            skipInitialSequenceToggle.onValueChanged.AddListener(SetSkipInitialSequence);
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
