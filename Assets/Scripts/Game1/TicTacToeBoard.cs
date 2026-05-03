using System;
using UnityEngine;

public class TicTacToeBoard : MonoBehaviour
{
    public enum Mark { X, O }

    [Serializable]
    public class Scenario
    {
        [Tooltip("GameObject сценария (например Scenario1). Активируется когда сценарий выбран.")]
        public GameObject root;

        [Tooltip("За какой символ ходит игрок в этом сценарии.")]
        public Mark playerSymbol = Mark.X;

        [Tooltip("Правильная клетка: row (0=верх, 2=низ).")]
        [Range(0, 2)] public int targetRow = 0;

        [Tooltip("Правильная клетка: col (0=лево, 2=право).")]
        [Range(0, 2)] public int targetCol = 0;
    }

    [Header("Refs")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Camera boardCamera;          // RightGameCamera
    [SerializeField] private Transform lensTransform;     // Lens2 (с MeshCollider)
    [SerializeField] private Transform marksParent;       // куда спавнить спрайты (по умолчанию — self)

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

    [Header("Scenarios")]
    [SerializeField] private Scenario[] scenarios;
    [Tooltip("Автопереход к следующему сценарию после клика.")]
    [SerializeField] private bool advanceOnClick = true;
    [Tooltip("Задержка в секундах перед сменой сценария.")]
    [SerializeField] private float advanceDelay = 1f;

    private MeshCollider _lensCollider;
    private int _current = -1;
    private bool _finished;
    private bool _advancing;

    private void Awake()
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
        if (lensTransform != null) _lensCollider = lensTransform.GetComponent<MeshCollider>();
        if (marksParent == null) marksParent = transform;

        SetScenario(0);
    }

    private void Update()
    {
        if (_finished)
        {
            if (Input.GetKeyDown(KeyCode.R)) SetScenario(0);
            return;
        }
        if (_advancing) return;
        if (!Input.GetMouseButtonDown(0)) return;
        if (mainCamera == null || boardCamera == null || _lensCollider == null) return;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (!_lensCollider.Raycast(ray, out RaycastHit hit, 1000f)) return;

        Vector3 world = UvToBoardWorld(hit.textureCoord);
        int idx = WorldToCellIndex(world);
        if (idx < 0) return;

        var sc = scenarios[_current];
        int targetIdx = sc.targetRow * 3 + sc.targetCol;
        int row = idx / 3, col = idx % 3;
        string scName = sc.root != null ? sc.root.name : "<none>";

        SpawnMark(row, col, sc.playerSymbol);

        if (idx == targetIdx)
            Debug.Log($"[TicTacToe] WIN. Scenario '{scName}' as {sc.playerSymbol} clicked correct cell ({row},{col}).");
        else
            Debug.Log($"[TicTacToe] LOSE. Scenario '{scName}' as {sc.playerSymbol} wrong cell ({row},{col}), expected ({sc.targetRow},{sc.targetCol}).");

        if (advanceOnClick)
        {
            int next = _current + 1;
            if (advanceDelay > 0f)
            {
                _advancing = true;
                Invoke(nameof(AdvanceNow), advanceDelay);
                _pendingNext = next;
            }
            else
            {
                SetScenario(next);
            }
        }
    }

    private int _pendingNext;
    private readonly System.Collections.Generic.List<GameObject> _spawnedMarks = new System.Collections.Generic.List<GameObject>();

    private void SpawnMark(int row, int col, Mark mark)
    {
        Sprite sprite = mark == Mark.X ? spriteX : spriteO;
        if (sprite == null) return;

        float x = boardCenter.x + (col - 1) * cellSpacing;
        float y = boardCenter.y + (1 - row) * cellSpacing;

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
    }

    private void ClearSpawnedMarks()
    {
        for (int i = 0; i < _spawnedMarks.Count; i++)
            if (_spawnedMarks[i] != null) Destroy(_spawnedMarks[i]);
        _spawnedMarks.Clear();
    }

    private void AdvanceNow()
    {
        _advancing = false;
        SetScenario(_pendingNext);
    }

    private void SetScenario(int index)
    {
        ClearSpawnedMarks();
        if (scenarios == null || scenarios.Length == 0)
        {
            _finished = true;
            return;
        }
        for (int i = 0; i < scenarios.Length; i++)
            if (scenarios[i] != null && scenarios[i].root != null) scenarios[i].root.SetActive(false);

        if (index < 0 || index >= scenarios.Length)
        {
            _current = -1;
            _finished = true;
            Debug.Log("[TicTacToe] All scenarios complete. Press R to restart.");
            return;
        }

        _current = index;
        _finished = false;
        var sc = scenarios[_current];
        if (sc != null && sc.root != null) sc.root.SetActive(true);
        string scName = sc != null && sc.root != null ? sc.root.name : "<none>";
        int tr = sc != null ? sc.targetRow : -1;
        int tc = sc != null ? sc.targetCol : -1;
        Debug.Log($"[TicTacToe] Scenario '{scName}' active. Target cell ({tr},{tc}).");
    }

    private Vector3 UvToBoardWorld(Vector2 uv)
    {
        Vector3 c = boardCamera.transform.position;
        float halfH = boardCamera.orthographicSize;
        float halfW = halfH * boardCamera.aspect;
        float x = Mathf.Lerp(c.x - halfW, c.x + halfW, uv.x);
        float y = Mathf.Lerp(c.y - halfH, c.y + halfH, uv.y);
        return new Vector3(x, y, 0f);
    }

    private int WorldToCellIndex(Vector3 world)
    {
        float half = (cellHitSize > 0f ? cellHitSize : cellSpacing) * 0.5f;
        int bestIdx = -1;
        float bestSqr = float.MaxValue;
        for (int r = 0; r < 3; r++)
        {
            for (int c = 0; c < 3; c++)
            {
                float cx = boardCenter.x + (c - 1) * cellSpacing;
                float cy = boardCenter.y + (1 - r) * cellSpacing;
                float dx = Mathf.Abs(world.x - cx);
                float dy = Mathf.Abs(world.y - cy);
                if (dx > half || dy > half) continue;
                float sqr = dx * dx + dy * dy;
                if (sqr < bestSqr) { bestSqr = sqr; bestIdx = r * 3 + c; }
            }
        }
        return bestIdx;
    }
}
