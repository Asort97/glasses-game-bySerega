using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// Система здоровья одной линзы.
/// - 3 HP. При потере HP: затемняет линзу (материал -> чёрный), блокирует мини-игру.
/// - Восстановление: нажатиями (Space для левой, LMB для правой).
///   7 нажатий -> сердца Health_1, ещё 8 -> сердца Health_0, линза включается.
/// Вешай на тот же объект что и LensMinigameManager (LeftGameContent / RightGameContent).
/// </summary>
public class LensHealthSystem : MonoBehaviour
{
    public enum LensButton { Space, LeftMouseButton }

    [Header("Настройки")]
    [SerializeField] private LensButton activationButton = LensButton.Space;
    [SerializeField] private int maxHP = 3;
    [SerializeField] private LensMinigameManager manager;
    [SerializeField] private LensGameOverController gameOverController;

    [Header("Сердца UI")]
    [SerializeField] private HeartWidget[]  hearts;           // 3 сердца
    [SerializeField] private RectTransform  heartsParent;     // LeftLensHearts / RightLensHearts
    [SerializeField] private bool            loseFromLeft;     // true = левая линза (теряет слева), false = правая (теряет справа)

    [Header("Линза")]
    [SerializeField] private Renderer lensRenderer;      // MeshRenderer на Lens1/Lens2
    [SerializeField] private string   colorProperty = "_BaseColor";
    [SerializeField] private Renderer otherLensNoiseRenderer;
    [SerializeField] private string   noiseStrengthProperty = "_TVNoiseStrength";
    [SerializeField] private float    recoveryDuration = 5f;

    private int  _hp;
    private bool _broken;          // линза сейчас выключена
    private bool _gameOver;
    private float _recoveryTimer;
    private Material _otherLensMaterial;

    public bool IsBroken => _broken;

    // Восстановление
    private int  _pressCount;
    private bool _phase2;          // прошли первые 7, идут следующие 8
    private const int Phase1Target = 7;
    private const int Phase2Target = 8;

    private void Awake()
    {
        _hp = maxHP;
        CacheOtherLensMaterial();
    }

    /// <summary>Вызывается из LensMinigameManager при поражении в мини-игре.</summary>
    public void OnLose()
    {
        if (_broken || _gameOver) return;

        _hp--;
        if (_hp < 0) _hp = 0;

        BreakLens(); // каждая потеря HP — линза гаснет и игры останавливаются
    }

    private void BreakLens()
    {
        _broken = true;
        _pressCount = 0;
        _phase2     = false;
        _recoveryTimer = 0f;
        SetOtherLensNoise(0f);

        // Затемнить линзу
        SetLensColor(Color.black);
        LensAudioService.Instance.PlayTVon(false, loseFromLeft ? -1f : 1f);
        
        // Остановить мини-игры
        if (manager != null) manager.SetPaused(true);

        // Все оставшиеся сердца -> Health_2
        RefreshHeartsUI();

        if (gameOverController != null)
            gameOverController.NotifyLensBroken(this);
    }

    private void Update()
    {
        if (!_broken || _gameOver) return;

        _recoveryTimer += Time.deltaTime;
        SetOtherLensNoise(Mathf.Clamp01(_recoveryTimer / Mathf.Max(0.01f, recoveryDuration)));

        if (_recoveryTimer >= recoveryDuration)
        {
            if (gameOverController != null)
                gameOverController.TriggerGameOver();
            return;
        }

        bool pressed = activationButton == LensButton.Space
            ? Input.GetKeyDown(KeyCode.Space)
            : Input.GetMouseButtonDown(0);

        if (!pressed) return;

        // Тряска родителя сердец
        ShakeHearts();

        LensAudioService.Instance.PlayHeartClick();

        _pressCount++;

        if (!_phase2 && _pressCount >= Phase1Target)
        {
            _phase2 = true;
            _pressCount = 0;
            // Сердца -> Health_1

            // LensAudioService.Instance.PlayHeartHalf();

            SetAllHeartsState(1);
        }
        else if (_phase2 && _pressCount >= Phase2Target)
        {

            LensAudioService.Instance.PlayHeartHalf();

            // LensAudioService.Instance.PlayHeartFinish();

            RestoreLens();
        }
    }

    private void RestoreLens()
    {
        _broken = false;
        _recoveryTimer = 0f;
        SetOtherLensNoise(0f);
        // _hp НЕ восстанавливается — потерянные сердца остаются потерянными

        // Вернуть белый цвет линзе
        SetLensColor(Color.white);
        LensAudioService.Instance.PlayTVon(true, loseFromLeft ? -1f : 1f);

        // Оставшиеся сердца -> Health_0
        RefreshHeartsUI();

        // Возобновить мини-игры
        if (manager != null) manager.SetPaused(false);

        if (gameOverController != null)
            gameOverController.NotifyLensRestored(this);
    }

    public void StopForGameOver()
    {
        _gameOver = true;
        SetOtherLensNoise(0f);
        if (manager != null)
            manager.SetPaused(true);
    }

    private void RefreshHeartsUI()
    {
        if (hearts == null) return;
        int lost = maxHP - _hp; // сколько сердец потеряно
        for (int i = 0; i < hearts.Length; i++)
        {
            if (hearts[i] == null) continue;
            // loseFromLeft: скрываем первые lost штук (индексы 0..lost-1)
            // loseFromRight: скрываем последние lost штук (индексы length-lost..length-1)
            bool hidden = loseFromLeft ? (i < lost) : (i >= hearts.Length - lost);
            if (hidden)
            {
                hearts[i].gameObject.SetActive(false);
            }
            else
            {
                hearts[i].gameObject.SetActive(true);
                hearts[i].SetState(_broken ? 2 : 0);
            }
        }
    }

    private void SetAllHeartsState(int state)
    {
        if (hearts == null) return;
        foreach (var h in hearts)
            if (h != null && h.gameObject.activeSelf) h.SetState(state);
    }

    private void ShakeHearts()
    {
        if (hearts == null) return;
        foreach (var h in hearts)
            if (h != null && h.gameObject.activeSelf) h.Shake();
    }

    private void SetLensColor(Color color)
    {
        if (lensRenderer == null) return;
        // Создаём instance материала чтобы не трогать шаред
        var mat = lensRenderer.material;
        if (mat.HasProperty(colorProperty))
            mat.SetColor(colorProperty, color);
        else if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", color);
    }

    private void CacheOtherLensMaterial()
    {
        if (otherLensNoiseRenderer == null)
            return;

        _otherLensMaterial = otherLensNoiseRenderer.material;
    }

    private void SetOtherLensNoise(float value)
    {
        if (_otherLensMaterial == null)
            CacheOtherLensMaterial();

        if (_otherLensMaterial != null && _otherLensMaterial.HasProperty(noiseStrengthProperty))
            _otherLensMaterial.SetFloat(noiseStrengthProperty, value);
    }
}
