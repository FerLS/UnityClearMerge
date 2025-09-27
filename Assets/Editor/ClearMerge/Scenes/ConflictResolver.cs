using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClearMerge.Utils;
using UnityEditor;
using UnityEngine;

namespace ClearMerge.Scenes
{
    /// <summary>
    /// Handles conflict resolution and scene saving
    /// </summary>
    public class ConflictResolver
    {
        private List<GameObjectConflict> gameObjectConflicts;


        /// <summary>
        /// Constructor que recibe referencias a las colecciones de datos
        /// </summary>
        public ConflictResolver(
            ref List<GameObjectConflict> gameObjectConflicts
           )
        {
            this.gameObjectConflicts = gameObjectConflicts;

        }



        public static bool PrepareSceneVersions(string sceneAssetPath, string outHeadPath, string outIncomingPath, ref List<GameObjectConflict> gameObjectConflicts)
        {
            try
            {
                // Obtener contenido HEAD
                string headContent = GitManager.GetFileContent(sceneAssetPath);
                if (string.IsNullOrEmpty(headContent))
                    return false;

                File.WriteAllText(outHeadPath, headContent);
                AssetDatabase.ImportAsset(outHeadPath);

                // Obtener contenido actual escena (INCOMING)
                string fullPath = Path.Combine(Path.GetDirectoryName(Application.dataPath), sceneAssetPath);
                if (!File.Exists(fullPath))
                    return false;

                string currentSceneContent = File.ReadAllText(fullPath);

                // Resolver conflictos básicos de git manteniendo incoming
                List<string> resolvedContent = ResolveConflictsKeepingIncoming(currentSceneContent);

                // Aquí llamas a tu método para resolver referencias cruzadas,
                // pasándole los conflictos y el contenido ya parcialmente resuelto
                resolvedContent = RemoveDuplicateBlocks(resolvedContent);
                resolvedContent = FixCrossReferenceConflicts(resolvedContent, gameObjectConflicts);

                // Guardar la escena INCOMING ya corregida
                File.WriteAllText(outIncomingPath, string.Join("\n", resolvedContent));
                AssetDatabase.ImportAsset(outIncomingPath);

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error preparando escenas: {ex.Message}");
                return false;
            }
        }



        /// <summary>
        /// Resolves conflicts keeping incoming changes
        /// </summary>
        private static List<string> ResolveConflictsKeepingIncoming(string content)
        {
            string[] lines = content.Split('\n');
            List<string> resolvedLines = new List<string>();
            bool inConflict = false;
            bool keepingIncoming = false;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];

                if (line.Contains("<<<<<<< HEAD"))
                {
                    inConflict = true;
                    keepingIncoming = false;
                    continue;
                }
                else if (line.Contains("======="))
                {
                    keepingIncoming = true;
                    continue;
                }
                else if (line.Contains(">>>>>>> "))
                {
                    inConflict = false;
                    keepingIncoming = false;
                    continue;
                }

                if (!inConflict || keepingIncoming)
                {
                    resolvedLines.Add(line);
                }
            }

