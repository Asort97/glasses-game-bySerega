using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Boots the scene after the glasses appearance animation.
/// Keeps lenses and minigames hidden until the intro animation is fully finished.
/// </summary>
public sealed class GlassesIntroBootstrap : MonoBehaviour
{
    [Header("Intro")]
    [SerializeField] private Animator glassesAnimator;
    [SerializeField] private string glassesAnimatorObjectName = "Glasses_Appearance_Animation (3)";
    [SerializeField] private float fallbackAnimationWait = 3f;
    [SerializeField] private float animationEndPadding = 0.05f;
    [SerializeField] private string exitAnimationStateName = "Exit";
    [FormerlySerializedAs("reverseAnimationSpeed")]
    [Min(0.01f)] [SerializeField] private float exitAnimationSpeed = 2f;

    [Header("Scene Objects")]
    [SerializeField] private GameObject[] gameplayRoots;
    [SerializeField] private Renderer[] lensRenderers;
    [SerializeField] private LensMinigameManager[] minigameManagers;
    [SerializeField] private DesktopScreenshotBackground desktopScreenshotBackground;
    [SerializeField] private LensCrtPowerOffController crtPowerOffController;

    [Header("Test Mode")]
    [SerializeField] private bool testMode;
    [SerializeField] private GameObject testGame;
    [SerializeField] private GameObject[] testGamePartners;

    [Header("Lens Color")]
    [SerializeField] private Color hiddenLensColor = Color.black;
    [SerializeField] private Color visibleLensColor = Color.white;
    [SerializeField] private float lensFadeDuration = 0.0f;

    private static readonly string[] ColorProperties =
    {
        "_BaseColor",
        "_Color",
        "_TintColor",
        "_Tint"
    };

    private bool _skipInitialSequence;

    private void Awake()
    {
        ResolveMissingReferences();
        _skipInitialSequence = !testMode && GameAdminSettings.SkipInitialIntroAndTutorial;

        if (testMode)
            PrepareTestState();
        else if (_skipInitialSequence)
            PrepareSkippedState();
        else
            PrepareHiddenState();
    }

    private void Start()
    {
        if (testMode)
            StartCoroutine(TestModeRoutine());
        else if (_skipInitialSequence)
            StartCoroutine(SkippedBootstrapRoutine());
        else
            StartCoroutine(BootstrapRoutine());
    }

    private void ResolveMissingReferences()
    {
        if (glassesAnimator == null)
        {
            GameObject glasses = FindSceneObject(glassesAnimatorObjectName);
            if (glasses != null)
                glassesAnimator = glasses.GetComponent<Animator>();
        }

        if (gameplayRoots == null || gameplayRoots.Length == 0)
        {
            gameplayRoots = new[]
            {
                FindSceneObject("LeftGameContent"),
                FindSceneObject("RightGameContent")
            };
        }

        if (lensRenderers == null || lensRenderers.Length == 0)
        {
            lensRenderers = new[]
            {
                FindRenderer("Lens1"),
                FindRenderer("Lens2")
            };
        }

        if (minigameManagers == null || minigameManagers.Length == 0)
        {
            minigameManagers = FindManagers(gameplayRoots);
        }
    }

    private void PrepareHiddenState()
    {
        SetManagersEnabled(false);
        HideMinigames();
        SetLensColor(hiddenLensColor);
        // LensAudioService.Instance.PlayTVon(false);

        if (glassesAnimator != null)
            glassesAnimator.enabled = false;
    }

    private void PrepareTestState()
    {
        SetManagersEnabled(false);
        SetActive(gameplayRoots, true);
        HideMinigames();
        SetLensColor(hiddenLensColor);

        if (testGame != null)
            testGame.SetActive(false);

        if (glassesAnimator != null)
            glassesAnimator.enabled = false;
    }

    private void PrepareSkippedState()
    {
        SetManagersEnabled(false);
        SetActive(gameplayRoots, true);
        HideMinigames();
        ApplyIntroEndPose();
        SetLensColor(visibleLensColor);

        if (crtPowerOffController != null)
            crtPowerOffController.ResetEffect();
    }

