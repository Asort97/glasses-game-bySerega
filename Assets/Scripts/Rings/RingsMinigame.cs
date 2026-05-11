using UnityEngine;

/// <summary>
/// Мини-игра с кольцами (левая линза).
/// Победа — собрать все кольца на уровне.
/// Поражение — не успел до конца таймера.
/// </summary>
public class RingsMinigame : MinigameBase
{
    private Ring[]    _rings;
    private int       _remaining;
    private bool      _running;
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
        _running   = true;
        Progress   = 1f;

        // Сбросить позицию игрока на стартовую
        if (_playerTransform != null)
        {
            var rb = _playerTransform.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;
            _playerTransform.position = _playerStartPos;
        }

        // Включить все кольца (могли быть выключены после прошлого раунда)
        var allRings = GetComponentsInChildren<Ring>(true);
        foreach (var ring in allRings)
            ring.gameObject.SetActive(true);

        _rings = GetComponentsInChildren<Ring>(false);
        _remaining = _rings.Length;

        foreach (var ring in _rings)
            ring.OnCollected += OnRingCollected;

        if (_remaining == 0)
        {
            _running = false;
            RaiseWin();
        }
    }

    public override void StopGame()
    {
        _running = false;
        if (_rings != null)
        {
            foreach (var ring in _rings)
                if (ring != null) ring.OnCollected -= OnRingCollected;
        }
    }

    protected override void Update()
    {
        if (!_running) return;

        _elapsed += Time.deltaTime;
        Progress  = 1f - Mathf.Clamp01(_elapsed / surviveTime);

        if (_elapsed >= surviveTime)
        {
            _running = false;
            RaiseLose();
        }
    }

    private void OnRingCollected()
    {
        _remaining--;
        if (_remaining <= 0)
        {
            _running = false;
            RaiseWin();
        }
    }
}
