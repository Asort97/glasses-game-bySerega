using System;

// Cell indices run left to right, top to bottom. The solved empty cell is the center.
public sealed class SlidePuzzleBoard
{
    public const int CellCount = 9;
    public const int CenterCell = 4;
    private readonly int[] _tiles = new int[CellCount];

    public int EmptyCell { get; private set; }
    public bool IsSolved
    {
        get
        {
            for (int i = 0; i < CellCount; i++)
                if (_tiles[i] != (i == CenterCell ? -1 : i)) return false;
            return true;
        }
    }

    public SlidePuzzleBoard() => Reset();

    public void Reset()
    {
        for (int i = 0; i < CellCount; i++)
            _tiles[i] = i == CenterCell ? -1 : i;
        EmptyCell = CenterCell;
    }

    public int TileAt(int cell) => _tiles[cell];

    public static bool AreNeighbors(int a, int b)
    {
        return a >= 0 && a < CellCount && b >= 0 && b < CellCount
            && Math.Abs(a / 3 - b / 3) + Math.Abs(a % 3 - b % 3) == 1;
    }

    public bool TrySlide(int sourceCell, out int tile, out int destinationCell)
    {
        tile = -1;
        destinationCell = EmptyCell;
        if (!AreNeighbors(sourceCell, EmptyCell)) return false;
        tile = _tiles[sourceCell];
        _tiles[EmptyCell] = tile;
        _tiles[sourceCell] = -1;
        EmptyCell = sourceCell;
        return true;
    }

    public static int[] CreateShuffle(int requestedMoves, Random random)
    {
        int[] path = new int[Math.Max(1, Math.Min(CellCount - 1, requestedMoves))];
        bool[] visited = new bool[CellCount];
        visited[CenterCell] = true;
        // Backtrack rather than truncate a random path that gets stuck in a corner.
        if (!FindPath(CenterCell, 0, path, visited, random))
            throw new InvalidOperationException("Could not create a slide puzzle shuffle.");
        return path;
    }

    private static bool FindPath(int empty, int depth, int[] path, bool[] visited, Random random)
    {
        if (depth == path.Length) return true;
        int[] candidates = { empty - 3, empty + 1, empty + 3, empty - 1 };
        for (int i = candidates.Length - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            int temp = candidates[i];
            candidates[i] = candidates[j];
            candidates[j] = temp;
        }
        foreach (int next in candidates)
        {
            if (!AreNeighbors(empty, next) || visited[next]) continue;
            visited[next] = true;
            path[depth] = next;
            if (FindPath(next, depth + 1, path, visited, random)) return true;
            visited[next] = false;
        }
        return false;
    }
}