    private IEnumerator TestModeRoutine()
    {
        yield return PlayIntroAnimation();

        SetLensColor(visibleLensColor);
        LensAudioService.Instance.SwitchToMenuTheme();
        LensAudioService.Instance.PlayTVon(true, -1f);
        LensAudioService.Instance.PlayTVon(true, 1f);

        if (desktopScreenshotBackground != null)
            desktopScreenshotBackground.PlayLensEnabledEffect();

        if (testGame == null)
        {
            Debug.LogError($"[{nameof(GlassesIntroBootstrap)}] Test Game is not assigned.", this);
            yield break;
        }

        testGame.SetActive(true);
        SetActive(testGamePartners, true);

        MinigameBase minigame = testGame.GetComponent<MinigameBase>();
        if (minigame != null)
        {
            LensMinigameManager manager = minigame.GetComponentInParent<LensMinigameManager>(true);
            if (manager == null)
            {
                Debug.LogError($"[{nameof(GlassesIntroBootstrap)}] Test minigame has no LensMinigameManager parent.", testGame);
                yield break;
            }

            manager.StartTestMinigame(minigame);
        }

        if (crtPowerOffController != null)
            yield return crtPowerOffController.PlayPowerOn();
    }

    private IEnumerator BootstrapRoutine()
    {
        yield return PlayIntroAnimation();

        SetLensColor(visibleLensColor);
        LensAudioService.Instance.SwitchToMenuTheme();
        LensAudioService.Instance.PlayTVon(true, -1f);
        LensAudioService.Instance.PlayTVon(true, 1f);

        // yield return FadeLenses(hiddenLensColor, visibleLensColor, lensFadeDuration);
        
        if (desktopScreenshotBackground != null)
            desktopScreenshotBackground.PlayLensEnabledEffect();

        SetActive(gameplayRoots, true);
        StartManagers();

        if (crtPowerOffController != null)
            yield return crtPowerOffController.PlayPowerOn();
    }

    private IEnumerator SkippedBootstrapRoutine()
    {
        SetLensColor(visibleLensColor);
        LensAudioService.Instance.SwitchToMenuTheme();
        LensAudioService.Instance.PlayTVon(true, -1f);
        LensAudioService.Instance.PlayTVon(true, 1f);

        if (desktopScreenshotBackground != null)
            desktopScreenshotBackground.PlayLensEnabledEffect();

        SetActive(gameplayRoots, true);
        StartManagers(true);
        yield break;
    }

    private IEnumerator PlayIntroAnimation()
    {
        if (glassesAnimator == null)
        {
            if (fallbackAnimationWait > 0f)
                yield return new WaitForSecondsRealtime(fallbackAnimationWait);
            yield break;
        }

        glassesAnimator.gameObject.SetActive(true);
        glassesAnimator.enabled = true;
        glassesAnimator.Rebind();
        glassesAnimator.Update(0f);

        float clipWait = GetLongestClipLength(glassesAnimator);
        if (glassesAnimator.speed > 0f)
            clipWait /= glassesAnimator.speed;

        float wait = Mathf.Max(clipWait, fallbackAnimationWait);
        wait += animationEndPadding;
        if (wait > 0f)
            yield return new WaitForSecondsRealtime(wait);
    }

    public IEnumerator PlayExitAnimation()
    {
        yield return PlayExitAnimation(1f);
    }

