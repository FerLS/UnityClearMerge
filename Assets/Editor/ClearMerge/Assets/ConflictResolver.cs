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
    /// Comprehensive asset conflict resolver that handles both meta files and asset content conflicts
    /// </summary>
    public static class AssetConflictResolver
    {
        #region Public Methods

        /// <summary>
        /// Resolves a single asset conflict by applying selected property resolutions
        /// or copying the chosen binary version.
        /// </summary>
        public static bool ResolveAssetConflict(AssetConflict conflict)
        {
            if (conflict == null || string.IsNullOrEmpty(conflict.assetPath))
                return false;

            try
            {
                string fullPath = Path.GetFullPath(conflict.assetPath);
                if (!File.Exists(fullPath))
                {
                    Debug.LogWarning($"Asset file not found: {conflict.assetPath}");
                    return false;
                }

                string ext = Path.GetExtension(fullPath).ToLower();
                // Binary case: copy before/after temp file
                if (ClearMergeConstants.BINARY_ASSET_EXTENSIONS.Contains(ext))
                {
                    return ResolveBinary(conflict, fullPath);
                }

                // Textual case: apply property-level replacements
                string content = File.ReadAllText(fullPath);
                string resolved = ApplyPropertyConflicts(content, conflict.propertyConflicts);

                File.WriteAllText(fullPath, resolved);
                Debug.Log($"Resolved text conflict in: {conflict.assetPath}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error resolving conflict in {conflict.assetPath}: {e.Message}");
                return false;
            }
        }

        #endregion

        #region Private Helpers

        private static bool ResolveBinary(AssetConflict conflict, string fullPath)
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "ClearMergePreview");
            string fileName = Path.GetFileName(fullPath);
            string beforeFile = Path.Combine(tempDir, "before_" + fileName);
            string afterFile = Path.Combine(tempDir, "after_" + fileName);

            // Determine choice by majority selection on properties (or default BEFORE)
            bool pickAfter = conflict.propertyConflicts.Count > 0 &&
                             conflict.propertyConflicts.All(p => p.SelectedValue == ConflictChoice.After);

            string src = pickAfter ? afterFile : beforeFile;
            if (!File.Exists(src))
            {
                Debug.LogWarning($"Binary temp file not found: {src}");
                return false;
            }

            File.Copy(src, fullPath, true);
            GitManager.StageFileInGit(conflict.assetPath);
            Debug.Log($"Resolved binary conflict ({(pickAfter ? "AFTER" : "BEFORE")}) in: {conflict.assetPath}");
            return true;
        }



        /// <summary>
        /// Applies each PropertyConflict by replacing its BEFORE line with its AFTER line.
        /// Also removes any leftover Git conflict markers.
        /// </summary>
        private static string ApplyPropertyConflicts(string content, List<PropertyConflict> props)
        {
            // First, remove Git markers entirely
            var lines = content
                .Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
                .Where(l => !l.StartsWith(ClearMergeConstants.CONFLICT_START_MARKER)
                         && !l.StartsWith(ClearMergeConstants.CONFLICT_MIDDLE_MARKER)
                         && !l.StartsWith(ClearMergeConstants.CONFLICT_END_MARKER))
                .ToList();

            // Join back to a single string
            string result = string.Join("\n", lines);

            // Then apply each property-level replacement
            foreach (var p in props)
            {
                if (p.SelectedValue == ConflictChoice.After)
                {
                    result = ReplaceLine(result, p.BeforeValue, p.AfterValue);
                }
                // if Before or None, keep the original
            }

            return result;
        }

        /// <summary>
        /// Replaces an old line with a new line in the full text.
        /// </summary>
        private static string ReplaceLine(string text, string oldLine, string newLine)
        {
            if (string.IsNullOrEmpty(oldLine)) return text;
            // Trim to avoid whitespace mismatches
            string o = oldLine.Trim();
            string n = newLine.Trim();
            return text.Replace(o, n);
        }

        #endregion
    }
}
