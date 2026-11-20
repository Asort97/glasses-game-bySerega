using System;
using DG.Tweening;
using UnityEngine;

public class TicTacToeBoard : MonoBehaviour
{
    /// <summary>Игрок кликнул правильную клетку — победа.</summary>
    public event Action OnCorrectClick;
    /// <summary>Игрок кликнул неправильную клетку — поражение.</summary>
    public event Action OnWrongClick;
    public enum Mark { X, O }

    // Rows top to bottom. The minority symbol is the player's and wins at the target cell.
    // Rules: the majority has two in a line with an empty finishing cell plus one more mark;
    // the minority has two in a line whose empty cell is the target.
    private struct Pattern
    {
        public string[] Rows;
        public int TargetRow, TargetCol;
        public Pattern(int row, int col, params string[] rows) { Rows = rows; TargetRow = row; TargetCol = col; }
    }

    private static readonly Pattern[] Patterns =
    {
        new Pattern(0, 2, "OO.", "XX.", ".X."),
        new Pattern(2, 2, "OO.", "..O", "XX."),
        new Pattern(2, 0, "XO.", "XO.", "..O"),
        new Pattern(2, 2, "XXO", "X.O", "..."),
        new Pattern(1, 2, ".O.", "XX.", "OO."),
    };

    [Header("Refs")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Camera boardCamera;          // RightGameCamera
    [SerializeField] private Transform lensTransform;     // Lens2 (с MeshCollider)
    [SerializeField] private Transform marksParent;       // куда спавнить спрайты (по умолчанию — self)
    [SerializeField] private Transform boardTransform;    // центр спрайта сетки TicTacToeATL_0

    [Header("Sprites")]
    [SerializeField] private Sprite spriteX;              // TicTacToeATL_2
    [SerializeField] private Sprite spriteO;              // TicTacToeATL_1
    [SerializeField] private float markScale = 1f;
    [SerializeField] private float markZ = -4.5f;
    [SerializeField] private int sortingOrder = 5;
    [SerializeField] private string sortingLayerName = "Default";

    [Header("Board layout (world space)")]
    [SerializeField] private Vector2 boardCenter = new Vector2(16.88f, -1.79f);
    [SerializeField] private float cellSpacing = 1.93f;
    [Tooltip("Сторона квадратной зоны клика по клетке. ≤0 = равно cellSpacing.")]
    [SerializeField] private float cellHitSize = 1.5f;
    [SerializeField] private bool invertClickX = false;
    [SerializeField] private bool invertClickY = false;

    [Header("Wrong Click")]
    [SerializeField] private float wrongShakeDuration = 0.3f;
    [SerializeField] private float wrongShakeStrength = 0.15f;

    private MeshCollider _lensCollider;
    private MeshFilter _lensMeshFilter;
    private Bounds _lensLocalBounds;
    private int _uAxis = 0;
    private int _vAxis = 1;
    private int _normalAxis = 2;
    private int _targetIndex = -1;
    private Mark _playerMark;
    private bool _locked = true;
    private readonly System.Collections.Generic.List<GameObject> _spawnedMarks = new System.Collections.Generic.List<GameObject>();

    private void Awake()
    {
        ResolveRefs();
        EnsureLensCollider();
        if (marksParent == null) marksParent = transform;
    }

    private void OnEnable()
    {
        ResolveRefs();
        EnsureLensCollider();
    }

    private void OnDisable()
    {
        _locked = true;
    }

    private void ResolveRefs()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (lensTransform == null)
        {
            var go = GameObject.Find("Lens2");
            if (go != null) lensTransform = go.transform;
        }
        if (boardCamera == null)
        {
            var go = GameObject.Find("RightGameCamera");
            if (go != null) boardCamera = go.GetComponent<Camera>();
        }
        if (boardTransform == null)
            boardTransform = transform.Find("TicTacToeATL_0");
    }