    public IEnumerator PlayExitAnimation(float speedMultiplier)
    {
        if (glassesAnimator == null)
            yield break;

        glassesAnimator.gameObject.SetActive(true);
        glassesAnimator.enabled = true;

        float originalSpeed = glassesAnimator.speed;
        float playbackSpeed = Mathf.Max(0.01f, exitAnimationSpeed)
            * Mathf.Max(0.01f, speedMultiplier);
        float duration = GetClipLength(glassesAnimator, exitAnimationStateName) / playbackSpeed;
        int stateHash = Animator.StringToHash("Base Layer." + exitAnimationStateName);
        glassesAnimator.speed = 0f;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float normalizedTime = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration));
            glassesAnimator.Play(stateHash, 0, normalizedTime);
            glassesAnimator.Update(0f);
            yield return null;
        }

        glassesAnimator.Play(stateHash, 0, 1f);
        glassesAnimator.Update(0f);
        glassesAnimator.speed = originalSpeed;
    }

    private void SetLensColor(Color color)
    {
        if (lensRenderers == null)
            return;

        foreach (Renderer lensRenderer in lensRenderers)
        {
            if (lensRenderer == null)
                continue;

            Material material = lensRenderer.material;
            foreach (string property in ColorProperties)
            {
                if (material.HasProperty(property))
                    material.SetColor(property, color);
            }
        }
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

    private void SetManagersEnabled(bool enabled)
    {
        if (minigameManagers == null)
            return;

        foreach (LensMinigameManager manager in minigameManagers)
        {
            if (manager != null)
                manager.enabled = enabled;
        }
    }

    private void StartManagers(bool skipInitialSequence = false)
    {
        if (minigameManagers == null)
            return;

        foreach (LensMinigameManager manager in minigameManagers)
        {
            if (manager == null)
                continue;

            manager.enabled = true;
            if (skipInitialSequence)
                manager.BeginWithoutInitialSequence(true);
            else
                manager.Begin(true);
        }
    }

    private void ApplyIntroEndPose()
    {
        if (glassesAnimator == null)
            return;

        glassesAnimator.gameObject.SetActive(true);
        glassesAnimator.enabled = true;
        glassesAnimator.Rebind();
        glassesAnimator.Update(0f);

        AnimatorStateInfo state = glassesAnimator.GetCurrentAnimatorStateInfo(0);
        glassesAnimator.Play(state.fullPathHash, 0, 1f);
        glassesAnimator.Update(0f);
        glassesAnimator.enabled = false;
    }

    private void HideMinigames()
    {
        if (gameplayRoots == null)
            return;

        foreach (GameObject root in gameplayRoots)
        {
            if (root == null)
                continue;

            MinigameBase[] minigames = root.GetComponentsInChildren<MinigameBase>(true);
            foreach (MinigameBase minigame in minigames)
            {
                if (minigame == null)
                    continue;

                minigame.StopGame();
                minigame.gameObject.SetActive(false);
            }
        }
    }

    private static float GetLongestClipLength(Animator animator)
    {
        RuntimeAnimatorController controller = animator.runtimeAnimatorController;
        if (controller == null || controller.animationClips == null)
            return 0f;

        float longest = 0f;
        foreach (AnimationClip clip in controller.animationClips)
        {
            if (clip != null && clip.length > longest)
                longest = clip.length;
        }

        return longest;
    }

    private static float GetClipLength(Animator animator, string clipName)
    {
        RuntimeAnimatorController controller = animator.runtimeAnimatorController;
        if (controller != null && controller.animationClips != null)
        {
            foreach (AnimationClip clip in controller.animationClips)
                if (clip != null && clip.name == clipName)
                    return clip.length;
        }

        return GetLongestClipLength(animator);
    }

    private static Renderer FindRenderer(string objectName)
    {
        GameObject obj = FindSceneObject(objectName);
        return obj != null ? obj.GetComponent<Renderer>() : null;
    }

    private static LensMinigameManager[] FindManagers(GameObject[] roots)
    {
        if (roots == null)
            return new LensMinigameManager[0];

        LensMinigameManager[] managers = new LensMinigameManager[roots.Length];
        for (int i = 0; i < roots.Length; i++)
            managers[i] = roots[i] != null ? roots[i].GetComponent<LensMinigameManager>() : null;

        return managers;
    }

    private static GameObject FindSceneObject(string objectName)
    {
        if (string.IsNullOrEmpty(objectName))
            return null;

        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (obj.name == objectName && obj.scene.IsValid())
                return obj;
        }

        return null;
    }
}
