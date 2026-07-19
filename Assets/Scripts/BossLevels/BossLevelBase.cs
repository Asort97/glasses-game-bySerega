using System;
using UnityEngine;

public abstract class BossLevelBase : MonoBehaviour
{
    public event Action<BossLevelBase> OnCompleted;
    public event Action<BossLevelBase> OnFailed;

    public bool IsRunning { get; private set; }

    public virtual float PreviewImageDuration => 0f;
    public virtual float PreviewAnimationDuration => 0f;

    public virtual void ShowPreviewImage()
    {
    }

    public virtual void PlayPreviewAnimation()
    {
    }

    public virtual void HidePreview()
    {
    }

    public virtual void StartBoss()
    {
        IsRunning = true;
    }

    public virtual void StopBoss()
    {
        IsRunning = false;
    }

    public void CompleteBoss()
    {
        if (!IsRunning)
            return;

        OnCompleted?.Invoke(this);
    }

    public void FailBoss()
    {
        if (!IsRunning)
            return;

        OnFailed?.Invoke(this);
    }
}
