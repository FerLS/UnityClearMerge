using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ClearMerge.Utils;
using Codice.Client.BaseCommands;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Se encarga de mapear objetos entre las dos escenas
/// </summary>
/// 

namespace ClearMerge.Scenes
{

    public class ObjectMapper
    {
        private List<GameObjectConflict> gameObjectConflicts;
        private Dictionary<string, GameObjectConflict> conflictLookup;

        // Referencias a las escenas para el control de visibilidad
        private Scene beforeScene;
        private Scene afterScene;

        /// <summary>
        /// Constructor que recibe referencias a las colecciones de datos
        /// </summary>
        public ObjectMapper(
            ref List<GameObjectConflict> gameObjectConflicts
      )
        {
            this.gameObjectConflicts = gameObjectConflicts;
        }

        /// <summary>
        /// Actualiza el diccionario de búsqueda rápida de conflictos
        /// </summary>

        /// <summary>
        /// Obtiene un conflicto por ID usando búsqueda optimizada
        /// </summary>
        private GameObjectConflict GetConflictById(string id)
        {
            conflictLookup.TryGetValue(id, out GameObjectConflict conflict);
            return conflict;
        }

        /// <summary>
        /// Mapea GameObjects en ambas escenas a sus identificadores
        /// </summary>

        public void MapGameObjectsInScenes()
        {
            conflictLookup = gameObjectConflicts.ToDictionary(c => c.Id, c => c);


            // Obtener referencias a las escenas para el control de visibilidad
            afterScene = EditorSceneManager.GetSceneByPath(ClearMergeConstants.TempAfterScenePath);
            beforeScene = EditorSceneManager.GetSceneByPath(ClearMergeConstants.TempBeforeScenePath);

            foreach (GameObject go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Scene objectScene = go.scene;
                bool isBeforeScene = objectScene.path == beforeScene.path;
                // Guardar el GameObject y su escena

                PropertyInfo inspectorModeInfo = typeof(SerializedObject).GetProperty("inspectorMode",
                    BindingFlags.NonPublic | BindingFlags.Instance);

                // Obtener el objeto serializado del GameObject directamente
                SerializedObject serializedObject = new SerializedObject(go);
                inspectorModeInfo?.SetValue(serializedObject, InspectorMode.Debug, null);

                // Obtener el LocalIdentifier del GameObject
                SerializedProperty localIdProp = serializedObject.FindProperty("m_LocalIdentfierInFile");
                long localId = localIdProp.longValue;

                if (localId > 0)
                {
                    string goId = localId.ToString();

                    // Verificar si este ID está en la lista de IDs en conflicto usando cache optimizado
                    var conflict = GetConflictById(goId);
                    if (conflict != null)
                    {
                        if (isBeforeScene)
                        {
                            conflict.BeforeVersion = go;
                            Debug.Log($"Mapped {go.name} to BEFORE with ID {goId}");
                        }
                        else
                        {
                            conflict.AfterVersion = go;
                            Debug.Log($"Mapped {go.name} to AFTER with ID {goId}");
                        }
                    }
                }
                else
                {
                    MapPrefabInstance(go, isBeforeScene);
                }
            }

            // Log any IDs that weren't mapped successfully
            foreach (string id in gameObjectConflicts.Select(c => c.Id).Distinct())
            {
                if (!gameObjectConflicts.Any(c => c.Id == id && c.BeforeVersion != null))
                    Debug.LogWarning($"Could not find object with ID {id} in BEFORE scene");
                if (!gameObjectConflicts.Any(c => c.Id == id && c.AfterVersion != null))
                    Debug.LogWarning($"Could not find object with ID {id} in AFTER scene");
            }

            // Inicializar con la escena AFTER visible por defecto
            SetSceneVisibility(false); // Mostrar escena AFTER
        }

