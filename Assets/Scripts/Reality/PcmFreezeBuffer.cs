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
    public bool IsFrozen { get; private set; }

    public PcmFreezeBuffer(int capacityFrames)
    {
        _capacityFrames = Math.Max(1, capacityFrames);
        _history = new float[_capacityFrames * MaxChannels];
    }

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
            }
        }
    }
}
