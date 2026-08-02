using System;
using System.Collections;
using UnityEngine;

public sealed class SynchronizedLensCutscene : MonoBehaviour
{
    [Serializable]
    private sealed class Stage
    {
        [SerializeField] private GameObject leftState;
        [SerializeField] private GameObject rightState;
        [Min(0f)] [SerializeField] private float delayBeforeShow = 0.2f;
        [Min(0f)] [SerializeField] private float displayDuration = 1f;

        public GameObject LeftState => leftState;
        public GameObject RightState => rightState;
        public float DelayBeforeShow => delayBeforeShow;
        public float DisplayDuration => displayDuration;
    }

    [SerializeField] private GameObject leftRoot;
    [SerializeField] private GameObject rightRoot;
    [SerializeField] private Stage[] stages;

    private void Awake()
    {
        StopAndHide();
    }

    public IEnumerator Play()
    {
        HideAllStates();
        leftRoot.SetActive(true);
        rightRoot.SetActive(true);

        foreach (Stage stage in stages)
        {
            if (stage == null)
                continue;

            HideAllStates();

            if (stage.DelayBeforeShow > 0f)
                yield return new WaitForSeconds(stage.DelayBeforeShow);

            stage.LeftState.SetActive(true);
            stage.RightState.SetActive(true);

            if (stage.DisplayDuration > 0f)
                yield return new WaitForSeconds(stage.DisplayDuration);
        }

        StopAndHide();
    }

    public void StopAndHide()
    {
        HideAllStates();
        leftRoot.SetActive(false);
        rightRoot.SetActive(false);
    }

    private void HideAllStates()
    {
        if (stages == null)
            return;

        foreach (Stage stage in stages)
        {
            if (stage == null)
                continue;

            if (stage.LeftState != null)
                stage.LeftState.SetActive(false);
            if (stage.RightState != null)
                stage.RightState.SetActive(false);
        }
    }
}
