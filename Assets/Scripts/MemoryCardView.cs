using DG.Tweening;
using UnityEngine;

public sealed class MemoryCardView : MonoBehaviour
{
    [SerializeField] private SpriteRenderer cardRenderer;
    [SerializeField] private float faceScaleX = 2f;

    public int PairId { get; private set; }
    public bool IsMatched { get; private set; }
    public float WorldZ => transform.position.z;
    private Sprite _face, _back;

    public void ResetCard(int pairId, Sprite face, Sprite back)
    {
        PairId = pairId;
        IsMatched = false;
        _face = face;
        _back = back;
        cardRenderer.sprite = back;
        Vector3 scale = transform.localScale;
        scale.x = -Mathf.Abs(faceScaleX);
        transform.localScale = scale;
    }

    public Tween Flip(bool open, float duration)
    {
        return DOTween.Sequence()
            .Append(transform.DOScaleX(0f, duration * 0.5f).SetEase(Ease.InQuad))
            .AppendCallback(() => cardRenderer.sprite = open ? _face : _back)
            .Append(transform.DOScaleX((open ? 1f : -1f) * Mathf.Abs(faceScaleX),
                duration * 0.5f).SetEase(Ease.OutQuad));
    }

    public Tween Collapse(float duration) =>
        transform.DOScaleX(0f, duration).SetEase(Ease.InQuad);

    public bool Contains(Vector3 point)
    {
        Bounds bounds = cardRenderer.bounds;
        point.z = bounds.center.z;
        return bounds.Contains(point);
    }

    public void MarkMatched() => IsMatched = true;
}
