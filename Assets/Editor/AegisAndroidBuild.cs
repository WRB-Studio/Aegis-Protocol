using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace WRBStudio.AegisProtocol.Editor
{
    public static class AegisAndroidBuild
    {
        public static void BuildFromEnvironment()
        {
            var outputPath = RequireEnvironmentVariable("AEGIS_BUILD_OUTPUT");
            var format = RequireEnvironmentVariable("AEGIS_BUILD_FORMAT");
            var isBundle = string.Equals(format, "aab", StringComparison.OrdinalIgnoreCase);

            if (!isBundle && !string.Equals(format, "apk", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("AEGIS_BUILD_FORMAT must be 'apk' or 'aab'.");
            }

            var originalKeystoreName = PlayerSettings.Android.keystoreName;
            var originalKeystorePass = PlayerSettings.Android.keystorePass;
            var originalKeyaliasName = PlayerSettings.Android.keyaliasName;
            var originalKeyaliasPass = PlayerSettings.Android.keyaliasPass;
            var originalUseCustomKeystore = PlayerSettings.Android.useCustomKeystore;
            var originalBuildAppBundle = EditorUserBuildSettings.buildAppBundle;
            var originalVersionCode = PlayerSettings.Android.bundleVersionCode;

            try
            {
                PlayerSettings.Android.useCustomKeystore = true;
                PlayerSettings.Android.keystoreName = RequireEnvironmentVariable("AEGIS_KEYSTORE_PATH");
                PlayerSettings.Android.keystorePass = RequireEnvironmentVariable("AEGIS_KEYSTORE_PASSWORD");
                PlayerSettings.Android.keyaliasName = RequireEnvironmentVariable("AEGIS_KEY_ALIAS");
                PlayerSettings.Android.keyaliasPass = RequireEnvironmentVariable("AEGIS_KEY_ALIAS_PASSWORD");
                EditorUserBuildSettings.buildAppBundle = isBundle;

                var requestedVersionCode = Environment.GetEnvironmentVariable("AEGIS_VERSION_CODE");
                if (!string.IsNullOrWhiteSpace(requestedVersionCode))
                {
                    if (!int.TryParse(requestedVersionCode, out var versionCode) || versionCode < 1)
                    {
                        throw new ArgumentException("AEGIS_VERSION_CODE must be a positive integer.");
                    }

                    PlayerSettings.Android.bundleVersionCode = versionCode;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? throw new InvalidOperationException("Invalid output path."));
                var scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
                if (scenes.Length == 0)
                {
                    throw new InvalidOperationException("No enabled scenes are configured for the build.");
                }

                var report = BuildPipeline.BuildPlayer(scenes, outputPath, BuildTarget.Android, BuildOptions.None);
                if (report.summary.result != BuildResult.Succeeded)
                {
                    throw new InvalidOperationException($"Android build failed: {report.summary.result}");
                }

                Debug.Log($"Android {format.ToUpperInvariant()} created: {outputPath}");
            }
            finally
            {
                PlayerSettings.Android.keystoreName = originalKeystoreName;
                PlayerSettings.Android.keystorePass = originalKeystorePass;
                PlayerSettings.Android.keyaliasName = originalKeyaliasName;
                PlayerSettings.Android.keyaliasPass = originalKeyaliasPass;
                PlayerSettings.Android.useCustomKeystore = originalUseCustomKeystore;
                EditorUserBuildSettings.buildAppBundle = originalBuildAppBundle;
                PlayerSettings.Android.bundleVersionCode = originalVersionCode;
            }
        }

        private static string RequireEnvironmentVariable(string name)
        {
            return Environment.GetEnvironmentVariable(name)
                   ?? throw new InvalidOperationException($"Missing environment variable: {name}");
        }
    }
}
