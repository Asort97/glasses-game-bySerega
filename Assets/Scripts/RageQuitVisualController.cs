using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public sealed class RageQuitVisualController
{
    private readonly LensMinigameManager[] _lensManagers;
    private readonly DesktopScreenshotBackground _desktopScreenshot;

    public RageQuitVisualController(
        LensMinigameManager[] lensManagers,
        DesktopScreenshotBackground desktopScreenshot)
    {
        _lensManagers = lensManagers;
        _desktopScreenshot = desktopScreenshot;
    }

    public IEnumerator Play(float duration)
    {
        HideMiniTutorials();
        _desktopScreenshot?.CancelLightMultiplyTransition();
        _desktopScreenshot?.CancelDarknessTransition();

        Vignette vignette = VolumeManager.instance.stack.GetComponent<Vignette>();
        float startVignette = vignette != null ? vignette.intensity.value : 0f;
        float startLightMultiply = _desktopScreenshot != null
            ? _desktopScreenshot.GetLightMultiplyStrength()
            : 0f;
        float startDarkness = _desktopScreenshot != null
            ? _desktopScreenshot.GetDarkness()
            : 0f;
        float startSaturation = _desktopScreenshot != null
            ? _desktopScreenshot.GetSaturation()
            : 1f;
        float elapsed = 0f;
        float safeDuration = Mathf.Max(0.01f, duration);

        while (elapsed < safeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / safeDuration);
            SetVignette(vignette, Mathf.Lerp(startVignette, 0f, progress));
            _desktopScreenshot?.SetLightMultiplyStrength(
                Mathf.Lerp(startLightMultiply, 0f, progress));
            _desktopScreenshot?.SetDarkness(Mathf.Lerp(startDarkness, 0f, progress));
            _desktopScreenshot?.SetSaturation(Mathf.Lerp(startSaturation, 1f, progress));
            yield return null;
        }

        SetVignette(vignette, 0f);
        _desktopScreenshot?.SetLightMultiplyStrength(0f);
        _desktopScreenshot?.SetDarkness(0f);
        _desktopScreenshot?.SetSaturation(1f);
    }

    private void HideMiniTutorials()
    {
        if (_lensManagers == null)
            return;

        foreach (LensMinigameManager manager in _lensManagers)
            manager?.HideTutorial();
    }

    private static void SetVignette(Vignette vignette, float intensity)
    {
        if (vignette != null)
            vignette.intensity.value = Mathf.Clamp01(intensity);
    }
}
