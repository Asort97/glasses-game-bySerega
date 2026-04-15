using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

public class TransparentWindow : MonoBehaviour
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);

    [DllImport("user32.dll")]
    private static extern uint GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("dwmapi.dll")]
    private static extern int DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS pMarInset);

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
    private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);

    [Header("Settings")]
    [Tooltip("If true, mouse clicks pass through transparent areas")]
    public bool clickThrough = true;

    [Tooltip("If true, window stays on top of all other windows")]
    public bool alwaysOnTop = true;

    private Material _alphaClearMat;

    private void Start()
    {
#if !UNITY_EDITOR
        IntPtr hWnd = GetActiveWindow();

        SetWindowLong(hWnd, GWL_STYLE, WS_POPUP | WS_VISIBLE);

        uint exStyle = WS_EX_LAYERED;
        if (clickThrough)
            exStyle |= WS_EX_TRANSPARENT;
        SetWindowLong(hWnd, GWL_EXSTYLE, exStyle);

        MARGINS margins = new MARGINS { left = -1, right = -1, top = -1, bottom = -1 };
        DwmExtendFrameIntoClientArea(hWnd, ref margins);

        if (alwaysOnTop)
        {
            SetWindowPos(hWnd, HWND_TOPMOST, 0, 0, Screen.width, Screen.height, 0x0040);
        }
#endif

        // Force camera to output alpha
        var cam = GetComponent<Camera>();
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
            cam.allowHDR = false;
            cam.allowMSAA = false;
        }
    }
}
