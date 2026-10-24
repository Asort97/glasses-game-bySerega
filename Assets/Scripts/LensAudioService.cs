using FMODUnity;
using FMOD.Studio;
using System.Collections.Generic;
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
    [SerializeField] private EventReference eye;
    [SerializeField] private EventReference cantCloseDebuff;
    [SerializeField] private EventReference closeDebuff;
    [SerializeField] private EventReference loadingDebuff;

    [Header("Win Reverb")]
    [Range(0.1f, 10f)] [SerializeField] private float winReverbDecay = 1.8f;
    [Range(-80f, 0f)] [SerializeField] private float winReverbWetLevel = -6f;
    private EventInstance _musicInstance;
    private EventInstance _eyeInstance;
    private readonly Dictionary<int, EventInstance> _debuffLoadingInstances = new Dictionary<int, EventInstance>();
    private int _nextDebuffLoadingId;
    private readonly List<AdjustedFmodOneShot> _adjustedOneShots = new List<AdjustedFmodOneShot>();

    public static LensAudioService Instance;

    private void Awake()
    {
        if(!Instance)
        {
            Instance = this;
        }
    }

    private System.Collections.IEnumerator Start()
    {
        while (!DesktopLaunchSession.HasPresented)
            yield return null;
        if (!_musicInstance.isValid())
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

    public void PlayWin(float speed, float pitch, bool reverb = false)
    {
        if (winEvent.IsNull) return;
        if (!reverb && Mathf.Approximately(speed, 1f) && Mathf.Approximately(pitch, 1f))
            PlayWin();
        else
            PlayAdjustedWin(speed, pitch, reverb);
    }

    private void PlayAdjustedWin(float speed, float pitch, bool reverb)
    {
        try
        {
            _adjustedOneShots.Add(new AdjustedFmodOneShot(winEvent, speed, pitch,
                reverbDecay: reverb ? winReverbDecay : 0f, reverbWetLevel: winReverbWetLevel));
        }
        catch (System.Exception exception) { Debug.LogException(exception, this); }
    }

    private void Update()
    {
        for (int i = _adjustedOneShots.Count - 1; i >= 0; i--)
        {
            try { if (_adjustedOneShots[i].Tick()) continue; }
            catch (System.Exception exception) { Debug.LogException(exception, this); }
            _adjustedOneShots[i].Dispose();
            _adjustedOneShots.RemoveAt(i);
        }
    }

    private void OnDisable()
    {
        foreach (AdjustedFmodOneShot sound in _adjustedOneShots) sound.Dispose();
        _adjustedOneShots.Clear();
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

    public void PlayEye()
    {
        StopEye();
        if (eye.IsNull)
            return;

        _eyeInstance = RuntimeManager.CreateInstance(eye);
        _eyeInstance.start();
    }

    public void StopEye()
    {
        if (!_eyeInstance.isValid())
            return;

        _eyeInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        _eyeInstance.release();
        _eyeInstance = default;
    }

    public void PlayHeartFinish()
    {
        PlayOneShot(heartClickingFinish);    
    }

    public void PlayCloseDebuff()
    {
        PlayOneShot(closeDebuff);
    }

    public int StartDebuffLoading(float volume = 1f)
    {
        if (loadingDebuff.IsNull)
            return -1;

        EventInstance instance = RuntimeManager.CreateInstance(loadingDebuff);
        instance.setVolume(Mathf.Clamp01(volume));
        instance.start();

        int id = _nextDebuffLoadingId++;
        _debuffLoadingInstances.Add(id, instance);
        return id;
    }

    public void StopDebuffLoading(int id)
    {
        if (!_debuffLoadingInstances.TryGetValue(id, out EventInstance instance))
            return;

        if (instance.isValid())
        {
            instance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            instance.release();
        }

        _debuffLoadingInstances.Remove(id);
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
        OnDisable();
        StopAllDebuffLoading();
        StopEye();
        StopMusic();

        if (Instance == this)
            Instance = null;
    }

    private void StopAllDebuffLoading()
    {
        foreach (EventInstance instance in _debuffLoadingInstances.Values)
        {
            if (!instance.isValid())
                continue;

            instance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            instance.release();
        }

        _debuffLoadingInstances.Clear();
    }
}
