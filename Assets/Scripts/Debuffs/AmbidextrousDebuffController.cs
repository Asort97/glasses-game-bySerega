using System.Collections;
using UnityEngine;

public sealed class AmbidextrousDebuffController : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private DebuffSpawner debuffSpawner;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Renderer leftLensRenderer;
    [SerializeField] private Renderer rightLensRenderer;
    [SerializeField] private Transform leftMiniTutorial;
    [SerializeField] private Transform rightMiniTutorial;

    [Header("Timing")]
    [Min(0.1f)] [SerializeField] private float duration = 30f;

    private Material _leftMaterial;
    private Material _rightMaterial;
    private Vector3 _leftTutorialPosition;
    private Vector3 _rightTutorialPosition;
    private Coroutine _routine;
    private bool _swapped;

    private void Awake()
    {
        _leftTutorialPosition = leftMiniTutorial.localPosition;
        _rightTutorialPosition = rightMiniTutorial.localPosition;
    }

    private void OnEnable()
    {
        debuffSpawner.DebuffApplied += HandleDebuffApplied;
        debuffSpawner.RunReset += StopAndRestore;
    }

    private void OnDisable()
    {
        debuffSpawner.DebuffApplied -= HandleDebuffApplied;
        debuffSpawner.RunReset -= StopAndRestore;
        StopAndRestore();
    }

    private void HandleDebuffApplied(DebuffId id)
    {
        if (id == DebuffId.AmbidextrousUpdate)
            Play();
    }

    [ContextMenu("Play Ambidextrous Update")]
    public void Play()
    {
        if (_routine != null)
            StopCoroutine(_routine);

        if (!_swapped)
            SwapVisuals();

        _routine = StartCoroutine(DurationRoutine());
    }

    private IEnumerator DurationRoutine()
    {
        yield return new WaitForSecondsRealtime(duration);
        _routine = null;
        RestoreVisuals();
    }

    private void SwapVisuals()
    {
        _leftMaterial = leftLensRenderer.sharedMaterial;
        _rightMaterial = rightLensRenderer.sharedMaterial;

        leftLensRenderer.sharedMaterial = _rightMaterial;
        rightLensRenderer.sharedMaterial = _leftMaterial;

        Vector2 leftLensCenter = mainCamera.WorldToScreenPoint(leftLensRenderer.bounds.center);
        Vector2 rightLensCenter = mainCamera.WorldToScreenPoint(rightLensRenderer.bounds.center);
        GameInput.SetLensSidesSwapped(leftLensCenter, rightLensCenter);

        leftMiniTutorial.localPosition = _rightTutorialPosition;
        rightMiniTutorial.localPosition = _leftTutorialPosition;
        _swapped = true;
    }

    private void StopAndRestore()
    {
        if (_routine != null)
        {
            StopCoroutine(_routine);
            _routine = null;
        }

        RestoreVisuals();
    }

    private void RestoreVisuals()
    {
        if (!_swapped)
            return;

        leftLensRenderer.sharedMaterial = _leftMaterial;
        rightLensRenderer.sharedMaterial = _rightMaterial;
        GameInput.ResetLensSidesSwapped();
        leftMiniTutorial.localPosition = _leftTutorialPosition;
        rightMiniTutorial.localPosition = _rightTutorialPosition;
        _swapped = false;
    }
}