            return resolvedLines;
        }


        /// <summary>
        /// Genera el contenido de la escena resuelta basado en las selecciones del usuario
        /// </summary>
        public string GenerateResolvedSceneContent(string sceneAssetPath)
        {
            string scenePath = Path.GetFullPath(sceneAssetPath);
            if (!File.Exists(scenePath)) return null;

            string[] lines = File.ReadAllLines(scenePath);
            List<string> outputLines = new List<string>();

            // Añadir registro para depuración
            Debug.Log($"=== Generating resolved scene with {gameObjectConflicts.Count} conflicts ===");
            foreach (var conflict in gameObjectConflicts)
            {
                Debug.Log($"Object ID: {conflict.Id}, Name: {conflict.Name}, Choice: {conflict.Choice}");
            }

            // Crear diccionario de búsqueda rápida para optimizar las consultas
            var conflictLookup = gameObjectConflicts.ToDictionary(c => c.Id, c => c);


            bool inConflictBlock = false;
            bool collectingBeforeContent = false;
            bool collectingAfterContent = false;
            string currentGameObjectId = null;
            string currentConflictId = null;  // Track the ID associated with the current conflict

            List<string> beforeContent = new List<string>();
            List<string> afterContent = new List<string>();

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];

                // Save current ID when we find a prefab instance or GameObject
                if (line.StartsWith("--- !u!1001 &"))
                {
                    currentGameObjectId = line.Split('&')[1].Trim();
                    outputLines.Add(line);
                    continue;
                }
                else if (line.StartsWith("--- !u!1 &"))
                {
                    currentGameObjectId = line.Split('&')[1].Trim();
                    outputLines.Add(line);
                    continue;
                }

                // When finding conflict start, store the current ID as conflict ID
                if (line.Contains("<<<<<<< HEAD"))
                {
                    inConflictBlock = true;
                    collectingBeforeContent = true;
                    collectingAfterContent = false;
                    beforeContent.Clear();
                    afterContent.Clear();

                    if (lines[i + 1].StartsWith("--- !u!1001 &") || lines[i + 1].StartsWith("--- !u!1 &"))
                    {
                        // Use the last saved GameObject ID as the conflict ID
                        currentGameObjectId = lines[i + 1].Split('&')[1].Trim();
                    }
                    // Save the current GameObject ID as the conflict ID
                    currentConflictId = currentGameObjectId;
                    Debug.Log($"Found conflict for object ID: {currentConflictId}");
                    continue;
                }

                // Detectar separación entre versiones
                if (line.Contains("======="))
                {
                    collectingBeforeContent = false;
                    collectingAfterContent = true;
                    continue;
                }

                // At the end of conflict, use the stored conflict ID to look up selection
                if (line.Contains(">>>>>>> "))
                {
                    inConflictBlock = false;
                    collectingBeforeContent = false;
                    collectingAfterContent = false;

                    // Use the stored conflict ID, which we know is associated with this conflict
                    bool useBeforeVersion = true; // DEFAULT a BEFORE

                    if (!string.IsNullOrEmpty(currentConflictId))
                    {
                        // Buscar el conflicto correspondiente usando el diccionario optimizado
                        if (conflictLookup.TryGetValue(currentConflictId, out GameObjectConflict conflict))
                        {
                            // Usar la selección del GameObjectConflict
                            switch (conflict.Choice)
                            {
                                case ConflictChoice.Before:
                                    useBeforeVersion = true;
                                    break;
                                case ConflictChoice.After:
                                    useBeforeVersion = false;
                                    break;
                                default: // ConflictChoice.None
                                    useBeforeVersion = true; // Default to BEFORE
                                    break;
                            }

                            Debug.Log($"Using selection for ID {currentConflictId}: {(useBeforeVersion ? "BEFORE" : "AFTER")} version (Choice: {conflict.Choice})");
                        }
                        else
                        {
                            Debug.Log($"ID {currentConflictId} not found in conflicts, defaulting to BEFORE");
                        }
                    }

                    // Add the selected content
                    if (useBeforeVersion)
                    {
                        outputLines.AddRange(beforeContent);
                        Debug.Log($"Added BEFORE content for ID {currentConflictId} ({beforeContent.Count} lines)");
                    }
                    else
                    {
                        outputLines.AddRange(afterContent);
                        Debug.Log($"Added AFTER content for ID {currentConflictId} ({afterContent.Count} lines)");
                    }

                    // Clear the conflict ID as we're done with this conflict
                    currentConflictId = null;
                    continue;
                }

                // Collect content for each version
                if (inConflictBlock)
                {
                    if (collectingBeforeContent)
                    {
                        beforeContent.Add(line);
                    }
                    else if (collectingAfterContent)
                    {
                        afterContent.Add(line);
                    }
                }
                else
                {
                    // Non-conflict lines go straight to output
                    outputLines.Add(line);
                }
            }
            outputLines = RemoveDuplicateBlocks(outputLines);
            outputLines = FixCrossReferenceConflicts(outputLines, gameObjectConflicts);

            return string.Join(Environment.NewLine, outputLines);
        }

        private static List<string> RemoveDuplicateBlocks(List<string> lines)
        {
            var result = new List<string>();
            var seenFileIDs = new HashSet<string>();

            int i = 0;
            while (i < lines.Count)
            {
                string line = lines[i];

                if (line.StartsWith("--- !u!") && line.Contains("&"))
                {
                    string fileID = ExtractFileIDFromLine(line);

                    if (seenFileIDs.Contains(fileID))
                    {
                        // Saltar este bloque completo
                        i++;
                        while (i < lines.Count && !lines[i].StartsWith("--- !u!"))
                            i++;
                        continue; // no añadir al resultado
                    }

                    seenFileIDs.Add(fileID);
                }

                result.Add(lines[i]);
                i++;
            }

            return result;
        }



        public static List<string> FixCrossReferenceConflicts(List<string> lines, List<GameObjectConflict> gameObjectConflicts)
        {
            foreach (var conflict in gameObjectConflicts)
            {
                foreach (var componentConflict in conflict.ComponentConflicts)
                {
                    foreach (var crossRef in componentConflict.CrossReferences)
                    {
                        if (crossRef.ConflictType == CrossReferenceConflict.CrossReferenceConflictType.ParentChanged)
                        {
                            string parentComponentIdToModify = conflict.Choice == ConflictChoice.Before
                                ? crossRef.AfterReferenceId
                                : crossRef.BeforeReferenceId;

                            string childComponentIdToRemove = componentConflict.Id;
                            int parentBlockStartLine = -1;


                            // Buscar el inicio del bloque del padre
                            for (int i = 0; i < lines.Count; i++)
                            {
                                if (lines[i].Contains($"&{parentComponentIdToModify}"))
                                {
                                    parentBlockStartLine = i;
                                    break;
                                }
                            }

                            // Buscar la línea con "m_Children:"
                            for (int i = parentBlockStartLine + 1; i < lines.Count; i++)
                            {
                                string line = lines[i].Trim();
                                if (line.StartsWith("--- !u!")) break;
                                if (line.StartsWith("m_Children:"))
                                {
                                    //Buscar- {fileID: + childComponentIdToRemove + "}" y borrar esa línea

                                    for (int j = i + 1; j < lines.Count; j++)
                                    {
                                        line = lines[j].Trim();
                                        if (line.StartsWith("- {fileID:") && line.Contains(childComponentIdToRemove))
                                        {
                                            lines.RemoveAt(j);
                                            break;
                                        }

                                    }
                                }

                                Debug.Log("C");
                            }
                        }
                    }
                }

                return lines;
            }

            // Ensure a return in case gameObjectConflicts is empty
            return lines;
        }



        private static string ExtractFileIDFromLine(string line)
        {
            int ampIndex = line.IndexOf('&');
            if (ampIndex == -1) return null;

            int endIndex = line.IndexOfAny(new[] { ' ', '\r', '\n' }, ampIndex);
            if (endIndex == -1) endIndex = line.Length;

            return line.Substring(ampIndex + 1, endIndex - ampIndex - 1).Trim();
        }



    }
}