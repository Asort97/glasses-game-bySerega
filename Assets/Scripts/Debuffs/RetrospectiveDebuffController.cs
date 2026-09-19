using System.Collections;
using UnityEngine;

public sealed class RetrospectiveDebuffController : MonoBehaviour
{
    private const int NormalScoreDelta = 1;
    private const int RetrospectiveScoreDelta = -1;

    [Header("Scene References")]
    [SerializeField] private DebuffSpawner debuffSpawner;
    [SerializeField] private GameScoreSystem gameScoreSystem;

    [Header("Timing")]
    [Min(0.1f)] [SerializeField] private float duration = 30f;

    private Coroutine _routine;

    private void OnEnable()
    {
        debuffSpawner.DebuffApplied += HandleDebuffApplied;
        debuffSpawner.RunReset += StopAndRestore;
    }

    private void OnDisable()
    {
        debuffSpawner.DebuffApplied -= HandleDebuffApplied;
        debuffSpawner.RunReset -= StopAndRestore;
        StopAndRestore();
    }

    private void HandleDebuffApplied(DebuffId id)
    {
        if (id == DebuffId.Retrospective)
            Play();
    }

    [ContextMenu("Play Retrospective")]
    public void Play()
    {
        if (_routine != null)
            StopCoroutine(_routine);

        gameScoreSystem.SetRegularMinigameScoreDelta(RetrospectiveScoreDelta);
        _routine = StartCoroutine(DurationRoutine());
    }

    private IEnumerator DurationRoutine()
    {
        yield return new WaitForSecondsRealtime(duration);
        _routine = null;
        gameScoreSystem.SetRegularMinigameScoreDelta(NormalScoreDelta);
    }

    private void StopAndRestore()
    {
        if (_routine != null)
        {
            StopCoroutine(_routine);
            _routine = null;
        }

        gameScoreSystem.SetRegularMinigameScoreDelta(NormalScoreDelta);
    }
}
