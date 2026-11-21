using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BreachPoint.EditorTools
{
    /// <summary>
    /// Builds the booth app: Builds/BreachPoint.app (macOS, Apple silicon, fullscreen).
    /// Menu: Breach Point → Build Mac App. Headless: -executeMethod BreachPoint.EditorTools.BuildApp.BuildMac
    /// (set BP_BUILD_OUT to choose the output .app path).
    /// </summary>
    public static class BuildApp
    {
        [MenuItem("Breach Point/Build Mac App", priority = 40)]
        public static void BuildMacMenu()
        {
            if (EditorApplication.isPlaying) EditorApplication.isPlaying = false;
            var path = Build();
            if (path != null) EditorUtility.RevealInFinder(path);
        }

        /// <summary>Entry point for batch mode; exits with code 1 on failure.</summary>
        public static void BuildMac()
        {
            var path = Build();
            if (Application.isBatchMode) EditorApplication.Exit(path != null ? 0 : 1);
        }

        static string Build()
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            string outPath = Environment.GetEnvironmentVariable("BP_BUILD_OUT");
            if (string.IsNullOrEmpty(outPath)) outPath = Path.Combine(projectRoot, "Builds", "BreachPoint.app");
            Directory.CreateDirectory(Path.GetDirectoryName(outPath));

            PlayerSettings.companyName = "BreachPoint";
            PlayerSettings.productName = "Breach Point";
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Standalone, "com.breachpoint.game");
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.defaultIsNativeResolution = true;
            PlayerSettings.runInBackground = true;
            PlayerSettings.visibleInBackground = true;
            PlayerSettings.resizableWindow = false;
            PlayerSettings.SplashScreen.showUnityLogo = false;
            UnityEditor.OSXStandalone.UserBuildSettings.architecture = UnityEditor.Build.OSArchitecture.ARM64;

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ProjectSetup.MainScene },
                locationPathName = outPath,
                target = BuildTarget.StandaloneOSX,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.None,
            };
            var report = BuildPipeline.BuildPlayer(options);
            var sum = report.summary;
            Debug.Log($"[BreachPoint] Build {sum.result}: {outPath} ({sum.totalSize / (1024 * 1024)} MB, {sum.totalErrors} errors, {sum.totalTime.TotalSeconds:0}s)");
            return sum.result == BuildResult.Succeeded ? outPath : null;
        }
    }
}
