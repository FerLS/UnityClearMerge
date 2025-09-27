using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using ClearMerge.Utils;
using System.IO;

namespace ClearMerge.Assets
{
    /// <summary>
    /// Window for resolving asset conflicts with visual conflict resolution interface
    /// </summary>
    public class ConflictViewerWindow : EditorWindow
    {
        #region Fields
        private List<AssetConflict> detectedConflicts = new List<AssetConflict>();
        private Vector2 scrollPosition;
        private Vector2 detailScrollPosition;
        private AssetConflict selectedConflict = null;
        private string searchFilter = "";

        private GUIStyle headerStyle;
        private GUIStyle conflictItemStyle;
        private GUIStyle resolvedItemStyle;
        private GUIStyle beforeChoiceStyle;
        private GUIStyle afterChoiceStyle;
        private GUIStyle selectedChoiceStyle;
        #endregion

        #region Window Management
        public static void ShowWindow()
        {
            var window = GetWindow<ConflictViewerWindow>("Asset Conflict Resolver");
            window.minSize = new Vector2(1000, 700);
            window.ScanForConflicts();
        }

        private void OnEnable()
        {
            ScanForConflicts();
        }
        #endregion

        #region Style Initialization
        private void InitializeStyles()
        {
            if (headerStyle == null)
            {
                headerStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 16,
                    normal = { textColor = Color.white }
                };
            }

            if (conflictItemStyle == null)
            {
                conflictItemStyle = new GUIStyle(EditorStyles.helpBox)
                {
                    normal = { textColor = new Color(1f, 0.8f, 0.8f) }
                };
            }

            if (resolvedItemStyle == null)
            {
                resolvedItemStyle = new GUIStyle(EditorStyles.helpBox)
                {
                    normal = { textColor = new Color(0.8f, 1f, 0.8f) }
                };
            }

            if (beforeChoiceStyle == null)
            {
                beforeChoiceStyle = new GUIStyle(EditorStyles.helpBox)
                {
                    normal = { textColor = new Color(0.9f, 0.9f, 0.6f) },
                    padding = new RectOffset(8, 8, 8, 8)
                };
            }

            if (afterChoiceStyle == null)
            {
                afterChoiceStyle = new GUIStyle(EditorStyles.helpBox)
                {
                    normal = { textColor = new Color(0.6f, 0.9f, 0.9f) },
                    padding = new RectOffset(8, 8, 8, 8)
                };
            }

            if (selectedChoiceStyle == null)
            {
                selectedChoiceStyle = new GUIStyle(EditorStyles.helpBox)
                {
                    normal = { textColor = Color.white },
                    padding = new RectOffset(8, 8, 8, 8)
                };
            }
        }
        #endregion

        #region GUI
        private void OnGUI()
        {
            InitializeStyles();

            EditorGUILayout.BeginVertical();
            DrawHeader();
            DrawToolbar();
            DrawMainContent();
            EditorGUILayout.EndVertical();
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Asset Conflict Resolver", headerStyle);
            EditorGUILayout.Space();

            int total = detectedConflicts.Count;
            int unresolved = detectedConflicts.Count(c => !c.isResolved);

            if (total == 0)
            {
                EditorGUILayout.HelpBox("No conflicts detected in the project.", MessageType.Info);
            }
            else if (unresolved == 0)
            {
                EditorGUILayout.HelpBox($"All {total} conflicts have been resolved!", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox($"{unresolved} unresolved conflicts out of {total} total.", MessageType.Warning);
            }
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button("Scan for Conflicts", EditorStyles.toolbarButton))
                ScanForConflicts();

            GUILayout.FlexibleSpace();
            GUILayout.Label("Filter:", GUILayout.Width(50));
            searchFilter = GUILayout.TextField(searchFilter, EditorStyles.toolbarTextField, GUILayout.Width(200));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
        }

