using UnityEngine;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using System;


namespace ClearMerge.Assets
{
    /// <summary>
    /// Handles meta file conflict detection and resolution
    /// </summary>
    public class MetaConflictResolver
    {
        #region Public Methods
        public static void FixMetaFiles()
        {
            List<string> conflictFiles = FindMetaConflicts();

            if (conflictFiles.Count == 0)
            {
                EditorUtility.DisplayDialog("Fix Meta Files", "No conflicts found in .meta files", "OK");
                return;
            }

            bool fix = EditorUtility.DisplayDialog(
                "Fix Meta Files",
                $"Found {conflictFiles.Count} .meta files with conflicts.\n\nDo you want to fix them?",
                "Fix",
                "Cancel"
            );

            if (fix)
            {
                int fixedCount = FixConflicts(conflictFiles);
                EditorUtility.DisplayDialog("Fix Meta Files", $"Fixed {fixedCount} conflicts", "OK");
                AssetDatabase.Refresh();
            }
        }

        public static List<string> FindMetaConflicts()
        {
            List<string> conflicts = new List<string>();
            string[] metaFiles = Directory.GetFiles(Application.dataPath, "*.meta", SearchOption.AllDirectories);

            foreach (string file in metaFiles)
            {
                if (File.ReadAllText(file).Contains("<<<<<<< HEAD"))
                {
                    conflicts.Add(file);
                }
            }

            return conflicts;
        }

        public static int FixConflicts(List<string> files)
        {
            int fixedCount = 0;

            foreach (string file in files)
            {
                if (FixSingleFile(file))
                {
                    fixedCount++;

                    string assetFile = file.Substring(0, file.Length - 5);
                    if (File.Exists(assetFile) && File.ReadAllText(assetFile).Contains("<<<<<<< HEAD"))
                    {
                        FixSingleFile(assetFile);
                    }
                }
            }

            return fixedCount;
        }
        #endregion

        #region Private Methods
        private static bool FixSingleFile(string filePath)
        {
            try
            {
                string content = File.ReadAllText(filePath);
                string newGuid = GUID.Generate().ToString().Replace("-", "");

                string lineBreak = content.Contains("\r\n") ? "\r\n" : "\n";

                string[] lines = content.Split(new string[] { lineBreak }, StringSplitOptions.None);
                List<string> newLines = new List<string>();
                bool inConflict = false;

                foreach (string line in lines)
                {
                    if (line.Contains("<<<<<<< HEAD"))
                    {
                        inConflict = true;
                        continue;
                    }

                    if (line.Contains(">>>>>>>"))
                    {
                        inConflict = false;
                        continue;
                    }

                    if (line.Contains("======="))
                    {
                        continue;
                    }

                    if (!inConflict)
                    {
                        newLines.Add(line);
                    }
                    else
                    {
                        if (line.Contains("guid:"))
                        {
                            newLines.Add($"guid: {newGuid}");
                        }
                        else
                        {
                            newLines.Add(line);
                        }
                    }
                }

                string result = string.Join(lineBreak, newLines);
                File.WriteAllText(filePath, result);
                return true;
            }
            catch
            {
                return false;
            }
        }
        #endregion
    }


}