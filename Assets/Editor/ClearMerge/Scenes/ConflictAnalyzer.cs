using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using ClearMerge.Utils;
using static ClearMerge.Scenes.CrossReferenceConflict;

namespace ClearMerge.Scenes
{
    /// <summary>
    /// Analyzes scene files to detect conflicts during Git merges
    /// </summary>
    public class ConflictAnalyzer
    {
        private List<GameObjectConflict> gameObjectConflicts;
        private Dictionary<string, GameObjectConflict> conflictLookup = new(); // Cache para búsquedas rápidas

        public ConflictAnalyzer(
            ref List<GameObjectConflict> gameObjectConflicts)
        {
            this.gameObjectConflicts = gameObjectConflicts;
        }

        /// <summary>
        /// Analyzes a scene for conflicts
        /// </summary>
        public void AnalyzeScene(string assetPath)
        {
            gameObjectConflicts.Clear();
            UpdateConflictLookup(); // Actualizar cache después de limpiar

            string fullPath = Path.Combine(Path.GetDirectoryName(Application.dataPath), assetPath);
            if (!File.Exists(fullPath))
            {
                Debug.LogError($"Scene file not found: {fullPath}");
                return;
            }

            DetectAndAnalyzeConflicts(fullPath);

            // Actualizar cache final después del análisis completo
            UpdateConflictLookup();

            //Print every conflict for debugging
            foreach (var conflict in gameObjectConflicts)
            {
                Debug.Log($"Conflict ID: {conflict.Id}, Name: {conflict.Name}");

            }
        }

        /// <summary>
        /// Actualiza el cache de búsqueda rápida de conflictos
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
        /// Obtiene un conflicto por ID de manera optimizada
        /// </summary>
        private GameObjectConflict GetConflictById(string id)
        {
            conflictLookup.TryGetValue(id, out GameObjectConflict conflict);
            return conflict;
        }

        #region Private Methods


