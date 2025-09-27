using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClearMerge.Utils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ClearMerge.Scenes
{
    /// <summary>
    /// Main editor window for managing and resolving conflicts in Unity scenes
    /// </summary>
    public class ConflictViewerWindow : EditorWindow
    {
        #region Properties and Fields
        private string sceneAssetPath;
        private string headSceneContent;
        private Vector2 scrollPosPending;
        private Vector2 scrollPosResolved;
        private List<GameObjectConflict> gameObjectConflicts = new();
        private Dictionary<string, GameObjectConflict> conflictLookup = new(); // Cache for fast lookups
        private Dictionary<string, GameObject> idToGameObjectChanged = new();

        bool isOnPreview = false; // Indicates if we are in scene preview mode
        bool isInFinalPreview = false; // Indicates if we are in the final preview before saving
        string tempPreviewScenePath; // Path for the temporary preview scene

        // GUI Styles
        private GUIStyle headerStyle;
        private GUIStyle cardStyle;
        private GUIStyle beforeButtonStyle;
        private GUIStyle afterButtonStyle;
        private GUIStyle selectedButtonStyle;

        // Access to other components
        private ConflictAnalyzer analyzer;
        private SceneConflictRenderer renderer;
        private ObjectMapper mapper;
        private ConflictResolver resolver;
        #endregion

        #region Window Management
        /// <summary>
        /// Shows the scene conflict viewer window
        /// </summary>
        public static void ShowWindow()
        {
            if (!ClearMerge.Utils.ClearMergeSetup.IsSetupComplete())
            {
                EditorUtility.DisplayDialog("Setup Required",
                    "Scene Conflict Viewer requires setup before use. Opening setup window.", "OK");
                ClearMergeSetup.ShowWindow();
                return;
            }

            var window = GetWindow<ConflictViewerWindow>("Scene Conflict Viewer");
            window.minSize = new Vector2(800, 400);
            window.AutoDetectScene();
        }

        /// <summary>
        /// Configures the window when enabled
        /// </summary>
        private void OnEnable()
        {
            // Initialize components
            analyzer = new ConflictAnalyzer(
                ref gameObjectConflicts);

            mapper = new ObjectMapper(
                ref gameObjectConflicts
              );

            renderer = new SceneConflictRenderer(
                ref gameObjectConflicts,
                this
              );

            resolver = new ConflictResolver(
                ref gameObjectConflicts
              );

            PropertyConflictDrawer.Initialize(
                ref gameObjectConflicts
            );
            SceneView.duringSceneGui -= renderer.OnSceneGUI;
            SceneView.duringSceneGui += renderer.OnSceneGUI;
            isOnPreview = false;
            isInFinalPreview = false;

            // Assign a path for the temporary preview scene
            tempPreviewScenePath = ClearMergeConstants.TempFinalPreviewScenePath;
        }

        /// <summary>
        /// Cleans up resources when the window is disabled
        /// </summary>
        private void OnDisable()
        {
            PropertyConflictDrawer.Shutdown();
            SceneView.duringSceneGui -= renderer.OnSceneGUI;
        }

        /// <summary>
        /// Cleans up resources when the window is closed
        /// </summary>
        private void OnDestroy()
        {
            // Always return to the original scene when the window is closed, regardless of state
            if (!string.IsNullOrEmpty(sceneAssetPath))
                EditorSceneManager.OpenScene(sceneAssetPath);

            if (File.Exists(ClearMergeConstants.TempBeforeScenePath))
                AssetDatabase.DeleteAsset(ClearMergeConstants.TempBeforeScenePath);

            if (File.Exists(ClearMergeConstants.TempAfterScenePath))
                AssetDatabase.DeleteAsset(ClearMergeConstants.TempAfterScenePath);

            // Ensure you delete the correct file
            if (File.Exists(ClearMergeConstants.TempFinalPreviewScenePath))
                AssetDatabase.DeleteAsset(ClearMergeConstants.TempFinalPreviewScenePath);

            gameObjectConflicts.Clear();
        }

        /// <summary>
        /// Updates the fast conflict lookup cache
        /// </summary>
        private void UpdateConflictLookup()
        {
            conflictLookup.Clear();
            foreach (var conflict in gameObjectConflicts)
            {
                conflictLookup[conflict.Id] = conflict;
            }
        }

        /// <summary>
        /// Gets a conflict by ID in an optimized way
        /// </summary>
        private GameObjectConflict GetConflictById(string id)
        {
            conflictLookup.TryGetValue(id, out GameObjectConflict conflict);
            return conflict;
        }

        /// <summary>
        /// Automatically detects the active scene
        /// </summary>
        private void AutoDetectScene()
        {
            string activeScenePath = EditorSceneManager.GetActiveScene().path;
            if (!string.IsNullOrEmpty(activeScenePath))
            {
                sceneAssetPath = activeScenePath;
                analyzer.AnalyzeScene(sceneAssetPath);
                UpdateConflictLookup(); // Update cache after analysis
                LoadHeadScene();
            }
        }

        /// <summary>
        /// Loads the HEAD version of the scene from Git
        /// </summary>
        private void LoadHeadScene()
        {
            headSceneContent = ClearMerge.Utils.GitManager.GetFileContent(sceneAssetPath);
            if (string.IsNullOrEmpty(headSceneContent))
                EditorUtility.DisplayDialog("Error", "Could not get HEAD version of the scene.", "OK");
        }
        #endregion

        #region GUI
        /// <summary>
        /// Initializes GUI styles
        /// </summary>
        private void InitStyles()
        {
            if (headerStyle == null)
            {
                headerStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 18,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(0.2f, 0.4f, 0.8f) }
                };
                cardStyle = new GUIStyle(EditorStyles.helpBox)
                {
                    padding = new RectOffset(12, 12, 8, 8),
                    margin = new RectOffset(0, 0, 4, 4),
                    fontSize = 13
                };

                beforeButtonStyle = new GUIStyle(GUI.skin.button);
                afterButtonStyle = new GUIStyle(GUI.skin.button);
                selectedButtonStyle = new GUIStyle(GUI.skin.button)
                {
                    fontStyle = FontStyle.Bold,
                    normal = { background = Texture2D.grayTexture, textColor = Color.white }
                };
            }
        }

        /// <summary>
        /// Draws the user interface
        /// </summary>
        private void OnGUI()
        {
            InitStyles();

            // General padding for the main layout
            GUIStyle paddedStyle = new GUIStyle();
            paddedStyle.padding = new RectOffset(20, 20, 16, 16);

            EditorGUILayout.BeginVertical(paddedStyle);

            GUILayout.Space(8);

            if (isInFinalPreview)
            {
                DrawFinalPreviewGUI();
            }
            else
            {
                DrawConflictResolutionGUI();
            }

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Draws the final preview interface before saving
        /// </summary>
        private void DrawFinalPreviewGUI()
        {
            // Style for the info bar
            var previewInfoStyle = new GUIStyle(EditorStyles.helpBox);
            previewInfoStyle.normal.textColor = Color.white;
            previewInfoStyle.alignment = TextAnchor.MiddleCenter;
            previewInfoStyle.fontSize = 14;
            previewInfoStyle.fontStyle = FontStyle.Bold;

            // Background for the info bar
            Texture2D backgroundTexture = new Texture2D(1, 1);
            backgroundTexture.SetPixel(0, 0, new Color(0.2f, 0.6f, 0.9f));
            backgroundTexture.Apply();
            previewInfoStyle.normal.background = backgroundTexture;

            GUILayout.Label("🔍 FINAL SCENE PREVIEW", headerStyle);
            GUILayout.Space(10);

            EditorGUILayout.LabelField("This is a preview of how the scene will look with your changes.", previewInfoStyle, GUILayout.Height(40));
            GUILayout.Space(10);
            EditorGUILayout.LabelField("You can make additional changes directly in the scene before confirming.", EditorStyles.wordWrappedLabel);
            GUILayout.Space(20);

            EditorGUILayout.LabelField($"📂 Scene: {Path.GetFileName(sceneAssetPath)}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Status: Preview (unsaved changes)", EditorStyles.miniBoldLabel);

            GUILayout.Space(20);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button(new GUIContent("⬅️ Back to Resolution", "Return to conflict resolution"),
                    GUILayout.Height(30), GUILayout.Width(200)))
            {
                CancelPreview();
            }

            GUILayout.Space(20);

            if (GUILayout.Button(new GUIContent("✅ CONFIRM AND SAVE", "Save the scene with all changes"),
                    GUILayout.Height(40), GUILayout.Width(250)))
            {
                SaveFinalScene();
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Draws the conflict resolution interface
        /// </summary>
        private void DrawConflictResolutionGUI()
        {
            GUILayout.Label("🧠 Scene Conflict Viewer", headerStyle);

            if (string.IsNullOrEmpty(sceneAssetPath))
            {
                EditorGUILayout.HelpBox("No active scene detected. Open a scene and try again.", MessageType.Warning);
                return;
            }

            GUILayout.Label($"📂 Scene: {Path.GetFileName(sceneAssetPath)}", EditorStyles.helpBox);

            // Button to preview both scenes
            if (gameObjectConflicts.Count > 0)
            {
                GUILayout.Space(6);
                if (!isOnPreview && GUILayout.Button(new GUIContent("🔄 Preview Scenes", "Load both versions for visual comparison"), GUILayout.Height(28)))
                {
                    isOnPreview = true;
                    LoadDualSceneComparison();
                }
            }

            GUILayout.Space(10);

            // Buttons to select all with symmetric spacing

            if (isOnPreview)
            {
                EditorGUILayout.BeginHorizontal();

                float buttonWidth = (position.width - 50) / 2; // Divide evenly minus padding and spacing

                if (GUILayout.Button("Select ALL BEFORE", GUILayout.Height(28), GUILayout.Width(buttonWidth)))
                {
                    foreach (var conflict in gameObjectConflicts)
                        conflict.Choice = ConflictChoice.Before;
                }

                GUILayout.Space(10); // Spacing between buttons

                if (GUILayout.Button("Select ALL AFTER", GUILayout.Height(28), GUILayout.Width(buttonWidth)))
                {
                    foreach (var conflict in gameObjectConflicts)
                        conflict.Choice = ConflictChoice.After;
                }

                EditorGUILayout.EndHorizontal();
            }
            GUILayout.Space(10);

            // Separate lists
            DrawConflictLists();

            // Fixed preview button at the bottom
            GUILayout.FlexibleSpace();
            GUILayout.Space(10);
            DrawSaveButton();
        }

        /// <summary>
        /// Draws the lists of pending and resolved conflicts
        /// </summary>
        private void DrawConflictLists()
        {
            List<GameObjectConflict> pending = new List<GameObjectConflict>();
            List<GameObjectConflict> selected = new List<GameObjectConflict>();

            foreach (var conflict in gameObjectConflicts)
            {
                if (conflict.Choice == ConflictChoice.None)
                    pending.Add(conflict);
                else
                    selected.Add(conflict);
            }

            // Calculate available height for the lists
            float availableHeight = position.height - 200; // Reserve space for header and buttons
            float listHeight = Mathf.Max(200, availableHeight);

            // Calculate symmetric widths with uniform spacing
            float totalWidth = position.width - 40; // Subtract general padding
            float spacing = 10; // Spacing between columns
            float columnWidth = (totalWidth - spacing) / 2; // Divide evenly

            // Horizontal layout for the two lists side by side
            EditorGUILayout.BeginHorizontal();

            // Left column - Pending
            EditorGUILayout.BeginVertical(GUILayout.Width(columnWidth));
            GUILayout.Label($"⏳ Pending ({pending.Count})", EditorStyles.boldLabel);

            if (pending.Count == 0)
            {
                EditorGUILayout.HelpBox("No pending objects to resolve.", MessageType.Info);
            }
            else
            {
                scrollPosPending = EditorGUILayout.BeginScrollView(scrollPosPending, GUILayout.Height(listHeight));
                foreach (var conflict in pending)
                    DrawConflictCard(conflict);
                EditorGUILayout.EndScrollView();
            }
            EditorGUILayout.EndVertical();

            // Spacing between columns
            GUILayout.Space(spacing);

            // Right column - Resolved
            EditorGUILayout.BeginVertical(GUILayout.Width(columnWidth));
            GUILayout.Label($"✅ Resolved ({selected.Count})", EditorStyles.boldLabel);

            if (selected.Count == 0)
            {
                EditorGUILayout.HelpBox("No objects resolved yet.", MessageType.Info);
            }
            else
            {
                scrollPosResolved = EditorGUILayout.BeginScrollView(scrollPosResolved, GUILayout.Height(listHeight));
                foreach (var conflict in selected)
                    DrawConflictCard(conflict);
                EditorGUILayout.EndScrollView();
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Draws an individual card for a conflict
        /// </summary>
        private void DrawConflictCard(GameObjectConflict conflict)
        {
            EditorGUILayout.BeginVertical(cardStyle);
            EditorGUILayout.LabelField($"🧩 {conflict.Name ?? "Unknown"}", EditorStyles.boldLabel);
            DrawComponentConflicts(conflict.Id, conflict.ComponentConflicts);
            EditorGUILayout.EndVertical();
            GUILayout.Space(5);
        }

        /// <summary>
        /// Draws component conflicts for an object
        /// </summary>
        private void DrawComponentConflicts(string id, List<ComponentConflict> conflicts)
        {
            // Get the conflict reference only once
            var gameObjectConflict = GetConflictById(id);
            if (gameObjectConflict == null) return;

            foreach (var conflict in conflicts)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"  📋 {conflict.Name}", GUILayout.Width(150));
                if (isOnPreview)
                {
                    // Focus buttons with uniform spacing
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button(new GUIContent("🟡", "Focus BEFORE version (controls visibility with eye icon)"), GUILayout.Width(25)))
                        FocusObject(id, true);

                    GUILayout.Space(5);

                    if (GUILayout.Button(new GUIContent("🔴", "Focus AFTER version (controls visibility with eye icon)"), GUILayout.Width(25)))
                        FocusObject(id, false);

                    GUILayout.Space(10);

                    // Setup selection status using cached reference
                    bool hasSelection = gameObjectConflict.Choice != ConflictChoice.None;
                    bool isAfterSelected = hasSelection && gameObjectConflict.Choice == ConflictChoice.After;

                    // Build tooltips from all properties
                    string beforeTooltip = "BEFORE version (HEAD):\n";
                    string afterTooltip = "AFTER version (changes):\n";
                    foreach (var change in conflict.ChangedProperties)
                    {
                        beforeTooltip += $"• {change.PropertyName}: {change.BeforeValue}\n";
                        afterTooltip += $"• {change.PropertyName}: {change.AfterValue}\n";
                    }

                    // BEFORE/AFTER buttons with fixed width and symmetric spacing
                    float buttonWidth = 85;
                    float buttonSpacing = 5;
                    // BEFORE button (only visible in preview mode)

                    if (GUILayout.Button(new GUIContent((hasSelection && !isAfterSelected) ? "✓ BEFORE" : "BEFORE", beforeTooltip),
                        (hasSelection && !isAfterSelected) ? selectedButtonStyle : beforeButtonStyle, GUILayout.Width(buttonWidth)))
                    {
                        if (hasSelection && !isAfterSelected)
                            gameObjectConflict.Choice = ConflictChoice.None;
                        else
                        {
                            gameObjectConflict.Choice = ConflictChoice.Before;
                            // Switch to show BEFORE scene using eye icon
                            mapper.SetSceneVisibility(true);
                            FocusObject(id, true);
                        }
                    }


                    GUILayout.Space(buttonSpacing);

                    // AFTER button
                    if (GUILayout.Button(new GUIContent((hasSelection && isAfterSelected) ? "✓ AFTER" : "AFTER", afterTooltip),
                        (hasSelection && isAfterSelected) ? selectedButtonStyle : afterButtonStyle, GUILayout.Width(buttonWidth)))
                    {
                        if (hasSelection && isAfterSelected)
                            gameObjectConflict.Choice = ConflictChoice.None;
                        else
                        {
                            gameObjectConflict.Choice = ConflictChoice.After;
                            // Switch to show AFTER scene using eye icon
                            mapper.SetSceneVisibility(false);
                            FocusObject(id, false);
                        }
                        Debug.Log($"Conflict ID: {id}, Choice: {gameObjectConflict.Choice}");

                    }
                }
                EditorGUILayout.EndHorizontal();

                // Display properties outside the horizontal layouts
                if (conflict.ChangedProperties.Count > 0)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    int displayCount = Mathf.Min(conflict.ChangedProperties.Count, 2);

                    for (int i = 0; i < displayCount; i++)
                    {
                        var propChange = conflict.ChangedProperties[i];
                        EditorGUILayout.LabelField($"    • {propChange.PropertyName}: {propChange.BeforeValue} → {propChange.AfterValue}");
                    }

                    if (conflict.ChangedProperties.Count > displayCount)
                        EditorGUILayout.LabelField($"    • ... and {conflict.ChangedProperties.Count - displayCount} more change(s)");

                    EditorGUILayout.EndVertical();
                }
            }
        }

        /// <summary>
        /// Draws the save button
        /// </summary>
        private void DrawSaveButton()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            float buttonWidth = Mathf.Min(400, position.width - 80); // Maximum width but responsive

            // Use gameObjectConflicts to determine if all conflicts are resolved
            bool allResolved = gameObjectConflicts.Count > 0 &&
                       gameObjectConflicts.All(c => c.Choice != ConflictChoice.None);

            if (allResolved)
            {
                if (GUILayout.Button(new GUIContent("🔍 PREVIEW", "Preview the scene with resolved conflicts before saving"),
                    GUILayout.Height(40), GUILayout.Width(buttonWidth)))
                {
                    CreatePreviewScene();
                }
            }
            else
            {
                GUI.enabled = false;
                GUILayout.Button(new GUIContent("🔍 PREVIEW", "You must resolve all conflicts before previewing"),
                    GUILayout.Height(40), GUILayout.Width(buttonWidth));
                GUI.enabled = true;
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Creates and loads a preview scene with resolved conflicts
        /// </summary>
        private void CreatePreviewScene()
        {
            string resolvedContent = resolver.GenerateResolvedSceneContent(sceneAssetPath);

            if (!string.IsNullOrEmpty(resolvedContent))
            {
                // Ensure the directory exists
                string directory = Path.GetDirectoryName(tempPreviewScenePath);
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                // Save the temporary scene with your selections
                File.WriteAllText(tempPreviewScenePath, resolvedContent);
                AssetDatabase.ImportAsset(tempPreviewScenePath);

                // Create backup of the current scene
                string tempBackupPath = tempPreviewScenePath.Replace(".unity", "_backup.unity");
                EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), tempBackupPath, false);

                // Open the preview scene
                EditorSceneManager.OpenScene(tempPreviewScenePath);

                // Switch interface to preview mode
                isInFinalPreview = true;

                // Show informative message with selection details
                int afterCount = gameObjectConflicts.Count(kv => kv.Choice == ConflictChoice.After);
                int beforeCount = gameObjectConflicts.Count(kv => kv.Choice == ConflictChoice.Before);

                EditorUtility.DisplayDialog("Preview Activated",
                    $"Scene loaded with your selections:\n" +
                    $"- {beforeCount} objects with BEFORE version (HEAD)\n" +
                    $"- {afterCount} objects with AFTER version (new changes)\n\n" +
                    "Make any additional adjustments directly in the scene before confirming save.",
                    "OK");

                // Delete the temporary backup file when no longer needed
                if (File.Exists(tempBackupPath))
                    AssetDatabase.DeleteAsset(tempBackupPath);
            }
            else
            {
                EditorUtility.DisplayDialog("Error",
                    "Could not create the preview scene.",
                    "OK");
            }
        }

        /// <summary>
        /// Cancels the preview and returns to conflict resolution
        /// </summary>
        private void CancelPreview()
        {
            isInFinalPreview = false;

            // Restore original scenes for conflict resolution
            if (isOnPreview)
            {
                LoadDualSceneComparison();
            }
            else
            {
                // Return to the original scene with conflicts
                EditorSceneManager.OpenScene(sceneAssetPath);
            }
        }

        /// <summary>
        /// Saves the final scene with applied changes
        /// </summary>
        private void SaveFinalScene()
        {
            Scene currentScene = EditorSceneManager.GetActiveScene();

            // The user may have made additional changes in the previewed scene,
            // so we save the scene as it currently is

            // Determine if we save as a new file or overwrite
            bool isNewScene = ClearMergeSettings.MergeResolution == ClearMergeSettings.MergeResolutionStrategy.SaveAsNew;
            string finalPath;

            if (isNewScene)
            {
                string directory = Path.GetDirectoryName(sceneAssetPath);
                string fileName = $"NEW_{Path.GetFileName(sceneAssetPath)}";
                finalPath = Path.Combine(directory, fileName);
            }
            else
            {
                finalPath = sceneAssetPath;
            }

            // Save the final scene
            bool success = EditorSceneManager.SaveScene(currentScene, finalPath, true);

            if (success)
            {
                // Git options according to configuration
                if (ClearMergeSettings.GitCommitAction != ClearMergeSettings.GitCommitStrategy.DontCommit)
                {
                    bool commitSuccess = ClearMerge.Utils.GitManager.CommitAndPushResolvedScene(
                        finalPath,
                        isNewScene,
                        Path.GetFileName(finalPath));

                    if (commitSuccess)
                    {
                        EditorUtility.DisplayDialog("Scene Saved and Committed",
                            $"Conflicts have been resolved and changes committed to Git.\nBranch: {ClearMerge.Utils.GitManager.GetCurrentBranch()}",
                            "OK");
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Scene Saved",
                            "Conflicts have been resolved but there was a problem with the Git commit.",
                            "OK");
                    }
                }
                else
                {
                    EditorUtility.DisplayDialog("Scene Saved",
                        $"Scene successfully saved at: {finalPath}\n\nRemember to commit the changes manually.",
                        "OK");
                }

                // Clean up tempPreviewScenePath if it exists
                if (File.Exists(tempPreviewScenePath))
                {
                    AssetDatabase.DeleteAsset(tempPreviewScenePath);
                }

                // Close the window
                Close();
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "Could not save the final scene.", "OK");
            }
        }
        #endregion

        #region Scene Comparison
        /// <summary>
        /// Loads both scenes for visual comparison
        /// </summary>
        private void LoadDualSceneComparison()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                if (ConflictResolver.PrepareSceneVersions(
                    sceneAssetPath,
                    ClearMergeConstants.TempBeforeScenePath,
                    ClearMergeConstants.TempAfterScenePath,
                    ref gameObjectConflicts))
                {
                    EditorSceneManager.OpenScene(ClearMergeConstants.TempBeforeScenePath, OpenSceneMode.Single);
                    EditorSceneManager.OpenScene(ClearMergeConstants.TempAfterScenePath, OpenSceneMode.Additive);

                    mapper.MapGameObjectsInScenes();
                    // Visibility is now controlled using SceneVisibilityManager with the eye icon

                    EditorUtility.DisplayDialog("Preview Activated",
                        "Both versions loaded for comparison:\n- Use the conflict viewer buttons to switch between BEFORE and AFTER scenes\n- Notice the eye icon in the hierarchy to see which scene is visible", "OK");
                }
                else
                {
                    EditorUtility.DisplayDialog("Error",
                        "Failed to prepare scene versions for comparison.", "OK");
                }
                PropertyConflictDrawer.UpdateGameObjectLookup();
            }
        }
        #region Object Focus
        /// <summary>
        /// Focuses an object in the scene (before or after)
        /// </summary>

        private void FocusObject(string id, bool isBefore)
        {
            // Use the mapper's scene-level visibility system
            mapper.SetSceneVisibility(isBefore);

            GameObject targetObject = null;

            // Use cache for optimized lookup
            var conflict = GetConflictById(id);
            if (conflict != null)
            {
                if (isBefore)
                    targetObject = conflict.BeforeVersion;
                else if (!isBefore)
                    targetObject = conflict.AfterVersion;
            }


            if (targetObject != null)
            {
                Selection.activeGameObject = targetObject;
                if (SceneView.lastActiveSceneView != null)
                {
                    SceneView.lastActiveSceneView.Frame(new Bounds(targetObject.transform.position, Vector3.one * 3), false);
                    SceneView.lastActiveSceneView.Repaint();
                }
            }
            else
            {
                Debug.LogWarning($"Could not find GameObject with ID: {id} in {(isBefore ? "BEFORE" : "AFTER")} scene");
            }
        }

        /// <summary>
        /// Public method for SceneConflictRenderer to focus objects
        /// </summary>
        public void FocusObjectFromRenderer(string id, bool isBefore)
        {
            FocusObject(id, isBefore);
        }
        #endregion // Object Focus
        #endregion // Scene Comparison
    }
}
