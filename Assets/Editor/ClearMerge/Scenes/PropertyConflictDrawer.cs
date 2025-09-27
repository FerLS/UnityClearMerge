using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using ClearMerge.Utils;

namespace ClearMerge.Scenes
{
    /// <summary>
    /// Class responsible for displaying conflict information in Unity Inspector
    /// </summary>
    public class PropertyConflictDrawer
    {
        // Vibrant colors for better visibility
        private static Color headerColor = new Color(1f, 0.5f, 0f, 1f);

        private static bool isInitialized = false;

        // Simplified references using GameObjectConflict
        private static List<GameObjectConflict> gameObjectConflicts;
        private static GameObjectConflict currentConflict;
        private static Dictionary<GameObject, GameObjectConflict> gameObjectToConflictLookup = new();

        /// <summary>
        /// Initializes the PropertyConflictDrawer with references to the data
        /// </summary>
        public static void Initialize(ref List<GameObjectConflict> conflictList)
        {
            gameObjectConflicts = conflictList;
            UpdateGameObjectLookup();

            if (!isInitialized)
            {
                // Register events
                Selection.selectionChanged += OnSelectionChanged;
                Editor.finishedDefaultHeaderGUI += DrawConflictData;
                EditorApplication.delayCall += OnSelectionChanged; // First update

                isInitialized = true;
                Debug.Log("PropertyConflictDrawer initialized and ready to display conflicts in the Inspector");
            }
        }

        /// <summary>
        /// Cleans up resources when no longer needed
        /// </summary>
        public static void Shutdown()
        {
            if (isInitialized)
            {
                // Unregister events
                Selection.selectionChanged -= OnSelectionChanged;
                Editor.finishedDefaultHeaderGUI -= DrawConflictData;

                // Clear references
                gameObjectConflicts = null;
                currentConflict = null;

                isInitialized = false;
                Debug.Log("PropertyConflictDrawer deactivated");
            }
        }

        /// <summary>
        /// Updates the fast lookup cache from GameObjects to conflicts
        /// </summary>
        public static void UpdateGameObjectLookup()
        {
            gameObjectToConflictLookup.Clear();
            if (gameObjectConflicts != null)
            {
                foreach (var conflict in gameObjectConflicts)
                {
                    if (conflict.BeforeVersion != null)
                    {
                        gameObjectToConflictLookup[conflict.BeforeVersion] = conflict;

                    }
                    if (conflict.AfterVersion != null)
                    {
                        gameObjectToConflictLookup[conflict.AfterVersion] = conflict;

                    }
                }

            }
        }

        /// <summary>
        /// Called when the selection changes in the editor
        /// </summary>
        static void OnSelectionChanged()
        {
            // Clear current conflict
            currentConflict = null;

            // Check that we have references and there is a selected object
            if (Selection.activeGameObject == null || gameObjectConflicts == null)
                return;

            GameObject selectedObj = Selection.activeGameObject;

            // Search using the optimized cache
            gameObjectToConflictLookup.TryGetValue(selectedObj, out currentConflict);
        }

        /// <summary>
        /// Draws conflict information in the Inspector
        /// </summary>
        static void DrawConflictData(Editor editor)
        {
            if (editor.target == null || !(editor.target is GameObject)) return;

            GameObject go = (GameObject)editor.target;

            // Check if the selected object has conflicts
            if (currentConflict == null ||
                (go != currentConflict.BeforeVersion && go != currentConflict.AfterVersion))
                return;

            // Check if there are component conflicts
            if (currentConflict.ComponentConflicts == null || currentConflict.ComponentConflicts.Count == 0)
                return;

            EditorGUILayout.Space(10);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.backgroundColor = headerColor;
            EditorGUILayout.LabelField($"⚠️ CONFLICTS DETECTED - {currentConflict.Name}", EditorStyles.boldLabel);
            GUI.backgroundColor = Color.white;

            foreach (var componentConflict in currentConflict.ComponentConflicts)
            {
                EditorGUILayout.LabelField($"🧩 {componentConflict.Name}", EditorStyles.boldLabel);

                foreach (var property in componentConflict.ChangedProperties)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.LabelField(property.PropertyName, EditorStyles.boldLabel);

                    EditorGUILayout.LabelField("Before", property.BeforeValue);
                    EditorGUILayout.LabelField("After", property.AfterValue);

                    EditorGUILayout.EndVertical();
                }

                EditorGUILayout.Space(4);
            }

            EditorGUILayout.EndVertical();
        }
    }
}
