using FMODUnity;
using UnityEngine;

public class LensAudioService : MonoBehaviour
{
    [SerializeField] private EventReference winEvent;
    [SerializeField] private EventReference loseEvent;

    public void PlayWin()
    {
        PlayOneShot(winEvent);
    }

    public void PlayLose()
    {
        PlayOneShot(loseEvent);
    }

    private void PlayOneShot(EventReference eventReference)
    {
        if (eventReference.IsNull)
            return;

        RuntimeManager.PlayOneShot(eventReference);
    }
}
