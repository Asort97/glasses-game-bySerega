using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

public sealed class PostGameStartTutorialSequence : MonoBehaviour
{
    [Header("Tutorial Games")]
    [SerializeField] private RingsMinigame leftTutorial;
    [SerializeField] private TutorGameMinigame rightTutorial;
    [SerializeField] private RingsMinigame leftRecoveryTutorial;
    [SerializeField] private UnpressableButtonMinigame rightRecoveryTutorial;
    [SerializeField] private LensMinigameManager leftMinigameManager;
    [SerializeField] private LensMinigameManager rightMinigameManager;
    [SerializeField] private LensHealthSystem leftHealth;
    [SerializeField] private LensHealthSystem rightHealth;
    [SerializeField] private CanvasCursor canvasCursor;
    [SerializeField] private LensAudioService audioService;

    [Header("Mini Tutorials")]
    [SerializeField] private MiniTutorialController leftTutorialView;
    [SerializeField] private MiniTutorialController rightTutorialView;

    [Header("Preview")]
    [SerializeField] private SpriteRenderer leftPreviewTitle;
    [SerializeField] private SpriteRenderer rightPreviewTitle;
    [SerializeField] private Sprite rightPreviewPlaceholder;

    [Header("Right Tutorial States")]
    [SerializeField] private GameObject[] rightInputStates;
    [SerializeField] private GameObject rightCompletedQuad;

    [Header("Timing")]
    [Min(0f)] [SerializeField] private float delayBeforeLeftTutorial = 2f;
    [Min(0f)] [SerializeField] private float delayBeforeRightTutorial = 2f;
    [Min(0f)] [SerializeField] private float completedEyesDuration = 3f;
    [Min(0f)] [SerializeField] private float previewDuration = 2f;
    [Min(0f)] [SerializeField] private float previewEndBlankDelay = 0.2f;
    [Min(0.05f)] [SerializeField] private float miniTutorialFrameInterval = 0.6f;
    [FormerlySerializedAs("recoverySpaceBlinkInterval")]
    [Min(0.02f)] [SerializeField] private float recoveryInputBlinkInterval = 0.1f;

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

        yield return ShowPreview(
            leftPreviewTitle,
            leftTutorial != null ? leftTutorial.PreviewTitleSprite : null,
            leftTutorialView,
            leftTutorial != null ? leftTutorial.TutorialType : MiniTutorialType.None);

        if (leftTutorial != null)
            leftTutorial.SetTimeLimitEnabled(false);
        yield return PlayUntilWin(leftTutorial, leftTutorialView, true);
        if (leftTutorial != null)
            leftTutorial.SetTimeLimitEnabled(true);

        if (delayBeforeRightTutorial > 0f)
            yield return new WaitForSeconds(delayBeforeRightTutorial);

        yield return ShowPreview(
            rightPreviewTitle,
            rightPreviewPlaceholder,
            rightTutorialView,
            rightTutorial != null ? rightTutorial.TutorialType : MiniTutorialType.None);

        SetActive(rightInputStates, true);
        SetCompletedQuadActive(false);
        SetCursorVisible(true);
        yield return PlayUntilWin(rightTutorial, rightTutorialView, false);

        SetCursorVisible(false);
        SetActive(rightInputStates, false);
        SetCompletedQuadActive(true);
        if (audioService != null)
            audioService.PlayEye();

        if (completedEyesDuration > 0f)
            yield return new WaitForSeconds(completedEyesDuration);

        if (audioService != null)
            audioService.StopEye();
        rightTutorial.gameObject.SetActive(false);
        SetCompletedQuadActive(false);

        yield return ShowPreview(
            leftPreviewTitle,
            leftRecoveryTutorial != null ? leftRecoveryTutorial.PreviewTitleSprite : null,
            leftTutorialView,
            leftRecoveryTutorial != null ? leftRecoveryTutorial.TutorialType : MiniTutorialType.None);

        yield return PlayLeftRecoveryTutorial();

        yield return ShowPreview(
            rightPreviewTitle,
            rightRecoveryTutorial != null ? rightRecoveryTutorial.PreviewTitleSprite : null,
            rightTutorialView,
            rightRecoveryTutorial != null ? rightRecoveryTutorial.TutorialType : MiniTutorialType.None);

