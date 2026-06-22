using FMODUnity;
using FMOD.Studio;
using UnityEngine;

public class LensAudioService : MonoBehaviour
{
    [SerializeField] private EventReference winEvent;
    [SerializeField] private EventReference loseEvent;
    [SerializeField] private EventReference heartClicking;
    [SerializeField] private EventReference heartClickingHalf;
    [SerializeField] private EventReference heartClickingFinish;
    [SerializeField] private EventReference tvON;
    [SerializeField] private EventReference tvOFF;
    [SerializeField] private EventReference gameStart;
    [SerializeField] private EventReference menuTheme;
    [SerializeField] private EventReference click;
    private EventInstance _musicInstance;

    public static LensAudioService Instance;

    private void Awake()
    {
        if(!Instance)
        {
            Instance = this;
        }
    }

    private void Start()
    {
        PlayMusic(gameStart);
    }

    public void SwitchToMenuTheme()
    {
        StopMusic();
        PlayMusic(menuTheme);
    }

    public void PlayWin()
    {
        PlayOneShot(winEvent);
    }

    public void PlayLose()
    {
        PlayOneShot(loseEvent);
    }

    public void PlayHeartClick()
    {
        PlayOneShot(heartClicking);    
    }
    
    public void Click()
    {
        PlayOneShot(click);    
    }

    public void PlayHeartHalf()
    {
        PlayOneShot(heartClickingHalf);    
    }

    public void PlayTVon(bool active, float pan)
    {
        EventReference sound = active ? tvON : tvOFF;
        if (sound.IsNull)
            return;

        EventInstance instance = RuntimeManager.CreateInstance(sound);
        instance.setParameterByName("Pan", pan);
        instance.start();
        instance.release();
    }

    public void PlayHeartFinish()
    {
        PlayOneShot(heartClickingFinish);    
    }

    private void PlayOneShot(EventReference eventReference)
    {
        if (eventReference.IsNull)
            return;

        RuntimeManager.PlayOneShot(eventReference);
    }

    private void PlayMusic(EventReference music)
    {
        if (music.IsNull)
            return;

        _musicInstance = RuntimeManager.CreateInstance(music);
        _musicInstance.start();
    }

    private void StopMusic()
    {
        if (!_musicInstance.isValid())
            return;

        _musicInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        _musicInstance.release();
        _musicInstance = default;
    }

    private void OnDestroy()
    {
        StopMusic();

        if (Instance == this)
            Instance = null;
    }
}
