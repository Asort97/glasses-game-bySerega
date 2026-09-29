using System;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

public class DesktopScreenshotBackground : MonoBehaviour
{
    private const string LogPrefix = "[DesktopScreenshotBackground]";

    [SerializeField] private Camera targetCamera;
    [SerializeField] private MeshRenderer backgroundRenderer;
    [SerializeField] private float distanceFromCamera = 900f;
    [SerializeField] private bool forceFullscreen = true;
    [SerializeField] private bool captureVirtualDesktop;
    [SerializeField] private float captureDelayAfterHide = 0.2f;
    [SerializeField] private float targetDarkness = 0.7f;
    [SerializeField] private float darkenDuration = 5f;
    [SerializeField] private float saturation = 0.5f;
    [SerializeField] private float targetLightMultiplyStrength = 1f;
    [SerializeField] private float lightAppearDelay = 0.33f;
    [SerializeField] private bool saveDebugCapture;

    public bool IsReady { get; private set; }

    private Texture2D _desktopTexture;
    private Material _runtimeMaterial;
    private Coroutine _darkenRoutine;
    private Coroutine _lightRoutine;
    private DesktopLaunchSession _launchSession;
    private IntPtr _hiddenWindow;

    private const int SM_CXSCREEN = 0;
    private const int SM_CYSCREEN = 1;
    private const int SM_XVIRTUALSCREEN = 76;
    private const int SM_YVIRTUALSCREEN = 77;
    private const int SM_CXVIRTUALSCREEN = 78;
    private const int SM_CYVIRTUALSCREEN = 79;
    private const int BI_RGB = 0;
    private const int SW_HIDE = 0;
    private const int SW_SHOW = 5;
    private const uint SRCCOPY = 0x00CC0020;
    private const int DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = -4;

    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int command);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    [DllImport("user32.dll")]
    private static extern bool SetProcessDPIAware();

    [DllImport("user32.dll")]
    private static extern bool SetProcessDpiAwarenessContext(IntPtr dpiContext);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int cx, int cy);

    [DllImport("gdi32.dll")]
    private static extern IntPtr SelectObject(IntPtr hdc, IntPtr h);

    [DllImport("gdi32.dll")]
    private static extern bool BitBlt(IntPtr hdc, int x, int y, int cx, int cy, IntPtr hdcSrc, int x1, int y1, uint rop);

    [DllImport("gdi32.dll")]
    private static extern int GetDIBits(IntPtr hdc, IntPtr hbm, uint start, uint lines, byte[] bits, ref BitmapInfo bmi, uint usage);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr ho);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteDC(IntPtr hdc);

    [StructLayout(LayoutKind.Sequential)]
    private struct BitmapInfoHeader
    {
        public uint size;
        public int width;
        public int height;
        public ushort planes;
        public ushort bitCount;
        public uint compression;
        public uint sizeImage;
        public int xPelsPerMeter;
        public int yPelsPerMeter;
        public uint clrUsed;
        public uint clrImportant;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BitmapInfo
    {
        public BitmapInfoHeader header;
    }

    private void Awake()
    {
        Debug.Log($"{LogPrefix} Awake. targetCamera={(targetCamera != null ? targetCamera.name : "null")}, backgroundRenderer={(backgroundRenderer != null ? backgroundRenderer.name : "null")}");

        if (targetCamera == null)
        {
            targetCamera = GetComponent<Camera>();
            Debug.Log($"{LogPrefix} targetCamera from same GameObject: {(targetCamera != null ? targetCamera.name : "null")}");
        }

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        if (!DesktopLaunchSession.IsRequested)
        {
            _hiddenWindow = ResolveGameWindow();
            if (_hiddenWindow != IntPtr.Zero)
                ShowWindow(_hiddenWindow, SW_HIDE);
        }
#endif
    }

    private IEnumerator Start()
    {
        Debug.Log($"{LogPrefix} Start. platform={Application.platform}, editor={Application.isEditor}, forceFullscreen={forceFullscreen}, currentFullscreen={Screen.fullScreenMode}, resolution={Screen.width}x{Screen.height}");
        if (!DesktopLaunchSession.IsRequested)
            MakeProcessDpiAware();

        Texture2D screenshot = null;
        if (DesktopLaunchSession.IsRequested)
        {
            try
            {
                _launchSession = new DesktopLaunchSession();
                screenshot = _launchSession.LoadCapture();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                Application.Quit(1);
                yield break;
            }
        }
        else
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            yield return new WaitForSecondsRealtime(Mathf.Max(0.05f, captureDelayAfterHide));
#endif
            screenshot = CaptureDesktopPixels();
        }

        ApplyTexture(screenshot);
        if (_runtimeMaterial == null && _launchSession != null)
        {
            Application.Quit(1);
            yield break;
        }

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        // The launcher already chose the desktop resolution and borderless window mode.
        if (forceFullscreen && _launchSession == null && screenshot != null)
            Screen.SetResolution(screenshot.width, screenshot.height, FullScreenMode.FullScreenWindow);
#endif

        // Wait for rendering, not an arbitrary loading delay. Intro remains paused.
#if UNITY_EDITOR
        // EndOfFrame can suspend indefinitely when the editor displays Scene instead of Game.
        yield return null;
#else
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
#endif
        if (_launchSession != null)
        {
            _launchSession.SignalFrameReady();
            float deadline = Time.realtimeSinceStartup + 30f;
            while (!_launchSession.PollPresented())
            {
                if (Time.realtimeSinceStartup >= deadline)
                {
                    Debug.LogError($"{LogPrefix} Launcher did not present the ready frame.");
                    Application.Quit(1);
                    yield break;
                }
                yield return null;
            }
            _launchSession.Dispose();
            _launchSession = null;
        }
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        else if (_hiddenWindow != IntPtr.Zero)
        {
            ShowWindow(_hiddenWindow, SW_SHOW);
            _hiddenWindow = IntPtr.Zero;
        }
#endif
        IsReady = true;
        StartDarken();
        Debug.Log($"{LogPrefix} First background frame presented; intro may start.");
    }

    private void LateUpdate()
    {
        FitBackgroundToCamera();
    }

    private void OnDestroy()
    {
        _launchSession?.Dispose();
        if (_desktopTexture != null)
        {
            Debug.Log($"{LogPrefix} Destroy desktop texture.");
            Destroy(_desktopTexture);
        }

        if (_runtimeMaterial != null)
        {
            Debug.Log($"{LogPrefix} Destroy runtime material.");
            Destroy(_runtimeMaterial);
        }
    }

    private void ApplyTexture(Texture2D texture)
    {
        if (backgroundRenderer == null)
        {
            Debug.LogError($"{LogPrefix} Cannot apply texture: backgroundRenderer is null.");
            return;
        }

        _desktopTexture = texture;
        if (_desktopTexture == null)
        {
            Debug.LogError($"{LogPrefix} Cannot apply texture: captured texture is null.");
            return;
        }

        Material sourceMaterial = backgroundRenderer.sharedMaterial;
        if (sourceMaterial == null)
        {
            Debug.LogError($"{LogPrefix} Cannot apply texture: DesktopScreenshot renderer has no source material.");
            return;
        }

        _runtimeMaterial = new Material(sourceMaterial);
        _runtimeMaterial.mainTexture = _desktopTexture;
        if (_runtimeMaterial.HasProperty("_BaseMap"))
        {
            _runtimeMaterial.SetTexture("_BaseMap", _desktopTexture);
            Debug.Log($"{LogPrefix} Assigned texture to _BaseMap.");
        }
        else
        {
            Debug.Log($"{LogPrefix} Material has no _BaseMap, assigned only mainTexture.");
        }

        if (_runtimeMaterial.HasProperty("_BaseColor"))
            _runtimeMaterial.SetColor("_BaseColor", Color.white);

        SetSaturation(1f);
        SetDarkness(0f);
        SetLightMultiplyStrength(0f);

        backgroundRenderer.material = _runtimeMaterial;
        Debug.Log($"{LogPrefix} Applied texture to renderer '{backgroundRenderer.name}' material '{_runtimeMaterial.shader.name}'.");

        FitBackgroundToCamera();
    }

    public void PlayLensEnabledEffect()
    {
        Debug.Log($"{LogPrefix} PlayLensEnabledEffect.");
        StartLightMultiply();
    }

    public void CancelLightMultiplyTransition()
    {
        if (_lightRoutine == null)
            return;

        StopCoroutine(_lightRoutine);
        _lightRoutine = null;
    }

    public void CancelDarknessTransition()
    {
        if (_darkenRoutine == null)
            return;

        StopCoroutine(_darkenRoutine);
        _darkenRoutine = null;
    }

    public float GetLightMultiplyStrength()
    {
        if (_runtimeMaterial == null || !_runtimeMaterial.HasProperty("_LightMultiplyStrength"))
            return 0f;

        return _runtimeMaterial.GetFloat("_LightMultiplyStrength");
    }

    public float GetDarkness()
    {
        if (_runtimeMaterial == null || !_runtimeMaterial.HasProperty("_Darkness"))
            return 0f;

        return _runtimeMaterial.GetFloat("_Darkness");
    }

    public float GetSaturation()
    {
        if (_runtimeMaterial == null || !_runtimeMaterial.HasProperty("_Saturation"))
            return 1f;

        return _runtimeMaterial.GetFloat("_Saturation");
    }

    private static IntPtr ResolveGameWindow()
    {
        IntPtr processWindow = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
        if (processWindow != IntPtr.Zero)
        {
            Debug.Log($"{LogPrefix} Window handle from current process.");
            return processWindow;
        }

        IntPtr gameWindow = GetActiveWindow();
        GetWindowThreadProcessId(gameWindow, out uint processId);
        return processId == (uint)System.Diagnostics.Process.GetCurrentProcess().Id
            ? gameWindow : IntPtr.Zero;
    }

    private Texture2D CaptureDesktopPixels()
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        int x = captureVirtualDesktop ? GetSystemMetrics(SM_XVIRTUALSCREEN) : 0;
        int y = captureVirtualDesktop ? GetSystemMetrics(SM_YVIRTUALSCREEN) : 0;
        int width = captureVirtualDesktop ? GetSystemMetrics(SM_CXVIRTUALSCREEN) : GetSystemMetrics(SM_CXSCREEN);
        int height = captureVirtualDesktop ? GetSystemMetrics(SM_CYVIRTUALSCREEN) : GetSystemMetrics(SM_CYSCREEN);
        Debug.Log($"{LogPrefix} CaptureDesktopPixels rect x={x}, y={y}, width={width}, height={height}, virtual={captureVirtualDesktop}");

        if (width <= 0 || height <= 0)
        {
            Debug.LogError($"{LogPrefix} Invalid desktop size.");
            return null;
        }

        IntPtr screenDc = GetDC(IntPtr.Zero);
        IntPtr memoryDc = CreateCompatibleDC(screenDc);
        IntPtr bitmap = CreateCompatibleBitmap(screenDc, width, height);
        IntPtr oldBitmap = SelectObject(memoryDc, bitmap);
        Debug.Log($"{LogPrefix} GDI handles screenDc=0x{screenDc.ToInt64():X}, memoryDc=0x{memoryDc.ToInt64():X}, bitmap=0x{bitmap.ToInt64():X}");

        try
        {
            if (!BitBlt(memoryDc, 0, 0, width, height, screenDc, x, y, SRCCOPY))
            {
                Debug.LogError($"{LogPrefix} BitBlt failed.");
                return null;
            }

            Debug.Log($"{LogPrefix} BitBlt ok.");

            byte[] pixels = new byte[width * height * 4];
            BitmapInfo info = new BitmapInfo
            {
                header = new BitmapInfoHeader
                {
                    size = (uint)Marshal.SizeOf<BitmapInfoHeader>(),
                    width = width,
                    height = -height,
                    planes = 1,
                    bitCount = 32,
                    compression = BI_RGB,
                    sizeImage = (uint)pixels.Length
                }
            };

            SelectObject(memoryDc, oldBitmap);
            int copiedLines = GetDIBits(memoryDc, bitmap, 0, (uint)height, pixels, ref info, 0);
            Debug.Log($"{LogPrefix} GetDIBits copiedLines={copiedLines}");
            if (copiedLines == 0)
            {
                Debug.LogError($"{LogPrefix} GetDIBits failed.");
                return null;
            }

            Texture2D texture = new Texture2D(width, height, TextureFormat.BGRA32, false);
            texture.LoadRawTextureData(pixels);
            texture.Apply(false, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Point;
            Debug.Log($"{LogPrefix} Texture created and applied.");
            if (saveDebugCapture && (Application.isEditor || Debug.isDebugBuild))
                SaveDebugCapture(texture);
            return texture;
        }
        finally
        {
            SelectObject(memoryDc, oldBitmap);
            DeleteObject(bitmap);
            DeleteDC(memoryDc);
            ReleaseDC(IntPtr.Zero, screenDc);
        }
#else
        Debug.LogError($"{LogPrefix} Desktop capture is supported only on Windows standalone/editor.");
        return null;
#endif
    }

    private static void SaveDebugCapture(Texture2D texture)
    {
        try
        {
            string path = Path.Combine(Application.persistentDataPath, "desktop_capture_debug.png");
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Debug.Log($"{LogPrefix} Debug capture saved: {path}");
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"{LogPrefix} Failed to save debug capture: {exception.Message}");
        }
    }

    private static void MakeProcessDpiAware()
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        if (SetProcessDpiAwarenessContext(new IntPtr(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2)))
        {
            Debug.Log($"{LogPrefix} DPI awareness set: PerMonitorV2.");
            return;
        }

        if (SetProcessDPIAware())
            Debug.Log($"{LogPrefix} DPI awareness set: System DPI aware.");
        else
            Debug.LogWarning($"{LogPrefix} Failed to set DPI awareness.");
