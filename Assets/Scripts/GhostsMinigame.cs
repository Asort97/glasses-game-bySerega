using UnityEngine;

/// <summary>
/// Мини-игра с призраками (правая линза).
/// Проигрыш — столкновение с BouncingGhost.
/// Победа — пережить surviveTime секунд.
/// </summary>
public class GhostsMinigame : MinigameBase
{
    private ContactReporter _playerContact;
    private bool _ready; // защита от мгновенной коллизии при включении

    private void Awake()
    {
        var playerT = transform.Find("player");
        if (playerT == null)
        {
            Debug.LogError("[GhostsMinigame] Дочерний объект 'player' не найден!");
            return;
        }

        _playerContact = playerT.GetComponent<ContactReporter>();
        if (_playerContact == null)
            _playerContact = playerT.gameObject.AddComponent<ContactReporter>();
    }

    public override void StartGame()
    {
        base.StartGame();
        _ready = false;
        if (_playerContact == null) return;
        _playerContact.OnTriggerEntered   += HandleTrigger;
        _playerContact.OnCollisionEntered += HandleCollision;
        // 0.4 сек задержка — призраки могут быть на позиции игрока при включении
        Invoke(nameof(SetReady), 0.4f);
    }

    public override void StopGame()
    {
        CancelInvoke(nameof(SetReady));
        _ready = false;
        base.StopGame();
        if (_playerContact == null) return;
        _playerContact.OnTriggerEntered   -= HandleTrigger;
        _playerContact.OnCollisionEntered -= HandleCollision;
    }

    private void SetReady() => _ready = true;

    private void HandleTrigger(Collider2D other)
    {
        if (!_ready) return;
        if (other.GetComponent<BouncingGhost>() != null)
            RaiseLose();
    }

    private void HandleCollision(Collision2D col)
    {
        if (!_ready) return;
        if (col.gameObject.GetComponent<BouncingGhost>() != null)
            RaiseLose();
    }
}
