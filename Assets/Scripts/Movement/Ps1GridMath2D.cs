using UnityEngine;

public static class Ps1GridMath2D
{
    public static Vector2 Snap(Vector2 position, float cellSize)
    {
        float safeCellSize = Mathf.Max(0.01f, cellSize);
        return new Vector2(
            Mathf.Round(position.x / safeCellSize) * safeCellSize,
            Mathf.Round(position.y / safeCellSize) * safeCellSize);
    }

    public static Vector2 Rotate(Vector2 direction, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        float sin = Mathf.Sin(radians);
        float cos = Mathf.Cos(radians);
        return new Vector2(
            direction.x * cos - direction.y * sin,
            direction.x * sin + direction.y * cos);
    }

    public static float RandomInterval(Vector2 interval)
    {
        float min = Mathf.Max(0.02f, Mathf.Min(interval.x, interval.y));
        float max = Mathf.Max(min, Mathf.Max(interval.x, interval.y));
        return Random.Range(min, max);
    }
}
