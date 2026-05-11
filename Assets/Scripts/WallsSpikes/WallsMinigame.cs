using UnityEngine;

/// <summary>
/// Мини-игра с прыжками по стенам (левая линза).
/// Проигрыш — удар шипом (SpikeMarker).
/// Победа — пережить surviveTime секунд.
/// Включает/выключает SpikeSpawner при старте/стопе.
/// </summary>
public class WallsMinigame : MinigameBase
{
    private SpikeSpawner    _spawner;
    private ContactReporter _playerContact;
    private bool            _ready;
    private Transform       _playerTransform;
    private Vector3         _playerStartPos;
    private SpriteRenderer  _playerSr;

    private void Awake()
    {
        _spawner = GetComponent<SpikeSpawner>();

        var playerT = transform.Find("player");
        if (playerT == null)
        {
            Debug.LogError("[WallsMinigame] Дочерний объект 'player' не найден!");
            return;
        }

        _playerTransform = playerT;
        _playerStartPos  = playerT.position;
        _playerSr        = playerT.GetComponent<SpriteRenderer>();

        _playerContact = playerT.GetComponent<ContactReporter>();
        if (_playerContact == null)
            _playerContact = playerT.gameObject.AddComponent<ContactReporter>();
    }

    public override void StartGame()
    {
        base.StartGame();
        _ready = false;

        // Сбросить позицию игрока на стартовую
        if (_playerTransform != null)
            _playerTransform.position = _playerStartPos;
        if (_playerSr != null)
            _playerSr.flipX = false;

        if (_spawner != null) _spawner.enabled = true;
        if (_playerContact != null)
            _playerContact.OnTriggerEntered += HandleTrigger;
        Invoke(nameof(SetReady), 0.5f);
    }

    public override void StopGame()
    {
        CancelInvoke(nameof(SetReady));
        _ready = false;
        base.StopGame();
        if (_spawner != null) _spawner.enabled = false;
        if (_playerContact != null)
            _playerContact.OnTriggerEntered -= HandleTrigger;
    }

    private void SetReady() => _ready = true;

    private void HandleTrigger(Collider2D other)
    {
        if (!_ready) return;
        if (other.GetComponent<SpikeMarker>() != null)
            RaiseLose();
    }
}
