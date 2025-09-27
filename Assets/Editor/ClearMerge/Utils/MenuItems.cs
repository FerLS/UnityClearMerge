using UnityEditor;


namespace ClearMerge.Utils
{
    /// <summary>
    /// Unity menu items for ClearMerge tools
    /// </summary>
    public static class MenuItems
    {

        [MenuItem("Tools/ClearMerge/Main Window", priority = 0)]
        public static void ShowMainWindow()
        {
            MainWindow.ShowWindow();
        }

        [MenuItem("Tools/ClearMerge/Scene Conflicts Viewer", priority = 1)]
        public static void OpenSceneConflictViewer()
        {
            ClearMerge.Scenes.ConflictViewerWindow.ShowWindow();
        }

        [MenuItem("Tools/ClearMerge/Asset Conflicts Viewer", priority = 2)]
        public static void OpenAssetConflictResolver()
        {
            ClearMerge.Assets.ConflictViewerWindow.ShowWindow();
        }

        [MenuItem("Tools/ClearMerge/Utils/Setup")]
        public static void OpenSceneSetup()
        {
            ClearMerge.Utils.ClearMergeSetup.ShowWindow();
        }

        [MenuItem("Tools/ClearMerge/Utils/Scan Now for Conflicts")]
        public static void ScanForConflicts()
        {
            GlobalConflictDetector.DetectAllConflicts();
        }

        [MenuItem("Tools/ClearMerge/Utils/Fix Meta Conflicts")]
        public static void FixMetaConflicts()
        {
            ClearMerge.Assets.MetaConflictResolver.FixMetaFiles();
        }

        [MenuItem("Tools/ClearMerge/Utils/Settings")]
        public static void OpenSettings()
        {
            SettingsService.OpenProjectSettings("Project/ClearMerge");
        }


    }

}
