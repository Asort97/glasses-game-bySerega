# Seamless Windows Startup

## Build and launch

- Unity menu: `Build > Build Seamless Windows`.
- Output: `Builds/SeamlessWindows/Play.exe`.
- Distribute the entire output directory, including `Play.player` and the Unity player files.
- Use `Play.exe` for shortcuts/store launch configuration, not the Unity executable.
- Regular Windows x64 builds also get a `Play.exe` alongside the player from the build callback.
- Unity's normal **Build And Run** still launches the Unity executable directly. Test seamless startup through `Play.exe` instead.

The launcher is compiled with the Windows .NET Framework compiler. No console window, administrator permissions, extra runtime installation, or asset-store package is required on supported Windows 10/11 systems. The source remains outside Assets so WinForms does not enter the Unity game assembly.

## Sequence

1. Capture the primary display before creating any game window or starting Unity.
2. Paint those same pixels in a borderless native cover at the original monitor resolution.
3. Start Unity as a normal top-level borderless player behind the cover. Do not use `-parentHWND` or cross-process reparenting: Unity needs its own foreground window for keyboard input.
4. Transfer the top-down BGRA capture through a unique temporary session file. No PNG compression on startup.
5. DesktopScreenshotBackground applies it and waits for two rendered frames, then signals a named event.
6. Position the ready player on the captured monitor. After an additional compositor allowance, focus Unity, signal presentation and fade the cover out over 0.25 seconds. Restore Unity focus after closing the cover as well. GlassesIntroBootstrap and startup music can now begin.
7. Delete the capture immediately after transfer/presentation. Dispose the session directory and launcher when Unity exits.

Unity splash and logo are disabled in Player Settings. Glasses remain inactive during preparation. Darkness starts at 0 and saturation at 1, then both blend to their inspector values over Darken Duration. Existing Point texture filtering is preserved.

## Fallback and limits

Direct launch still captures the desktop, applies the material before showing the window, and waits before starting the intro. However, Unity can display a native window before any scene script executes; direct launch cannot guarantee a seamless first frame. The editor tests gameplay, not the native startup sequence.

The seamless path targets the primary display. It intentionally does not combine multiple monitors into the background. HDR/color-managed desktops and monitors with different DPI settings need their own visual validation. A captured desktop is a still image; windows/videos moving behind the game cannot stay live or be guaranteed to match at exit.

Exit animation remains owned by the existing GameQuitController. The launcher does not restart a scene or override its exit sequence; it exits when Unity exits. Alt-F4 and input go directly to the standalone Unity window.

## Diagnostics

- Launcher stages: `%LOCALAPPDATA%/GlassesStartup/launcher.log` (timestamps only, no screenshot).
- Unity: `%USERPROFILE%/AppData/LocalLow/DefaultCompany/glasses-game-bySerega/Player.log`.
- Missing capture/handshake or a player that fails to prepare a frame fails visibly instead of hanging indefinitely (90-second startup timeout).
- `Save Debug Capture` on DesktopScreenshotBackground is opt-in and only writes PNG in the editor or a development build.

## Manual acceptance checks

- Launch Play.exe over a desktop with small text at 100%, 125%, and 150% DPI: no Unity splash, empty frame, window border, flipped image, or scaled jump.
- Repeat from a shortcut on the primary monitor and with a second monitor connected.
- Verify normal intro and the admin Skip Intro mode; both must wait for presentation.
- Verify WASD, arrows, Space, mouse, Alt-Tab, and Esc exit in the standalone Unity window.
- Exit normally and verify both processes terminate and no `GlassesStartup_*` capture directory remains in TEMP.
- Force a startup failure in a disposable build and verify the cover closes with an error instead of leaving a frozen desktop on top.

Reference: https://docs.unity3d.com/6000.0/Documentation/Manual/PlayerCommandLineArguments.html
