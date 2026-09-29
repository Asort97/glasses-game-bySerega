using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace GlassesLauncher
{
    internal static class Program
    {
        [STAThread]
        private static int Main()
        {
            string directory = AppDomain.CurrentDomain.BaseDirectory;
            string config = Path.Combine(directory, "Play.player");
            try
            {
                string filename = File.ReadAllText(config).Trim();
                if (Path.GetFileName(filename) != filename || !filename.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("Play.player must contain the adjacent player executable name.");
                string player = Path.Combine(directory, filename);
                if (!File.Exists(player) || string.Equals(player, Application.ExecutablePath, StringComparison.OrdinalIgnoreCase))
                    throw new FileNotFoundException("Unity player executable not found.", player);

                bool first;
                using (var mutex = new Mutex(true, @"Local\GlassesLauncher_" + filename, out first))
                {
                    if (!first) return 0;
                    Native.SetProcessDpiAwarenessContext(new IntPtr(-4));
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
                    LaunchLog.Write("Starting " + filename);
                    using (var launch = new LaunchContext(player))
                        Application.Run(launch);
                }
                return 0;
            }
            catch (Exception error)
            {
                LaunchLog.Write(error.ToString());
                MessageBox.Show(error.Message, "Unable to start game", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 1;
            }
        }
    }

    internal sealed class LaunchContext : ApplicationContext
    {
        private readonly string sessionDirectory;
        private readonly string capturePath;
        private readonly Bitmap desktop;
        private readonly EventWaitHandle ready;
        private readonly EventWaitHandle presented;
        private readonly DesktopWindow cover;
        private readonly Rectangle desktopBounds;
        private readonly System.Windows.Forms.Timer timer;
        private readonly Stopwatch clock = Stopwatch.StartNew();
        private Process player;
        private IntPtr child;
        private bool revealing;
        private double revealStarted;
        private int readyFrames;
        private bool windowPositioned;

        public LaunchContext(string executable)
        {
            string token = Guid.NewGuid().ToString("N");
            sessionDirectory = Path.Combine(Path.GetTempPath(), "GlassesStartup_" + token);
            capturePath = Path.Combine(sessionDirectory, "desktop.bgra");
            try
            {
                Rectangle bounds = Screen.PrimaryScreen.Bounds;
                desktopBounds = bounds;
                desktop = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);
                using (Graphics graphics = Graphics.FromImage(desktop))
                    graphics.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size, CopyPixelOperation.SourceCopy);
                Directory.CreateDirectory(sessionDirectory);
                WriteCapture(desktop, capturePath);
                LaunchLog.Write("Desktop captured: " + bounds.Width + "x" + bounds.Height);

                string prefix = @"Local\GlassesStartup_" + token;
                ready = new EventWaitHandle(false, EventResetMode.ManualReset, prefix + "_ready");
                presented = new EventWaitHandle(false, EventResetMode.ManualReset, prefix + "_presented");
                cover = new DesktopWindow(desktop, bounds);
                // Cover native Unity startup without making Unity a cross-process child window.
                cover.Show();
                cover.Update();

                string arguments = "-screen-fullscreen 0 -popupwindow -screen-width " + bounds.Width
                    + " -screen-height " + bounds.Height + " --desktop-session " + token;
                player = Process.Start(new ProcessStartInfo(executable, arguments)
                {
                    UseShellExecute = false,
                    WorkingDirectory = Path.GetDirectoryName(executable)
                });
                LaunchLog.Write("Unity launched, PID " + player.Id);
                timer = new System.Windows.Forms.Timer { Interval = 16 };
                timer.Tick += Tick;
                timer.Start();
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        private void Tick(object sender, EventArgs args)
        {
            if (player.HasExited)
            {
                LaunchLog.Write("Unity exited: " + player.ExitCode);
                if (!revealing)
                    throw new InvalidOperationException("The Unity player exited before preparing its first frame. Check Player.log.");
                ExitThread();
                return;
            }

            if (child == IntPtr.Zero)
                child = Native.FindPlayerWindow(player.Id);

            if (!revealing)
            {
                if (clock.Elapsed.TotalSeconds > 90)
                    throw new TimeoutException("Unity did not prepare its first frame within 90 seconds. Check Player.log.");
                if (child == IntPtr.Zero || !ready.WaitOne(0))
                    return;
                if (!windowPositioned)
                {
                    int disableTransitions = 1;
                    Native.DwmSetWindowAttribute(child, 3, ref disableTransitions, sizeof(int));
                    Native.MoveWindow(child, desktopBounds.X, desktopBounds.Y, desktopBounds.Width, desktopBounds.Height, true);
                    Native.ShowWindow(child, 8); // SW_SHOWNA: cover still shields the first presentation.
                    windowPositioned = true;
                    LaunchLog.Write("Standalone Unity window ready behind desktop cover at " + clock.ElapsedMilliseconds + "ms");
                }
                // Allow the positioned standalone window to present before removing its cover.
                if (++readyFrames < 3)
                    return;
                revealing = true;
                revealStarted = clock.Elapsed.TotalSeconds;
                Native.FocusPlayer(child);
                presented.Set();
                LaunchLog.Write("First frame ready; revealing game at " + clock.ElapsedMilliseconds + "ms");
                File.Delete(capturePath);
            }

            if (!cover.IsDisposed)
            {
                double progress = (clock.Elapsed.TotalSeconds - revealStarted) / 0.25;
                if (progress >= 1)
                {
                    cover.Close();
                    // Closing a native cover can change activation; leave focus on Unity, not Play.exe.
                    Native.FocusPlayer(child);
                    LaunchLog.Write("Desktop cover removed at " + clock.ElapsedMilliseconds + "ms");
                }
                else
                    cover.Opacity = 1 - Math.Max(0, progress);
            }
        }

        internal static void WriteCapture(Bitmap bitmap, string path)
        {
            BitmapData data = bitmap.LockBits(new Rectangle(Point.Empty, bitmap.Size), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            try
            {
                using (var stream = File.Create(path))
                using (var writer = new BinaryWriter(stream))
                {
                    writer.Write(bitmap.Width);
                    writer.Write(bitmap.Height);
                    byte[] row = new byte[bitmap.Width * 4];
                    for (int y = 0; y < bitmap.Height; y++)
                    {
                        Marshal.Copy(IntPtr.Add(data.Scan0, y * data.Stride), row, 0, row.Length);
                        for (int x = 3; x < row.Length; x += 4) row[x] = 255;
                        writer.Write(row);
                    }
                }
            }
            finally { bitmap.UnlockBits(data); }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (timer != null) { timer.Stop(); timer.Dispose(); }
                if (player != null)
                {
                    if (!player.HasExited)
                    {
                        if (child != IntPtr.Zero) Native.PostMessage(child, 0x0010, IntPtr.Zero, IntPtr.Zero);
                        if (!player.WaitForExit(2000)) player.Kill();
                    }
                    player.Dispose();
                    player = null;
                }
                if (cover != null) cover.Dispose();
                if (desktop != null) desktop.Dispose();
                if (ready != null) ready.Dispose();
                if (presented != null) presented.Dispose();
                // Delete only this launch's capture, never recursively remove temp content.
                if (File.Exists(capturePath)) File.Delete(capturePath);
                if (Directory.Exists(sessionDirectory)) Directory.Delete(sessionDirectory, false);
            }
            base.Dispose(disposing);
        }
    }

    internal sealed class DesktopWindow : Form
    {
        public DesktopWindow(Bitmap background, Rectangle bounds)
        {
            AutoScaleMode = AutoScaleMode.None;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            Bounds = bounds;
            Text = "Glasses";
            ShowInTaskbar = false;
            TopMost = true;
            BackgroundImage = background;
            BackgroundImageLayout = ImageLayout.None;
            DoubleBuffered = true;
            int disableTransitions = 1;
            Native.DwmSetWindowAttribute(Handle, 3, ref disableTransitions, sizeof(int));
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                // Acquire activation at launch, then transfer it to Unity when ready.
                cp.ExStyle |= 0x00000080; // TOOLWINDOW
                return cp;
            }
        }
    }

    internal static class Native
    {
        private delegate bool EnumWindowCallback(IntPtr window, IntPtr state);
        [DllImport("user32.dll")] internal static extern bool SetProcessDpiAwarenessContext(IntPtr context);
        [DllImport("user32.dll")] internal static extern bool MoveWindow(IntPtr window, int x, int y, int width, int height, bool repaint);
        [DllImport("user32.dll")] internal static extern bool ShowWindow(IntPtr window, int command);
        [DllImport("user32.dll")] internal static extern bool PostMessage(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll")] private static extern bool EnumWindows(EnumWindowCallback callback, IntPtr state);
        [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr window, out uint process);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetClassName(IntPtr window, System.Text.StringBuilder name, int length);
        [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr window);
        [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
        [DllImport("dwmapi.dll")] internal static extern int DwmSetWindowAttribute(IntPtr window, int attribute, ref int value, int size);

        public static IntPtr FindPlayerWindow(int process)
        {
            IntPtr found = IntPtr.Zero;
            EnumWindowCallback callback = delegate(IntPtr window, IntPtr state)
            {
                uint owner;
                GetWindowThreadProcessId(window, out owner);
                var name = new System.Text.StringBuilder(64);
                GetClassName(window, name, name.Capacity);
                if (owner != process || name.ToString() != "UnityWndClass") return true;
                found = window;
                return false;
            };
            EnumWindows(callback, IntPtr.Zero);
            return found;
        }

        public static void FocusPlayer(IntPtr window)
        {
            bool activated = SetForegroundWindow(window);
            LaunchLog.Write("Unity foreground=" + (GetForegroundWindow() == window) + ", activation=" + activated);
        }
    }

    internal static class LaunchLog
    {
        public static void Write(string message)
        {
            try
            {
                string directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GlassesStartup");
                Directory.CreateDirectory(directory);
                File.AppendAllText(Path.Combine(directory, "launcher.log"), DateTime.Now.ToString("O") + " " + message + Environment.NewLine);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}