        private string FindGameObjectNameByFileID(string[] lines, string fileID)
        {
            string header = $"--- !u!1 &{fileID}";
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].TrimStart() == header)
                {
                    // Buscar m_Name: solo dentro del bloque de este GameObject
                    for (int j = i + 1; j < lines.Length; j++)
                    {
                        string l = lines[j].TrimStart();
                        if (l.StartsWith("m_Name:"))
                        {
                            return l.Substring(l.IndexOf("m_Name:") + 7).Trim();
                        }
                        // Si llegamos a otro header, salimos
                        if (l.StartsWith("--- !u!"))
                            break;
                    }
                }
            }
            return null;
        }
        private string FindPrefabInstanceNameByFileID(string[] lines, string fileID)
        {
            string header = $"--- !u!1001 &{fileID}";
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].TrimStart() == header)
                {
                    // Buscar propertyPath: m_Name solo dentro del bloque de este PrefabInstance
                    for (int j = i + 1; j < lines.Length; j++)
                    {
                        string l = lines[j].TrimStart();
                        if (l.Contains("propertyPath: m_Name"))
                        {
                            if (j + 1 < lines.Length && lines[j + 1].TrimStart().StartsWith("value:"))
                            {
                                return lines[j + 1].Substring(lines[j + 1].IndexOf("value:") + 6).Trim();
                            }
                        }
                        // Si llegamos a otro header, salimos
                        if (l.StartsWith("--- !u!"))
                            break;
                    }
                }
            }
            return null;
        }


        /// <summary>
        /// Obtiene el ID del GameObject de las líneas del archivo
        /// /// Handles both GameObject and PrefabInstance declarations
        /// </summary>
        /// <param name="lines">Lines of the scene file</param>
        /// <param name="conflictIndex">Index of the conflict line</param>
        /// <returns>Tuple with GameObject ID and GlobalObjectId if applicable</returns>
        private (string, string) GetGameObjectIds(string[] lines, int conflictIndex)
        {

            //Look the first line forward in case of removed GameObject
            if (lines[conflictIndex + 1].StartsWith("--- !u!1 &"))
            {
                return (lines[conflictIndex + 1].Split('&')[1].Trim(), null);
            }
            else if (lines[conflictIndex + 1].StartsWith("--- !u!1001 &"))
            {
                // For prefabs, generate a GlobalObjectId
                string globalID = GenerateGlobalID(lines, conflictIndex + 1);
                if (!string.IsNullOrEmpty(globalID))
                {
                    return (lines[conflictIndex + 1].Split('&')[1].Trim(), globalID);
                }
                Debug.LogWarning("No corresponding GameObject ID found for prefab instance.");
                // Fallback to prefab instance ID
                return (lines[conflictIndex + 1].Split('&')[1].Trim(), null);
            }
            // Look backwards for GameObject or PrefabInstance declaration
            for (int i = conflictIndex; i >= 0; i--)
            {
                if (lines[i].StartsWith("--- !u!1 &"))
                {
                    return (lines[i].Split('&')[1].Trim(), null);
                }
                else if (lines[i].StartsWith("--- !u!1001 &"))
                {
                    // For prefabs, generate a GlobalObjectId
                    string globalID = GenerateGlobalID(lines, i);
                    if (!string.IsNullOrEmpty(globalID))
                    {
                        return (lines[i].Split('&')[1].Trim(), globalID);
                    }
                    Debug.LogWarning("No corresponding GameObject ID found for prefab instance.");
                    // Fallback to prefab instance ID
                    return (lines[i].Split('&')[1].Trim(), null);
                }
            }


            return (null, null);
        }

        public void DetectAndAnalyzeConflicts(string assetPath)
        {
            gameObjectConflicts.Clear();
            UpdateConflictLookup();

            string fullPath = Path.Combine(Path.GetDirectoryName(Application.dataPath), assetPath);
            if (!File.Exists(fullPath))
            {
                Debug.LogError($"Scene file not found: {fullPath}");
                return;
            }

            string[] lines = File.ReadAllLines(fullPath);

            string fileID = "";
            string globalId = "";
            string currentComponent = "";
            bool inConflict = false;
            bool inBeforeSection = false;
            bool removedConflict = false;
            List<string> beforeLines = new();
            List<string> afterLines = new();

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].TrimEnd();

                if (line.StartsWith("--- !u!1 &") || line.StartsWith("--- !u!1001 &"))
                {
                    fileID = ExtractFileID(line);
                    currentComponent = GetComponentType(line);
                }
                else if (line.StartsWith("--- !u!"))
                {
                    currentComponent = GetComponentType(line);
                }

                if (line.Contains("<<<<<<< HEAD"))
                {
                    (fileID, globalId) = GetGameObjectIds(lines, i);
                    removedConflict = i + 1 < lines.Length && lines[i + 1].Contains("--- !u!1001 &");

                    // Si aún no hemos registrado este conflicto, lo creamos
                    if (!string.IsNullOrEmpty(fileID) && !conflictLookup.ContainsKey(fileID))
                    {
                        string name = FindGameObjectNameByFileID(lines, fileID)
                                   ?? FindPrefabInstanceNameByFileID(lines, fileID);

                        var newConflict = new GameObjectConflict(fileID, globalId, name);
                        gameObjectConflicts.Add(newConflict);
                        conflictLookup[fileID] = newConflict;
                    }

                    inConflict = true;
                    inBeforeSection = true;
                    beforeLines.Clear();
                    afterLines.Clear();
                    continue;
                }

                if (inConflict && line.Contains("======="))
                {
                    inBeforeSection = false;
                    continue;
                }

                if (inConflict && line.Contains(">>>>>>>"))
                {
                    inConflict = false;

                    if (gameObjectConflicts.Any(c => c.Id == fileID))
                    {
                        ProcessConflict(fileID, currentComponent, beforeLines, afterLines, removedConflict);
                    }
                    continue;
                }

                if (inConflict)
                {
                    if (inBeforeSection)
                        beforeLines.Add(line);
                    else
                        afterLines.Add(line);
                }
            }

            UpdateConflictLookup(); // Final update
        }

        /// <summary>
        /// Finds the corresponding GameObject ID for a PrefabInstance by looking for m_Name modifications
        /// </summary>
        private string GenerateGlobalID(string[] lines, int prefabInstanceIndex)
        {
            string prefabfileID = ExtractFileID(lines[prefabInstanceIndex]);
            // Look for m_SourcePrefab section to get the GUID
            for (int i = prefabInstanceIndex + 1; i < Math.Min(ClearMergeConstants.MAX_LOOK_AHEAD * 10, lines.Length); i++)
            {
                string line = lines[i].TrimEnd();

                // Stop if we hit another component or GameObject declaration
                if ((line.StartsWith("--- !u!1001 &") || line.StartsWith("--- !u!1 &")) && i > prefabInstanceIndex + 1)
                {
                    break;
                }


                // Look for m_CorrespondingSourceObject line
                if (line.TrimStart().StartsWith("m_CorrespondingSourceObject") && line.Contains("fileID: "))
                {
                    // Example line: m_CorrespondingSourceObject: {fileID: 880970725214738984, guid: c60edd51c065ceb46b345849a03bc106, type: 3}
                    string fileID = line.Split(new[] { "fileID:" }, StringSplitOptions.None)[1].Split(',')[0].Trim();

                    // Generate GlobalObjectId string using the GUID
                    // Format: GlobalObjectId_V1-1-[guid]-[targetObjectId]-[targetPrefabId]
                    // For prefab instances, we use a simplified approach
                    string globalObjectId = $"GlobalObjectId_V1-2-0-{fileID}-{prefabfileID}";
                    Debug.Log($"Generated GlobalObjectId for prefab: {globalObjectId}");
                    return globalObjectId;

                }
            }

            return null;
        }

        private string ExtractFileID(string line)
        {
            if (line.Contains("&"))
            {
                return line.Split('&')[1].Trim();
            }
            return "";
        }

        private string GetComponentType(string line)
        {
            // Extract Unity component type from the declaration
            if (line.StartsWith("--- !u!"))
            {
                string[] parts = line.Split(' ');
                if (parts.Length > 1)
                {
                    string typeCode = parts[1].Replace("!u!", "");
                    return GetComponentNameFromTypeCode(typeCode);
                }
            }
            return "Unknown";
        }

        private string GetComponentNameFromTypeCode(string typeCode)
        {
            // Map common Unity type codes to readable names
            var typeMap = new Dictionary<string, string>
            {
                {"1", "GameObject"},
                {"4", "Transform"},
                {"23", "MeshRenderer"},
                {"33", "MeshFilter"},
                {"65", "BoxCollider"},
                {"114", "MonoBehaviour"},
                {"1001", "PrefabInstance"}
            };

            return typeMap.ContainsKey(typeCode) ? typeMap[typeCode] : $"Component_{typeCode}";
        }

        private void ProcessConflict(string fileID, string componentType, List<string> beforeLines, List<string> afterLines, bool removedConflict)
        {
            if (string.IsNullOrEmpty(fileID))
                return;

            // Find property differences
            var beforeProps = ExtractProperties(beforeLines);
            var afterProps = ExtractProperties(afterLines);

            // Get game object conflict using optimized lookup
            var gameObjectConflict = GetConflictById(fileID);
            if (gameObjectConflict == null) return;


            // Create or get component conflict for this component type
            var componentId = ExtractFileID(beforeLines.FirstOrDefault() ?? "");
            var conflict = gameObjectConflict.ComponentConflicts.FirstOrDefault(c => c.Id == componentId);
            if (conflict == null)
            {
                conflict = new ComponentConflict(removedConflict ? ClearMergeConstants.CONFLICT_REMOVED : componentType, componentId);
                gameObjectConflict.ComponentConflicts.Add(conflict);
            }

            foreach (var beforeProp in beforeProps)
            {
                if (afterProps.ContainsKey(beforeProp.Key) &&
                    beforeProp.Value != afterProps[beforeProp.Key])
                {
                    conflict.ChangedProperties.Add(new PropertyConflict(
                        beforeProp.Key,
                        beforeProp.Value,
                        afterProps[beforeProp.Key]
                    ));
                }
            }

            // Check for properties only in after
            foreach (var afterProp in afterProps)
            {
                if (!beforeProps.ContainsKey(afterProp.Key))
                {
                    conflict.ChangedProperties.Add(new PropertyConflict(
                        afterProp.Key,
                        "(not set)",
                        afterProp.Value
                    ));
                }
            }

            AnalyzeConflictCrossReferences(conflict);
        }

        private Dictionary<string, string> ExtractProperties(List<string> lines)
        {
            var properties = new Dictionary<string, string>();

            string currentPropertyPath = null;
            string parentKey = null;

            foreach (string rawLine in lines)
            {
                string line = rawLine.Trim();

                // PrefabInstance-style
                if (line.StartsWith("propertyPath:"))
                {
                    currentPropertyPath = line.Substring("propertyPath:".Length).Trim();
                }
                else if (line.StartsWith("value:"))
                {
                    if (currentPropertyPath != null)
                    {
                        string value = line.Substring("value:".Length).Trim();
                        properties[currentPropertyPath] = value;
                        currentPropertyPath = null;
                    }
                    else
                    {
                        // No hay propertyPath anterior => ignorar este value
                        continue;
                    }
                }

                // Component-style (direct key: value pairs)
                else if (line.Contains(":") && !line.StartsWith("#"))
                {
                    int colon = line.IndexOf(':');
                    string key = line.Substring(0, colon).Trim();
                    string value = line.Substring(colon + 1).Trim();

                    // Handle multi-line structures (e.g. m_LocalPosition with subfields)
                    if (string.IsNullOrEmpty(value))
                    {
                        parentKey = key;
                    }
                    else if (parentKey != null)
                    {
                        properties[$"{parentKey}.{key}"] = value;
                    }
                    else
                    {
                        properties[key] = value;
                    }
                }
            }

            return properties;
        }

        #endregion


        private void AnalyzeConflictCrossReferences(ComponentConflict componentConflict)
        {

            foreach (var propertyConflict in componentConflict.ChangedProperties)
            {
                CrossReferenceConflictType? type = propertyConflict.PropertyName switch
                {
                    "m_Father" or "m_Parent" => CrossReferenceConflictType.ParentChanged,
                    "m_Target" => CrossReferenceConflictType.TargetChanged,
                    "m_Dependencies" => CrossReferenceConflictType.DependencyChanged,
                    "m_EventListeners" => CrossReferenceConflictType.EventListenerChanged,
                    _ => null
                };

                if (type == null)
                {
                    return; // No known cross-reference conflict type
                }
                CrossReferenceConflict crossRef = new CrossReferenceConflict(
                    type.Value,
                    ExtractFileIDFromProperty(propertyConflict.BeforeValue),
                    ExtractFileIDFromProperty(propertyConflict.AfterValue)

                );

                componentConflict.CrossReferences.Add(crossRef);

            }




        }

        private string ExtractFileIDFromProperty(string property)
        {
            // Extract fileID from a property string like "{fileID: 667157448}"
            int start = property.IndexOf("{fileID:");
            if (start == -1)
                return null;

            start += "{fileID:".Length;
            int end = property.IndexOf('}', start);
            if (end == -1)
                return null;

            return property.Substring(start, end - start).Trim();
        }
    }
}