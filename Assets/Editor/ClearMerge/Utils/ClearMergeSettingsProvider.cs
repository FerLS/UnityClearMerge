// Place this in an Editor folder
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace ClearMerge.Utils
{
    public static class ClearMergeSettings
    {
        private const string Prefix = "SCV_";

        // Enums for the new options
        public enum MergeResolutionStrategy
        {
            OverwriteScene,
            SaveAsNew
        }

        public enum GitCommitStrategy
        {
            AutoCommitCurrentBranch,
            CreateNewBranchAndCommit,
            DontCommit
        }

        // Add the new Safe Mode setting
        public static bool SafeModePushes
        {
            get => EditorPrefs.GetBool(Prefix + "SafeModePushes", true); // Default to true
            set => EditorPrefs.SetBool(Prefix + "SafeModePushes", value);
        }

        public static Dictionary<string, string> CustomComponentMap
        {
            get
            {
                string raw = EditorPrefs.GetString(Prefix + "CustomTypes", "");
                var dict = new Dictionary<string, string>();
                foreach (var pair in raw.Split(new[] { ";" }, System.StringSplitOptions.RemoveEmptyEntries))
                {
                    var parts = pair.Split(':');
                    if (parts.Length == 2)
                        dict[parts[0]] = parts[1];
                }
                return dict;
            }
            set
            {
                var entries = value.Select(kv => $"{kv.Key}:{kv.Value}");
                EditorPrefs.SetString(Prefix + "CustomTypes", string.Join(";", entries));
            }
        }

        // New preference properties
        public static MergeResolutionStrategy MergeResolution
        {
            get => (MergeResolutionStrategy)EditorPrefs.GetInt(Prefix + "MergeResolution", 0);
            set => EditorPrefs.SetInt(Prefix + "MergeResolution", (int)value);
        }

        public static GitCommitStrategy GitCommitAction
        {
            get => (GitCommitStrategy)EditorPrefs.GetInt(Prefix + "GitCommitAction", 0);
            set => EditorPrefs.SetInt(Prefix + "GitCommitAction", (int)value);
        }

        public static Dictionary<string, string> BuiltInComponentMap => BuiltInTypes;

        // List of special object types to disable during merge
        public static List<string> DisableSpecialObjectTypes
        {
            get
            {
                string raw = EditorPrefs.GetString(Prefix + "DisableObjectTypes", "Light"); // Default to Light
                return raw.Split(new[] { ";" }, System.StringSplitOptions.RemoveEmptyEntries).ToList();
            }
            set
            {
                EditorPrefs.SetString(Prefix + "DisableObjectTypes", string.Join(";", value));
            }
        }

        private static readonly Dictionary<string, string> BuiltInTypes = new()
    {
        {"1", "GameObject"}, {"2", "Component"}, {"3", "LevelGameManager"}, {"4", "Transform"}, {"5", "TimeManager"},
        {"6", "GlobalGameManager"}, {"8", "Behaviour"}, {"9", "GameManager"}, {"11", "AudioManager"}, {"12", "ParticleAnimator"},
        {"13", "InputManager"}, {"15", "EllipsoidParticleEmitter"}, {"17", "Pipeline"}, {"18", "EditorExtension"},
        {"19", "Physics2DSettings"}, {"20", "Camera"}, {"21", "Material"}, {"23", "MeshRenderer"}, {"25", "Renderer"},
        {"26", "ParticleRenderer"}, {"27", "Texture"}, {"28", "Texture2D"}, {"29", "SceneSettings"}, {"30", "GraphicsSettings"},
        {"33", "MeshFilter"}, {"41", "OcclusionPortal"}, {"43", "Mesh"}, {"45", "Skybox"}, {"47", "QualitySettings"},
        {"48", "Shader"}, {"49", "TextAsset"}, {"50", "Rigidbody2D"}, {"51", "Physics2DManager"}, {"53", "Collider2D"},
        {"54", "Rigidbody"}, {"55", "PhysicsManager"}, {"56", "Collider"}, {"57", "Joint"}, {"58", "CircleCollider2D"},
        {"59", "HingeJoint"}, {"60", "PolygonCollider2D"}, {"61", "BoxCollider2D"}, {"62", "PhysicsMaterial2D"},
        {"64", "MeshCollider"}, {"65", "BoxCollider"}, {"66", "SpriteCollider2D"}, {"68", "EdgeCollider2D"},
        {"70", "CapsuleCollider2D"}, {"72", "ComputeShader"}, {"74", "AnimationClip"}, {"75", "ConstantForce"},
        {"76", "WorldParticleCollider"}, {"78", "TagManager"}, {"81", "AudioListener"}, {"82", "AudioSource"},
        {"83", "AudioClip"}, {"84", "RenderTexture"}, {"86", "CustomRenderTexture"}, {"89", "Cubemap"}, {"90", "Avatar"},
        {"91", "AnimatorController"}, {"92", "GUILayer"}, {"93", "RuntimeAnimatorController"}, {"94", "ScriptMapper"},
        {"95", "Animator"}, {"96", "TrailRenderer"}, {"98", "DelayedCallManager"}, {"102", "TextMesh"},
        {"104", "RenderSettings"}, {"108", "Light"}, {"109", "CGProgram"}, {"110", "BaseAnimationTrack"},
        {"111", "Animation"}, {"114", "MonoBehaviour"}, {"115", "MonoScript"}, {"116", "MonoManager"},
        {"117", "Texture3D"}, {"118", "NewAnimationTrack"}, {"119", "Projector"}, {"120", "LineRenderer"},
        {"121", "Flare"}, {"122", "Halo"}, {"123", "LensFlare"}, {"124", "FlareLayer"}, {"125", "HaloLayer"},
        {"126", "NavMeshAreas"}, {"127", "HaloManager"}, {"128", "Font"}, {"129", "PlayerSettings"},
        {"130", "NamedObject"}, {"131", "GUITexture"}, {"132", "GUIText"}, {"133", "GUIElement"},
        {"134", "PhysicMaterial"}, {"135", "SphereCollider"}, {"136", "CapsuleCollider"}, {"137", "SkinnedMeshRenderer"},
        {"138", "FixedJoint"}, {"140", "RaycastCollider"}, {"141", "BuildSettings"}, {"142", "AssetBundle"},
        {"143", "CharacterController"}, {"144", "CharacterJoint"}, {"145", "SpringJoint"}, {"146", "WheelCollider"},
        {"147", "ResourceManager"}, {"148", "NetworkView"}, {"149", "NetworkManager"}, {"150", "PreloadData"},
        {"152", "MovieTexture"}, {"153", "ConfigurableJoint"}, {"154", "TerrainCollider"}, {"155", "MasterServerInterface"},
        {"156", "TerrainData"}, {"157", "LightmapSettings"}, {"158", "WebCamTexture"}, {"159", "EditorSettings"},
        {"160", "InteractiveCloth"}, {"161", "ClothRenderer"}, {"163", "SkinnedCloth"}, {"164", "AudioReverbFilter"},
        {"165", "AudioHighPassFilter"}, {"166", "AudioChorusFilter"}, {"167", "AudioReverbZone"}, {"168", "AudioEchoFilter"},
        {"169", "AudioLowPassFilter"}, {"170", "AudioDistortionFilter"}, {"180", "AudioBehaviour"}, {"181", "AudioFilter"},
        {"182", "WindZone"}, {"183", "Cloth"}, {"184", "SubstanceArchive"}, {"185", "ProceduralMaterial"},
        {"186", "ProceduralTexture"}, {"191", "OffMeshLink"}, {"192", "OcclusionArea"}, {"193", "Tree"},
        {"194", "NavMesh"}, {"195", "NavMeshAgent"}, {"196", "NavMeshSettings"}, {"197", "LightProbeCloud"},
        {"198", "ParticleSystem"}, {"199", "ParticleSystemRenderer"}, {"205", "LODGroup"}, {"220", "LightProbeGroup"},
        {"1001", "Prefab"}, {"1002", "EditorExtensionImpl"}, {"1003", "AssetImporter"}, {"1004", "AssetDatabase"}
    };
    }

    public class ClearMergeSettingsProvider : SettingsProvider
    {
        private string newId = "";
        private string newName = "";
        private bool showBuiltins = false;
        private Vector2 scrollPosition;
        private bool showMergeSettings = true;
        private bool showGitSettings = true;
        private bool showComponentSettings = true;
        private bool showSpecialObjectsSettings = true;
        private string newSpecialObjectType = "";

        public ClearMergeSettingsProvider(string path, SettingsScope scope)
            : base(path, scope) { }

        public override void OnGUI(string searchContext)
        {
            var boldHeaderStyle = new GUIStyle(EditorStyles.boldLabel);
            boldHeaderStyle.fontSize += 2;

            EditorGUILayout.Space(10);
            GUILayout.Label("⚙️ ClearMerge Settings", boldHeaderStyle);
            EditorGUILayout.Space(10);

            // Use scroll view to handle large content
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            DrawMergeSettings();

            EditorGUILayout.Space(10);

            DrawGitSettings();

            EditorGUILayout.Space(10);

            DrawSpecialObjectSettings();

            EditorGUILayout.Space(10);

            DrawComponentSettings();

            EditorGUILayout.EndScrollView();
        }

        private void DrawMergeSettings()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            showMergeSettings = EditorGUILayout.Foldout(showMergeSettings, "🔄 Merge Resolution Strategy", true, EditorStyles.foldoutHeader);

            if (showMergeSettings)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("When resolving scene conflicts:", GUILayout.Width(200));
                ClearMergeSettings.MergeResolution = (ClearMergeSettings.MergeResolutionStrategy)EditorGUILayout.EnumPopup(
                    ClearMergeSettings.MergeResolution, GUILayout.ExpandWidth(true));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(5);
                EditorGUILayout.HelpBox(GetMergeStrategyDescription(ClearMergeSettings.MergeResolution), MessageType.Info);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawGitSettings()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            showGitSettings = EditorGUILayout.Foldout(showGitSettings, "📝 Git Commit Options", true, EditorStyles.foldoutHeader);

            if (showGitSettings)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("After resolving conflicts:", GUILayout.Width(200));
                ClearMergeSettings.GitCommitAction = (ClearMergeSettings.GitCommitStrategy)EditorGUILayout.EnumPopup(
                    ClearMergeSettings.GitCommitAction, GUILayout.ExpandWidth(true));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(5);
                EditorGUILayout.HelpBox(GetGitActionDescription(ClearMergeSettings.GitCommitAction), MessageType.Info);

                // Add Safe Mode toggle
                EditorGUILayout.Space(10);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Safe Mode (Block pushes when scene conflicts exist):", GUILayout.Width(300));
                bool previousSafeMode = ClearMergeSettings.SafeModePushes;
                ClearMergeSettings.SafeModePushes = EditorGUILayout.Toggle(ClearMergeSettings.SafeModePushes);

                // Update marker file when the setting changes
                if (previousSafeMode != ClearMergeSettings.SafeModePushes)
                {
                    UpdateSafeModeMarker();
                }

                EditorGUILayout.EndHorizontal();

                if (ClearMergeSettings.SafeModePushes)
                {
                    EditorGUILayout.HelpBox("Safe Mode will prevent pushing to remote repositories while scene merge conflicts exist.", MessageType.Info);
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawSpecialObjectSettings()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            showSpecialObjectsSettings = EditorGUILayout.Foldout(showSpecialObjectsSettings, "🚫 Special Objects to Disable", true, EditorStyles.foldoutHeader);

            if (showSpecialObjectsSettings)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.HelpBox("Special objects that will be disabled in one scene to avoid duplicates.", MessageType.Info);
                EditorGUILayout.Space(5);

                var disableTypes = ClearMergeSettings.DisableSpecialObjectTypes;
                var typesToRemove = new List<string>();

                if (disableTypes.Count > 0)
                {
                    EditorGUILayout.LabelField("Object Types to Disable", EditorStyles.boldLabel);

                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    foreach (var type in disableTypes)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(type);
                        if (GUILayout.Button("🗑", GUILayout.Width(25)))
                            typesToRemove.Add(type);
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUILayout.EndVertical();
                }
                else
                {
                    EditorGUILayout.HelpBox("No special object types defined. Add one below.", MessageType.Warning);
                }

                foreach (var type in typesToRemove)
                {
                    disableTypes.Remove(type);
                }

                // Add new type section
                EditorGUILayout.Space(10);
                GUILayout.Label("Add New Type", EditorStyles.boldLabel);

                EditorGUILayout.BeginHorizontal();
                newSpecialObjectType = EditorGUILayout.TextField(newSpecialObjectType);
                GUI.enabled = !string.IsNullOrWhiteSpace(newSpecialObjectType) && !disableTypes.Contains(newSpecialObjectType);
                if (GUILayout.Button("Add", GUILayout.Width(60)))
                {
                    disableTypes.Add(newSpecialObjectType.Trim());
                    newSpecialObjectType = "";
                }
                GUI.enabled = true;
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.HelpBox("Examples: Light, AudioListener, ReflectionProbe, etc. Enter Unity component type names.", MessageType.Info);

                // Save changes
                ClearMergeSettings.DisableSpecialObjectTypes = disableTypes;
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawComponentSettings()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            showComponentSettings = EditorGUILayout.Foldout(showComponentSettings, "🧩 Component Type Mappings", true, EditorStyles.foldoutHeader);

            if (showComponentSettings)
            {
                EditorGUILayout.Space(5);

                // Built-in component types
                showBuiltins = EditorGUILayout.Foldout(showBuiltins, "📦 Built-in Types", true);
                if (showBuiltins)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    var builtinScroll = EditorGUILayout.BeginScrollView(Vector2.zero, GUILayout.Height(200));

                    foreach (var kv in ClearMergeSettings.BuiltInComponentMap.OrderBy(kv => int.Parse(kv.Key)))
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField($"{kv.Key}", GUILayout.Width(50));
                        EditorGUILayout.LabelField(kv.Value, EditorStyles.boldLabel);
                        EditorGUILayout.EndHorizontal();
                    }

                    EditorGUILayout.EndScrollView();
                    EditorGUILayout.EndVertical();
                }

                EditorGUILayout.Space(10);

                // Custom component types
                GUILayout.Label("🔧 Custom Types", EditorStyles.boldLabel);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                var customTypes = ClearMergeSettings.CustomComponentMap;
                var keysToRemove = new List<string>();

                if (customTypes.Count > 0)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("ID", EditorStyles.boldLabel, GUILayout.Width(50));
                    EditorGUILayout.LabelField("Type Name", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField("", GUILayout.Width(30));
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.Space(2);

                    foreach (var kv in customTypes.OrderBy(kv => kv.Key))
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField($"{kv.Key}", GUILayout.Width(50));
                        EditorGUILayout.LabelField(kv.Value);
                        if (GUILayout.Button("🗑", GUILayout.Width(25)))
                            keysToRemove.Add(kv.Key);
                        EditorGUILayout.EndHorizontal();
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("No custom types defined yet. Add one below.", MessageType.Info);
                }

                foreach (var key in keysToRemove)
                {
                    customTypes.Remove(key);
                }

                EditorGUILayout.EndVertical();

                EditorGUILayout.Space(10);

                // Add new custom type
                GUILayout.Label("➕ Add New Custom Type", EditorStyles.boldLabel);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("ID:", GUILayout.Width(50));
                newId = EditorGUILayout.TextField(newId, GUILayout.ExpandWidth(true));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Name:", GUILayout.Width(50));
                newName = EditorGUILayout.TextField(newName, GUILayout.ExpandWidth(true));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(5);

                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                var canAdd = !string.IsNullOrWhiteSpace(newId) && !string.IsNullOrWhiteSpace(newName);
                GUI.enabled = canAdd;
                if (GUILayout.Button("Add Custom Type", GUILayout.Width(150), GUILayout.Height(25)))
                {
                    if (!customTypes.ContainsKey(newId))
                    {
                        customTypes[newId.Trim()] = newName.Trim();
                        newId = "";
                        newName = "";
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Duplicate ID", "This ID already exists in the custom types.", "OK");
                    }
                }
                GUI.enabled = true;
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();

                ClearMergeSettings.CustomComponentMap = customTypes;
            }

            EditorGUILayout.EndVertical();
        }

        private string GetMergeStrategyDescription(ClearMergeSettings.MergeResolutionStrategy strategy)
        {
            return strategy switch
            {
                ClearMergeSettings.MergeResolutionStrategy.OverwriteScene =>
                    "Overwrite the original scene file with the resolved version.",
                ClearMergeSettings.MergeResolutionStrategy.SaveAsNew =>
                    "Save the resolved version as a new scene file, preserving the original.",
                _ => "Unknown strategy"
            };
        }

        private string GetGitActionDescription(ClearMergeSettings.GitCommitStrategy action)
        {
            return action switch
            {
                ClearMergeSettings.GitCommitStrategy.AutoCommitCurrentBranch =>
                    "Automatically commit and push the resolved scene to the current branch.",
                ClearMergeSettings.GitCommitStrategy.CreateNewBranchAndCommit =>
                    "Create a new branch and commit and push the resolved scene to it.",
                ClearMergeSettings.GitCommitStrategy.DontCommit =>
                    "Don't automatically commit the changes. You'll need to commit manually.",
                _ => "Unknown action"
            };
        }

        private void UpdateSafeModeMarker()
        {
            string markerPath = "Assets/Editor/SceneConflictSafeMode.marker";

            if (ClearMergeSettings.SafeModePushes)
            {
                // Create or ensure marker file exists
                if (!File.Exists(markerPath))
                {
                    File.WriteAllText(markerPath, "Safe mode enabled");
                    AssetDatabase.Refresh();
                }
            }
            else
            {
                // Remove marker file if it exists
                if (File.Exists(markerPath))
                {
                    File.Delete(markerPath);
                    AssetDatabase.Refresh();
                }
            }
        }

        [SettingsProvider]
        public static SettingsProvider CreateProvider()
        {
            return new ClearMergeSettingsProvider("Project/ClearMerge", SettingsScope.Project);
        }
    }
}
