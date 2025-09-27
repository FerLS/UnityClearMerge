using System.Collections.Generic;
using ClearMerge.Assets;
using ClearMerge.Scenes;
using UnityEditor;
using UnityEngine;

namespace ClearMerge
{
    /// <summary>
    /// Main window for ClearMerge tool - provides overview and access to conflict resolution features
    /// </summary>
    public class MainWindow : EditorWindow
    {
        #region Fields
        private int cachedMetaConflicts = 0;
        private List<AssetConflict> cachedAssetConflicts = new();
        private int cachedSceneConflicts = 0;
        private bool isDataLoaded = false;
        #endregion

        #region Window Management
        public static void ShowWindow()
        {
            var window = GetWindow<MainWindow>("ClearMerge");
            window.minSize = new Vector2(400, 300);
            window.Show();
        }

        private void OnEnable()
        {
            RefreshConflictData();
        }
        #endregion

        #region GUI
        private void OnGUI()
        {
            DrawHeader();
            GUILayout.Space(20);
            DrawConflictStatus();
            GUILayout.Space(20);
            DrawToolButtons();
            GUILayout.Space(20);
            DrawSettingsButton();
        }

        private void DrawHeader()
        {
            GUILayout.BeginVertical("box");

            var titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 24,
                alignment = TextAnchor.MiddleCenter
            };
            GUILayout.Label("ClearMerge", titleStyle);

            GUILayout.Space(5);

            var descStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
            GUILayout.Label("Tool to resolve Unity merge conflicts simply and efficiently", descStyle);

            GUILayout.EndVertical();
        }
        #endregion

        #region Data Management
        private void RefreshConflictData()
        {
            isDataLoaded = false;
            EditorUtility.DisplayProgressBar("ClearMerge", "Scanning for conflicts...", 0.0f);

            try
            {
                EditorUtility.DisplayProgressBar("ClearMerge", "Scanning .meta files...", 0.33f);
                cachedMetaConflicts = ClearMerge.Assets.MetaConflictResolver.FindMetaConflicts().Count;

                EditorUtility.DisplayProgressBar("ClearMerge", "Scanning assets...", 0.66f);
                cachedAssetConflicts = ClearMerge.Assets.ConflictAnalyzer.ScanProjectForConflicts();

                EditorUtility.DisplayProgressBar("ClearMerge", "Scanning scenes...", 1.0f);
                cachedSceneConflicts = GetSceneConflicts();

                isDataLoaded = true;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                Repaint();
            }
        }

        private int GetSceneConflicts()
        {
            var sceneFiles = System.IO.Directory.GetFiles("Assets", "*.unity", System.IO.SearchOption.AllDirectories);
            int conflicts = 0;

            foreach (string scenePath in sceneFiles)
            {
                if (System.IO.File.Exists(scenePath))
                {
                    string content = System.IO.File.ReadAllText(scenePath);
                    if (content.Contains("<<<<<<< HEAD") ||
                        content.Contains("=======") ||
                        content.Contains(">>>>>>> "))
                    {
                        conflicts++;
                    }
                }
            }

            return conflicts;
        }
        #endregion

        #region UI Components
        private void DrawConflictStatus()
        {
            GUILayout.BeginVertical("box");

            var headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16
            };
            GUILayout.Label("📊 Current Status", headerStyle);

            GUILayout.Space(10);

            if (!isDataLoaded)
            {
                GUILayout.Label("⏳ Loading data...", EditorStyles.centeredGreyMiniLabel);
            }
            else
            {
                DrawStatusLine("📄 .meta files:", cachedMetaConflicts);
                DrawStatusLine("🎨 Assets:", cachedAssetConflicts.Count);
                DrawStatusLine("🎭 Scenes:", cachedSceneConflicts);
            }

            GUILayout.Space(10);

            if (GUILayout.Button("🔍 Scan Now", GUILayout.Height(25)))
            {
                RefreshConflictData();
            }

            GUILayout.EndVertical();
        }

        private void DrawStatusLine(string label, int count)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(120));

            var color = count > 0 ? Color.red : Color.green;
            var oldColor = GUI.color;
            GUI.color = color;

            GUILayout.Label(count.ToString(), EditorStyles.boldLabel);
            GUI.color = oldColor;

            if (count > 0)
            {
                GUILayout.Label("conflicts");
            }
            else
            {
                GUILayout.Label("no conflicts");
            }

            GUILayout.EndHorizontal();
        }

        private void DrawToolButtons()
        {
            GUILayout.BeginVertical("box");

            var headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16
            };
            GUILayout.Label("🛠️ Tools", headerStyle);

            GUILayout.Space(10);

            if (GUILayout.Button("🎭 Resolve Scene Conflicts", GUILayout.Height(30)))
            {
                Scenes.ConflictViewerWindow.ShowWindow();
            }

            GUILayout.Space(5);

            if (GUILayout.Button("🎨 Resolve Asset Conflicts", GUILayout.Height(30)))
            {
                Assets.ConflictViewerWindow.ShowWindow();
            }

            GUILayout.EndVertical();
        }

        private void DrawSettingsButton()
        {
            if (GUILayout.Button("⚙️ Settings", GUILayout.Height(25)))
            {
                SettingsService.OpenProjectSettings("Project/ClearMerge");
            }
        }
        #endregion
    }
}
