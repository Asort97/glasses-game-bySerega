using DG.Tweening;
using UnityEngine;

public sealed class RecordCrtScaleAnimator : MonoBehaviour
{
    [SerializeField] private RectTransform target;
    [SerializeField] private Ease powerOffEase = Ease.InCubic;
    [SerializeField] private Ease powerOnEase = Ease.OutCubic;

    private Tween _scaleTween;

    public void PlayPowerOff(float duration)
    {
        Animate(1f, 0f, duration-0.1f, powerOffEase);
    }

    public void PlayPowerOn(float duration)
    {
        Animate(0f, 1f, duration-0.1f, powerOnEase);
    }

    public void SetCollapsed()
    {
        SetScaleY(0f);
    }

    public void SetExpanded()
    {
        SetScaleY(1f);
    }

    private void Animate(float startScaleY, float targetScaleY, float duration, Ease ease)
    {
        KillTween();
        SetScaleY(startScaleY);

        _scaleTween = target
            .DOScaleY(targetScaleY, Mathf.Max(0.01f, duration))
            .SetEase(ease)
            .SetUpdate(true)
            .OnComplete(() => _scaleTween = null);
    }

    private void SetScaleY(float scaleY)
    {
        KillTween();

        Vector3 scale = target.localScale;
        scale.y = scaleY;
        target.localScale = scale;
    }

    private void KillTween()
    {
        _scaleTween?.Kill();
        _scaleTween = null;
    }

    private void OnDisable()
    {
        KillTween();
    }
}
