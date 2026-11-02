using System.Collections.Generic;
using UnityEngine;

// Grid rules are independent of rendering and the minigame lifecycle.
public sealed class SnakeBoard
{
    public enum StepResult { Moved, Ate, Won, Collision }
    public readonly List<Vector2Int> Cells = new List<Vector2Int>();
    public readonly List<Vector2Int> Fruits = new List<Vector2Int>();
    public Vector2Int Direction { get; private set; }
    private Vector2Int _queued;
    private readonly Vector2Int _size;

    public SnakeBoard(Vector2Int size, int length, int fruitCount)
    {
        _size = size;
        Direction = _queued = Vector2Int.left;
        for (int i = 0; i < length; i++)
            Cells.Add(new Vector2Int(size.x + i, size.y / 2));

        SpawnFruits(fruitCount);
    }

    private void SpawnFruits(int count)
    {
        count = Mathf.Clamp(count, 0, ((_size.x + 1) / 2) * ((_size.y + 1) / 2));
        var available = new List<Vector2Int>();
        for (int attempt = 0; attempt <= 32; attempt++)
        {
            Fruits.Clear();
            available.Clear();
            // A spaced fallback guarantees the requested count on crowded boards.
            int stride = attempt == 32 ? 2 : 1;
            for (int y = 0; y < _size.y; y += stride)
                for (int x = 0; x < _size.x; x += stride)
                    available.Add(new Vector2Int(x, y));
            while (Fruits.Count < count && available.Count > 0)
            {
                Vector2Int fruit = available[Random.Range(0, available.Count)];
                Fruits.Add(fruit);
                available.RemoveAll(cell => Mathf.Abs(cell.x - fruit.x) <= 1
                    && Mathf.Abs(cell.y - fruit.y) <= 1);
            }
            if (Fruits.Count == count) return;
        }
    }

    public void QueueDirection(Vector2Int direction)
    {
        if (Mathf.Abs(direction.x) + Mathf.Abs(direction.y) != 1 || direction == -Direction)
            return;
        _queued = direction;
    }

    public StepResult Step()
    {
        // The first step always enters from the right.
        if (Cells[0].x >= _size.x) _queued = Vector2Int.left;
        Vector2Int next = Cells[0] + _queued;
        next = new Vector2Int(Wrap(next.x, _size.x), Wrap(next.y, _size.y));
        bool eats = Fruits.Contains(next);
        int collisionCount = Cells.Count - (eats ? 0 : 1);
        for (int i = 0; i < collisionCount; i++)
            if (Cells[i] == next) return StepResult.Collision;
        Direction = _queued;
        Cells.Insert(0, next);
        if (!eats) Cells.RemoveAt(Cells.Count - 1);
        else Fruits.Remove(next);
        return eats ? (Fruits.Count == 0 ? StepResult.Won : StepResult.Ate) : StepResult.Moved;
    }

    public Vector2Int DirectionBetween(Vector2Int from, Vector2Int to)
    {
        Vector2Int delta = to - from;
        if (Mathf.Abs(delta.x) > 1) delta.x = -System.Math.Sign(delta.x);
        if (Mathf.Abs(delta.y) > 1) delta.y = -System.Math.Sign(delta.y);
        return delta;
    }

    private static int Wrap(int value, int size) => (value % size + size) % size;
}