        SetCursorVisible(true);
        yield return PlayRightRecoveryTutorial();
        SetCursorVisible(false);
        RestoreTutorialHealth();
        yield return null;
    }

    public void StopAndHide()
    {
        StopActiveTutorial();

        if (leftTutorial != null)
        {
            leftTutorial.SetTimeLimitEnabled(true);
            leftTutorial.StopGame();
            leftTutorial.gameObject.SetActive(false);
        }

        if (rightTutorial != null)
        {
            rightTutorial.StopGame();
            rightTutorial.gameObject.SetActive(false);
        }

        if (leftRecoveryTutorial != null)
        {
            leftRecoveryTutorial.SetWinEnabled(true);
            leftRecoveryTutorial.StopGame();
            leftRecoveryTutorial.gameObject.SetActive(false);
        }

        if (rightRecoveryTutorial != null)
        {
            rightRecoveryTutorial.StopGame();
            rightRecoveryTutorial.gameObject.SetActive(false);
        }

        if (leftMinigameManager != null)
            leftMinigameManager.HideTutorialMinigameTimer();

        if (rightMinigameManager != null)
            rightMinigameManager.HideTutorialMinigameTimer();

        SetActive(rightInputStates, true);
        SetCompletedQuadActive(false);
        SetCursorVisible(false);
        HidePreviewTitles();
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

    private IEnumerator ShowPreview(
        SpriteRenderer previewTitle,
        Sprite sprite,
        MiniTutorialController tutorialView,
        MiniTutorialType tutorialType)
    {
        if (previewTitle != null)
        {
            previewTitle.sprite = sprite;
            previewTitle.gameObject.SetActive(sprite != null);
        }

        if (tutorialView != null)
            tutorialView.Show(tutorialType, miniTutorialFrameInterval);

        PlayMinigameSwitchSound();

        if (previewDuration > 0f)
            yield return new WaitForSeconds(previewDuration);

        if (previewTitle != null)
            previewTitle.gameObject.SetActive(false);

        if (previewEndBlankDelay > 0f)
            yield return new WaitForSeconds(previewEndBlankDelay);
    }

    private IEnumerator PlayLeftRecoveryTutorial()
    {
        if (leftRecoveryTutorial == null)
            yield break;

        _activeTutorial = leftRecoveryTutorial;
        _tutorialLost = false;
        leftRecoveryTutorial.SetTimeLimitEnabled(true);
        leftRecoveryTutorial.SetWinEnabled(false);
        leftRecoveryTutorial.OnLose += HandleTutorialLose;
        leftRecoveryTutorial.gameObject.SetActive(true);
        if (leftMinigameManager != null)
            leftMinigameManager.ShowTutorialMinigameTimer(leftRecoveryTutorial);

        leftRecoveryTutorial.StartGame();

        while (!_tutorialLost)
            yield return null;

        StopActiveTutorial();
        leftRecoveryTutorial.SetWinEnabled(true);

        if (leftMinigameManager != null)
            leftMinigameManager.HideTutorialMinigameTimer();

        if (leftTutorialView != null)
            leftTutorialView.Hide();

        if (leftMinigameManager != null)
            yield return leftMinigameManager.PlayTutorialLossRecovery(
                leftRecoveryTutorial,
                MiniTutorialType.KeyboardSpaceOnly,
                recoveryInputBlinkInterval);
        else
            leftRecoveryTutorial.gameObject.SetActive(false);
    }

    private IEnumerator PlayRightRecoveryTutorial()
    {
        if (rightRecoveryTutorial == null)
            yield break;

        _activeTutorial = rightRecoveryTutorial;
        _tutorialLost = false;
        rightRecoveryTutorial.OnLose += HandleTutorialLose;
        rightRecoveryTutorial.gameObject.SetActive(true);

        if (rightMinigameManager != null)
            rightMinigameManager.ShowTutorialMinigameTimer(rightRecoveryTutorial);

        rightRecoveryTutorial.StartGame();

        while (!_tutorialLost)
            yield return null;

        StopActiveTutorial();

        if (rightMinigameManager != null)
            rightMinigameManager.HideTutorialMinigameTimer();

        if (rightTutorialView != null)
            rightTutorialView.Hide();

        if (rightMinigameManager != null)
            yield return rightMinigameManager.PlayTutorialLossRecovery(
                rightRecoveryTutorial,
                MiniTutorialType.MouseClick,
                recoveryInputBlinkInterval);
        else
            rightRecoveryTutorial.gameObject.SetActive(false);
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

    private void SetCompletedQuadActive(bool active)
    {
        if (rightCompletedQuad != null)
            rightCompletedQuad.SetActive(active);
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

    private void HidePreviewTitles()
    {
        if (leftPreviewTitle != null)
            leftPreviewTitle.gameObject.SetActive(false);

        if (rightPreviewTitle != null)
            rightPreviewTitle.gameObject.SetActive(false);
    }

    private void PlayMinigameSwitchSound()
    {
        if (audioService != null)
            audioService.Click();
    }

    private void RestoreTutorialHealth()
    {
        if (leftHealth != null)
            leftHealth.RestoreFullHealthAfterTutorial();

        if (rightHealth != null)
            rightHealth.RestoreFullHealthAfterTutorial();
    }
}
