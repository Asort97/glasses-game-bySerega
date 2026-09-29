using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public sealed class SeamlessWindowsBuild : IPostprocessBuildWithReport
{
    public int callbackOrder => 1000;

    [MenuItem("Build/Build Seamless Windows")]
    public static void Build()
    {
        if (EditorApplication.isPlaying || EditorApplication.isCompiling)
            throw new BuildFailedException("Stop Play Mode and wait for compilation before building.");

        string directory = Path.GetFullPath("Builds/SeamlessWindows");
        Directory.CreateDirectory(directory);
        PlayerSettings.SplashScreen.show = false;
        PlayerSettings.SplashScreen.showUnityLogo = false;
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray(),
            locationPathName = Path.Combine(directory, PlayerSettings.productName + ".exe"),
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        });
        if (report.summary.result != BuildResult.Succeeded)
            throw new BuildFailedException("Seamless Windows build failed: " + report.summary.result);
        UnityEngine.Debug.Log("[SeamlessWindowsBuild] Complete. Launch " + Path.Combine(directory, "Play.exe"));
    }

    public void OnPostprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.StandaloneWindows64 || Application.platform != RuntimePlatform.WindowsEditor)
            return;
        CreateLauncher(report.summary.outputPath);
    }

    public static void CreateLauncher(string playerPath)
    {
        string directory = Path.GetDirectoryName(Path.GetFullPath(playerPath));
        string launcher = Path.Combine(directory, "Play.exe");
        if (string.Equals(Path.GetFullPath(playerPath), launcher, StringComparison.OrdinalIgnoreCase))
            throw new BuildFailedException("Name the Unity player something other than Play.exe; that name is reserved for the launcher.");

        string source = Path.GetFullPath("Tools/SeamlessLauncher");
        string compiler = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Microsoft.NET/Framework64/v4.0.30319/csc.exe");
        if (!File.Exists(compiler))
            throw new BuildFailedException("Windows .NET Framework C# compiler not found: " + compiler);
        var info = new ProcessStartInfo(compiler,
            "/nologo /target:winexe /platform:x64 /optimize+ /out:\"" + launcher + "\""
            + " /reference:System.Drawing.dll /reference:System.Windows.Forms.dll"
            + " /win32manifest:\"" + Path.Combine(source, "app.manifest") + "\""
            + " \"" + Path.Combine(source, "Program.cs") + "\"")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true
        };
        using (var process = Process.Start(info))
        {
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            if (process.ExitCode != 0)
                throw new BuildFailedException("Launcher compilation failed:\n" + output);
        }
        File.WriteAllText(Path.Combine(directory, "Play.player"), Path.GetFileName(playerPath));
        UnityEngine.Debug.Log("[SeamlessWindowsBuild] Created " + launcher);
    }
}
