using System.Collections;
using DG.Tweening;
using UnityEngine;

public sealed class NewHorizonsDebuffController : MonoBehaviour
{
    [SerializeField] private DebuffSpawner debuffSpawner;
    [SerializeField] private Transform mainCameraTransform;
    [SerializeField] private SplitChromaticAberrationFeature chromaticAberration;

    [Header("Timing")]
    [Min(0f)] [SerializeField] private float duration = 10f;
    [Min(0.01f)] [SerializeField] private float transitionDuration = 0.5f;
    [SerializeField] private Ease transitionEase = Ease.InOutSine;

    private Quaternion _normalRotation;
    private Coroutine _durationRoutine;
    private Tween _transition;
    private float _progress;
    private float _cameraRotation;
    private float _transitionProgress;

    private void Awake()
    {
        _normalRotation = mainCameraTransform.localRotation;
        ApplyState(0f, 0f);
    }

    private void OnEnable()
    {
        debuffSpawner.DebuffApplied += HandleDebuffApplied;
    }

    private void OnDisable()
    {
        debuffSpawner.DebuffApplied -= HandleDebuffApplied;
        StopEffect();
        ApplyState(0f, 0f);
    }

    private void HandleDebuffApplied(DebuffId id)
    {
        if (id == DebuffId.NewHorizons)
            Play();
    }

    [ContextMenu("Play New Horizons")]
    public void Play()
    {
        StopEffect();
        TweenTo(1f, StartDuration);
    }

    private void StartDuration()
    {
        _durationRoutine = StartCoroutine(DurationRoutine());
    }

    private IEnumerator DurationRoutine()
    {
        yield return new WaitForSecondsRealtime(duration);
        _durationRoutine = null;
        TweenTo(0f, null);
    }

    private void TweenTo(float target, TweenCallback completed)
    {
        float startProgress = _progress;
        float startRotation = _cameraRotation;
        float targetRotation = GetNextClockwiseRotation(startRotation, target);
        _transitionProgress = 0f;

        _transition = DOTween.To(
                () => _transitionProgress,
                value =>
                {
                    _transitionProgress = value;
                    ApplyState(
                        Mathf.Lerp(startProgress, target, value),
                        Mathf.Lerp(startRotation, targetRotation, value));
                },
                1f,
                transitionDuration)
            .SetEase(transitionEase)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                _transition = null;

                if (target <= 0f)
                    ApplyState(0f, 0f);

                completed?.Invoke();
            });
    }

    private float GetNextClockwiseRotation(float currentRotation, float targetProgress)
    {
        float targetAngle = targetProgress >= 1f ? 180f : 0f;
        float currentAngle = Mathf.Repeat(currentRotation, 360f);
        float clockwiseDelta = Mathf.Repeat(targetAngle - currentAngle, 360f);
        return currentRotation + clockwiseDelta;
    }

    private void ApplyState(float progress, float cameraRotation)
    {
        _progress = Mathf.Clamp01(progress);
        _cameraRotation = cameraRotation;

        mainCameraTransform.localRotation = _normalRotation
            * Quaternion.Euler(0f, 0f, _cameraRotation);
        GameInput.SetMouseRotation(_cameraRotation);
        chromaticAberration.SetColorSwap(_progress);
    }

    private void StopEffect()
    {
        if (_durationRoutine != null)
        {
            StopCoroutine(_durationRoutine);
            _durationRoutine = null;
        }

        _transition?.Kill();
        _transition = null;
    }
}
