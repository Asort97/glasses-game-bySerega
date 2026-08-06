using UnityEngine;

public class LensHealthSystem : MonoBehaviour
{
    public enum LensButton
    {
        Space,
        LeftMouseButton
    }

    [Header("Settings")]
    [SerializeField] private LensButton activationButton = LensButton.Space;
    [SerializeField] private int maxHP = 3;
    [SerializeField] private LensMinigameManager manager;
    [SerializeField] private LensGameOverController gameOverController;

    [Header("Hearts")]
    [SerializeField] private LensHeartsView heartsView;
    [SerializeField] private bool loseFromLeft;

    [Header("Lens")]
    [SerializeField] private Renderer lensRenderer;
    [SerializeField] private string colorProperty = "_BaseColor";
    [SerializeField] private Renderer otherLensNoiseRenderer;
    [SerializeField] private string noiseStrengthProperty = "_TVNoiseStrength";
    [SerializeField] private float recoveryDuration = 5f;

    private const int Phase1Target = 7;
    private const int Phase2Target = 8;

    private int _hp;
    private bool _broken;
    private bool _permanentlyBroken;
    private bool _gameOver;
    private float _recoveryTimer;
    private int _pressCount;
    private bool _phase2;
    private Material _otherLensMaterial;

    public bool IsBroken => _broken;
    public bool IsPermanentlyBroken => _permanentlyBroken;
    public int CurrentHp => _hp;
    public bool LoseFromLeft => loseFromLeft;

    private void Awake()
    {
        _hp = maxHP;
        CacheOtherLensMaterial();

        if (heartsView != null)
            heartsView.Hide();
    }

    public void OnLose()
    {
        if (_broken || _gameOver)
            return;

        _hp = Mathf.Max(0, _hp - 1);
        _permanentlyBroken = _hp == 0;
        BreakLens();
    }

    private void BreakLens()
    {
        _broken = true;
        _pressCount = 0;
        _phase2 = false;
        _recoveryTimer = 0f;
        SetOtherLensNoise(0f);

        SetLensColor(_permanentlyBroken ? Color.black : Color.white);
        LensAudioService.Instance.PlayTVon(false, loseFromLeft ? -1f : 1f);

        if (manager != null)
            manager.SetPaused(true);

        RefreshHeartsUI();

        if (gameOverController != null)
            gameOverController.NotifyLensBroken(this);
    }

    private void Update()
    {
        if (!_broken || _gameOver)
            return;

        _recoveryTimer += Time.deltaTime;
        SetOtherLensNoise(Mathf.Clamp01(_recoveryTimer / Mathf.Max(0.01f, recoveryDuration)));

        if (_recoveryTimer >= recoveryDuration)
        {
            if (gameOverController != null)
                gameOverController.TriggerGameOver();
            return;
        }

        if (_permanentlyBroken)
            return;

        bool pressed = activationButton == LensButton.Space
            ? Input.GetKeyDown(KeyCode.Space)
            : Input.GetMouseButtonDown(0);

        if (!pressed)
            return;

        if (heartsView != null)
            heartsView.Shake();

        LensAudioService.Instance.PlayHeartClick();
        _pressCount++;

        if (!_phase2 && _pressCount >= Phase1Target)
        {
            _phase2 = true;
            _pressCount = 0;
            SetAllHeartsState(1);
        }
        else if (_phase2 && _pressCount >= Phase2Target)
        {
            LensAudioService.Instance.PlayHeartHalf();
            RestoreLens();
        }
    }

    private void RestoreLens()
    {
        if (_permanentlyBroken)
            return;

        _broken = false;
        _recoveryTimer = 0f;
        SetOtherLensNoise(0f);
        SetLensColor(Color.white);
        LensAudioService.Instance.PlayTVon(true, loseFromLeft ? -1f : 1f);

        if (heartsView != null)
            heartsView.Hide();

        if (manager != null)
            manager.SetPaused(false);

        if (gameOverController != null)
            gameOverController.NotifyLensRestored(this);
    }

    public void StopForGameOver()
    {
        _gameOver = true;
        _broken = true;
        SetOtherLensNoise(0f);
        SetLensColor(Color.white);

        if (heartsView != null)
            heartsView.Hide();

        if (manager != null)
        {
            manager.SetPaused(true);
            manager.ShowGameOverVisual();
        }
    }

    public void CompleteGameOverShutdown()
    {
        SetLensColor(Color.black);
    }

    public void ResetHealth()
    {
        _hp = maxHP;
        _broken = false;
        _permanentlyBroken = false;
        _gameOver = false;
        _recoveryTimer = 0f;
        _pressCount = 0;
        _phase2 = false;

        SetOtherLensNoise(0f);
        SetLensColor(Color.white);

        if (heartsView != null)
            heartsView.Hide();

        LensAudioService.Instance.PlayTVon(true, loseFromLeft ? -1f : 1f);
    }

    public void ShowSingleHeartForBoss()
    {
        if (heartsView != null)
            heartsView.ShowSingleHeart(_hp, loseFromLeft);
    }

    public void RestoreHeartsAfterBoss()
    {
        if (heartsView != null)
            heartsView.Hide();
    }

    private void RefreshHeartsUI()
    {
        if (heartsView == null)
            return;

        if (_permanentlyBroken)
        {
            heartsView.Hide();
            return;
        }

        heartsView.ShowAfterLoss(_hp, loseFromLeft, _broken ? 2 : 0);
    }

    private void SetAllHeartsState(int state)
    {
        if (heartsView != null)
            heartsView.ShowAfterLoss(_hp, loseFromLeft, state);
    }

    private void SetLensColor(Color color)
    {
        if (lensRenderer == null)
            return;

        Material material = lensRenderer.material;
        if (material.HasProperty(colorProperty))
            material.SetColor(colorProperty, color);
        else if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);
    }

    private void CacheOtherLensMaterial()
    {
        if (otherLensNoiseRenderer != null)
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
