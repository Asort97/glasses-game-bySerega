using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Менеджер пула мини-игр для одной линзы.
/// Вешается на LeftGameContent или RightGameContent.
/// Перемешивает игры (без двух одинаковых подряд), переключает через SetActive,
/// и обновляет fill bar таймера.
/// </summary>
public class LensMinigameManager : MonoBehaviour
{
    [SerializeField] private MinigameBase[] minigames;
    [SerializeField] private Image          timerFill;
    [SerializeField] private GameObject     previewTitle;
    [SerializeField] private float          blankDelay = 0.5f;
    [SerializeField] private float          previewDuration = 2f;

    private int[]            _order;
    private int              _orderIndex;
    private MinigameBase     _current;
    private int              _lastIdx = -1;
    private bool             _paused;
    private LensHealthSystem _health;
    private SpriteRenderer   _previewSpriteRenderer;
    private Image            _previewImage;
    private Coroutine        _switchRoutine;

    private void Start()
    {
        _health = GetComponent<LensHealthSystem>();
        ResolvePreviewTitle();
        HidePreviewTitle();

        // Скрываем все мини-игры
        foreach (var mg in minigames)
            if (mg != null) mg.gameObject.SetActive(false);

        BuildOrder(-1);
        QueueNext();
    }

    private void Update()
    {
        if (_current != null && timerFill != null)
            timerFill.fillAmount = _current.Progress;
        else if (timerFill != null)
            timerFill.fillAmount = 0f;
    }

    /// <summary>Приостановить/возобновить смену мини-игр (при потере HP).</summary>
    public void SetPaused(bool paused)
    {
        _paused = paused;
        if (paused)
        {
            StopSwitchRoutine();
            HidePreviewTitle();

            // Остановить текущую мини-игру и скрыть
            if (_current != null)
            {
                Detach(_current);
                _current.StopGame();
                _current.gameObject.SetActive(false);
                _current = null;
            }
        }
        else
        {
            // Возобновить — запустить следующую
            QueueNext();
        }
    }

    private void QueueNext()
    {
        StopSwitchRoutine();
        _switchRoutine = StartCoroutine(SwitchToNextRoutine());
    }

    private IEnumerator SwitchToNextRoutine()
    {
        HideAllMinigames();
        HidePreviewTitle();

        yield return new WaitForSeconds(blankDelay);

        if (_paused) yield break;

        MinigameBase next = PickNext();
        if (next == null)
            yield break;

        ShowPreviewTitle(next.PreviewTitleSprite);
        yield return new WaitForSeconds(previewDuration);

        HidePreviewTitle();
        if (_paused) yield break;

        StartMinigame(next);
        _switchRoutine = null;
    }

    private MinigameBase PickNext()
    {
        if (minigames == null || minigames.Length == 0)
        {
            Debug.LogError($"[{nameof(LensMinigameManager)}] No minigames assigned on {name}.", this);
            return null;
        }

        if (_order == null || _order.Length == 0 || _orderIndex >= _order.Length)
            BuildOrder(_lastIdx);

        Detach(_current);

        MinigameBase next = null;
        for (int attempts = 0; attempts < _order.Length; attempts++)
        {
            int idx = _order[_orderIndex++];
            if (_orderIndex >= _order.Length)
                BuildOrder(_lastIdx);

            if (idx < 0 || idx >= minigames.Length || minigames[idx] == null)
                continue;

            _lastIdx = idx;
            next = minigames[idx];
            break;
        }

        if (next == null)
        {
            Debug.LogError($"[{nameof(LensMinigameManager)}] All minigame slots are empty on {name}.", this);
            return null;
        }

        return next;
    }

    private void StartMinigame(MinigameBase next)
    {
        _current = next;
        _current.OnWin  += HandleWin;
        _current.OnLose += HandleLose;
        _current.gameObject.SetActive(true);
        _current.StartGame();
    }

    private void HandleWin()  => FinishGame(false);
    private void HandleLose() => FinishGame(true);

    private void FinishGame(bool isLose)
    {
        MinigameBase finished = _current;
        Detach(finished);
        if (finished != null)
        {
            finished.StopGame();
            finished.gameObject.SetActive(false);
        }

        if (_paused) return;

        if (isLose && _health != null)
        {
            _health.OnLose();
            if (_paused) return;
        }

        QueueNext();
    }

    private void StopSwitchRoutine()
    {
        if (_switchRoutine == null) return;
        StopCoroutine(_switchRoutine);
        _switchRoutine = null;
    }

    private void HideAllMinigames()
    {
        if (minigames == null) return;

        foreach (var mg in minigames)
        {
            if (mg == null) continue;
            if (mg != _current)
                Detach(mg);
            mg.StopGame();
            mg.gameObject.SetActive(false);
        }

        _current = null;
    }

    private void ResolvePreviewTitle()
    {
        if (previewTitle == null)
        {
            Transform preview = transform.Find("PreviewTitle");
            if (preview != null)
                previewTitle = preview.gameObject;
        }

        if (previewTitle == null) return;

        _previewSpriteRenderer = previewTitle.GetComponent<SpriteRenderer>();
        _previewImage = previewTitle.GetComponent<Image>();
    }

    private void ShowPreviewTitle(Sprite sprite)
    {
        if (previewTitle == null)
            ResolvePreviewTitle();
        if (previewTitle == null)
            return;

        if (_previewSpriteRenderer != null)
            _previewSpriteRenderer.sprite = sprite;
        if (_previewImage != null)
            _previewImage.sprite = sprite;

        previewTitle.SetActive(sprite != null);
    }

    private void HidePreviewTitle()
    {
        if (previewTitle == null)
            ResolvePreviewTitle();
        if (previewTitle != null)
            previewTitle.SetActive(false);
    }

    private void Detach(MinigameBase mg)
    {
        if (mg == null) return;
        mg.OnWin  -= HandleWin;
        mg.OnLose -= HandleLose;
    }

    /// <summary>
    /// Перемешивает порядок (Fisher-Yates).
    /// noFirstIdx — индекс который не должен оказаться первым (нет двух подряд).
    /// </summary>
    private void BuildOrder(int noFirstIdx)
    {
        _orderIndex = 0;
        if (minigames == null)
        {
            _order = new int[0];
            return;
        }

        _order = new int[minigames.Length];
        for (int i = 0; i < _order.Length; i++) _order[i] = i;

        // Fisher-Yates shuffle
        for (int i = _order.Length - 1; i > 0; i--)
        {
            int j   = Random.Range(0, i + 1);
            int tmp = _order[i];
            _order[i] = _order[j];
            _order[j] = tmp;
        }

        // Гарантируем: первый != предыдущий последний
        if (noFirstIdx >= 0 && _order.Length > 1 && _order[0] == noFirstIdx)
        {
            int swap = Random.Range(1, _order.Length);
            int tmp  = _order[0];
            _order[0]    = _order[swap];
            _order[swap] = tmp;
        }
    }
}
