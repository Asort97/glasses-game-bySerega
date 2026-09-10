using System.Collections;
using UnityEngine;

public sealed class ButtonPressAnimation : MonoBehaviour
{
    [SerializeField] private SpriteRenderer targetRenderer;
    [SerializeField] private Sprite[] frames;
    [SerializeField, Min(0.01f)] private float frameInterval = 0.06f;

    private Coroutine _animationRoutine;

    private void OnEnable()
    {
        ShowFirstFrame();
    }

    public void Play()
    {
        if (targetRenderer == null || frames == null || frames.Length == 0)
            return;

        if (_animationRoutine != null)
            StopCoroutine(_animationRoutine);

        _animationRoutine = StartCoroutine(PlayRoutine());
    }

    private IEnumerator PlayRoutine()
    {
        for (int i = 0; i < frames.Length; i++)
        {
            if (frames[i] != null)
                targetRenderer.sprite = frames[i];

            yield return new WaitForSeconds(frameInterval);
        }

        _animationRoutine = null;
    }

    private void ShowFirstFrame()
    {
        _animationRoutine = null;

        if (targetRenderer != null && frames != null && frames.Length > 0 && frames[0] != null)
            targetRenderer.sprite = frames[0];
    }
}