    private void EnsureLensCollider()
    {
        if (lensTransform == null)
            return;

        _lensMeshFilter = lensTransform.GetComponent<MeshFilter>();
        if (_lensMeshFilter == null || _lensMeshFilter.sharedMesh == null)
            return;

        _lensLocalBounds = _lensMeshFilter.sharedMesh.bounds;
        ConfigureLensMapping(_lensLocalBounds.size);

        _lensCollider = lensTransform.GetComponent<MeshCollider>();
        if (_lensCollider == null)
            _lensCollider = lensTransform.gameObject.AddComponent<MeshCollider>();

        _lensCollider.sharedMesh = _lensMeshFilter.sharedMesh;
        _lensCollider.convex = false;
    }

    private void Update()
    {
        if (_locked || _targetIndex < 0 || !Input.GetMouseButtonDown(0)) return;
        if (mainCamera == null || boardCamera == null || _lensCollider == null)
        {
            ResolveRefs();
            EnsureLensCollider();
            if (mainCamera == null || boardCamera == null || _lensCollider == null) return;
        }

        Ray ray = mainCamera.ScreenPointToRay(GameInput.MousePosition);
        if (!_lensCollider.Raycast(ray, out RaycastHit hit, 1000f)) return;

        int idx = UvToCellIndex(GetUvFromLensHit(hit.point));
        if (idx < 0) return;

        _locked = true;
        GameObject mark = SpawnMark(idx / 3, idx % 3, _playerMark);
        if (idx != _targetIndex)
        {
            if (mark != null)
                mark.transform.DOShakePosition(wrongShakeDuration, wrongShakeStrength, 25, 90f, false, true);
            OnWrongClick?.Invoke();
            return;
        }
        OnCorrectClick?.Invoke();
    }

    private GameObject SpawnMark(int row, int col, Mark mark)
    {
        Sprite sprite = mark == Mark.X ? spriteX : spriteO;
        if (sprite == null) return null;

        Vector2 center = GetBoardCenter();
        float x = center.x + (col - 1) * cellSpacing;
        float y = center.y + (1 - row) * cellSpacing;

        var go = new GameObject($"Mark_{row}_{col}_{mark}");
        go.transform.SetParent(marksParent, true);
        go.transform.position = new Vector3(x, y, markZ);
        go.transform.localScale = Vector3.one * markScale;
        go.layer = marksParent != null ? marksParent.gameObject.layer : 0;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = sortingOrder;
        if (!string.IsNullOrEmpty(sortingLayerName)) sr.sortingLayerName = sortingLayerName;

        _spawnedMarks.Add(go);
        return go;
    }

    private void ClearSpawnedMarks()
    {
        for (int i = 0; i < _spawnedMarks.Count; i++)
            if (_spawnedMarks[i] != null)
            {
                _spawnedMarks[i].transform.DOKill();
                Destroy(_spawnedMarks[i]);
            }
        _spawnedMarks.Clear();
    }

    /// <summary>Случайный паттерн, повёрнутый на случайные 0/90/180/270 градусов.</summary>
    public void ResetGame()
    {
        ClearSpawnedMarks();
        Pattern pattern = Patterns[UnityEngine.Random.Range(0, Patterns.Length)];
        int turns = UnityEngine.Random.Range(0, 4);
        int xCount = 0, oCount = 0;
        for (int r = 0; r < 3; r++)
            for (int c = 0; c < 3; c++)
            {
                char symbol = pattern.Rows[r][c];
                if (symbol == '.') continue;
                Mark mark = symbol == 'X' ? Mark.X : Mark.O;
                if (mark == Mark.X) xCount++; else oCount++;
                Rotate(r, c, turns, out int row, out int col);
                SpawnMark(row, col, mark);
            }
        _playerMark = xCount < oCount ? Mark.X : Mark.O;
        Rotate(pattern.TargetRow, pattern.TargetCol, turns, out int targetRow, out int targetCol);
        _targetIndex = targetRow * 3 + targetCol;
        _locked = false;
    }

