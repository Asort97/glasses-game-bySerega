using System;
using DG.Tweening;
using UnityEngine;

public sealed class BottleVictoryPresentation : MonoBehaviour
{
    [SerializeField] private Transform bottle;
    [SerializeField] private BottleFillView fillView;
    [SerializeField] private ParticleSystem explosion;
    [Min(0.01f)] [SerializeField] private float shakeDuration = 1f;
    [Min(0f)] [SerializeField] private float shakeStrength = 0.08f;
    [Min(1)] [SerializeField] private int shakeVibrato = 25;
    [Min(1)] [SerializeField] private int bubbleCount = 48;
    [Min(0f)] [SerializeField] private float resultDelay = 0.5f;

    private Sequence _sequence;

    public void Play(Vector3 center, float returnDuration, Action onComplete)
    {
        Cancel();
        _sequence = DOTween.Sequence()
            .Append(bottle.DOLocalMove(center, returnDuration).SetEase(Ease.OutCubic))
            .Append(bottle.DOShakePosition(shakeDuration,
                new Vector3(shakeStrength, shakeStrength, 0f), shakeVibrato, 90f, false, false))
            .AppendCallback(() =>
            {
                bottle.localPosition = center;
                fillView.ClearParticles();
                explosion.transform.position = bottle.position;
                bottle.gameObject.SetActive(false);
                explosion.Play(true);
                explosion.Emit(bubbleCount);
            })
            .AppendInterval(resultDelay)
            .OnComplete(() =>
            {
                _sequence = null;
                onComplete?.Invoke();
            });
    }

    public void Cancel()
    {
        _sequence?.Kill();
        _sequence = null;
    }

    public void ResetPresentation()
    {
        Cancel();
        if (explosion) explosion.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (bottle) bottle.gameObject.SetActive(true);
    }

    private void OnDisable()
    {
        Cancel();
        if (explosion) explosion.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }
}
