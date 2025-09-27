using System;
using System.Collections.Generic;

namespace ClearMerge.Utils
{
    /// <summary>
    /// Constants used in conflict detection and resolution process
    /// </summary>
    public static class ClearMergeConstants
    {
        #region Scene Paths
        // Usar los paths de ClearMerge (ajusta si necesitas los de SceneMerger)

        public const string TempBeforeScenePath = "Assets/Editor/ClearMerge/Temp/TempBeforeMerge.unity";
        public const string TempAfterScenePath = "Assets/Editor/ClearMerge/Temp/TempAfterMerge.unity";
        public const string TempFinalPreviewScenePath = "Assets/Editor/ClearMerge/Temp/TempFinalPreview.unity";
        #endregion

        #region Performance Settings
        public const int MAX_LOOK_AHEAD = 2000;
        #endregion


        #region Asset Extensions
        public static readonly string[] BINARY_ASSET_EXTENSIONS =
        {
            ".png", ".jpg", ".jpeg", ".tga", ".bmp", ".gif", ".psd", ".tiff", ".exr", ".hdr",
            ".wav", ".mp3", ".ogg", ".aiff", ".flac", ".m4a", ".wma", ".aac",
            ".fbx", ".obj", ".dae", ".3ds", ".blend", ".max", ".ma", ".mb", ".ply", ".stl",
            ".dll", ".so", ".dylib", ".a", ".lib", ".bundle",
            ".txt", ".xml", ".json", ".csv", ".md", ".pdf", ".doc", ".docx", ".xls", ".xlsx"
        };
        #endregion

        #region Git Conflict Markers
        public const string CONFLICT_START_MARKER = "<<<<<<< HEAD";
        public const string CONFLICT_MIDDLE_MARKER = "=======";
        public const string CONFLICT_END_MARKER = ">>>>>>>";
        #endregion

        public const string CONFLICT_REMOVED = "GameObject Removed";
    }
    #region SharedDataStructures

    /// <summary>
    /// Resolution status for conflicts
    /// </summary>
    public enum ConflictResolution
    {
        Unresolved,
        Resolved
    }

    /// <summary>
    /// Choice options for conflict resolution
    /// </summary>
    public enum ConflictChoice
    {
        None,
        Before,
        After
    }


    /// <summary>
    /// Information about a property conflict (shared structure)
    /// </summary>
    [Serializable]
    public class PropertyConflict
    {
        public string PropertyName { get; set; }
        public string BeforeValue { get; set; }
        public string AfterValue { get; set; }
        public ConflictChoice SelectedValue { get; set; } = ConflictChoice.None;

        public PropertyConflict(string name, string before, string after)
        {
            PropertyName = name;
            BeforeValue = before;
            AfterValue = after;
        }

        public PropertyConflict()
        {
        }
    }

    #endregion
}
