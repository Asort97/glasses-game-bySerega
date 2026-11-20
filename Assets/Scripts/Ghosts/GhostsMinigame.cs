using UnityEngine;

/// <summary>
/// Мини-игра с призраками (правая линза).
/// Проигрыш — столкновение с BouncingGhost.
/// Победа — пережить surviveTime секунд.
/// </summary>
public class GhostsMinigame : MinigameBase
{
    [SerializeField] private CanvasCursor canvasCursor;
    [Tooltip("Ghosts stand still this long after the game starts.")]
    [Min(0f)] [SerializeField] private float ghostHoldDuration = 0.4f;
    [Tooltip("Then they accelerate from zero to full speed over this time.")]
    [Min(0f)] [SerializeField] private float ghostAccelerationDuration = 1f;

    private Ps1GridMovement2D[] _ghosts;
    private float[] _ghostSpeeds;
    private float _ghostElapsed;

    private ContactReporter _playerContact;
    private bool _ready; // защита от мгновенной коллизии при включении

    private void Awake()
    {
        var playerT = transform.Find("player");
        _ghosts = System.Array.ConvertAll(GetComponentsInChildren<BouncingGhost>(true),
            ghost => ghost.GetComponent<Ps1GridMovement2D>());
        _ghostSpeeds = new float[_ghosts.Length];
        for (int i = 0; i < _ghosts.Length; i++) _ghostSpeeds[i] = _ghosts[i].Speed;
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
        canvasCursor.SetShowOutsideRightLensWhenHidden(true);
        canvasCursor.SetCursorSpriteVisible(false);
        _ready = false;
        _ghostElapsed = 0f;
        ApplyGhostSpeed();
        if (_playerContact == null) return;
        _playerContact.OnTriggerEntered   += HandleTrigger;
        _playerContact.OnCollisionEntered += HandleCollision;
        // 0.4 сек задержка — призраки могут быть на позиции игрока при включении
        Invoke(nameof(SetReady), 0.4f);
    }

    public override void StopGame()
    {
        canvasCursor.SetShowOutsideRightLensWhenHidden(false);
        canvasCursor.SetCursorSpriteVisible(true);
        CancelInvoke(nameof(SetReady));
        _ready = false;
        _ghostElapsed = float.MaxValue;
        ApplyGhostSpeed();
        base.StopGame();
        if (_playerContact == null) return;
        _playerContact.OnTriggerEntered   -= HandleTrigger;
        _playerContact.OnCollisionEntered -= HandleCollision;
    }

    protected override void Update()
    {
        base.Update();
        if (_ghostElapsed >= ghostHoldDuration + ghostAccelerationDuration) return;
        _ghostElapsed += Time.deltaTime;
        ApplyGhostSpeed();
    }

    private void ApplyGhostSpeed()
    {
        if (_ghosts == null) return;
        float t = ghostAccelerationDuration > 0f
            ? Mathf.Clamp01((_ghostElapsed - ghostHoldDuration) / ghostAccelerationDuration)
            : (_ghostElapsed >= ghostHoldDuration ? 1f : 0f);
        for (int i = 0; i < _ghosts.Length; i++)
            if (_ghosts[i]) _ghosts[i].SetSpeed(_ghostSpeeds[i] * t);
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