        /// <summary>
        /// Mapea instancias de prefabs usando GlobalObjectId
        /// </summary>
        private void MapPrefabInstance(GameObject go, bool isBeforeScene)
        {


            // Check if this is a prefab instance
            if (PrefabUtility.IsPartOfPrefabInstance(go))
            {
                // Get the GlobalObjectId for this prefab instance
                GlobalObjectId globalId = GlobalObjectId.GetGlobalObjectIdSlow(go.transform);

                if (globalId.identifierType == 2) // Prefab instance
                {
                    // Convert the targetPrefabId to string

                    // Check if this ID is in our conflicted IDs list using optimized cache

                    string customGlobalId = globalId.ToString();

                    // convert the original global id to                     string globalObjectId = $"GlobalObjectId_V1-1-0-{fileID}-{prefabfileID}";
                    string globalIdTargetObjectId = customGlobalId.Split('-')[3]; // Get the targetObjectId part
                    string globalIdTargetPrefabId = customGlobalId.Split('-')[4]; // Get the targetPrefabId part
                    customGlobalId = $"GlobalObjectId_V1-2-0-{globalIdTargetObjectId}-{globalIdTargetPrefabId}";

                    var conflict = gameObjectConflicts
                        .FirstOrDefault(c => c.GlobalId == customGlobalId);


                    if (conflict != null)
                    {
                        if (isBeforeScene)
                        {
                            conflict.BeforeVersion = go;
                            Debug.Log($"Mapped prefab {go.name} to BEFORE with GlobalObjectId: {customGlobalId}");

                        }
                        else
                        {
                            conflict.AfterVersion = go;
                            Debug.Log($"Mapped prefab {go.name} to AFTER with GlobalObjectId: {customGlobalId}");

                        }
                    }
                }
            }

        }

        /// <summary>
        /// Controla la visibilidad de las escenas completas usando SceneVisibilityManager
        /// También controla luces, audio y efectos para evitar que se sumen las de ambas escenas
        /// </summary>
        /// <param name="showBeforeScene">Si es true, muestra la escena BEFORE y oculta AFTER; si es false, al revés</param>
        public void SetSceneVisibility(bool showBeforeScene)
        {
            // Controlar visibilidad de la escena BEFORE
            if (beforeScene.IsValid())
            {
                GameObject[] beforeRoots = beforeScene.GetRootGameObjects();
                foreach (GameObject root in beforeRoots)
                {
                    if (showBeforeScene)
                        SceneVisibilityManager.instance.Show(root, true);
                    else
                        SceneVisibilityManager.instance.Hide(root, true);
                }

                // Controlar luces y otros componentes para evitar efectos acumulativos
                ControlSceneLights(beforeScene, showBeforeScene);
            }

            // Controlar visibilidad de la escena AFTER
            if (afterScene.IsValid())
            {
                GameObject[] afterRoots = afterScene.GetRootGameObjects();
                foreach (GameObject root in afterRoots)
                {
                    if (showBeforeScene)
                        SceneVisibilityManager.instance.Hide(root, true);
                    else
                        SceneVisibilityManager.instance.Show(root, true);
                }

                // Controlar luces y otros componentes para evitar efectos acumulativos
                ControlSceneLights(afterScene, !showBeforeScene);
            }

            // Repintar la jerarquía para reflejar los cambios de visibilidad
            EditorApplication.RepaintHierarchyWindow();
        }

        /// <summary>
        /// Controla las luces de una escena específica para evitar suma de iluminación
        /// También controla otros componentes que pueden causar efectos acumulativos
        /// </summary>
        /// <param name="scene">La escena a controlar</param>
        /// <param name="enableComponents">Si es true, habilita los componentes; si es false, los deshabilita</param>
        private void ControlSceneLights(Scene scene, bool enableComponents)
        {
            if (!scene.IsValid()) return;

            int lightsControlled = 0;
            int audioSourcesControlled = 0;

            // Obtener todos los componentes que pueden causar efectos acumulativos
            GameObject[] rootObjects = scene.GetRootGameObjects();
            foreach (GameObject root in rootObjects)
            {
                // Controlar luces para evitar suma de iluminación
                Light[] lights = root.GetComponentsInChildren<Light>(true);
                foreach (Light light in lights)
                {
                    if (light != null)
                    {
                        light.enabled = enableComponents;
                        EditorUtility.SetDirty(light);
                        lightsControlled++;
                    }
                }

                // Controlar AudioSources para evitar suma de audio
                AudioSource[] audioSources = root.GetComponentsInChildren<AudioSource>(true);
                foreach (AudioSource audioSource in audioSources)
                {
                    if (audioSource != null)
                    {
                        audioSource.enabled = enableComponents;
                        EditorUtility.SetDirty(audioSource);
                        audioSourcesControlled++;
                    }
                }

                // También podríamos controlar ParticleSystems si es necesario
                ParticleSystem[] particleSystems = root.GetComponentsInChildren<ParticleSystem>(true);
                foreach (ParticleSystem particles in particleSystems)
                {
                    if (particles != null)
                    {
                        var emission = particles.emission;
                        emission.enabled = enableComponents;
                        EditorUtility.SetDirty(particles);
                    }
                }
            }
        }





    }

}