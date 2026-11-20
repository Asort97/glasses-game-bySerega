using System;

// Accessed by the mixer thread, or by the main thread while FMOD's DSP is locked.
internal sealed class PcmFreezeBuffer
{
    public const int MaxChannels = 32;
    private readonly float[] _history;
    private readonly int _capacityFrames;
    private int _channels;
    private int _writeFrame;
    private int _recordedFrames;
    private int _loopStart;
    private int _loopFrames;
    private int _loopPosition;
    private float _gain;
    private readonly float[][] _earLoops;
    private readonly int[] _earLength = new int[2], _earPosition = new int[2];
    private readonly float[] _earGain = new float[2];
    public bool IsFrozen { get; private set; }

    public PcmFreezeBuffer(int capacityFrames)
    {
        _capacityFrames = Math.Max(1, capacityFrames);
        _history = new float[_capacityFrames * MaxChannels];
        _earLoops = new[] { new float[_capacityFrames], new float[_capacityFrames] };
    }

    public void FreezeEar(int ear, int frames, float gain)
    {
        if (_channels < 2) return;
        int count = Math.Min(Math.Max(frames, 1), _recordedFrames);
        int start = _writeFrame - count + _capacityFrames;
        for (int i = 0; i < count; i++)
            _earLoops[ear][i] = _history[((start + i) % _capacityFrames) * _channels + ear];
        _earLength[ear] = count;
        _earPosition[ear] = 0;
        _earGain[ear] = gain;
    }

    public void ResumeEar(int ear) => _earLength[ear] = 0;

    public void Freeze(int frames, float gain)
    {
        if (IsFrozen) return;
        _loopFrames = Math.Min(Math.Max(1, frames), _recordedFrames);
        _loopStart = (_writeFrame - _loopFrames + _capacityFrames) % _capacityFrames;
        _loopPosition = 0;
        _gain = gain;
        IsFrozen = true;
    }

    public void Resume()
    {
        IsFrozen = false;
        _recordedFrames = 0;
    }

    public void Process(float[] samples, int frames, int channels)
    {
        if (_channels != channels)
        {
            _channels = channels;
            _writeFrame = _recordedFrames = _loopFrames = _loopPosition = 0;
            _earLength[0] = _earLength[1] = 0;
        }

        for (int frame = 0; frame < frames; frame++)
        {
            int offset = frame * channels;
            if (IsFrozen)
            {
                int source = ((_loopStart + _loopPosition) % _capacityFrames) * channels;
                for (int channel = 0; channel < channels; channel++)
                    samples[offset + channel] = _loopFrames > 0 ? _history[source + channel] * _gain : 0f;
                if (_loopFrames > 0)
                    _loopPosition = (_loopPosition + 1) % _loopFrames;
            }
            else
            {
                Array.Copy(samples, offset, _history, _writeFrame * channels, channels);
                _writeFrame = (_writeFrame + 1) % _capacityFrames;
                _recordedFrames = Math.Min(_recordedFrames + 1, _capacityFrames);
                // Record dry input so simultaneous skips never capture another skip's loop.
                for (int ear = 0; ear < 2; ear++)
                {
                    int count = _earLength[ear];
                    if (count == 0) continue;
                    samples[offset + ear] = _earLoops[ear][_earPosition[ear]] * _earGain[ear];
                    if (++_earPosition[ear] == count) _earPosition[ear] = 0;
                }
            }
        }
    }
}
