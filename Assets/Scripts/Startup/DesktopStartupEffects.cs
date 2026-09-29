using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[DefaultExecutionOrder(-1000)]
public sealed class DesktopStartupEffects : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private DesktopScreenshotBackground desktopBackground;
    [Min(0f)] [SerializeField] private float fadeDelay;
    [Min(0.01f)] [SerializeField] private float fadeDuration = 5f;

    private static DesktopStartupEffects _active;
    private readonly StartupVolumeBlend _volumes = new StartupVolumeBlend();
    private UniversalAdditionalCameraData _cameraData;
    private AntialiasingMode _originalAntialiasing;
    private bool _originalPostProcessing;
    private float _progress;
    private Tween _tween;

    public float Progress => _progress;

    private void Awake()
    {
        if (targetCamera == null || !targetCamera.TryGetComponent(out _cameraData))
        {
            Debug.LogError("DesktopStartupEffects requires its target camera and URP camera data.", this);
            enabled = false;
            return;
        }
        _originalAntialiasing = _cameraData.antialiasing;
        _originalPostProcessing = _cameraData.renderPostProcessing;
        _active = this;
        SetProgress(0f);
        RenderPipelineManager.endCameraRendering += OnCameraRendered;
    }

    private IEnumerator Start()
    {
        if (desktopBackground != null && desktopBackground.isActiveAndEnabled)
            yield return new WaitUntil(() => desktopBackground.IsReady);

        _tween = DOTween.To(() => _progress, SetProgress, 1f, fadeDuration)
            .SetDelay(fadeDelay)
            .SetEase(Ease.Linear)
            .SetUpdate(true)
            .OnComplete(() => _tween = null);
    }

    private void SetProgress(float progress)
    {
        _progress = Mathf.Clamp01(progress);
        _cameraData.renderPostProcessing = _originalPostProcessing && _progress > 0f;
        // FXAA has no strength parameter. Restore it once the gradual effects have appeared.
        _cameraData.antialiasing = _progress >= 1f ? _originalAntialiasing : AntialiasingMode.None;
    }

    public static float GetBlend(Camera camera)
    {
        return _active != null && _active.isActiveAndEnabled && _active.targetCamera == camera
            ? _active._progress : 1f;
    }

    public static void ApplyVolumes(Camera camera)
    {
        if (_active != null && _active.isActiveAndEnabled && _active.targetCamera == camera)
            _active._volumes.Apply(VolumeManager.instance.stack, _active._progress);
    }

    private void OnCameraRendered(ScriptableRenderContext context, Camera camera)
    {
        if (camera == targetCamera)
            _volumes.Restore();
    }

    private void OnDisable()
    {
        _tween?.Kill();
        _tween = null;
        RenderPipelineManager.endCameraRendering -= OnCameraRendered;
        _volumes.Dispose();
        if (_cameraData != null)
        {
            _cameraData.antialiasing = _originalAntialiasing;
            _cameraData.renderPostProcessing = _originalPostProcessing;
        }
        if (_active == this)
            _active = null;
    }
}
