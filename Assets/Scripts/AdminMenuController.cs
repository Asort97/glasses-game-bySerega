using UnityEngine;
using UnityEngine.UI;

public sealed class AdminMenuController : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Button openButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Toggle skipInitialSequenceToggle;

    private void Awake()
    {
        if (panel != null)
            panel.SetActive(false);

        if (skipInitialSequenceToggle != null)
        {
            skipInitialSequenceToggle.SetIsOnWithoutNotify(
                GameAdminSettings.SkipInitialIntroAndTutorial);
        }
    }

    private void OnEnable()
    {
        if (openButton != null)
            openButton.onClick.AddListener(TogglePanel);
        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePanel);
        if (skipInitialSequenceToggle != null)
            skipInitialSequenceToggle.onValueChanged.AddListener(SetSkipInitialSequence);
    }

    private void OnDisable()
    {
        if (openButton != null)
            openButton.onClick.RemoveListener(TogglePanel);
        if (closeButton != null)
            closeButton.onClick.RemoveListener(ClosePanel);
        if (skipInitialSequenceToggle != null)
            skipInitialSequenceToggle.onValueChanged.RemoveListener(SetSkipInitialSequence);
    }

    private void TogglePanel()
    {
        if (panel != null)
            panel.SetActive(!panel.activeSelf);
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
}
