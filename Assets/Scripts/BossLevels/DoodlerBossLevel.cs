using UnityEngine;

public class DoodlerBossLevel : BossLevelBase
{
    [SerializeField] private GameObject leftBossRoot;
    [SerializeField] private GameObject rightBossRoot;
    [SerializeField] private DoodlerPlatformSystem platformSystem;
    [SerializeField] private Doodler doodler;

    [Header("Preview")]
    [SerializeField] private GameObject leftPreviewTitle;
    [SerializeField] private GameObject rightPreviewTitle;
    [SerializeField] private GameObject leftPreviewTitleName;
    [SerializeField] private GameObject rightPreviewTitleName;
    [Min(0f)]
    [SerializeField] private float previewImageDuration = 2f;
    [Min(0f)]
    [SerializeField] private float previewAnimationDuration = 3f;
    [Min(0f)]
    [SerializeField] private float blankAfterAnimationDuration = 0.2f;
    [Min(0f)]
    [SerializeField] private float namePreviewDuration = 2f;
    [Min(0f)]
    [SerializeField] private float blankBeforeBossDuration = 0.2f;
    [SerializeField] private Animator leftPreviewAnimator;
    [SerializeField] private Animator rightPreviewAnimator;
    [Min(1)]
    [SerializeField] private int platformsToWin = 8;

    private int _platformsCleared;

    public override float PreviewImageDuration => previewImageDuration;
    public override float PreviewAnimationDuration => previewAnimationDuration;
    public override float BlankAfterAnimationDuration => blankAfterAnimationDuration;
    public override float NamePreviewDuration => namePreviewDuration;
    public override float BlankBeforeBossDuration => blankBeforeBossDuration;

    public override void ShowPreviewImage()
    {
        leftPreviewTitle.SetActive(true);
        rightPreviewTitle.SetActive(true);
        SetPreviewAnimatorsEnabled(false);
    }

    public override void PlayPreviewAnimation()
    {
        SetPreviewAnimatorsEnabled(true);
    }

    public override void HidePreview()
    {
        leftPreviewTitle.SetActive(false);
        rightPreviewTitle.SetActive(false);
        SetPreviewAnimatorsEnabled(false);
    }

    public override void ShowNamePreview()
    {
        leftPreviewTitleName.SetActive(true);
        rightPreviewTitleName.SetActive(true);
    }

    public override void HideNamePreview()
    {
        leftPreviewTitleName.SetActive(false);
        rightPreviewTitleName.SetActive(false);
    }

    public override void StartBoss()
    {
        _platformsCleared = 0;
        platformSystem.OnNewPlatformLanded += HandlePlatformLanded;
        HidePreview();
        HideNamePreview();
        leftBossRoot.SetActive(true);
        rightBossRoot.SetActive(true);
        doodler.ResetForBoss();
        base.StartBoss();
    }

    public override void StopBoss()
    {
        platformSystem.OnNewPlatformLanded -= HandlePlatformLanded;
        HidePreview();
        HideNamePreview();
        leftBossRoot.SetActive(false);
        rightBossRoot.SetActive(false);
        base.StopBoss();
    }

    private void HandlePlatformLanded(DoodlerPlatform platform)
    {
        _platformsCleared++;
        if (_platformsCleared >= platformsToWin)
            CompleteBoss();
    }

    private void SetPreviewAnimatorsEnabled(bool enabled)
    {
        if (leftPreviewAnimator != null)
            leftPreviewAnimator.enabled = enabled;
        if (rightPreviewAnimator != null)
            rightPreviewAnimator.enabled = enabled;
    }
}
