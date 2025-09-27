using System.IO;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace ClearMerge.Utils
{
    public class ClearMergeSetup : EditorWindow
    {
        private bool gitignoreUpdated = false;
        private bool tempDirExists = false;
        private bool prePushHookExists = false;
        private const string TempScenePath = "Assets/Editor/ClearMerge/Temp";
        private const string GitIgnorePath = ".gitignore";
        private const string GitHooksPath = ".git/hooks";
        private const string PrePushHookPath = ".git/hooks/pre-push";
        private Vector2 scrollPosition;

        public static void ShowWindow()
        {
            var window = GetWindow<ClearMergeSetup>("ClearMerge Setup");
            window.minSize = new Vector2(450, 350);
            window.CheckSetupStatus();
        }

        private void OnEnable()
        {
            CheckSetupStatus();
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            // Header
            GUILayout.Space(10);
            EditorGUILayout.LabelField("🛠️ ClearMerge Setup", EditorStyles.boldLabel);
            GUILayout.Space(5);
            EditorGUILayout.HelpBox(
                "ClearMerge requires this setup before use. " +
                "This tool helps resolve Git merge conflicts in Unity projects, including scenes, assets, and meta files.",
                MessageType.Info);
            GUILayout.Space(15);

            // Setup status section
            EditorGUILayout.LabelField("Setup Status", EditorStyles.boldLabel);
            GUILayout.Space(5);

            // GitIgnore status
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(gitignoreUpdated ? "✅" : "❌", GUILayout.Width(20));
            EditorGUILayout.LabelField("Temporary files directory added to .gitignore");
            EditorGUILayout.EndHorizontal();

            if (!gitignoreUpdated)
            {
                GUILayout.Space(5);
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(25);
                if (GUILayout.Button("Add Temp Directory to .gitignore"))
                {
                    UpdateGitIgnore();
                    CheckSetupStatus();
                }
                EditorGUILayout.EndHorizontal();
            }

            GUILayout.Space(10);

            // Directory creation status
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(tempDirExists ? "✅" : "❌", GUILayout.Width(20));
            EditorGUILayout.LabelField("Temporary files directory exists");
            EditorGUILayout.EndHorizontal();

            if (!tempDirExists)
            {
                GUILayout.Space(5);
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(25);
                if (GUILayout.Button("Create Temp Directory"))
                {
                    CreateTempDirectory();
                    CheckSetupStatus();
                }
                EditorGUILayout.EndHorizontal();
            }

            GUILayout.Space(10);

            // Add Git pre-push hook setup
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(prePushHookExists ? "✅" : "❌", GUILayout.Width(20));
            EditorGUILayout.LabelField("Git pre-push hook for ClearMerge Safe Mode installed");
            EditorGUILayout.EndHorizontal();

            if (!prePushHookExists)
            {
                GUILayout.Space(5);
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(25);
                if (GUILayout.Button("Install Git Pre-Push Hook"))
                {
                    SetupPrePushHook();
                    CheckSetupStatus();
                }
                EditorGUILayout.EndHorizontal();
            }

            GUILayout.Space(20);

            // Status summary
            bool setupComplete = gitignoreUpdated && tempDirExists && prePushHookExists;
            EditorGUILayout.LabelField("Overall Status:", EditorStyles.boldLabel);
            GUILayout.Space(5);
            EditorGUILayout.HelpBox(
                setupComplete ?
                "✅ Setup Complete! You can now use ClearMerge tools." :
                "❌ Setup Incomplete. Please complete all required steps above.",
                setupComplete ? MessageType.Info : MessageType.Warning);

            GUILayout.Space(15);
            if (setupComplete)
            {
                if (GUILayout.Button("Open ClearMerge Main Window", GUILayout.Height(35)))
                {
                    MainWindow.ShowWindow();
                    Close();
                }
            }
            else
            {
                if (GUILayout.Button("Complete All Setup Steps", GUILayout.Height(35)))
                {
                    CompleteSetup();
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void CheckSetupStatus()
        {
            // Check if gitignore has the temp directory
            gitignoreUpdated = IsInGitIgnore();

            // Check if temp directory exists
            tempDirExists = Directory.Exists(TempScenePath);

            // Check if pre-push hook exists and contains our code
            prePushHookExists = File.Exists(PrePushHookPath) &&
                               File.ReadAllText(PrePushHookPath).Contains("ClearMerge Safe Mode");
        }

        private bool IsInGitIgnore()
        {
            if (!File.Exists(GitIgnorePath))
            {
                return false;
            }

            string gitignoreContent = File.ReadAllText(GitIgnorePath);
            return gitignoreContent.Contains("Editor/ClearMerge/Temp") || gitignoreContent.Contains("Editor/ClearMerge/Temp/");
        }

        private void UpdateGitIgnore()
        {
            try
            {
                // Create gitignore if it doesn't exist
                if (!File.Exists(GitIgnorePath))
                {
                    File.WriteAllText(GitIgnorePath, "# Unity generated files\n");
                }

                string gitignoreContent = File.ReadAllText(GitIgnorePath);
                if (!gitignoreContent.Contains("Editor/ClearMerge/Temp") && !gitignoreContent.Contains("Editor/ClearMerge/Temp/"))
                {
                    string newEntry = "\n# ClearMerge temporary files\nEditor/ClearMerge/Temp/\n";
                    File.AppendAllText(GitIgnorePath, newEntry);
                    gitignoreUpdated = true;

                    Debug.Log("Added Editor/ClearMerge/Temp/ to .gitignore successfully");
                    EditorUtility.DisplayDialog("Success", "Added Editor/ClearMerge/Temp/ to .gitignore successfully", "OK");
                }
                else
                {
                    gitignoreUpdated = true;
                    Debug.Log("Editor/ClearMerge/Temp/ already exists in .gitignore");
                    EditorUtility.DisplayDialog("Info", "Editor/ClearMerge/Temp/ is already in .gitignore", "OK");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to update .gitignore: {e.Message}");
                EditorUtility.DisplayDialog("Error", $"Failed to update .gitignore: {e.Message}", "OK");
            }
        }

        private void CreateTempDirectory()
        {
            try
            {
                if (!Directory.Exists(TempScenePath))
                {
                    Directory.CreateDirectory(TempScenePath);
                    AssetDatabase.Refresh();
                    Debug.Log("Created temporary files directory at " + TempScenePath);
                    EditorUtility.DisplayDialog("Success", "Created temporary files directory at " + TempScenePath, "OK");
                    tempDirExists = true;
                }
                else
                {
                    Debug.Log("Temporary files directory already exists");
                    EditorUtility.DisplayDialog("Info", "Temporary files directory already exists", "OK");
                    tempDirExists = true;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to create temporary directory: {e.Message}");
                EditorUtility.DisplayDialog("Error", $"Failed to create temporary directory: {e.Message}", "OK");
            }
        }

        private void CompleteSetup()
        {
            // Update gitignore if needed
            if (!gitignoreUpdated)
            {
                UpdateGitIgnore();
            }

            // Create temp directory if needed
            if (!tempDirExists)
            {
                CreateTempDirectory();
            }

            // Setup Git pre-push hook if needed
            if (!prePushHookExists)
            {
                SetupPrePushHook();
            }

            CheckSetupStatus();
        }

        private void SetupPrePushHook()
        {
            try
            {
                // Ensure hooks directory exists
                if (!Directory.Exists(GitHooksPath))
                {
                    Directory.CreateDirectory(GitHooksPath);
                }

                // Create or update pre-push hook
                string hookContent = @"#!/bin/sh
# ClearMerge Safe Mode Hook

# Check if Unity Safe Mode is enabled for merge conflicts
if [ -f ""Assets/Editor/ClearMergeSafeMode.marker"" ]; then
    # Look for Unity merge conflicts
    CONFLICTS=$(git status --porcelain | grep -E ""^(UU|AA).*\.(unity|asset|prefab)$"")
    
    if [ -n ""$CONFLICTS"" ]; then
        echo ""ERROR: ClearMerge Safe Mode is enabled and there are unresolved Unity merge conflicts:""
        echo ""$CONFLICTS""
        echo ""Please resolve these conflicts using the ClearMerge tools before pushing.""
        echo ""To override this check, disable Safe Mode in Unity Editor: Project Settings > ClearMerge""
        exit 1
    fi
fi

exit 0
";

                File.WriteAllText(PrePushHookPath, hookContent.Replace("\r\n", "\n"));

                // Make executable on Unix systems
                if (Application.platform != RuntimePlatform.WindowsEditor)
                {
                    Process process = new Process();
                    process.StartInfo.FileName = "chmod";
                    process.StartInfo.Arguments = $"+x \"{PrePushHookPath}\"";
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.Start();
                    process.WaitForExit();
                }

                // Create marker file for hook to check
                UpdateSafeModeMarker();

                prePushHookExists = true;
                Debug.Log("Git pre-push hook for ClearMerge installed");
                EditorUtility.DisplayDialog("Success", "Git pre-push hook installed successfully", "OK");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to set up git pre-push hook: {e.Message}");
                EditorUtility.DisplayDialog("Error", $"Failed to set up git pre-push hook: {e.Message}", "OK");
            }
        }

        private void UpdateSafeModeMarker()
        {
            string markerPath = "Assets/Editor/ClearMergeSafeMode.marker";

            if (ClearMergeSettings.SafeModePushes)
            {
                // Create or ensure marker file exists
                if (!File.Exists(markerPath))
                {
                    File.WriteAllText(markerPath, "Safe mode enabled");
                    AssetDatabase.Refresh();
                }
            }
            else
            {
                // Remove marker file if it exists
                if (File.Exists(markerPath))
                {
                    File.Delete(markerPath);
                    AssetDatabase.Refresh();
                }
            }
        }

        // Static method to check if setup is complete that other classes can use
        public static bool IsSetupComplete()
        {
            bool gitignoreUpdated = false;

            // Check gitignore
            if (File.Exists(".gitignore"))
            {
                string gitignoreContent = File.ReadAllText(".gitignore");
                gitignoreUpdated = gitignoreContent.Contains("Editor/ClearMerge/Temp") || gitignoreContent.Contains("Editor/ClearMerge/Temp/");
            }

            // Check temp directory
            bool tempDirExists = Directory.Exists("Assets/Editor/ClearMerge/Temp");

            return gitignoreUpdated && tempDirExists;
        }
    }
}