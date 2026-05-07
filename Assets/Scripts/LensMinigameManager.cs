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

    private int[]        _order;
    private int          _orderIndex;
    private MinigameBase _current;
    private int          _lastIdx = -1;

    private void Start()
    {
        // Скрываем все мини-игры
        foreach (var mg in minigames)
            if (mg != null) mg.gameObject.SetActive(false);

        BuildOrder(-1);
        ShowNext();
    }

    private void Update()
    {
        if (_current != null && timerFill != null)
            timerFill.fillAmount = _current.Progress;
    }

    private void ShowNext()
    {
        if (_orderIndex >= _order.Length)
            BuildOrder(_lastIdx);

        Detach(_current);

        int idx = _order[_orderIndex++];
        _lastIdx = idx;
        _current = minigames[idx];

        _current.gameObject.SetActive(true);
        _current.OnWin  += HandleEnd;
        _current.OnLose += HandleEnd;
        _current.StartGame();
    }

    private void HandleEnd()
    {
        MinigameBase finished = _current;

        // Сначала отписываемся
        Detach(finished);

        // Останавливаем и скрываем
        finished.StopGame();
        finished.gameObject.SetActive(false);

        // Показываем следующую
        ShowNext();
    }

    private void Detach(MinigameBase mg)
    {
        if (mg == null) return;
        mg.OnWin  -= HandleEnd;
        mg.OnLose -= HandleEnd;
    }

    /// <summary>
    /// Перемешивает порядок (Fisher-Yates).
    /// noFirstIdx — индекс который не должен оказаться первым (нет двух подряд).
    /// </summary>
    private void BuildOrder(int noFirstIdx)
    {
        _orderIndex = 0;
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
