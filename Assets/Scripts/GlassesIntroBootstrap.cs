using System.Collections;
using UnityEngine;

/// <summary>
/// Boots the scene after the glasses appearance animation.
/// Keeps lenses, hearts and minigames hidden until the intro animation is fully finished.
/// </summary>
public sealed class GlassesIntroBootstrap : MonoBehaviour
{
    [Header("Intro")]
    [SerializeField] private Animator glassesAnimator;
    [SerializeField] private string glassesAnimatorObjectName = "Glasses_Appearance_Animation (3)";
    [SerializeField] private float fallbackAnimationWait = 3f;
    [SerializeField] private float animationEndPadding = 0.05f;

    [Header("Scene Objects")]
    [SerializeField] private GameObject[] gameplayRoots;
    [SerializeField] private GameObject[] heartsRoots;
    [SerializeField] private Renderer[] lensRenderers;
    [SerializeField] private LensMinigameManager[] minigameManagers;

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

    private void Awake()
    {
        ResolveMissingReferences();
        PrepareHiddenState();
    }

    private void Start()
    {
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

        if (heartsRoots == null || heartsRoots.Length == 0)
        {
            heartsRoots = new[]
            {
                FindSceneObject("LeftLensHearts"),
                FindSceneObject("RightLensHearts")
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
        SetActive(heartsRoots, false);
        SetLensColor(hiddenLensColor);

        if (glassesAnimator != null)
            glassesAnimator.enabled = false;
    }

    private IEnumerator BootstrapRoutine()
    {
        yield return PlayIntroAnimation();
        yield return FadeLenses(hiddenLensColor, visibleLensColor, lensFadeDuration);

        SetActive(gameplayRoots, true);
        SetActive(heartsRoots, true);
        StartManagers();
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

    private IEnumerator FadeLenses(Color from, Color to, float duration)
    {
        if (duration <= 0f)
        {
            SetLensColor(to);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetLensColor(Color.Lerp(from, to, Mathf.Clamp01(elapsed / duration)));
            yield return null;
        }

        SetLensColor(to);
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

    private void StartManagers()
    {
        if (minigameManagers == null)
            return;

        foreach (LensMinigameManager manager in minigameManagers)
        {
            if (manager == null)
                continue;

            manager.enabled = true;
            manager.Begin(true);
        }
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
