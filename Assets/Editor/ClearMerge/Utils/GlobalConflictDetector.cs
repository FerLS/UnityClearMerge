using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

namespace ClearMerge.Utils
{
    /// <summary>
    /// Global conflict detector - monitors for conflicts and provides notifications
    /// </summary>
    [InitializeOnLoad]
    public static class GlobalConflictDetector
    {
        #region Fields
        private static bool isDetecting = false;
        private static double lastDetectionTime = 0;
        private static readonly double DETECTION_COOLDOWN = 2.0;
        #endregion

        #region Initialization
        static GlobalConflictDetector()
        {
            // Run both global and meta conflict detection after editor loads
            EditorApplication.delayCall += () =>
            {
                DetectAllConflicts();
                CheckForMetaConflictsOnStartup();
            };
        }
        #endregion

        #region Conflict Detection Methods

        public static void DetectAllConflicts()
        {
            if (isDetecting) return;

            double currentTime = EditorApplication.timeSinceStartup;
            if (currentTime - lastDetectionTime < DETECTION_COOLDOWN)
            {
                return;
            }

            isDetecting = true;
            lastDetectionTime = currentTime;

            try
            {
                var conflictMessages = new List<string>();
                bool hasConflicts = false;

                // Check for meta conflicts
                var metaConflicts = ClearMerge.Assets.MetaConflictResolver.FindMetaConflicts();
                if (metaConflicts.Count > 0)
                {
                    conflictMessages.Add($"📄 {metaConflicts.Count} .meta files with conflicts");
                    hasConflicts = true;
                }

                // Check for asset conflicts
                var assetConflicts = ClearMerge.Assets.ConflictAnalyzer.ScanProjectForConflicts();
                if (assetConflicts.Count > 0)
                {
                    conflictMessages.Add($"🎨 {assetConflicts.Count} assets with conflicts");
                    hasConflicts = true;
                }

                // Check for scene conflicts
                var sceneConflicts = DetectSceneConflicts();
                if (sceneConflicts.Count > 0)
                {
                    conflictMessages.Add($"🎬 {sceneConflicts.Count} scenes with conflicts");
                    hasConflicts = true;
                }

                // Show notification if conflicts found
                if (hasConflicts)
                {
                    string message = "Conflicts detected:\n" + string.Join("\n", conflictMessages);
                    ShowConflictNotification(message);
                }
            }
            finally
            {
                isDetecting = false;
            }
        }

        public static void ScanNowForConflicts()
        {
            lastDetectionTime = 0; // Reset cooldown
            DetectAllConflicts();
        }

        private static List<string> DetectSceneConflicts()
        {
            var conflicts = new List<string>();
            string[] sceneFiles = Directory.GetFiles(Application.dataPath, "*.unity", SearchOption.AllDirectories);

            foreach (string scenePath in sceneFiles)
            {
                try
                {
                    string content = File.ReadAllText(scenePath);
                    if (content.Contains("<<<<<<< HEAD") || content.Contains("=======") || content.Contains(">>>>>>> "))
                    {
                        conflicts.Add(Path.GetFileName(scenePath));
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"Could not check scene file {scenePath}: {e.Message}");
                }
            }

            return conflicts;
        }

        private static void ShowConflictNotification(string message)
        {
            if (EditorUtility.DisplayDialog("ClearMerge - Conflicts Detected",
                message + "\n\nWould you like to open the ClearMerge window to resolve them?",
                "Open ClearMerge", "Later"))
            {
                MainWindow.ShowWindow();
            }
        }

        private static void CheckForMetaConflictsOnStartup()
        {
            EditorApplication.delayCall -= CheckForMetaConflictsOnStartup;

            EditorApplication.delayCall += () =>
            {
                List<string> conflicts = ClearMerge.Assets.MetaConflictResolver.FindMetaConflicts();

                if (conflicts.Count > 0)
                {
                    bool fix = EditorUtility.DisplayDialog(
                        "⚠️ Meta Conflicts Detected",
                        $"Detected {conflicts.Count} .meta files with conflicts.\n\nDo you want to fix them automatically?",
                        "Fix",
                        "Later"
                    );

                    if (fix)
                    {
                        int fixedCount = ClearMerge.Assets.MetaConflictResolver.FixConflicts(conflicts);
                        EditorUtility.DisplayDialog("Conflicts Resolved", $"✅ Fixed {fixedCount} conflicts automatically", "OK");
                        AssetDatabase.Refresh();
                    }
                }
            };
        }

        #endregion
    }
}
