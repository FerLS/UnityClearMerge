using System.Collections.Generic;
using ClearMerge.Utils;
using UnityEditor;
using UnityEngine;

namespace ClearMerge.Scenes
{
    /// <summary>
    /// Handles conflict visualization in the scene window
    /// </summary>
    public class SceneConflictRenderer
    {
        private List<GameObjectConflict> gameObjectConflicts;
        private ConflictViewerWindow parentWindow;

        /// <summary>
        /// Constructor que recibe referencias a las colecciones de datos
        /// </summary>
        public SceneConflictRenderer(
            ref List<GameObjectConflict> gameObjectConflicts,
            ConflictViewerWindow window = null
          )
        {
            this.gameObjectConflicts = gameObjectConflicts;
            this.parentWindow = window;
        }

        /// <summary>
        /// Dibuja visualizaciones de objetos en conflicto en la vista de escena
        /// </summary>
        public void OnSceneGUI(SceneView sceneView)
        {
            if (gameObjectConflicts == null)
                return;

            foreach (var conflict in gameObjectConflicts)
            {
                var beforeObj = conflict.BeforeVersion;
                var afterObj = conflict.AfterVersion;
                string conflictId = conflict.Id;

                // Determinar qué versión mostrar basado en la selección del usuario (si existe)
                bool hasSelection = conflict.Choice != ConflictChoice.None;
                bool showAfterVersion = hasSelection && conflict.Choice == ConflictChoice.After;
                bool showBeforeVersion = hasSelection && conflict.Choice == ConflictChoice.Before;

                if (!hasSelection)
                {
                    // Mostrar BEFORE (amarillo)
                    if (beforeObj != null)
                    {
                        Vector3 position = beforeObj.transform.position;
                        Handles.color = new Color(1f, 0.9f, 0f, 0.8f); // Amarillo
                        Handles.SphereHandleCap(0, position, Quaternion.identity, 0.5f, EventType.Repaint);
                        Handles.Label(position + Vector3.up * 1.5f, $"⏳ BEFORE: {beforeObj.name}", EditorStyles.boldLabel);

                        if (Handles.Button(position, Quaternion.identity, 0.5f, 0.5f, Handles.SphereHandleCap))
                        {
                            FocusObjectInScene(conflictId, true);
                        }
                    }

                    // Mostrar AFTER (rojo) ligeramente desplazado si están en la misma posición
                    if (afterObj != null)
                    {
                        Vector3 position = afterObj.transform.position;
                        Vector3 offset = (beforeObj != null && Vector3.Distance(beforeObj.transform.position, position) < 0.1f)
                            ? Vector3.right * 0.6f : Vector3.zero;

                        Handles.color = new Color(1f, 0f, 0f, 0.8f); // Rojo
                        Handles.SphereHandleCap(0, position + offset, Quaternion.identity, 0.5f, EventType.Repaint);
                        Handles.Label(position + offset + Vector3.up * 1.5f, $"⏳ AFTER: {afterObj.name}", EditorStyles.boldLabel);

                        if (Handles.Button(position + offset, Quaternion.identity, 0.5f, 0.5f, Handles.SphereHandleCap))
                        {
                            FocusObjectInScene(conflictId, false);
                        }
                    }
                }
                else
                {
                    // El usuario ya hizo una selección - mostrar solo la versión seleccionada
                    GameObject selectedObj = showAfterVersion ? afterObj : beforeObj;
                    if (selectedObj != null)
                    {
                        Vector3 position = selectedObj.transform.position;
                        string versionLabel = showAfterVersion ? "AFTER" : "BEFORE";
                        Handles.color = showAfterVersion ? new Color(1f, 0f, 0f, 0.8f) : new Color(1f, 0.9f, 0f, 0.8f);

                        Handles.DrawWireCube(position, Vector3.one * 0.6f);
                        Handles.Label(position + Vector3.up * 1.5f, $"✅ {versionLabel}: {selectedObj.name}", EditorStyles.boldLabel);

                        if (Handles.Button(position, Quaternion.identity, 0.6f, 0.6f, Handles.SphereHandleCap))
                        {
                            FocusObjectInScene(conflictId, !showAfterVersion);
                        }
                    }
                }
            }

            sceneView.Repaint();
        }

        /// <summary>
        /// Enfoca un objeto en la escena, notificando al ObjectMapper
        /// </summary>
        private void FocusObjectInScene(string conflictId, bool focusBefore)
        {
            // Si tenemos referencia a la ventana padre, usar su método de focus
            if (parentWindow != null)
            {
                parentWindow.FocusObjectFromRenderer(conflictId, focusBefore);
            }
            Debug.Log($"Focused on conflict ID: {conflictId} ({(focusBefore ? "BEFORE" : "AFTER")} version)");
        }
    }
}