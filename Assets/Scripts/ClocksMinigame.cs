using UnityEngine;
using UnityEngine.EventSystems;

public sealed class ClocksMinigame : MinigameBase
{
    [SerializeField] private LensPointerProjection pointer;
    [SerializeField] private ClockGridLayout grid;
    [SerializeField] private ClockDial[] clocks;
    [Min(0.01f)] [SerializeField] private float minRevolutionDuration = 2f;
    [Min(0.01f)] [SerializeField] private float maxRevolutionDuration = 5f;
    [Range(0f, 1f)] [SerializeField] private float rewindPerClick = 0.05f;

    private float _elapsed;
    private bool _isPlaying;

    public override void StartGame()
    {
        StopGame();
        if (!pointer || !grid || clocks == null || clocks.Length != 3)
        {
            Debug.LogError("Clocks minigame needs a pointer, layout and three clocks.", this);
            return;
        }
        foreach (ClockDial clock in clocks)
            if (!clock || !clock.IsConfigured)
            {
                Debug.LogError("Clocks minigame has an unassigned dial or hand.", this);
                return;
            }
        if (!grid.Place(clocks))
        {
            Debug.LogError("Cannot fit the clocks: enlarge the placement area or reduce the gap.", this);
            return;
        }
        float min = Mathf.Max(0.01f, minRevolutionDuration);
        float max = Mathf.Max(min, maxRevolutionDuration);
        foreach (ClockDial clock in clocks) clock.ResetClock(Random.Range(min, max));
        base.StartGame();
        _elapsed = 0f;
        _isPlaying = true;
    }

    public override void StopGame()
    {
        _isPlaying = false;
        base.StopGame();
    }

    private void OnDisable() => StopGame();

    protected override void Update()
    {
        if (!_isPlaying) return;
        if (Input.GetMouseButtonDown(0) &&
            !(EventSystem.current && EventSystem.current.IsPointerOverGameObject()) &&
            pointer.TryGetPosition(clocks[0].FaceBounds.center.z, true, out Vector3 point))
            RewindAt(point);
        Tick(Time.deltaTime);
    }

    private void RewindAt(Vector3 point)
    {
        if (!_isPlaying) return;
        foreach (ClockDial clock in clocks)
            if (clock.Contains(point))
            {
                clock.Rewind(rewindPerClick);
                return;
            }
    }

    private void Tick(float deltaTime)
    {
        if (!_isPlaying) return;
        float duration = Mathf.Max(0.01f, surviveTime);
        float step = Mathf.Clamp(deltaTime, 0f, duration - _elapsed);
        _elapsed += step;
        Progress = 1f - Mathf.Clamp01(_elapsed / duration);
        bool expired = false;
        foreach (ClockDial clock in clocks) expired |= clock.Advance(step);
        if (expired)
        {
            StopGame();
            RaiseLose();
        }
        else if (_elapsed >= duration)
        {
            StopGame();
            RaiseWin();
        }
    }
}
