using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

public sealed class LensSkipEffect : MonoBehaviour
{
    [SerializeField] private Renderer lensRenderer;
    [Min(0.01f)] [SerializeField] private float fillDuration = 0.7f;
    [Min(0.01f)] [SerializeField] private float drainDuration = 2f;
    [Range(0f, 1f)] [SerializeField] private float remainingAtNextGame = 0.15f;

    private static readonly int OpacityId = Shader.PropertyToID("_SkipWhiteOpacity");
    private static readonly int DrainId = Shader.PropertyToID("_SkipWhiteDrain");
    private Material _material;
    private Tween _tween;

    // Keep the logical lens material, including when Ambidextrous swaps renderers.
    private void Awake()
    {
        if (lensRenderer) _material = lensRenderer.material;
        ResetEffect();
    }

    public bool IsConfigured => _material && _material.HasProperty(OpacityId);
    public Material LensMaterial => _material;

    public IEnumerator Play(Action clearPrevious, Action startNext)
    {
        ResetEffect();
        if (!IsConfigured) yield break;
        float opacity = 0f;
        _tween = DOTween.To(() => opacity, value =>
        {
            opacity = value;
            _material.SetFloat(OpacityId, value);
        }, 1f, fillDuration).SetEase(Ease.OutQuad).SetUpdate(true);
        yield return _tween.WaitForCompletion();
        clearPrevious?.Invoke();

        float drain = 0f;
        bool startedNext = false;
        _tween = DOTween.To(() => drain, value =>
        {
            drain = value;
            _material.SetFloat(DrainId, value);
            if (!startedNext && value >= 1f - remainingAtNextGame)
            {
                startedNext = true;
                startNext?.Invoke();
            }
        }, 1f, drainDuration).SetEase(Ease.Linear).SetUpdate(true);
        yield return _tween.WaitForCompletion();
        if (!startedNext) startNext?.Invoke();
        ResetEffect();
    }

    public void ResetEffect()
    {
        _tween?.Kill();
        _tween = null;
        if (!_material) return;
        _material.SetFloat(OpacityId, 0f);
        _material.SetFloat(DrainId, 0f);
    }

    private void OnDisable() => ResetEffect();
}
