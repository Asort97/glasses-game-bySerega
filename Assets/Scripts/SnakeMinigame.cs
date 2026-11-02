using System.Collections.Generic;
using UnityEngine;

public sealed class SnakeMinigame : MinigameBase
{
    [Header("Sprites")]
    [SerializeField] private SpriteRenderer head;
    [SerializeField] private SpriteRenderer fruitTemplate;
    [SerializeField] private Sprite headSprite;
    [SerializeField] private Sprite bodySprite;
    [SerializeField] private Sprite cornerSprite;
    [SerializeField] private Sprite fruitSprite;
    [Header("Grid")]
    [SerializeField] private Vector2Int gridSize = new Vector2Int(11, 7);
    [Min(0.01f)] [SerializeField] private float cellSize = 0.8f;
    [SerializeField] private Vector2 gridCenter;
    [Min(1)] [SerializeField] private int initialLength = 3;
    [Min(1)] [SerializeField] private int fruitCount = 3;
    [Min(0.01f)] [SerializeField] private float stepInterval = 0.5f;
    [Tooltip("Direction the unrotated head sprite faces, in degrees from right.")]
    [SerializeField] private float headSpriteAngle = 90f;
    [Tooltip("Rotation offset for a corner connecting up and right.")]
    [SerializeField] private float cornerSpriteAngle = 180f;

    private readonly List<SpriteRenderer> _segments = new List<SpriteRenderer>();
    private readonly List<SpriteRenderer> _fruits = new List<SpriteRenderer>();
    private SnakeBoard _board;
    private bool _playing;
    private float _elapsed;
    private float _stepTimer;

    public override void StartGame()
    {
        StopGame();
        if (!head || !fruitTemplate || !headSprite || !bodySprite || !cornerSprite || !fruitSprite)
        {
            Debug.LogError("Assign the snake renderers and all four sprites.", this);
            return;
        }
        base.StartGame();
        _board = new SnakeBoard(new Vector2Int(Mathf.Max(3, gridSize.x), Mathf.Max(3, gridSize.y)),
            Mathf.Clamp(initialLength, 1, Mathf.Max(3, gridSize.x) - 1), Mathf.Max(1, fruitCount));
        _elapsed = _stepTimer = 0f;
        _playing = true;
        head.gameObject.SetActive(true);
        fruitTemplate.gameObject.SetActive(false);
        RenderBoard();
    }

    public override void StopGame()
    {
        _playing = false;
        base.StopGame();
    }

    private void OnDisable() => StopGame();

    protected override void Update()
    {
        if (!_playing || Time.deltaTime <= 0f) return;
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) _board.QueueDirection(Vector2Int.up);
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) _board.QueueDirection(Vector2Int.down);
        else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) _board.QueueDirection(Vector2Int.left);
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) _board.QueueDirection(Vector2Int.right);
        Advance(Time.deltaTime);
    }

    private void Advance(float deltaTime)
    {
        if (!_playing) return;
        _elapsed += deltaTime;
        Progress = 1f - Mathf.Clamp01(_elapsed / Mathf.Max(0.01f, surviveTime));
        if (Progress <= 0f) { Finish(false); return; }
        _stepTimer += deltaTime;
        while (_playing && _stepTimer >= Mathf.Max(0.01f, stepInterval))
        {
            _stepTimer -= Mathf.Max(0.01f, stepInterval);
            SnakeBoard.StepResult result = _board.Step();
            RenderBoard();
            if (result == SnakeBoard.StepResult.Collision) Finish(false);
            else if (result == SnakeBoard.StepResult.Won) Finish(true);
        }
    }

    private void RenderBoard()
    {
        Place(head, _board.Cells[0], headSprite, Angle(_board.Direction) - headSpriteAngle);
        EnsurePool(_segments, _board.Cells.Count - 1, head, "Snake_segment");
        for (int i = 0; i < _segments.Count; i++)
        {
            bool visible = i + 1 < _board.Cells.Count;
            _segments[i].gameObject.SetActive(visible);
            if (!visible) continue;
            int index = i + 1;
            Vector2Int towardHead = _board.DirectionBetween(_board.Cells[index], _board.Cells[index - 1]);
            Sprite sprite = bodySprite;
            float angle = Angle(towardHead);
            if (index == _board.Cells.Count - 1)
            {
                sprite = headSprite;
                angle = Angle(-towardHead) - headSpriteAngle;
            }
            else
            {
                Vector2Int towardTail = _board.DirectionBetween(_board.Cells[index], _board.Cells[index + 1]);
                if (towardHead.x * towardTail.x + towardHead.y * towardTail.y == 0)
                {
                    sprite = cornerSprite;
                    Vector2Int corner = towardHead + towardTail;
                    angle = Angle(corner) - 45f + cornerSpriteAngle;
                }
            }
            Place(_segments[i], _board.Cells[index], sprite, angle);
        }
        EnsurePool(_fruits, _board.Fruits.Count, fruitTemplate, "Snake_fruit");
        for (int i = 0; i < _fruits.Count; i++)
        {
            _fruits[i].gameObject.SetActive(i < _board.Fruits.Count);
            if (i < _board.Fruits.Count) Place(_fruits[i], _board.Fruits[i], fruitSprite, 0f);
        }
    }

    private void Place(SpriteRenderer renderer, Vector2Int cell, Sprite sprite, float angle)
    {
        renderer.sprite = sprite;
        renderer.transform.localPosition = new Vector3(
            gridCenter.x + (cell.x - (Mathf.Max(3, gridSize.x) - 1) * 0.5f) * cellSize,
            gridCenter.y + (cell.y - (Mathf.Max(3, gridSize.y) - 1) * 0.5f) * cellSize, head.transform.localPosition.z);
        renderer.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    private static float Angle(Vector2Int direction) => Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

    private void EnsurePool(List<SpriteRenderer> pool, int count, SpriteRenderer template, string prefix)
    {
        while (pool.Count < count)
        {
            SpriteRenderer renderer = Instantiate(template, template.transform.parent);
            renderer.name = prefix + "_" + pool.Count;
            pool.Add(renderer);
        }
    }

    private void Finish(bool won)
    {
        StopGame();
        if (won) RaiseWin(); else RaiseLose();
    }
}
