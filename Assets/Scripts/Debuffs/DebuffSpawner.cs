using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class DebuffSpawner : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private DebuffWindow windowPrefab;
    [SerializeField] private RectTransform windowParent;
    [SerializeField] private LensMinigameManager[] gameplayManagers;
    [SerializeField] private CanvasCursor canvasCursor;
    [SerializeField] private LensAudioService audioService;

    [Header("Schedule")]
    [Min(0.1f)] [SerializeField] private float spawnInterval = 10f;
    [Min(0.1f)] [SerializeField] private float closeTime = 5f;
    [Min(1)] [SerializeField] private int maxConcurrentWindows = 3;
    [SerializeField] private bool spawnOnStart = true;

    [Header("Placement")]
    [Min(0f)] [SerializeField] private float screenPadding = 20f;
    [Min(1)] [SerializeField] private int placementAttempts = 12;

    [Header("Debuffs")]
    [SerializeField] private DebuffDefinition[] definitions;

    private readonly List<DebuffWindow> _activeWindows = new List<DebuffWindow>();
    private readonly Dictionary<DebuffWindow, int> _loadingSoundIds = new Dictionary<DebuffWindow, int>();
    private Coroutine _spawnRoutine;

    public event Action<DebuffId> DebuffApplied;
    public event Action<DebuffId> DebuffDismissed;

    private void Start()
    {
        if (spawnOnStart)
            StartSpawning();
    }

    private void OnDisable()
    {
        StopSpawning();
        ClearWindows();
    }

    public void StartSpawning()
    {
        if (_spawnRoutine == null)
            _spawnRoutine = StartCoroutine(SpawnRoutine());
    }

    public void StopSpawning()
    {
        if (_spawnRoutine == null)
            return;

        StopCoroutine(_spawnRoutine);
        _spawnRoutine = null;
    }

    public bool Show(DebuffId id)
    {
        DebuffDefinition definition = GetDefinition(id);
        return definition != null && SpawnWindow(definition);
    }

    public void ClearWindows()
    {
        for (int i = _activeWindows.Count - 1; i >= 0; i--)
        {
            DebuffWindow window = _activeWindows[i];
            StopLoadingSound(window);

            if (window != null)
                Destroy(window.gameObject);
        }

        _activeWindows.Clear();
        _loadingSoundIds.Clear();
        SetCursorOverride(false);
    }

    private IEnumerator SpawnRoutine()
    {
        float elapsed = 0f;

        while (true)
        {
            yield return null;

            if (!IsGameplayActive())
                continue;

            elapsed += Time.unscaledDeltaTime;
            if (elapsed < spawnInterval)
                continue;

            elapsed = 0f;
            DebuffDefinition definition = PickRandomDefinition();
            if (definition != null)
                SpawnWindow(definition);
        }
    }

    private bool SpawnWindow(DebuffDefinition definition)
    {
        RemoveMissingWindows();

        if (windowPrefab == null || windowParent == null)
        {
            Debug.LogError($"[{nameof(DebuffSpawner)}] Window prefab or parent is not assigned.", this);
            return false;
        }

        if (_activeWindows.Count >= maxConcurrentWindows)
            return false;

        DebuffWindow window = Instantiate(windowPrefab, windowParent);
        window.Initialize(definition, closeTime, HandleExpired, HandleClosed);
        Canvas.ForceUpdateCanvases();
        PlaceWindow(window.RectTransform);
        _activeWindows.Add(window);
        SetCursorOverride(true);

        if (audioService != null)
            _loadingSoundIds[window] = audioService.StartDebuffLoading();

        return true;
    }

    private DebuffDefinition PickRandomDefinition()
    {
        if (definitions == null || definitions.Length == 0)
            return null;

        float totalWeight = 0f;
        foreach (DebuffDefinition definition in definitions)
        {
            if (definition != null && definition.RandomSpawnEnabled)
                totalWeight += definition.RandomWeight;
        }

        if (totalWeight <= 0f)
            return null;

        float roll = UnityEngine.Random.value * totalWeight;
        foreach (DebuffDefinition definition in definitions)
        {
            if (definition == null || !definition.RandomSpawnEnabled)
                continue;

            roll -= definition.RandomWeight;
            if (roll <= 0f)
                return definition;
        }

        return null;
    }

    private DebuffDefinition GetDefinition(DebuffId id)
    {
        if (definitions == null)
            return null;

        foreach (DebuffDefinition definition in definitions)
        {
            if (definition != null && definition.Id == id)
                return definition;
        }

        return null;
    }

    private bool IsGameplayActive()
    {
        if (gameplayManagers == null)
            return false;

        foreach (LensMinigameManager manager in gameplayManagers)
        {
            if (manager != null && manager.IsPlayingRegularMinigame)
                return true;
        }

        return false;
    }

    private void PlaceWindow(RectTransform window)
    {
        Rect parentRect = windowParent.rect;
        Vector2 size = window.rect.size;
        Vector2 pivot = window.pivot;
        Vector2 fallback = Vector2.zero;

        for (int attempt = 0; attempt < placementAttempts; attempt++)
        {
            float minX = parentRect.xMin + screenPadding + size.x * pivot.x;
            float maxX = parentRect.xMax - screenPadding - size.x * (1f - pivot.x);
            float minY = parentRect.yMin + screenPadding + size.y * pivot.y;
            float maxY = parentRect.yMax - screenPadding - size.y * (1f - pivot.y);

            Vector2 position = new Vector2(
                UnityEngine.Random.Range(Mathf.Min(minX, maxX), Mathf.Max(minX, maxX)),
                UnityEngine.Random.Range(Mathf.Min(minY, maxY), Mathf.Max(minY, maxY)));

            fallback = position;
            if (!OverlapsActiveWindow(position, size, pivot))
            {
                window.anchoredPosition = position;
                return;
            }
        }

        window.anchoredPosition = fallback;
    }

    private bool OverlapsActiveWindow(Vector2 position, Vector2 size, Vector2 pivot)
    {
        Rect candidate = CreateRect(position, size, pivot);

        foreach (DebuffWindow activeWindow in _activeWindows)
        {
            if (activeWindow == null)
                continue;

            RectTransform rectTransform = activeWindow.RectTransform;
            Rect occupied = CreateRect(
                rectTransform.anchoredPosition,
                rectTransform.rect.size,
                rectTransform.pivot);

            if (candidate.Overlaps(occupied))
                return true;
        }

        return false;
    }

    private static Rect CreateRect(Vector2 position, Vector2 size, Vector2 pivot)
    {
        return new Rect(position - Vector2.Scale(size, pivot), size);
    }

    private void HandleExpired(DebuffWindow window, DebuffId id)
    {
        StopLoadingSound(window);
        _activeWindows.Remove(window);
        SetCursorOverride(_activeWindows.Count > 0);
        DebuffApplied?.Invoke(id);
        Debug.Log($"[DebuffSpawner] Debuff ready to apply: {id}", this);
    }

    private void HandleClosed(DebuffWindow window, DebuffId id)
    {
        StopLoadingSound(window);
        _activeWindows.Remove(window);
        SetCursorOverride(_activeWindows.Count > 0);

        if (audioService != null)
            audioService.PlayCloseDebuff();

        DebuffDismissed?.Invoke(id);
    }

    private void RemoveMissingWindows()
    {
        _activeWindows.RemoveAll(window => window == null);
        SetCursorOverride(_activeWindows.Count > 0);
    }

    private void SetCursorOverride(bool active)
    {
        if (canvasCursor != null)
            canvasCursor.SetUiInteractionOverride(active);
    }

    private void StopLoadingSound(DebuffWindow window)
    {
        if (audioService == null || !_loadingSoundIds.TryGetValue(window, out int id))
            return;

        audioService.StopDebuffLoading(id);
        _loadingSoundIds.Remove(window);
    }
}
