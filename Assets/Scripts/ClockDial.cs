using UnityEngine;

public sealed class ClockDial : MonoBehaviour
{
    [SerializeField] private SpriteRenderer face;
    [SerializeField] private Transform handPivot;

    public float RevolutionProgress { get; private set; }
    public float RevolutionDuration { get; private set; }
    public Bounds FaceBounds => face.bounds;
    public bool IsConfigured => face && handPivot;

    private Quaternion _initialRotation;
    private bool _rotationCached;

    public void ResetClock(float duration)
    {
        if (!_rotationCached)
        {
            _initialRotation = handPivot.localRotation;
            _rotationCached = true;
        }
        RevolutionDuration = Mathf.Max(0.01f, duration);
        RevolutionProgress = 0f;
        UpdateHand();
    }

    public bool Advance(float deltaTime)
    {
        RevolutionProgress = Mathf.Clamp01(RevolutionProgress + Mathf.Max(0f, deltaTime) / RevolutionDuration);
        UpdateHand();
        return RevolutionProgress >= 1f;
    }

    public void Rewind(float amount)
    {
        RevolutionProgress = Mathf.Max(0f, RevolutionProgress - Mathf.Max(0f, amount));
        UpdateHand();
    }

    public bool Contains(Vector3 worldPoint)
    {
        Vector3 local = face.transform.InverseTransformPoint(worldPoint);
        Bounds bounds = face.sprite.bounds;
        float x = (local.x - bounds.center.x) / bounds.extents.x;
        float y = (local.y - bounds.center.y) / bounds.extents.y;
        return x * x + y * y <= 1f;
    }

    private void UpdateHand()
    {
        handPivot.localRotation = _initialRotation * Quaternion.Euler(0f, 0f, -360f * RevolutionProgress);
    }
}
