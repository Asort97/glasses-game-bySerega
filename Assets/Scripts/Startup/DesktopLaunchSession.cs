using System;
using System.IO;
using System.Threading;
using UnityEngine;

// A one-launch handshake with Play.exe; the screenshot is captured before Unity starts.
public sealed class DesktopLaunchSession : IDisposable
{
    private readonly EventWaitHandle _ready;
    private readonly EventWaitHandle _presented;
    private readonly string _capturePath;

    public static bool IsRequested { get; private set; }
    public static bool HasPresented => !IsRequested || _hasPresented;
    private static string _token;
    private static bool _hasPresented;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Initialize()
    {
        _token = null;
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i + 1 < args.Length; i++)
            if (args[i] == "--desktop-session")
                _token = args[i + 1];
#endif
        IsRequested = _token != null;
        _hasPresented = false;
    }

    public DesktopLaunchSession()
    {
        if (!Guid.TryParseExact(_token, "N", out _))
            throw new InvalidDataException("Invalid desktop launch session.");

        string prefix = @"Local\GlassesStartup_" + _token;
        _capturePath = Path.Combine(Path.GetTempPath(), "GlassesStartup_" + _token, "desktop.bgra");
        _ready = EventWaitHandle.OpenExisting(prefix + "_ready");
        try { _presented = EventWaitHandle.OpenExisting(prefix + "_presented"); }
        catch { _ready.Dispose(); throw; }
    }

    public Texture2D LoadCapture()
    {
        using (var stream = File.OpenRead(_capturePath))
        using (var reader = new BinaryReader(stream))
        {
            int width = reader.ReadInt32();
            int height = reader.ReadInt32();
            if (width <= 0 || height <= 0 || width > SystemInfo.maxTextureSize || height > SystemInfo.maxTextureSize)
                throw new InvalidDataException("Unsupported desktop capture dimensions.");
            int length = checked(width * height * 4);
            if (stream.Length != length + 8L)
                throw new InvalidDataException("Incomplete desktop capture.");

            byte[] pixels = reader.ReadBytes(length);
            var texture = new Texture2D(width, height, TextureFormat.BGRA32, false);
            texture.LoadRawTextureData(pixels);
            texture.Apply(false, true);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Point;
            return texture;
        }
    }

    public void SignalFrameReady() => _ready.Set();

    public bool PollPresented()
    {
        if (_presented.WaitOne(0))
            _hasPresented = true;
        return HasPresented;
    }

    public void Dispose()
    {
        _ready.Dispose();
        _presented.Dispose();
    }
}
