using System.Collections.Generic;
using UnityEngine;

public sealed class TurretsMinigame : MinigameBase
{
    [Header("Scene References")]
    [SerializeField] private Rigidbody2D player;
    [SerializeField] private Collider2D playerCollider;
    [SerializeField] private TurretAim turretTemplate;
    [SerializeField] private TurretBullet bulletTemplate;
    [Tooltip("Left, right, bottom, top arena colliders.")]
    [SerializeField] private BoxCollider2D[] walls;
    [Header("Arena")]
    [SerializeField] private Vector2 arenaSize = new Vector2(9.2f, 6.2f);
    [SerializeField] private Vector2 playerHalfSize = new Vector2(0.3f, 0.4f);
    [UnityEngine.Serialization.FormerlySerializedAs("cornerMax")]
    [SerializeField] private Vector2 cornerOffset = new Vector2(3.5f, 2f);
    [Header("Speed")]
    [Min(0f)] [SerializeField] private float playerSpeed = 3f;
    [Min(0.01f)] [SerializeField] private float bulletSpeed = 1.2f;
    [Min(0.01f)] [SerializeField] private float shotInterval = 0.2f;
    [Tooltip("Pause after the last shot of a wave and before the first wave.")]
    [Min(0.01f)] [SerializeField] private float waveDelay = 0.5f;

    private readonly List<TurretAim> _turrets = new List<TurretAim>();
    private readonly List<TurretBullet> _bullets = new List<TurretBullet>();
    private readonly int[] _order = { 0, 1, 2, 3 };
    private int _orderIndex;
    private int _activeBullets;
    private float _elapsed;
    private float _shotTimeRemaining;
    private bool _playing;
    private Vector2 _input;

    public override void StartGame()
    {
        StopGame();
        if (!player || !playerCollider || !turretTemplate || !turretTemplate.IsConfigured
            || !bulletTemplate || !bulletTemplate.IsConfigured || walls == null || walls.Length != 4)
        {
            Debug.LogError("Assign TurretsMinigame references.", this);
            return;
        }
        base.StartGame();
        for (int i = 0; i < walls.Length; i++)
        {
            bool vertical = i < 2;
            float side = i % 2 == 0 ? -1f : 1f;
            walls[i].transform.localPosition = vertical
                ? new Vector3(side * (arenaSize.x * 0.5f + 0.2f), 0f, 0f)
                : new Vector3(0f, side * (arenaSize.y * 0.5f + 0.2f), 0f);
            walls[i].size = vertical ? new Vector2(0.4f, arenaSize.y + 0.8f)
                : new Vector2(arenaSize.x + 0.8f, 0.4f);
        }
        player.transform.localPosition = Vector3.zero;
        player.position = player.transform.position;
        player.simulated = true;
        player.gameObject.SetActive(true);
        turretTemplate.gameObject.SetActive(false);
        bulletTemplate.gameObject.SetActive(false);
        foreach (var bullet in _bullets) bullet.gameObject.SetActive(false);
        while (_turrets.Count < 4)
        {
            var turret = Instantiate(turretTemplate, transform);
            turret.name = "Turret_" + (_turrets.Count + 1);
            _turrets.Add(turret);
        }
        for (int i = 0; i < 4; i++)
        {
            float x = Mathf.Abs(cornerOffset.x) * (i % 2 == 0 ? -1f : 1f);
            float y = Mathf.Abs(cornerOffset.y) * (i < 2 ? -1f : 1f);
            _turrets[i].transform.localPosition = new Vector3(x, y, 0f);
            _turrets[i].gameObject.SetActive(true);
            _turrets[i].AimAt(player.transform.position);
        }
        _activeBullets = 0;
        _orderIndex = 4;
        _elapsed = 0f;
        _shotTimeRemaining = Mathf.Max(0.01f, waveDelay);
        _input = Vector2.zero;
        _playing = true;
    }

    public override void StopGame()
    {
        _playing = false;
        _input = Vector2.zero;
        if (player) { player.linearVelocity = Vector2.zero; player.simulated = false; }
        foreach (var bullet in _bullets) if (bullet) bullet.Stop();
        base.StopGame();
    }

    private void OnDisable() => StopGame();

    protected override void Update()
    {
        if (!_playing || Time.deltaTime <= 0f) return;
        _input = new Vector2(
            (Held(KeyCode.D, KeyCode.RightArrow) ? 1f : 0f) - (Held(KeyCode.A, KeyCode.LeftArrow) ? 1f : 0f),
            (Held(KeyCode.W, KeyCode.UpArrow) ? 1f : 0f) - (Held(KeyCode.S, KeyCode.DownArrow) ? 1f : 0f)).normalized;
        Advance(Time.deltaTime);
    }

    private void FixedUpdate()
    {
        if (!_playing) return;
        Vector3 position = transform.InverseTransformPoint(player.position);
        position += (Vector3)(_input * (playerSpeed * Time.fixedDeltaTime));
        Vector2 limit = Vector2.Max(Vector2.zero, arenaSize * 0.5f - playerHalfSize);
        position.x = Mathf.Clamp(position.x, -limit.x, limit.x);
        position.y = Mathf.Clamp(position.y, -limit.y, limit.y);
        player.MovePosition(transform.TransformPoint(position));
    }

    private void Advance(float deltaTime)
    {
        if (!_playing) return;
        _elapsed += deltaTime;
        Progress = 1f - Mathf.Clamp01(_elapsed / Mathf.Max(0.01f, surviveTime));
        if (Progress <= 0f) { Finish(true); return; }
        foreach (var turret in _turrets) turret.AimAt(player.transform.position);
        _shotTimeRemaining -= deltaTime;
        while (_shotTimeRemaining <= 0f)
        {
            Fire();
            _shotTimeRemaining += Mathf.Max(0.01f, _orderIndex >= 4 ? waveDelay : shotInterval);
        }
    }

    private void Fire()
    {
        if (_orderIndex >= 4)
        {
            for (int i = 0; i < 4; i++)
            {
                int pick = Random.Range(i, 4);
                (_order[i], _order[pick]) = (_order[pick], _order[i]);
            }
            _orderIndex = 0;
        }
        TurretAim turret = _turrets[_order[_orderIndex++]];
        if (_activeBullets == _bullets.Count)
        {
            var bullet = Instantiate(bulletTemplate, transform);
            bullet.name = "Bullet_" + _bullets.Count;
            bullet.PlayerHit += OnPlayerHit;
            _bullets.Add(bullet);
        }
        TurretBullet shot = _bullets[_activeBullets++];
        shot.Launch(turret.MuzzlePosition, player.transform.position - turret.MuzzlePosition, bulletSpeed, playerCollider);
        for (int i = 0; i < _activeBullets - 1; i++)
            Physics2D.IgnoreCollision(shot.Hitbox, _bullets[i].Hitbox);
    }

    private static bool Held(KeyCode a, KeyCode b) => Input.GetKey(a) || Input.GetKey(b);
    private void OnPlayerHit() => Finish(false);

    private void Finish(bool won)
    {
        if (!_playing) return;
        StopGame();
        if (won)
        {
            foreach (var bullet in _bullets)
                if (bullet) bullet.gameObject.SetActive(false);
            _activeBullets = 0;
            RaiseWin();
        }
        else RaiseLose();
    }
}