        private void DrawMainContent()
        {
            var list = GetFilteredConflicts();
            if (list.Count == 0)
            {
                EditorGUILayout.LabelField("No conflicts match the current filter.", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            EditorGUILayout.BeginHorizontal();
            // Left: list of assets
            EditorGUILayout.BeginVertical(GUILayout.Width(300));
            EditorGUILayout.LabelField("Assets with Conflicts", EditorStyles.boldLabel);
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, EditorStyles.helpBox);
            foreach (var conflict in list)
                DrawConflictItem(conflict);
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            // Right: details
            EditorGUILayout.BeginVertical();
            DrawConflictDetails();
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawConflictItem(AssetConflict conflict)
        {
            var style = conflict.isResolved ? resolvedItemStyle : conflictItemStyle;
            bool selected = selectedConflict == conflict;
            var origBg = GUI.backgroundColor;
            if (selected) GUI.backgroundColor = Color.cyan;

            EditorGUILayout.BeginVertical(style);
            EditorGUILayout.BeginHorizontal();
            // status icon
            GUI.color = conflict.isResolved ? Color.green : Color.yellow;
            GUILayout.Label(conflict.isResolved ? "✓" : "⚠", GUILayout.Width(20));
            GUI.color = Color.white;
            if (GUILayout.Button(conflict.assetName, EditorStyles.label))
                selectedConflict = conflict;
            EditorGUILayout.EndHorizontal();

            // propertyConflicts count
            EditorGUILayout.LabelField($"Conflicts: {conflict.propertyConflicts.Count}", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();

            var rect = GUILayoutUtility.GetLastRect();
            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                selectedConflict = conflict;
                Event.current.Use();
                Repaint();
            }
            GUI.backgroundColor = origBg;
        }

        private void DrawConflictDetails()
        {
            if (selectedConflict == null)
            {
                EditorGUILayout.LabelField("Select a conflict to see details", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            EditorGUILayout.LabelField("Conflict Details", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Asset: {selectedConflict.assetPath}");
            EditorGUILayout.Space();

            bool isBinary = IsSpecialAssetType(selectedConflict.assetPath);
            if (!isBinary && GUILayout.Button("Open Asset", GUILayout.Width(100)))
            {
                var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(selectedConflict.assetPath);
                if (asset != null) AssetDatabase.OpenAsset(asset);
            }
            EditorGUILayout.Space();

            if (isBinary)
            {
                DrawSpecialAssetControls();
            }
            else
            {
                DrawGlobalSelectionButtons();
                EditorGUILayout.Space();

                detailScrollPosition = EditorGUILayout.BeginScrollView(detailScrollPosition);
                foreach (var prop in selectedConflict.propertyConflicts)
                {
                    DrawPropertyConflict(prop);
                    EditorGUILayout.Space();
                }
                EditorGUILayout.EndScrollView();
            }

            EditorGUILayout.Space();
            DrawSaveButton();
        }

        private void DrawSpecialAssetControls()
        {
            EditorGUILayout.HelpBox(
                "Este es un archivo binario/especial. Puedes elegir la versión completa o previsualizar externamente.",
                MessageType.Info);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Previsualizar BEFORE", GUILayout.Height(40)))
                PreviewBinaryVersion(selectedConflict, true);
            if (GUILayout.Button("Previsualizar AFTER", GUILayout.Height(40)))
                PreviewBinaryVersion(selectedConflict, false);
            EditorGUILayout.EndHorizontal();

            // elegir BEFORE/AFTER
            EditorGUILayout.BeginHorizontal();
            var origBg = GUI.backgroundColor;
            bool beforeSel = selectedConflict.propertyConflicts.Count > 0 &&
                             selectedConflict.propertyConflicts[0].SelectedValue == ConflictChoice.Before;
            bool afterSel = selectedConflict.propertyConflicts.Count > 0 &&
                            selectedConflict.propertyConflicts[0].SelectedValue == ConflictChoice.After;

            if (beforeSel) GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Use BEFORE Version", GUILayout.Height(30)))
            {
                foreach (var p in selectedConflict.propertyConflicts) p.SelectedValue = ConflictChoice.Before;
                CheckConflictResolution();
            }
            GUI.backgroundColor = origBg;

            if (afterSel) GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Use AFTER Version", GUILayout.Height(30)))
            {
                foreach (var p in selectedConflict.propertyConflicts) p.SelectedValue = ConflictChoice.After;
                CheckConflictResolution();
            }
            GUI.backgroundColor = origBg;
            EditorGUILayout.EndHorizontal();
        }

        private void DrawGlobalSelectionButtons()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Select All BEFORE", GUILayout.Height(25)))
            {
                foreach (var p in selectedConflict.propertyConflicts) p.SelectedValue = ConflictChoice.Before;
                CheckConflictResolution();
            }
            if (GUILayout.Button("Select All AFTER", GUILayout.Height(25)))
            {
                foreach (var p in selectedConflict.propertyConflicts) p.SelectedValue = ConflictChoice.After;
                CheckConflictResolution();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawPropertyConflict(PropertyConflict prop)
        {
            EditorGUILayout.LabelField($"Property: {prop.PropertyName}", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            // BEFORE
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("BEFORE", EditorStyles.centeredGreyMiniLabel);
            var beforeStyle = prop.SelectedValue == ConflictChoice.Before ? selectedChoiceStyle : beforeChoiceStyle;
            var origBg = GUI.backgroundColor;
            if (prop.SelectedValue == ConflictChoice.Before) GUI.backgroundColor = Color.green;
            if (GUILayout.Button(FormatPropertyValue(prop.BeforeValue), beforeStyle, GUILayout.Height(60)))
            {
                prop.SelectedValue = ConflictChoice.Before;
                CheckConflictResolution();
            }
            GUI.backgroundColor = origBg;
            EditorGUILayout.EndVertical();

            GUILayout.Space(10);

            // AFTER
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("AFTER", EditorStyles.centeredGreyMiniLabel);
            var afterStyle = prop.SelectedValue == ConflictChoice.After ? selectedChoiceStyle : afterChoiceStyle;
            if (prop.SelectedValue == ConflictChoice.After) GUI.backgroundColor = Color.green;
            if (GUILayout.Button(FormatPropertyValue(prop.AfterValue), afterStyle, GUILayout.Height(60)))
            {
                prop.SelectedValue = ConflictChoice.After;
                CheckConflictResolution();
            }
            GUI.backgroundColor = origBg;
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawSaveButton()
        {
            if (selectedConflict != null && selectedConflict.isResolved)
            {
                if (GUILayout.Button("Save Resolution", GUILayout.Height(30)))
                    SaveConflictResolution();
            }
        }
        #endregion

        #region Logic Methods
        private void ScanForConflicts()
        {
            detectedConflicts = ConflictAnalyzer.ScanProjectForConflicts();
            selectedConflict = null;
            Repaint();
        }

        private List<AssetConflict> GetFilteredConflicts()
        {
            if (string.IsNullOrEmpty(searchFilter))
                return detectedConflicts;
            return detectedConflicts
                .Where(c => c.assetName.ToLower().Contains(searchFilter.ToLower())
                         || c.assetPath.ToLower().Contains(searchFilter.ToLower()))
                .ToList();
        }

        private bool IsSpecialAssetType(string assetPath)
        {
            var ext = Path.GetExtension(assetPath).ToLower();
            return ClearMergeConstants.BINARY_ASSET_EXTENSIONS.Contains(ext);
        }

        private string FormatPropertyValue(string val)
        {
            if (string.IsNullOrEmpty(val)) return "[Empty]";
            return val.Length <= 100 ? val : val.Substring(0, 97) + "...";
        }

        private void CheckConflictResolution()
        {
            if (selectedConflict == null) return;
            selectedConflict.isResolved =
                selectedConflict.propertyConflicts.All(p => p.SelectedValue != ConflictChoice.None);
        }

        private void PreviewBinaryVersion(AssetConflict conflict, bool before)
        {
            if (conflict == null) return;
            string projectRoot = Application.dataPath.Substring(0, Application.dataPath.Length - "/Assets".Length);
            string tempDir = Path.Combine(Path.GetTempPath(), "ClearMergePreview");
            Directory.CreateDirectory(tempDir);
            string tempFile = Path.Combine(tempDir, (before ? "before_" : "after_") + Path.GetFileName(conflict.assetPath));

            bool found = false;
            string[] stages = before ? new[] { ":2:", ":1:" } : new[] { ":3:", ":2:" };
            foreach (var stage in stages)
            {
                var psi = new System.Diagnostics.ProcessStartInfo("git", $"show {stage}{conflict.assetPath}")
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    WorkingDirectory = projectRoot,
                    CreateNoWindow = true
                };
                try
                {
                    using var proc = System.Diagnostics.Process.Start(psi);
                    using var fs = new FileStream(tempFile, FileMode.Create, FileAccess.Write);
                    proc.StandardOutput.BaseStream.CopyTo(fs);
                    proc.WaitForExit(2000);
                    if (fs.Length > 0) { found = true; break; }
                }
                catch { }
            }

            if (!found)
            {
                EditorUtility.DisplayDialog("Error", "No se pudo extraer la versión desde Git.", "OK");
                return;
            }

            try
            {
                var pi = new System.Diagnostics.ProcessStartInfo(tempFile) { UseShellExecute = true };
                System.Diagnostics.Process.Start(pi);
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("Error", $"No se pudo abrir el archivo: {e.Message}", "OK");
            }
        }

        private void SaveConflictResolution()
        {
            if (selectedConflict == null || !selectedConflict.isResolved) return;
            if (AssetConflictResolver.ResolveAssetConflict(selectedConflict))
            {
                EditorUtility.DisplayDialog("Success",
                    $"Conflict resolution saved for {selectedConflict.assetName}", "OK");
                AssetDatabase.Refresh();
                ScanForConflicts();
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "Failed to save resolution", "OK");
            }
        }
        #endregion
    }
}