    /// <summary>Блокирует ввод до следующего ResetGame.</summary>
    public void Lock() => _locked = true;

    // Clockwise quarter turns of the 3x3 grid.
    private static void Rotate(int row, int col, int turns, out int outRow, out int outCol)
    {
        for (int i = 0; i < turns; i++)
        {
            int temp = row;
            row = col;
            col = 2 - temp;
        }
        outRow = row;
        outCol = col;
    }

    // Сравниваем UV попадания с viewport-позицией каждой ячейки через WorldToViewportPoint.
    // Это работает независимо от положения камеры и offset доски.
    private int UvToCellIndex(Vector2 uv)
    {
        if (boardCamera == null) return -1;
        float camW = boardCamera.orthographicSize * boardCamera.aspect * 2f;
        float camH = boardCamera.orthographicSize * 2f;
        float halfHitX = (cellHitSize > 0f ? cellHitSize : cellSpacing) * 0.5f / camW;
        float halfHitY = (cellHitSize > 0f ? cellHitSize : cellSpacing) * 0.5f / camH;

        int bestIdx = -1;
        float bestSqr = float.MaxValue;
        Vector2 center = GetBoardCenter();
        for (int r = 0; r < 3; r++)
        {
            for (int c = 0; c < 3; c++)
            {
                float wx = center.x + (c - 1) * cellSpacing;
                float wy = center.y + (1 - r) * cellSpacing;
                Vector3 vp = boardCamera.WorldToViewportPoint(new Vector3(wx, wy, 0f));
                float dx = Mathf.Abs(uv.x - vp.x);
                float dy = Mathf.Abs(uv.y - vp.y);
                if (dx > halfHitX || dy > halfHitY) continue;
                float sqr = dx * dx + dy * dy;
                if (sqr < bestSqr) { bestSqr = sqr; bestIdx = r * 3 + c; }
            }
        }
        return bestIdx;
    }

    private Vector2 GetBoardCenter()
    {
        if (boardTransform != null)
            return boardTransform.position;

        return boardCenter;
    }

    private void ConfigureLensMapping(Vector3 size)
    {
        _normalAxis = GetSmallestAxis(size);

        int index = 0;
        for (int axis = 0; axis < 3; axis++)
        {
            if (axis == _normalAxis) continue;

            if (index == 0) _uAxis = axis;
            else _vAxis = axis;
            index++;
        }
    }

    private Vector2 GetUvFromLensHit(Vector3 worldPoint)
    {
        if (lensTransform == null || _lensMeshFilter == null)
            return Vector2.zero;

        Vector3 localPoint = lensTransform.InverseTransformPoint(worldPoint);

        float minU = GetAxisValue(_lensLocalBounds.min, _uAxis);
        float maxU = GetAxisValue(_lensLocalBounds.max, _uAxis);
        float minV = GetAxisValue(_lensLocalBounds.min, _vAxis);
        float maxV = GetAxisValue(_lensLocalBounds.max, _vAxis);

        float u = Mathf.InverseLerp(minU, maxU, GetAxisValue(localPoint, _uAxis));
        float v = Mathf.InverseLerp(minV, maxV, GetAxisValue(localPoint, _vAxis));

        if (invertClickX) u = 1f - u;
        if (invertClickY) v = 1f - v;

        return new Vector2(Mathf.Clamp01(u), Mathf.Clamp01(v));
    }

    private static int GetSmallestAxis(Vector3 value)
    {
        if (value.x <= value.y && value.x <= value.z) return 0;
        if (value.y <= value.z) return 1;
        return 2;
    }

    private static float GetAxisValue(Vector3 value, int axis)
    {
        switch (axis)
        {
            case 0: return value.x;
            case 1: return value.y;
            default: return value.z;
        }
    }
}
