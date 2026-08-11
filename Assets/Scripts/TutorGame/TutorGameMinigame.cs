using UnityEngine;

public sealed class TutorGameMinigame : MinigameBase
{
    [Header("Click Mapping")]
    [SerializeField] private Camera screenCamera;
    [SerializeField] private Camera gameCamera;
    [SerializeField] private MeshCollider lensCollider;

    [Header("Sequence")]
    [SerializeField] private SpriteRenderer[] orderedTargets;
    [SerializeField] private Color clickedColor = new Color(0.4f, 0.4f, 0.4f, 1f);

    private Color[] _initialColors;
    private int _nextTargetIndex;
    private float _elapsed;
    private bool _isRunning;

    private void Awake()
    {
        CacheInitialColors();
        ResetSequence();
    }

    public override void StartGame()
    {
        base.StartGame();
        CacheInitialColors();
        ResetSequence();
        _elapsed = 0f;
        _isRunning = true;
    }

    public override void StopGame()
    {
        _isRunning = false;
        ResetSequence();
        base.StopGame();
    }

    protected override void Update()
    {
        if (!_isRunning)
            return;

        _elapsed += Time.deltaTime;
        Progress = 1f - Mathf.Clamp01(_elapsed / surviveTime);

        if (_elapsed >= surviveTime)
        {
            _isRunning = false;
            RaiseLose();
            return;
        }

        if (Input.GetMouseButtonDown(0) && TryGetClickedTarget(out int targetIndex))
            HandleTargetClick(targetIndex);
    }

    private void HandleTargetClick(int targetIndex)
    {
        if (targetIndex != _nextTargetIndex)
        {
            ResetSequence();
            return;
        }

        orderedTargets[targetIndex].color = clickedColor;
        _nextTargetIndex++;

        if (_nextTargetIndex < orderedTargets.Length)
            return;

        _isRunning = false;
        RaiseWin();
    }

    private bool TryGetClickedTarget(out int targetIndex)
    {
        targetIndex = -1;
        if (screenCamera == null || gameCamera == null || lensCollider == null)
            return false;

        Ray ray = screenCamera.ScreenPointToRay(Input.mousePosition);
        if (!lensCollider.Raycast(ray, out RaycastHit hit, 1000f))
            return false;

        Vector3 worldPoint = gameCamera.ViewportToWorldPoint(
            new Vector3(hit.textureCoord.x, hit.textureCoord.y, gameCamera.nearClipPlane));

        for (int i = 0; i < orderedTargets.Length; i++)
        {
            SpriteRenderer target = orderedTargets[i];
            if (target == null)
                continue;

            Vector3 boundsPoint = new Vector3(worldPoint.x, worldPoint.y, target.bounds.center.z);
            if (target.bounds.Contains(boundsPoint))
            {
                targetIndex = i;
                return true;
            }
        }

        return false;
    }

    private void CacheInitialColors()
    {
        if (orderedTargets == null)
        {
            _initialColors = null;
            return;
        }

        _initialColors = new Color[orderedTargets.Length];
        for (int i = 0; i < orderedTargets.Length; i++)
        {
            if (orderedTargets[i] != null)
                _initialColors[i] = orderedTargets[i].color;
        }
    }

    private void ResetSequence()
    {
        _nextTargetIndex = 0;
        if (orderedTargets == null || _initialColors == null)
            return;

        int count = Mathf.Min(orderedTargets.Length, _initialColors.Length);
        for (int i = 0; i < count; i++)
        {
            if (orderedTargets[i] != null)
                orderedTargets[i].color = _initialColors[i];
        }
    }
}
