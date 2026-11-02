using System.Collections.Generic;
using TMPro;
using UnityEngine;

public sealed class PursuiersMinigame : MinigameBase
{
    [SerializeField] private ArenaKeyboardPlayer2D player;
    [SerializeField] private Collider2D playerCollider;
    [SerializeField] private PursuerBody2D enemyTemplate;
    [SerializeField] private TMP_Text countdown;
    [Tooltip("Left, right, bottom, top.")]
    [SerializeField] private BoxCollider2D[] walls;
    [SerializeField] private Vector2 arenaSize = new Vector2(9.2f, 6.2f);
    [Header("Pursuers")]
    [Min(1)] [SerializeField] private int enemyCount = 10;
    [Min(0.5f)] [SerializeField] private float spawnDistance = 1f;
    [Min(0f)] [SerializeField] private float acceleration = 6f;
    [Min(0.01f)] [SerializeField] private float maxSpeed = 5.2f;
    private readonly List<PursuerBody2D> _enemies = new List<PursuerBody2D>();
    private bool _playing;
    private float _elapsed;
    private int _shownSeconds = -1;

    public override void StartGame()
    {
        StopGame();
        if (!player || !playerCollider || !enemyTemplate || walls == null || walls.Length != 4)
        {
            Debug.LogError("Assign Pursuiers scene references.", this);
            return;
        }
        base.StartGame();
        for (int i = 0; i < 4; i++)
        {
            bool vertical = i < 2;
            float sign = i % 2 == 0 ? -1f : 1f;
            walls[i].transform.localPosition = vertical
                ? new Vector3(sign * (arenaSize.x * 0.5f + 0.2f), 0f, 0f)
                : new Vector3(0f, sign * (arenaSize.y * 0.5f + 0.2f), 0f);
            walls[i].size = vertical ? new Vector2(0.4f, arenaSize.y + 0.8f)
                : new Vector2(arenaSize.x + 0.8f, 0.4f);
        }
        enemyTemplate.gameObject.SetActive(false);
        player.Begin(arenaSize);
        while (_enemies.Count < enemyCount)
        {
            var enemy = Instantiate(enemyTemplate, transform);
            enemy.name = "Pursuer_" + (_enemies.Count + 1);
            enemy.PlayerHit += HandleHit;
            _enemies.Add(enemy);
        }
        Vector2 spawnHalfSize = arenaSize * 0.5f + Vector2.one * spawnDistance;
        float perimeter = 4f * (spawnHalfSize.x + spawnHalfSize.y);
        float spacing = perimeter / Mathf.Max(1, enemyCount);
        float startOffset = Random.Range(0f, perimeter);
        for (int i = 0; i < _enemies.Count; i++)
        {
            if (i >= enemyCount) { _enemies[i].gameObject.SetActive(false); continue; }
            float distance = startOffset + (i + Random.Range(-0.2f, 0.2f)) * spacing;
            Vector2 position = PerimeterPosition(distance, spawnHalfSize);
            _enemies[i].Begin(transform.TransformPoint(position), player.Body,
                playerCollider, acceleration, maxSpeed, transform, arenaSize, walls);
        }
        _elapsed = 0f;
        _shownSeconds = -1;
        _playing = true;
        UpdateCountdown();
    }

    private static Vector2 PerimeterPosition(float distance, Vector2 halfSize)
    {
        float width = halfSize.x * 2f;
        float height = halfSize.y * 2f;
        distance = Mathf.Repeat(distance, 2f * (width + height));
        if (distance < width) return new Vector2(-halfSize.x + distance, halfSize.y);
        distance -= width;
        if (distance < height) return new Vector2(halfSize.x, halfSize.y - distance);
        distance -= height;
        if (distance < width) return new Vector2(halfSize.x - distance, -halfSize.y);
        return new Vector2(-halfSize.x, -halfSize.y + distance - width);
    }

    public override void StopGame()
    {
        _playing = false;
        if (player) player.Stop();
        foreach (var enemy in _enemies) if (enemy) enemy.Stop();
        base.StopGame();
    }

    private void OnDisable() => StopGame();

    protected override void Update()
    {
        if (!_playing || Time.deltaTime <= 0f) return;
        Advance(Time.deltaTime);
    }

    private void Advance(float deltaTime)
    {
        if (!_playing) return;
        _elapsed += deltaTime;
        Progress = 1f - Mathf.Clamp01(_elapsed / Mathf.Max(0.01f, surviveTime));
        UpdateCountdown();
        if (Progress <= 0f) Finish(true);
    }

    private void UpdateCountdown()
    {
        int seconds = Mathf.Max(0, Mathf.CeilToInt(surviveTime - _elapsed));
        if (countdown && seconds != _shownSeconds) countdown.SetText("{0}", seconds);
        _shownSeconds = seconds;
    }

    private void HandleHit() => Finish(false);
    private void Finish(bool won)
    {
        if (!_playing) return;
        StopGame();
        if (won) RaiseWin(); else RaiseLose();
    }
}
