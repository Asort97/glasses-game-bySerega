using DG.Tweening;
using UnityEngine;

public sealed class NineSlideMinigame : MinigameBase, IMinigameWinPresentation
{
    private enum Phase { Stopped, Input, PlayerMove, VictoryPause, FinalEye }

    [Header("Board")]
    [Tooltip("Solved layout: left to right, top to bottom. Leave element 4 empty.")]
    [SerializeField] private Transform[] solvedTiles = new Transform[9];
    [SerializeField] private BossLevelDirector bossDirector;

    [Header("Difficulty")]
    [Range(1, 8)] [SerializeField] private int initialShuffleMoves = 3;
    [Min(0)] [SerializeField] private int extraMovesPerRound = 1;

    [Header("Timing")]
    [Min(0.01f)] [SerializeField] private float playerMoveDuration = 0.12f;

    [Header("Victory")]
    [SerializeField] private GameObject finalEye;
    [Min(0f)] [SerializeField] private float solvedPauseDuration = 0.5f;
    [Min(0f)] [SerializeField] private float finalEyeDuration = 0.5f;
    [Range(0.5f, 1f)] [SerializeField] private float winResultSpeed = 0.75f;
    [Range(0.5f, 1f)] [SerializeField] private float winSoundSpeed = 0.75f;
    [Range(0.25f, 1f)] [SerializeField] private float winSoundPitch = 0.4f;
    [SerializeField] private bool winSoundReverb = true;

    public float WinResultSpeed => winResultSpeed;
    public float WinSoundSpeed => winSoundSpeed;
    public float WinSoundPitch => winSoundPitch;
    public bool WinSoundReverb => winSoundReverb;

    private readonly SlidePuzzleBoard _board = new SlidePuzzleBoard();
    private readonly Vector3[] _positions = new Vector3[9];
    private bool _layoutCaptured;
    private Phase _phase;
    private float _inputElapsed;
    private float _victoryRemaining;
    private Tween _moveTween;
    private Transform _movingTile;
    private Vector3 _moveTarget;

    public int ShuffleMoveCount => (int)System.Math.Min(8L, System.Math.Max(1L,
        initialShuffleMoves + (long)(bossDirector != null ? bossDirector.CompletedRounds : 0)
        * System.Math.Max(0, extraMovesPerRound)));

    public override void StartGame()
    {
        StopGame();
        if (finalEye != null) finalEye.SetActive(false);
        base.StartGame();
        if (!CaptureLayout())
        {
            Debug.LogError("Assign eight solved tiles and an empty center on NineSlideMinigame.", this);
            base.StopGame();
            RaiseLose();
            return;
        }

        _board.Reset();
        for (int i = 0; i < solvedTiles.Length; i++)
            if (solvedTiles[i] != null) solvedTiles[i].localPosition = _positions[i];
        int[] shuffle = SlidePuzzleBoard.CreateShuffle(ShuffleMoveCount,
            new System.Random(Random.Range(0, int.MaxValue)));
        foreach (int cell in shuffle)
        {
            _board.TrySlide(cell, out int tile, out int destination);
            solvedTiles[tile].localPosition = _positions[destination];
        }
        _inputElapsed = 0f;
        _phase = Phase.Input;
    }

    public override void StopGame()
    {
        _phase = Phase.Stopped;
        _moveTween?.Kill();
        _moveTween = null;
        if (_movingTile != null) _movingTile.localPosition = _moveTarget;
        _movingTile = null;
        base.StopGame();
    }

    private void OnDisable() => StopGame();

    private bool CaptureLayout()
    {
        if (_layoutCaptured) return true;
        if (solvedTiles == null || solvedTiles.Length != 9 || solvedTiles[4] != null) return false;
        for (int i = 0; i < 9; i++)
        {
            if (i == 4) continue;
            if (solvedTiles[i] == null || solvedTiles[i].parent != transform) return false;
            for (int j = 0; j < i; j++)
                if (solvedTiles[i] == solvedTiles[j]) return false;
            _positions[i] = solvedTiles[i].localPosition;
        }
        _positions[4] = (_positions[1] + _positions[7]) * 0.5f;
        _layoutCaptured = true;
        return true;
    }

    protected override void Update()
    {
        bool acceptInput = _phase == Phase.Input;
        Advance(Time.deltaTime);
        if (!acceptInput || _phase != Phase.Input || Time.deltaTime <= 0f) return;

        int dx = 0, dy = 0;
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) dy--;
        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) dy++;
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) dx--;
        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) dx++;
        if (Mathf.Abs(dx) + Mathf.Abs(dy) != 1) return;
        // Pick the tile opposite the requested movement, so it slides into the gap.
        int x = _board.EmptyCell % 3 - dx;
        int y = _board.EmptyCell / 3 - dy;
        if (x >= 0 && x < 3 && y >= 0 && y < 3) SubmitCell(y * 3 + x);
    }

    private void Advance(float deltaTime)
    {
        if (_phase == Phase.Stopped || deltaTime <= 0f) return;
        if (_phase == Phase.VictoryPause || _phase == Phase.FinalEye)
        {
            _victoryRemaining -= deltaTime;
            if (_victoryRemaining > 0f) return;
            if (_phase == Phase.VictoryPause)
            {
                _phase = Phase.FinalEye;
                _victoryRemaining = Mathf.Max(0f, finalEyeDuration);
                if (finalEye != null) finalEye.SetActive(true);
            }
            else
                Finish(true);
            return;
        }
        _inputElapsed += deltaTime;
        Progress = 1f - Mathf.Clamp01(_inputElapsed / Mathf.Max(0.01f, surviveTime));
        if (Progress <= 0f) Finish(false);
    }

    private void SubmitCell(int cell)
    {
        if (_phase == Phase.Input) AnimateSlide(cell);
    }

    private void AnimateSlide(int cell)
    {
        if (!_board.TrySlide(cell, out int tile, out int destination)) return;
        _phase = Phase.PlayerMove;
        _movingTile = solvedTiles[tile];
        _moveTarget = _positions[destination];
        _moveTween = _movingTile.DOLocalMove(_moveTarget,
                Mathf.Max(0.01f, playerMoveDuration))
            .SetEase(Ease.InOutSine)
            .OnComplete(CompleteMove);
    }

    private void CompleteMove()
    {
        _moveTween = null;
        _movingTile = null;
        if (_phase == Phase.Stopped) return;
        if (_board.IsSolved)
        {
            _phase = Phase.VictoryPause;
            _victoryRemaining = Mathf.Max(0f, solvedPauseDuration);
        }
        else
            _phase = Phase.Input;
    }

    private void Finish(bool won)
    {
        if (_phase == Phase.Stopped) return;
        StopGame();
        if (won) RaiseWin();
        else RaiseLose();
    }
}
