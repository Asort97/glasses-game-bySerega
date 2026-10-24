using System;
using System.Runtime.InteropServices;
using FMODUnity;
using UnityEngine;

// Records the final FMOD mix continuously, then replaces it with a short frozen loop.
public sealed class RealityAudioFreeze : MonoBehaviour
{
    [Range(10f, 200f)] [SerializeField] private float loopMilliseconds = 50f;
    [Range(0f, 1f)] [SerializeField] private float freezeVolume = 1f;

    private static readonly FMOD.DSP_READ_CALLBACK ReadCallback = Read;
    private static readonly FMOD.DSP_SHOULDIPROCESS_CALLBACK ProcessCallback = ShouldProcess;
    private FMOD.System _system;
    private FMOD.ChannelGroup _master;
    private FMOD.DSP _dsp;
    private GCHandle _handle;
    private MixerState _state;
    private int _sampleRate;

    private sealed class MixerState
    {
        public PcmFreezeBuffer History;
        public float[] Scratch;
    }

    private void OnEnable()
    {
        try
        {
            _system = RuntimeManager.CoreSystem;
            Check(_system.getSoftwareFormat(out _sampleRate, out _, out _));
            Check(_system.getDSPBufferSize(out uint blockFrames, out _));
            _state = new MixerState
            {
                History = new PcmFreezeBuffer(Mathf.CeilToInt(_sampleRate * 0.2f)),
                Scratch = new float[(int)blockFrames * PcmFreezeBuffer.MaxChannels]
            };
            _handle = GCHandle.Alloc(_state);
            var description = new FMOD.DSP_DESCRIPTION
            {
                name = new byte[32],
                numinputbuffers = 1,
                numoutputbuffers = 1,
                read = ReadCallback,
                shouldiprocess = ProcessCallback,
                userdata = GCHandle.ToIntPtr(_handle)
            };
            System.Text.Encoding.ASCII.GetBytes("Reality Audio Freeze").CopyTo(description.name, 0);
            Check(_system.getMasterChannelGroup(out _master));
            Check(_system.createDSP(ref description, out _dsp));
            Check(_master.addDSP(FMOD.CHANNELCONTROL_DSP_INDEX.HEAD, _dsp));
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
            Release();
        }
    }

    public void BeginFreeze()
    {
        if (!_dsp.hasHandle() || _system.lockDSP() != FMOD.RESULT.OK) return;
        try
        {
            _state.History.Freeze(Mathf.RoundToInt(_sampleRate * Mathf.Clamp(loopMilliseconds, 10f, 200f) / 1000f),
                Mathf.Clamp01(freezeVolume));
        }
        finally { _system.unlockDSP(); }
    }

    public void EndFreeze()
    {
        if (!_dsp.hasHandle() || _system.lockDSP() != FMOD.RESULT.OK) return;
        try { _state.History.Resume(); }
        finally { _system.unlockDSP(); }
    }

    [AOT.MonoPInvokeCallback(typeof(FMOD.DSP_READ_CALLBACK))]
    private static FMOD.RESULT Read(ref FMOD.DSP_STATE dspState, IntPtr input, IntPtr output,
        uint length, int inChannels, ref int outChannels)
    {
        var dsp = new FMOD.DSP { handle = dspState.instance };
        if (inChannels <= 0 || inChannels > PcmFreezeBuffer.MaxChannels ||
            dsp.getUserData(out IntPtr userData) != FMOD.RESULT.OK || userData == IntPtr.Zero)
            return FMOD.RESULT.ERR_DSP_DONTPROCESS;

        var state = (MixerState)GCHandle.FromIntPtr(userData).Target;
        outChannels = inChannels;
        int processedFrames = 0;
        while (processedFrames < (int)length)
        {
            int frames = Math.Min((int)length - processedFrames, state.Scratch.Length / inChannels);
            int samples = frames * inChannels;
            int byteOffset = processedFrames * inChannels * sizeof(float);
            Marshal.Copy(IntPtr.Add(input, byteOffset), state.Scratch, 0, samples);
            state.History.Process(state.Scratch, frames, inChannels);
            Marshal.Copy(state.Scratch, 0, IntPtr.Add(output, byteOffset), samples);
            processedFrames += frames;
        }
        return FMOD.RESULT.OK;
    }

    [AOT.MonoPInvokeCallback(typeof(FMOD.DSP_SHOULDIPROCESS_CALLBACK))]
    private static FMOD.RESULT ShouldProcess(ref FMOD.DSP_STATE state, bool idle, uint length,
        FMOD.CHANNELMASK mask, int channels, FMOD.SPEAKERMODE mode)
    {
        // Keep generating the frozen loop even after game-over stops all source events.
        return FMOD.RESULT.OK;
    }

    private void OnDisable() => Release();

    private void Release()
    {
        if (_dsp.hasHandle())
        {
            _master.removeDSP(_dsp);
            FMOD.RESULT result = _dsp.release();
            _dsp.clearHandle();
            // Never free callback data while a native DSP might still reference it.
            if (result != FMOD.RESULT.OK && result != FMOD.RESULT.ERR_INVALID_HANDLE)
            {
                Debug.LogError("Reality audio DSP release failed: " + result, this);
                return;
            }
        }
        if (_handle.IsAllocated) _handle.Free();
        _state = null;
    }

    private static void Check(FMOD.RESULT result)
    {
        if (result != FMOD.RESULT.OK)
            throw new InvalidOperationException("Reality audio freeze: " + result);
    }
}
