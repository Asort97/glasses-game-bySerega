using UnityEngine;
using DG.Tweening;

public class BossApproachCamera : MonoBehaviour
{
    [SerializeField] private BossLevelDirector bossDirector;
    [SerializeField] private Transform mainCameraTransform;
    [SerializeField] private float approachSpeed = 1f;
    [Header("Z Positions")]
    [SerializeField] private float startZ = -11.2f;
    [SerializeField] private float bossZ = -10f;
    [Header("Game Over Reset")]
    [Min(0.01f)] [SerializeField] private float resetDuration = 1f;
    [SerializeField] private Ease resetEase = Ease.OutCubic;

    private Tween _moveTween;

    private void OnEnable()
    {
        bossDirector.BossProgressChanged += UpdateCameraPosition;
        UpdateCameraPosition(bossDirector.PassedMinigames, bossDirector.MinigamesPerBoss);
    }

    private void OnDisable()
    {
        bossDirector.BossProgressChanged -= UpdateCameraPosition;
        KillMoveTween();
    }

    public void ResetImmediately()
    {
        KillMoveTween();
        Vector3 position = mainCameraTransform.localPosition;
        position.z = startZ;
        mainCameraTransform.localPosition = position;
    }

    public void ResetSmoothly()
    {
        MoveToZ(startZ, resetDuration, resetEase);
    }

    private void UpdateCameraPosition(int passedMinigames, int minigamesPerBoss)
    {
        float progress = Mathf.Clamp01((float)passedMinigames / minigamesPerBoss);
        float targetZ = Mathf.Lerp(startZ, bossZ, progress);

        MoveToZ(targetZ, approachSpeed, Ease.Linear);
    }

    private void MoveToZ(float targetZ, float duration, Ease ease)
    {
        KillMoveTween();
        _moveTween = mainCameraTransform
            .DOLocalMoveZ(targetZ, duration)
            .SetEase(ease)
            .SetUpdate(true)
            .OnComplete(() => _moveTween = null);
    }

    private void KillMoveTween()
    {
        _moveTween?.Kill();
        _moveTween = null;
    }
}
