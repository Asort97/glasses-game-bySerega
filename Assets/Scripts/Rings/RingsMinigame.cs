using UnityEngine;

/// <summary>
/// Мини-игра с кольцами (левая линза).
/// Победа — собрать все кольца на уровне.
/// Поражение — не успел до конца таймера.
/// </summary>
public class RingsMinigame : MinigameBase
{
    [System.Serializable]
    public class Layout
    {
        [Tooltip("Local positions for the platforms, in the same order as Layout Platforms.")]
        public Vector2[] platforms;
        [Tooltip("Local positions for the rings, in the same order as Layout Rings.")]
        public Vector2[] rings;
    }

    [Header("Random Layouts (optional)")]
    [SerializeField] private Transform[] layoutPlatforms;
    [SerializeField] private Transform[] layoutRings;
    [SerializeField] private Layout[] layouts;

    private Ring[]    _rings;
    private int       _remaining;
    private bool      _ringsRunning;
    private bool      _timeLimitEnabled = true;
    private bool      _winEnabled = true;
    private float     _elapsed;

    // Игрок — объект с компонентом Player среди дочерних
    private Transform _playerTransform;
    private Vector3   _playerStartPos;

    private void Awake()
    {
        var playerComp = GetComponentInChildren<Player>(true);
        if (playerComp != null)
        {
            _playerTransform = playerComp.transform;
            _playerStartPos  = _playerTransform.position;
        }
    }

    public override void StartGame()
    {
        _elapsed   = 0f;
        _ringsRunning = true;
        Progress   = 1f;

        // Сбросить позицию игрока на стартовую
        if (_playerTransform != null)
        {
            var rb = _playerTransform.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;
            _playerTransform.position = _playerStartPos;
        }

        ApplyRandomLayout();

        // Включить все кольца (могли быть выключены после прошлого раунда)
        var allRings = GetComponentsInChildren<Ring>(true);
        foreach (var ring in allRings)
            ring.gameObject.SetActive(true);

        _rings = GetComponentsInChildren<Ring>(false);
        _remaining = _rings.Length;

        foreach (var ring in _rings)
            ring.OnCollected += OnRingCollected;

        if (_remaining == 0 && _winEnabled)
        {
            _ringsRunning = false;
            RaiseWin();
        }
    }

    public override void StopGame()
    {
        _ringsRunning = false;
        if (_rings != null)
        {
            foreach (var ring in _rings)
                if (ring != null) ring.OnCollected -= OnRingCollected;
        }
    }

    private void ApplyRandomLayout()
    {
        if (layouts == null || layouts.Length == 0) return;
        Layout layout = layouts[Random.Range(0, layouts.Length)];
        Place(layoutPlatforms, layout.platforms);
        Place(layoutRings, layout.rings);
    }

    private static void Place(Transform[] targets, Vector2[] positions)
    {
        if (targets == null || positions == null) return;
        for (int i = 0; i < targets.Length && i < positions.Length; i++)
            if (targets[i])
                targets[i].localPosition = new Vector3(positions[i].x, positions[i].y, targets[i].localPosition.z);
    }

    public void SetTimeLimitEnabled(bool enabled)
    {
        _timeLimitEnabled = enabled;
        Progress = 1f;
    }

    public void SetWinEnabled(bool enabled)
    {
        _winEnabled = enabled;
    }

    protected override void Update()
    {
        if (!_ringsRunning) return;
        if (!_timeLimitEnabled) return;

        _elapsed += Time.deltaTime;
        Progress  = 1f - Mathf.Clamp01(_elapsed / surviveTime);

        if (_elapsed >= surviveTime)
        {
            _ringsRunning = false;
            RaiseLose();
        }
    }

    private void OnRingCollected()
    {
        _remaining--;
        if (_remaining <= 0 && _winEnabled)
        {
            _ringsRunning = false;
            RaiseWin();
        }
    }
}
