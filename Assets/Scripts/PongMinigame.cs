using UnityEngine;

public sealed class PongMinigame : MinigameBase
{
    [Header("Scene References")]
    [SerializeField] private Rigidbody2D playerPaddle;
    [SerializeField] private Rigidbody2D botPaddle;
    [SerializeField] private PongBall ball;
    [SerializeField] private LensPointerProjection pointer;
    [SerializeField] private BoxCollider2D topWall;
    [SerializeField] private BoxCollider2D bottomWall;

    [Header("Arena")]
    [SerializeField] private Vector2 arenaSize = new Vector2(9f, 6f);
    [Min(0.01f)] [SerializeField] private float paddleHalfHeight = 0.8f;
    [Min(0.01f)] [SerializeField] private float ballRadius = 0.125f;

    [Header("Movement")]
    [Min(0f)] [SerializeField] private float launchDelay = 0.5f;
    [Min(0.1f)] [SerializeField] private float ballSpeed = 6f;
    [Min(0.1f)] [SerializeField] private float botSpeed = 8f;

    private Vector3 _playerStart, _botStart;
    private float _elapsed, _serveElapsed;
    private bool _initialized, _playing, _served;

    public override void StartGame()
    {
        StopGame();
        if (!playerPaddle || !botPaddle || !ball || !ball.IsConfigured || !pointer || !topWall || !bottomWall)
        {
            Debug.LogError("Assign PongMinigame references in the inspector.", this);
            return;
        }
        if (!_initialized)
        {
            _playerStart = playerPaddle.transform.localPosition;
            _botStart = botPaddle.transform.localPosition;
            _initialized = true;
        }
        base.StartGame();
        ResetPaddle(playerPaddle, _playerStart);
        ResetPaddle(botPaddle, _botStart);
        ConfigureWall(topWall, 1f);
        ConfigureWall(bottomWall, -1f);
        _elapsed = 0f;
        ResetServe();
        _playing = true;
    }

    public override void StopGame()
    {
        _playing = false;
        if (ball) ball.Stop();
        StopPaddle(playerPaddle);
        StopPaddle(botPaddle);
        base.StopGame();
    }

    private void OnDisable() => StopGame();

    protected override void Update()
    {
        if (!_playing || Time.deltaTime <= 0f) return;
        float ballX = transform.InverseTransformPoint(ball.Position).x;
        if (ballX > arenaSize.x * 0.5f + ballRadius) { Finish(false); return; }
        if (ballX < -arenaSize.x * 0.5f - ballRadius) ResetServe();

        _elapsed += Time.deltaTime;
        Progress = 1f - Mathf.Clamp01(_elapsed / Mathf.Max(0.01f, surviveTime));
        if (Progress <= 0f) { Finish(true); return; }
        if (_served) return;
        _serveElapsed += Time.deltaTime;
        if (_serveElapsed < launchDelay) return;
        _served = true;
        ball.Launch(ballSpeed);
    }

    private void FixedUpdate()
    {
        if (!_playing) return;
        float limit = Mathf.Max(0f, arenaSize.y * 0.5f - paddleHalfHeight);
        if (pointer.TryGetPosition(playerPaddle.transform.position.z, false, out Vector3 position))
        {
            Vector3 local = _playerStart;
            local.y = Mathf.Clamp(transform.InverseTransformPoint(position).y, -limit, limit);
            playerPaddle.MovePosition(transform.TransformPoint(local));
        }
        Vector3 bot = transform.InverseTransformPoint(botPaddle.position);
        float target = Mathf.Clamp(transform.InverseTransformPoint(ball.Position).y, -limit, limit);
        bot.y = Mathf.MoveTowards(bot.y, target, botSpeed * Time.fixedDeltaTime);
        botPaddle.MovePosition(transform.TransformPoint(bot));
    }

    private void ResetServe()
    {
        ball.ResetBall(transform.position);
        _serveElapsed = 0f;
        _served = false;
    }

    private void ResetPaddle(Rigidbody2D paddle, Vector3 position)
    {
        paddle.transform.localPosition = position;
        paddle.position = paddle.transform.position;
        paddle.simulated = true;
    }

    private static void StopPaddle(Rigidbody2D paddle)
    {
        if (!paddle) return;
        paddle.linearVelocity = Vector2.zero;
        paddle.simulated = false;
    }

    private void ConfigureWall(BoxCollider2D wall, float side)
    {
        wall.transform.localPosition = new Vector3(0f, side * (arenaSize.y * 0.5f + 0.2f), 0f);
        wall.size = new Vector2(arenaSize.x + 2f, 0.4f);
    }

    private void Finish(bool won)
    {
        if (!_playing) return;
        StopGame();
        if (won) RaiseWin(); else RaiseLose();
    }
}
