using System;
using FMODUnity;
using UnityEngine;

// Owns an event, its pitch correction and an optional isolated reverb send.
public sealed class AdjustedFmodOneShot : IDisposable
{
    private FMOD.Studio.EventInstance _instance;
    private FMOD.ChannelGroup _group;
    private FMOD.DSP _pitchShift;
    private FMOD.DSP _additionalPitchShift;
    private FMOD.DSP _reverb;
    private readonly float _reverbDecay;
    private readonly float _reverbWetLevel;
    private float _tailStartedAt = -1f;
    private readonly float _pitchCorrection;
    private readonly float _createdAt;
    private bool _ready;

    public AdjustedFmodOneShot(EventReference sound, float speed, float pitch, float volume = 1f,
        float reverbDecay = 0f, float reverbWetLevel = -6f)
    {
        if (speed <= 0f || pitch / speed < 0.25f || pitch / speed > 4f)
            throw new ArgumentOutOfRangeException(nameof(pitch), "Pitch/speed must be between 0.25 and 4.");
        _pitchCorrection = pitch / speed;
        _reverbDecay = reverbDecay > 0f ? Mathf.Clamp(reverbDecay, 0.1f, 10f) : 0f;
        _reverbWetLevel = Mathf.Clamp(reverbWetLevel, -80f, 0f);
        _createdAt = Time.realtimeSinceStartup;
        try
        {
            _instance = RuntimeManager.CreateInstance(sound);
            Check(_instance.setVolume(Mathf.Clamp01(volume)));
            Check(_instance.setPitch(speed));
            Check(_instance.setPaused(true));
            Check(_instance.start());
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    public bool Tick()
    {
        if (_tailStartedAt >= 0f)
            return Time.realtimeSinceStartup - _tailStartedAt < _reverbDecay * 1.5f + 0.1f;
        if (!_instance.isValid()) return false;
        if (!_ready)
        {
            // Studio creates the channel group asynchronously. Keep the sound silent until ready.
            if (_instance.getChannelGroup(out _group) != FMOD.RESULT.OK || !_group.hasHandle())
            {
                if (Time.realtimeSinceStartup - _createdAt > 2f)
                    throw new TimeoutException("FMOD one-shot channel group was not created.");
                return true;
            }
            // Each FMOD shifter supports 0.5..2; cascade two for deeper shifts without slowing playback.
            bool needsSecondStage = _pitchCorrection < 0.5f || _pitchCorrection > 2f;
            float stagePitch = needsSecondStage ? Mathf.Sqrt(_pitchCorrection) : _pitchCorrection;
            AddPitchShift(ref _pitchShift, stagePitch);
            if (needsSecondStage) AddPitchShift(ref _additionalPitchShift, stagePitch);
            if (_reverbDecay > 0f) AddReverbSend();
            Check(_instance.setPaused(false));
            _ready = true;
            return true;
        }
        Check(_instance.getPlaybackState(out FMOD.Studio.PLAYBACK_STATE state));
        if (state != FMOD.Studio.PLAYBACK_STATE.STOPPED) return true;
        if (!_reverb.hasHandle()) return false;
        // The wet path outlives the Studio event, so stopping the source cannot cut its tail.
        Check(_reverb.disconnectAll(true, false));
        _tailStartedAt = Time.realtimeSinceStartup;
        return true;
    }

    public void Dispose()
    {
        if (_instance.isValid()) _instance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        if (_reverb.hasHandle())
        {
            _reverb.disconnectAll(true, true);
            _reverb.release();
            _reverb.clearHandle();
        }
        ReleasePitchShift(ref _additionalPitchShift);
        ReleasePitchShift(ref _pitchShift);
        if (_instance.isValid()) _instance.release();
        _instance.clearHandle();
        _group.clearHandle();
    }

    private void AddPitchShift(ref FMOD.DSP dsp, float pitch)
    {
        Check(RuntimeManager.CoreSystem.createDSPByType(FMOD.DSP_TYPE.PITCHSHIFT, out dsp));
        Check(dsp.setParameterFloat((int)FMOD.DSP_PITCHSHIFT.PITCH, pitch));
        Check(_group.addDSP(FMOD.CHANNELCONTROL_DSP_INDEX.HEAD, dsp));
    }

    private void AddReverbSend()
    {
        Check(RuntimeManager.CoreSystem.createDSPByType(FMOD.DSP_TYPE.SFXREVERB, out _reverb));
        Check(_reverb.setParameterFloat((int)FMOD.DSP_SFXREVERB.DECAYTIME, _reverbDecay * 1000f));
        Check(_reverb.setParameterFloat((int)FMOD.DSP_SFXREVERB.DRYLEVEL, -80f));
        Check(_reverb.setParameterFloat((int)FMOD.DSP_SFXREVERB.WETLEVEL, _reverbWetLevel));
        Check(_reverb.setParameterFloat((int)FMOD.DSP_SFXREVERB.HIGHCUT, 6000f));

        Check(_group.getDSP(FMOD.CHANNELCONTROL_DSP_INDEX.HEAD, out FMOD.DSP source));
        Check(_group.getParentGroup(out FMOD.ChannelGroup parent));
        Check(parent.getDSP(FMOD.CHANNELCONTROL_DSP_INDEX.TAIL, out FMOD.DSP destination));
        Check(destination.addInput(_reverb));
        Check(_reverb.addInput(source));
        Check(_reverb.setActive(true));
    }

    private void ReleasePitchShift(ref FMOD.DSP dsp)
    {
        if (dsp.hasHandle())
        {
            if (_group.hasHandle()) _group.removeDSP(dsp);
            dsp.release();
            dsp.clearHandle();
        }
    }

    private static void Check(FMOD.RESULT result)
    {
        if (result != FMOD.RESULT.OK)
            throw new InvalidOperationException("FMOD adjusted one-shot: " + result);
    }
}
