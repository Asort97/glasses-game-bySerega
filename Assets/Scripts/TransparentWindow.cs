using System.Collections;
using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;

public class TransparentWindow : MonoBehaviour
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetLayeredWindowAttributes(IntPtr hWnd, uint colorKey, byte alpha, uint flags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern ushort RegisterClassEx(ref WNDCLASSEX windowClass);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr CreateWindowEx(
        uint exStyle,
        string className,
        string windowName,
        uint style,
        int x,
        int y,
        int width,
        int height,
        IntPtr parent,
        IntPtr menu,
        IntPtr instance,
        IntPtr param);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int command);

    [DllImport("user32.dll")]
    private static extern bool UpdateWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr DefWindowProc(IntPtr hWnd, uint message, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandle(string moduleName);

    [DllImport("gdi32.dll")]
    private static extern IntPtr GetStockObject(int objectIndex);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int index);

    [DllImport("dwmapi.dll")]
    private static extern int DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS pMarInset);

    private delegate IntPtr WindowProc(IntPtr hWnd, uint message, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WNDCLASSEX
    {
        public uint cbSize;
        public uint style;
        public WindowProc lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public IntPtr hInstance;
        public IntPtr hIcon;
        public IntPtr hCursor;
        public IntPtr hbrBackground;
        public string lpszMenuName;
        public string lpszClassName;
        public IntPtr hIconSm;
    }

    private struct MARGINS
    {
        public int left;
        public int right;
        public int top;
        public int bottom;
    }

    private const int GWL_STYLE = -16;
    private const int GWL_EXSTYLE = -20;
    private const uint WS_POPUP = 0x80000000;
    private const uint WS_VISIBLE = 0x10000000;
    private const uint WS_EX_LAYERED = 0x00080000;
    private const uint WS_EX_TRANSPARENT = 0x00000020;
    private const uint WS_EX_TOOLWINDOW = 0x00000080;
    private const uint WS_EX_NOACTIVATE = 0x08000000;
    private const uint LWA_COLORKEY = 0x00000001;
    private const uint LWA_ALPHA = 0x00000002;
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOACTIVATE = 0x0010;
    private const uint SWP_SHOWWINDOW = 0x0040;
    private const int SW_HIDE = 0;
    private const int SW_SHOWNA = 8;
    private const int BLACK_BRUSH = 4;
    private const int SM_XVIRTUALSCREEN = 76;
    private const int SM_YVIRTUALSCREEN = 77;
    private const int SM_CXVIRTUALSCREEN = 78;
    private const int SM_CYVIRTUALSCREEN = 79;
    private const uint WM_NCHITTEST = 0x0084;
    private static readonly IntPtr HTTRANSPARENT = new IntPtr(-1);
    private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    private const string DimmerClassName = "TransparentWindowDesktopDimmer";
    private static readonly Color32 TransparencyKeyColor = new Color32(255, 0, 255, 255);
    private static readonly WindowProc DimmerWindowProcDelegate = DimmerWindowProc;

    private static bool _dimmerClassRegistered;
    private static IntPtr _dimmerWindow = IntPtr.Zero;

    private IntPtr _unityWindow = IntPtr.Zero;
    private float _appliedDarkness = -1f;
    private int _appliedScreenX;
    private int _appliedScreenY;
    private int _appliedScreenWidth;
    private int _appliedScreenHeight;

    [Header("Settings")]
    [Tooltip("If true, mouse clicks pass through transparent areas")]
    public bool clickThrough = true;

    [Tooltip("If true, window stays on top of all other windows")]
    public bool alwaysOnTop = true;

    [FormerlySerializedAs("backgroundDarkness")]
    [Range(0f, 1f)]
    [Tooltip("How dark the desktop should be behind the Unity window")]
    public float desktopDarkness = 0.5f;

    [Tooltip("Fade in the desktop dimmer when the game starts")]
    public bool animateDimmerOnStart = true;

    [Min(0f)]
    [Tooltip("How long the background fade-in should take")]
    public float dimmerFadeDuration = 0.75f;

    [Min(0f)]
    [Tooltip("Delay before the background fade starts after the game window appears")]
    public float dimmerFadeStartDelay = 0.15f;

    [Tooltip("Use Windows color-key transparency instead of per-pixel alpha. More reliable with Unity/URP, but can create color fringes with AA/post FX.")]
    public bool useColorKeyTransparency = true;

    private float _currentDarkness;
    private float _fadeElapsed;
    private bool _isAnimatingDimmer;
    private bool _fadeStartQueued;
    private Coroutine _fadeStartCoroutine;

    private void Start()
    {
        ConfigureCamera();
        InitializeDimmerAnimation();

#if !UNITY_EDITOR
        _unityWindow = GetActiveWindow();
        if (_unityWindow == IntPtr.Zero)
            return;

        SetWindowLong(_unityWindow, GWL_STYLE, WS_POPUP | WS_VISIBLE);

        uint exStyle = WS_EX_LAYERED;
        if (clickThrough)
            exStyle |= WS_EX_TRANSPARENT;
        SetWindowLong(_unityWindow, GWL_EXSTYLE, exStyle);

        if (useColorKeyTransparency)
        {
            uint colorKey = ColorKeyFromColor32(TransparencyKeyColor);
            SetLayeredWindowAttributes(_unityWindow, colorKey, 0, LWA_COLORKEY);
        }
        else
        {
            MARGINS margins = new MARGINS { left = -1, right = -1, top = -1, bottom = -1 };
            DwmExtendFrameIntoClientArea(_unityWindow, ref margins);
        }

        if (alwaysOnTop)
            SetWindowPos(_unityWindow, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW);

        ApplyDesktopDimmer(true);

        if (_fadeStartQueued)
            _fadeStartCoroutine = StartCoroutine(BeginDimmerFadeAfterDelay());
#endif
    }

    private void LateUpdate()
    {
#if !UNITY_EDITOR
        UpdateDimmerAnimation();
        ApplyDesktopDimmer(false);
#endif
    }

    private void OnApplicationFocus(bool hasFocus)
    {
#if !UNITY_EDITOR
        if (_dimmerWindow == IntPtr.Zero)
            return;

        ShowWindow(_dimmerWindow, hasFocus ? SW_SHOWNA : SW_HIDE);

        if (hasFocus)
            ApplyDesktopDimmer(true);
#endif
    }

    private void OnDisable()
    {
#if !UNITY_EDITOR
        StopDimmerFadeCoroutine();
        DestroyDesktopDimmer();
#endif
    }

    private void OnDestroy()
    {
#if !UNITY_EDITOR
        StopDimmerFadeCoroutine();
        DestroyDesktopDimmer();
#endif
    }

    private void OnApplicationQuit()
    {
#if !UNITY_EDITOR
        StopDimmerFadeCoroutine();
        DestroyDesktopDimmer();
#endif
    }

    private void ConfigureCamera()
    {
        var cam = GetComponent<Camera>();
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            if (useColorKeyTransparency)
            {
                Color keyColor = new Color32(TransparencyKeyColor.r, TransparencyKeyColor.g, TransparencyKeyColor.b, TransparencyKeyColor.a);
                cam.backgroundColor = keyColor;
                cam.allowHDR = false;
                cam.allowMSAA = false;
            }
            else
            {
                cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
            }
        }

        var cameraData = GetComponent<UniversalAdditionalCameraData>();
        if (cameraData != null && useColorKeyTransparency)
        {
            cameraData.renderPostProcessing = false;
            cameraData.antialiasing = AntialiasingMode.None;
        }
    }

    private void InitializeDimmerAnimation()
    {
        if (animateDimmerOnStart && desktopDarkness > 0f && dimmerFadeDuration > 0f)
        {
            _currentDarkness = 0f;
            _fadeElapsed = 0f;
            _isAnimatingDimmer = false;
            _fadeStartQueued = true;
        }
        else
        {
            _currentDarkness = desktopDarkness;
            _fadeElapsed = dimmerFadeDuration;
            _isAnimatingDimmer = false;
            _fadeStartQueued = false;
        }
    }

    private IEnumerator BeginDimmerFadeAfterDelay()
    {
        if (dimmerFadeStartDelay > 0f)
            yield return new WaitForSecondsRealtime(dimmerFadeStartDelay);
        else
            yield return null;

        _fadeElapsed = 0f;
        _currentDarkness = 0f;
        _isAnimatingDimmer = true;
        _fadeStartQueued = false;
        _fadeStartCoroutine = null;
    }

    private void StopDimmerFadeCoroutine()
    {
        if (_fadeStartCoroutine == null)
            return;

        StopCoroutine(_fadeStartCoroutine);
        _fadeStartCoroutine = null;
    }

    private void UpdateDimmerAnimation()
    {
        if (_fadeStartQueued)
        {
            _currentDarkness = 0f;
            return;
        }

        if (!_isAnimatingDimmer)
        {
            _currentDarkness = desktopDarkness;
            return;
        }

        if (desktopDarkness <= 0f || dimmerFadeDuration <= 0f)
        {
            _currentDarkness = desktopDarkness;
            _isAnimatingDimmer = false;
            return;
        }

        _fadeElapsed += Time.unscaledDeltaTime;
        float progress = Mathf.Clamp01(_fadeElapsed / dimmerFadeDuration);
        float easedProgress = Mathf.SmoothStep(0f, 1f, progress);
        _currentDarkness = Mathf.Lerp(0f, desktopDarkness, easedProgress);

        if (progress >= 1f)
            _isAnimatingDimmer = false;
    }

    private static uint ColorKeyFromColor32(Color32 color)
    {
        return (uint)(color.r | (color.g << 8) | (color.b << 16));
    }

    private void ApplyDesktopDimmer(bool force)
    {
        if (_unityWindow == IntPtr.Zero)
            return;

        float targetDarkness = _currentDarkness;

        if (targetDarkness <= 0f)
        {
            _appliedDarkness = 0f;
            DestroyDesktopDimmer();
            return;
        }

        int screenX = GetSystemMetrics(SM_XVIRTUALSCREEN);
        int screenY = GetSystemMetrics(SM_YVIRTUALSCREEN);
        int screenWidth = GetSystemMetrics(SM_CXVIRTUALSCREEN);
        int screenHeight = GetSystemMetrics(SM_CYVIRTUALSCREEN);

        bool geometryChanged = force || screenX != _appliedScreenX || screenY != _appliedScreenY || screenWidth != _appliedScreenWidth || screenHeight != _appliedScreenHeight;
        bool opacityChanged = force || !Mathf.Approximately(_appliedDarkness, targetDarkness);

        EnsureDesktopDimmerWindow(screenX, screenY, screenWidth, screenHeight);
        if (_dimmerWindow == IntPtr.Zero)
            return;

        if (opacityChanged)
        {
            byte alpha = (byte)Mathf.Clamp(Mathf.RoundToInt(targetDarkness * 255f), 0, 255);
            SetLayeredWindowAttributes(_dimmerWindow, 0, alpha, LWA_ALPHA);
            _appliedDarkness = targetDarkness;
        }

        if (geometryChanged || opacityChanged)
        {
            if (alwaysOnTop)
                SetWindowPos(_unityWindow, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW);

            SetWindowPos(_dimmerWindow, _unityWindow, screenX, screenY, screenWidth, screenHeight, SWP_NOACTIVATE | SWP_SHOWWINDOW);

            _appliedScreenX = screenX;
            _appliedScreenY = screenY;
            _appliedScreenWidth = screenWidth;
            _appliedScreenHeight = screenHeight;
        }
    }

    private void EnsureDesktopDimmerWindow(int x, int y, int width, int height)
    {
        if (_dimmerWindow != IntPtr.Zero)
            return;

        if (!_dimmerClassRegistered)
        {
            IntPtr instance = GetModuleHandle(null);
            var windowClass = new WNDCLASSEX
            {
                cbSize = (uint)Marshal.SizeOf<WNDCLASSEX>(),
                lpfnWndProc = DimmerWindowProcDelegate,
                hInstance = instance,
                hbrBackground = GetStockObject(BLACK_BRUSH),
                lpszClassName = DimmerClassName
            };

            RegisterClassEx(ref windowClass);
            _dimmerClassRegistered = true;
        }

        _dimmerWindow = CreateWindowEx(
            WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE,
            DimmerClassName,
            string.Empty,
            WS_POPUP | WS_VISIBLE,
            x,
            y,
            width,
            height,
            IntPtr.Zero,
            IntPtr.Zero,
            GetModuleHandle(null),
            IntPtr.Zero);

        if (_dimmerWindow == IntPtr.Zero)
        {
            Debug.LogError("Failed to create desktop dimmer window.");
            return;
        }

        ShowWindow(_dimmerWindow, SW_SHOWNA);
        UpdateWindow(_dimmerWindow);
    }

    private static void DestroyDesktopDimmer()
    {
        if (_dimmerWindow == IntPtr.Zero)
            return;

        DestroyWindow(_dimmerWindow);
        _dimmerWindow = IntPtr.Zero;
    }

    private static IntPtr DimmerWindowProc(IntPtr hWnd, uint message, IntPtr wParam, IntPtr lParam)
    {
        if (message == WM_NCHITTEST)
            return HTTRANSPARENT;

        return DefWindowProc(hWnd, message, wParam, lParam);
    }
}
