using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClearMerge.Utils;

namespace ClearMerge.Assets
{
    /// <summary>
    /// Analyzes and parses conflicts in project assets
    /// </summary>
    public static class ConflictAnalyzer
    {
        #region Public Methods

        public static List<AssetConflict> ScanProjectForConflicts()
        {
            List<AssetConflict> conflicts = new();

            try
            {
                string assetsPath = Application.dataPath;
                ScanDirectoryForConflicts(assetsPath, conflicts);

                // Buscar conflictos binarios usando Git status
                var binaryConflicts = ScanForBinaryConflicts();
                conflicts.AddRange(binaryConflicts);

                Debug.Log($"Asset Conflict Scanner: Found {conflicts.Count} assets with conflicts");
                return conflicts;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error scanning for asset conflicts: {e.Message}");
                return conflicts;
            }
        }

        #endregion

        #region Private Methods

        private static void ScanDirectoryForConflicts(string directory, List<AssetConflict> conflicts)
        {
            try
            {
                foreach (string file in Directory.GetFiles(directory))
                {
                    if (Path.GetExtension(file).Equals(".unity", StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (!IsBinaryFile(file) && HasGitConflictMarkers(file))
                    {
                        AssetConflict conflict = ParseConflictFile(file);
                        if (conflict != null && conflict.propertyConflicts.Count > 0)
                        {
                            conflicts.Add(conflict);
                        }
                    }
                }

                foreach (string subDir in Directory.GetDirectories(directory))
                {
                    string dirName = Path.GetFileName(subDir);
                    if (dirName != "Library" && dirName != "Temp" && dirName != "Logs" &&
                        dirName != "UserSettings" && !dirName.StartsWith("."))
                    {
                        ScanDirectoryForConflicts(subDir, conflicts);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Error scanning directory {directory}: {e.Message}");
            }
        }

        private static bool IsBinaryFile(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLower();
            return ClearMergeConstants.BINARY_ASSET_EXTENSIONS.Contains(extension);
        }

        private static bool HasGitConflictMarkers(string filePath)
        {
            try
            {
                string content = File.ReadAllText(filePath);
                return content.Contains(ClearMergeConstants.CONFLICT_START_MARKER);
            }
            catch
            {
                return false;
            }
        }

        private static AssetConflict ParseConflictFile(string filePath)
        {
            try
            {
                string relativePath = "Assets" + filePath.Substring(Application.dataPath.Length);
                string[] lines = File.ReadAllLines(filePath);

                string before = "";
                string after = "";

                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].StartsWith(ClearMergeConstants.CONFLICT_START_MARKER))
                    {
                        ExtractConflictContent(lines, ref i, out string beforeContent, out string afterContent);
                        before = beforeContent;
                        after = afterContent;
                        break;
                    }
                }

                if (!string.IsNullOrEmpty(before) && !string.IsNullOrEmpty(after))
                {
                    var conflict = new AssetConflict(relativePath)
                    {
                        beforeContent = before,
                        afterContent = after,
                        propertyConflicts = AnalyzePropertyConflicts(before, after)
                    };
                    return conflict;
                }

                return null;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error parsing conflict file {filePath}: {e.Message}");
                return null;
            }
        }

        private static void ExtractConflictContent(string[] lines, ref int startIndex, out string before, out string after)
        {
            List<string> beforeLines = new();
            List<string> afterLines = new();
            bool inBefore = true;
            startIndex++;

            while (startIndex < lines.Length)
            {
                string line = lines[startIndex];

                if (line.StartsWith(ClearMergeConstants.CONFLICT_MIDDLE_MARKER))
                {
                    inBefore = false;
                }
                else if (line.StartsWith(ClearMergeConstants.CONFLICT_END_MARKER))
                {
                    break;
                }
                else
                {
                    if (inBefore)
                        beforeLines.Add(line);
                    else
                        afterLines.Add(line);
                }

                startIndex++;
            }

            before = string.Join("\n", beforeLines);
            after = string.Join("\n", afterLines);
        }

        private static List<PropertyConflict> AnalyzePropertyConflicts(string beforeContent, string afterContent)
        {
            var conflicts = new List<PropertyConflict>();

            var beforeLines = beforeContent.Split('\n');
            var afterLines = afterContent.Split('\n');

            foreach (var beforeLine in beforeLines)
            {
                if (beforeLine.Trim().StartsWith("- ") && beforeLine.Contains(":"))
                {
                    string propertyName = ExtractPropertyName(beforeLine);
                    if (!string.IsNullOrEmpty(propertyName))
                    {
                        string afterValue = FindPropertyInLines(propertyName, afterLines);
                        if (!string.IsNullOrEmpty(afterValue))
                        {
                            conflicts.Add(new PropertyConflict
                            {
                                PropertyName = propertyName,
                                BeforeValue = beforeLine,
                                AfterValue = afterValue,
                                SelectedValue = ConflictChoice.None
                            });
                        }
                    }
                }
            }

            return conflicts;
        }

        private static string ExtractPropertyName(string line)
        {
            var parts = line.Split(':');
            return parts.Length > 0 ? parts[0].Trim().Replace("- ", "") : "";
        }

        private static string FindPropertyInLines(string propertyName, string[] lines)
        {
            foreach (var line in lines)
            {
                if (line.Contains(propertyName) && line.Contains(":"))
                    return line;
            }
            return "";
        }

        /// <summary>
        /// Filtra la lista de rutas de ScanConflictsWithGit() para quedarse
        /// solo con aquellas dentro de Assets/ que sean archivos binarios,
        /// y genera la lista de AssetConflict correspondiente.
        /// </summary>
        private static List<AssetConflict> ScanForBinaryConflicts()
        {
            var binaryConflicts = new List<AssetConflict>();
            var allConflicts = GitManager.ScanConflictsWithGit();
            foreach (var filePath in allConflicts)
            {
                if (filePath.StartsWith("Assets/") && IsBinaryFile(filePath))
                {
                    var conflict = CreateBinaryAssetConflict(filePath);
                    if (conflict != null)
                    {
                        binaryConflicts.Add(conflict);
                        Debug.Log($"Found binary conflict via Git status: {filePath}");
                    }
                }
            }
            return binaryConflicts;
        }

        private static AssetConflict CreateBinaryAssetConflict(string assetPath)
        {
            try
            {
                var conflict = new AssetConflict(assetPath)
                {
                    beforeContent = "Versión actual (HEAD)",
                    afterContent = "Versión entrante (INCOMING)",
                    propertyConflicts = new List<PropertyConflict>
                    {
                        new PropertyConflict
                        {
                            PropertyName = "Binary Asset",
                            BeforeValue = "Versión actual (HEAD)",
                            AfterValue = "Versión entrante (INCOMING)",
                            SelectedValue = ConflictChoice.None
                        }
                    }
                };

                return conflict;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error creating binary asset conflict for {assetPath}: {e.Message}");
                return null;
            }
        }

        #endregion
    }
}
