using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public sealed class GameQuitController : MonoBehaviour
{
    [Header("Game Start")]
    [SerializeField] private LensMinigameManager[] lensManagers;
    [SerializeField] private Image[] gameStartExitFills;

    [Header("Lens Effects")]
    [SerializeField] private Renderer[] lensRenderers;
    [SerializeField] private LensCrtPowerOffController crtPowerOffController;
    [SerializeField] private float holdDuration = 5f;
    [FormerlySerializedAs("flickerReleaseDuration")]
    [Min(0.01f)] [SerializeField] private float holdReleaseDuration = 0.5f;
    [Range(0f, 1f)] [SerializeField] private float targetFlickerStrength = 0.5f;

    [Header("Quick Exit During Gameplay")]
    [Min(1)] [SerializeField] private int quickExitPressCount = 5;
    [Min(0.1f)] [SerializeField] private float quickExitPressWindow = 2f;
    [Min(1f)] [SerializeField] private float quickExitSpeedMultiplier = 5f;

    [Header("Glasses")]
    [SerializeField] private GlassesIntroBootstrap introBootstrap;
    [Min(0f)] [SerializeField] private float exitAnimationLeadTime = 0.5f;

    private static readonly int FlickerEnabledId = Shader.PropertyToID("_FlickerBandingEnabled");
    private static readonly int FlickerStrengthId = Shader.PropertyToID("_FlickerBandingStrength");

    private Material[] _lensMaterials;
    private float _heldTime;
    private float _currentFlickerStrength;
    private float _escapeSpamStartTime = float.NegativeInfinity;
    private float _previousTimeScale = 1f;
    private int _escapePressCount;
    private bool _quitting;
    private bool _exitAnimationFinished;

    private void Awake()
    {
        CacheMaterials();
        SetGameStartExitFill(0f);
    }

    private void Update()
    {
        if (_quitting)
            return;

        if (CanQuitFromGameStart())
        {
            ResetEscapeSpam();
            UpdateGameStartHold();
            return;
        }

        ReleaseHold();
        UpdateGameplayEscapeSpam();
    }

    private void UpdateGameStartHold()
    {
        if (!Input.GetKey(KeyCode.Escape))
        {
            ReleaseHold();
            return;
        }

        _heldTime += Time.unscaledDeltaTime;
        float progress = Mathf.Clamp01(_heldTime / Mathf.Max(0.01f, holdDuration));
        SetGameStartExitFill(progress);

        if (progress >= 1f)
            StartCoroutine(QuitRoutine(1f, false));
    }

    private bool CanQuitFromGameStart()
    {
        if (lensManagers == null || lensManagers.Length == 0)
            return false;

        foreach (LensMinigameManager manager in lensManagers)
            if (manager == null || !manager.IsPlayingGameStart)
                return false;

        return true;
    }

    private void UpdateGameplayEscapeSpam()
    {
        if (!CanQuickQuitFromGameplay())
        {
            ResetEscapeSpam();
            return;
        }

        if (!Input.GetKeyDown(KeyCode.Escape))
            return;

        float now = Time.unscaledTime;
        if (_escapePressCount == 0 || now - _escapeSpamStartTime > quickExitPressWindow)
        {
            _escapePressCount = 0;
            _escapeSpamStartTime = now;
        }

        _escapePressCount++;

        if (_escapePressCount >= Mathf.Max(1, quickExitPressCount))
            StartCoroutine(QuitRoutine(quickExitSpeedMultiplier, true));
    }

    private bool CanQuickQuitFromGameplay()
    {
        if (lensManagers == null || lensManagers.Length == 0)
            return false;

        bool hasRegularMinigame = false;
        foreach (LensMinigameManager manager in lensManagers)
        {
            if (manager == null || manager.IsPlayingGameStart)
                return false;

            if (manager.IsPlayingRegularMinigame)
                hasRegularMinigame = true;
        }

        return hasRegularMinigame;
    }

    private IEnumerator QuitRoutine(float speedMultiplier, bool rampFlicker)
    {
        _quitting = true;
        _previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        if (rampFlicker)
        {
            float rampDuration = Mathf.Max(0.01f, holdDuration)
                / Mathf.Max(1f, speedMultiplier);
            yield return RampFlicker(rampDuration);
        }
        else
        {
            SetGameStartExitFill(1f);
        }

        _exitAnimationFinished = introBootstrap == null;
        if (introBootstrap != null)
            StartCoroutine(PlayExitAnimation(speedMultiplier));

        float leadTime = Mathf.Max(0f, exitAnimationLeadTime)
            / Mathf.Max(1f, speedMultiplier);
        float leadElapsed = 0f;
        while (leadElapsed < leadTime)
        {
            leadElapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        LensAudioService.Instance.PlayTVon(false, -1f);
        LensAudioService.Instance.PlayTVon(false, 1f);

        if (crtPowerOffController != null)
            yield return crtPowerOffController.PlayPowerOff(speedMultiplier);

        while (!_exitAnimationFinished)
            yield return null;

        QuitApplication();
    }

    private IEnumerator PlayExitAnimation(float speedMultiplier)
    {
        yield return introBootstrap.PlayExitAnimation(speedMultiplier);
        _exitAnimationFinished = true;
    }

    private IEnumerator RampFlicker(float duration)
    {
        float startStrength = _currentFlickerStrength;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            SetFlicker(true, Mathf.Lerp(startStrength, targetFlickerStrength, progress));
            yield return null;
        }

        SetFlicker(true, targetFlickerStrength);
    }

    private void ReleaseHold()
    {
        if (_heldTime <= 0f)
            return;

        float releaseSpeed = Mathf.Max(0.01f, holdDuration)
            / Mathf.Max(0.01f, holdReleaseDuration);
        _heldTime = Mathf.MoveTowards(_heldTime, 0f, releaseSpeed * Time.unscaledDeltaTime);

        float progress = Mathf.Clamp01(_heldTime / Mathf.Max(0.01f, holdDuration));
        SetGameStartExitFill(progress);
    }

    private void ResetEscapeSpam()
    {
        _escapePressCount = 0;
        _escapeSpamStartTime = float.NegativeInfinity;
    }

    private void CacheMaterials()
    {
        if (lensRenderers == null)
        {
            _lensMaterials = new Material[0];
            return;
        }

        _lensMaterials = new Material[lensRenderers.Length];
        for (int i = 0; i < lensRenderers.Length; i++)
            if (lensRenderers[i] != null)
                _lensMaterials[i] = lensRenderers[i].material;
    }

    private void SetFlicker(bool enabled, float strength)
    {
        _currentFlickerStrength = enabled ? strength : 0f;

        if (_lensMaterials == null)
            CacheMaterials();

        foreach (Material material in _lensMaterials)
        {
            if (material == null)
                continue;

            if (material.HasProperty(FlickerEnabledId))
                material.SetFloat(FlickerEnabledId, enabled ? 1f : 0f);

            if (material.HasProperty(FlickerStrengthId))
                material.SetFloat(FlickerStrengthId, strength);
        }
    }

    private void SetGameStartExitFill(float value)
    {
        if (gameStartExitFills == null)
            return;

        float fill = Mathf.Clamp01(value);
        foreach (Image image in gameStartExitFills)
            if (image != null)
                image.fillAmount = fill;
    }

    private void QuitApplication()
    {
#if UNITY_EDITOR
        Time.timeScale = _previousTimeScale;
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
