using System.Collections;
using UnityEngine;

public sealed class ButtonInputMinigame : MinigameBase
{
    public enum Rule
    {
        Press,
        DontPress
    }

    public enum InputMode
    {
        Space,
        LeftMouseButton
    }

    [Header("Rules")]
    [SerializeField] private Rule rule;
    [SerializeField] private InputMode inputMode;
    [SerializeField] private bool requirePointerOverButton;
    [SerializeField, Min(0f)] private float pressedResultDelay = 1f;

    [Header("Right Lens Click Mapping")]
    [SerializeField] private Camera screenCamera;
    [SerializeField] private Camera gameCamera;
    [SerializeField] private MeshCollider lensCollider;
    [SerializeField] private SpriteRenderer buttonRenderer;
    [SerializeField] private ButtonPressAnimation pressAnimation;

    private float _elapsed;
    private bool _isRunning;
    private Coroutine _pressedResultRoutine;

    public override void StartGame()
    {
        base.StartGame();
        _elapsed = 0f;
        _isRunning = true;
    }

    public override void StopGame()
    {
        _isRunning = false;

        if (_pressedResultRoutine != null)
        {
            StopCoroutine(_pressedResultRoutine);
            _pressedResultRoutine = null;
        }

        base.StopGame();
    }

    protected override void Update()
    {
        if (!_isRunning)
            return;

        _elapsed += Time.deltaTime;
        Progress = 1f - Mathf.Clamp01(_elapsed / Mathf.Max(0.01f, surviveTime));

        if (WasPressed() && CanAcceptPress())
        {
            if (pressAnimation != null)
                pressAnimation.Play();

            _isRunning = false;
            _pressedResultRoutine = StartCoroutine(
                CompleteAfterPressDelay(rule == Rule.Press));
            return;
        }

        if (_elapsed >= surviveTime)
            CompleteGame(rule == Rule.DontPress);
    }

    private bool WasPressed()
    {
        return inputMode == InputMode.Space
            ? Input.GetKeyDown(KeyCode.Space)
            : Input.GetMouseButtonDown(0);
    }

    private bool CanAcceptPress()
    {
        if (!requirePointerOverButton)
            return true;

        if (screenCamera == null || gameCamera == null || lensCollider == null || buttonRenderer == null)
            return false;

        Ray ray = screenCamera.ScreenPointToRay(Input.mousePosition);
        if (!lensCollider.Raycast(ray, out RaycastHit hit, 1000f))
            return false;

        Vector3 worldPoint = gameCamera.ViewportToWorldPoint(
            new Vector3(hit.textureCoord.x, hit.textureCoord.y, gameCamera.nearClipPlane));

        worldPoint.z = buttonRenderer.bounds.center.z;
        return buttonRenderer.bounds.Contains(worldPoint);
    }

    private void CompleteGame(bool won)
    {
        _isRunning = false;

        if (won)
            RaiseWin();
        else
            RaiseLose();
    }

    private IEnumerator CompleteAfterPressDelay(bool won)
    {
        if (pressedResultDelay > 0f)
            yield return new WaitForSeconds(pressedResultDelay);

        _pressedResultRoutine = null;
        CompleteGame(won);
    }
}
