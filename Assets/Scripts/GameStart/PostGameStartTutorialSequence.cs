using System.Collections;
using UnityEngine;

public sealed class PostGameStartTutorialSequence : MonoBehaviour
{
    [Header("Tutorial Games")]
    [SerializeField] private RingsMinigame leftTutorial;
    [SerializeField] private TutorGameMinigame rightTutorial;
    [SerializeField] private CanvasCursor canvasCursor;
    [SerializeField] private LensAudioService audioService;

    [Header("Mini Tutorials")]
    [SerializeField] private MiniTutorialController leftTutorialView;
    [SerializeField] private MiniTutorialController rightTutorialView;

    [Header("Right Tutorial States")]
    [SerializeField] private GameObject[] rightInputStates;
    [SerializeField] private GameObject[] rightCompletedEyes;

    [Header("Timing")]
    [Min(0f)] [SerializeField] private float delayBeforeLeftTutorial = 2f;
    [Min(0f)] [SerializeField] private float delayBeforeRightTutorial = 2f;
    [Min(0f)] [SerializeField] private float completedEyesDuration = 3f;
    [Min(0.05f)] [SerializeField] private float miniTutorialFrameInterval = 0.6f;

    private MinigameBase _activeTutorial;
    private bool _tutorialWon;
    private bool _tutorialLost;

    private void Awake()
    {
        StopAndHide();
    }

    public IEnumerator Play()
    {
        StopAndHide();

        if (delayBeforeLeftTutorial > 0f)
            yield return new WaitForSeconds(delayBeforeLeftTutorial);

        PlayMinigameSwitchSound();
        yield return PlayUntilWin(leftTutorial, leftTutorialView, true);

        if (delayBeforeRightTutorial > 0f)
            yield return new WaitForSeconds(delayBeforeRightTutorial);

        SetActive(rightInputStates, true);
        SetActive(rightCompletedEyes, false);
        SetCursorVisible(true);
        PlayMinigameSwitchSound();
        yield return PlayUntilWin(rightTutorial, rightTutorialView, false);

        SetCursorVisible(false);
        SetActive(rightInputStates, false);
        SetActive(rightCompletedEyes, true);
        if (audioService != null)
            audioService.PlayEye();

        if (completedEyesDuration > 0f)
            yield return new WaitForSeconds(completedEyesDuration);

        if (audioService != null)
            audioService.StopEye();
        rightTutorial.gameObject.SetActive(false);
        SetActive(rightCompletedEyes, false);
        yield return null;
    }

    public void StopAndHide()
    {
        StopActiveTutorial();

        if (leftTutorial != null)
        {
            leftTutorial.StopGame();
            leftTutorial.gameObject.SetActive(false);
        }

        if (rightTutorial != null)
        {
            rightTutorial.StopGame();
            rightTutorial.gameObject.SetActive(false);
        }

        SetActive(rightInputStates, true);
        SetActive(rightCompletedEyes, false);
        SetCursorVisible(false);
        HideMiniTutorials();

        if (audioService != null)
            audioService.StopEye();
    }

    private IEnumerator PlayUntilWin(
        MinigameBase tutorial,
        MiniTutorialController tutorialView,
        bool hideAfterWin)
    {
        if (tutorial == null)
            yield break;

        _activeTutorial = tutorial;
        _tutorialWon = false;
        _tutorialLost = false;
        tutorial.OnWin += HandleTutorialWin;
        tutorial.OnLose += HandleTutorialLose;
        tutorial.gameObject.SetActive(true);
        if (tutorialView != null)
            tutorialView.Show(tutorial.TutorialType, miniTutorialFrameInterval);
        tutorial.StartGame();

        while (!_tutorialWon)
        {
            if (_tutorialLost)
            {
                _tutorialLost = false;
                tutorial.StopGame();
                tutorial.StartGame();
            }

            yield return null;
        }

        StopActiveTutorial();
        if (tutorialView != null)
            tutorialView.Hide();

        if (hideAfterWin)
            tutorial.gameObject.SetActive(false);
    }

    private void HandleTutorialWin()
    {
        _tutorialWon = true;
    }

    private void HandleTutorialLose()
    {
        _tutorialLost = true;
    }

    private void StopActiveTutorial()
    {
        if (_activeTutorial == null)
            return;

        _activeTutorial.OnWin -= HandleTutorialWin;
        _activeTutorial.OnLose -= HandleTutorialLose;
        _activeTutorial.StopGame();
        _activeTutorial = null;
    }

    private static void SetActive(GameObject[] objects, bool active)
    {
        if (objects == null)
            return;

        foreach (GameObject obj in objects)
        {
            if (obj != null)
                obj.SetActive(active);
        }
    }

    private void SetCursorVisible(bool visible)
    {
        if (canvasCursor != null)
            canvasCursor.SetCursorSpriteVisible(visible);
    }

    private void HideMiniTutorials()
    {
        if (leftTutorialView != null)
            leftTutorialView.Hide();

        if (rightTutorialView != null)
            rightTutorialView.Hide();
    }

    private void PlayMinigameSwitchSound()
    {
        if (audioService != null)
            audioService.Click();
    }
}