#endif
    }

    private void FitBackgroundToCamera()
    {
        if (targetCamera == null || backgroundRenderer == null)
        {
            Debug.LogWarning($"{LogPrefix} Cannot fit background: targetCamera or backgroundRenderer is null.");
            return;
        }

        Transform background = backgroundRenderer.transform;
        Transform cameraTransform = targetCamera.transform;

        background.position = cameraTransform.position + cameraTransform.forward * distanceFromCamera;
        background.rotation = cameraTransform.rotation;

        float height;
        if (targetCamera.orthographic)
            height = targetCamera.orthographicSize * 2f;
        else
            height = 2f * distanceFromCamera * Mathf.Tan(targetCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);

        float width = height * targetCamera.aspect;
        background.localScale = new Vector3(width, height, 1f);

        if (_runtimeMaterial != null)
            ApplyTextureTransform(width / height);
    }

    private void ApplyTextureTransform(float screenAspect)
    {
        if (_desktopTexture == null)
            return;

        float textureAspect = (float)_desktopTexture.width / _desktopTexture.height;
        Vector2 scale = Vector2.one;
        Vector2 offset = Vector2.zero;

        if (textureAspect > screenAspect)
        {
            scale.x = screenAspect / textureAspect;
            offset.x = (1f - scale.x) * 0.5f;
        }
        else if (textureAspect < screenAspect)
        {
            scale.y = textureAspect / screenAspect;
            offset.y = (1f - scale.y) * 0.5f;
        }

        scale.y *= -1f;
        offset.y = 1f - offset.y;

        SetTextureScaleOffset("_BaseMap", scale, offset);
        SetTextureScaleOffset("_MainTex", scale, offset);
    }

    private void SetTextureScaleOffset(string property, Vector2 scale, Vector2 offset)
    {
        if (!_runtimeMaterial.HasProperty(property))
            return;

        _runtimeMaterial.SetTextureScale(property, scale);
        _runtimeMaterial.SetTextureOffset(property, offset);
    }

    private void StartDarken()
    {
        if (_runtimeMaterial == null)
            return;

        if (_darkenRoutine != null)
            StopCoroutine(_darkenRoutine);

        _darkenRoutine = StartCoroutine(DarkenRoutine());
    }

    private void StartLightMultiply()
    {
        if (_runtimeMaterial == null)
            return;

        if (_lightRoutine != null)
            StopCoroutine(_lightRoutine);

        _lightRoutine = StartCoroutine(DelayedLightMultiplyRoutine());
    }

    private IEnumerator DarkenRoutine()
    {
        float duration = Mathf.Max(0.01f, darkenDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            SetDarkness(Mathf.Lerp(0f, targetDarkness, t));
            SetSaturation(Mathf.Lerp(1f, saturation, t));
            yield return null;
        }

        SetDarkness(targetDarkness);
        SetSaturation(saturation);
        _darkenRoutine = null;
        Debug.Log($"{LogPrefix} Darken complete. darkness={targetDarkness}");
    }

    private IEnumerator DelayedLightMultiplyRoutine()
    {
        if (lightAppearDelay > 0f)
            yield return new WaitForSecondsRealtime(lightAppearDelay);

        SetLightMultiplyStrength(targetLightMultiplyStrength);
        _lightRoutine = null;
        Debug.Log($"{LogPrefix} Light multiply complete. strength={targetLightMultiplyStrength}");
    }

    public void SetDarkness(float value)
    {
        if (_runtimeMaterial == null || !_runtimeMaterial.HasProperty("_Darkness"))
            return;

        _runtimeMaterial.SetFloat("_Darkness", Mathf.Clamp01(value));
    }

    public void SetSaturation(float value)
    {
        if (_runtimeMaterial == null || !_runtimeMaterial.HasProperty("_Saturation"))
            return;

        _runtimeMaterial.SetFloat("_Saturation", Mathf.Clamp01(value));
    }

    public void SetLightMultiplyStrength(float value)
    {
        if (_runtimeMaterial == null || !_runtimeMaterial.HasProperty("_LightMultiplyStrength"))
            return;

        _runtimeMaterial.SetFloat("_LightMultiplyStrength", Mathf.Clamp01(value));
    }
}
