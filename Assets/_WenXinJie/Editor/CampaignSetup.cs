using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using WenXinJie.Presentation;

namespace WenXinJie.Editor
{
    public static class CampaignSetup
    {
        private const string Root = "Assets/_WenXinJie/";

        [MenuItem("WenXinJie/Setup/Create Campaign Scene")]
        public static void Run()
        {
            if (EditorApplication.isPlaying || SceneManager.GetActiveScene().isDirty)
            {
                throw new InvalidOperationException("Exit Play Mode and save scene edits before setup.");
            }

            AssetDatabase.Refresh();
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { Root + "Art" }))
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid));
                importer.textureType = TextureImporterType.Default;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.maxTextureSize = 2048;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.SaveAndReimport();
            }

            const string catalogPath = Root + "Data/StoryMedia.asset";
            StoryMediaCatalog catalog = AssetDatabase.LoadAssetAtPath<StoryMediaCatalog>(catalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<StoryMediaCatalog>();
                AssetDatabase.CreateAsset(catalog, catalogPath);
            }

            var media = new SerializedObject(catalog);
            Assign(media, "_font", "Fonts/NotoSerifSC-Regular.ttf");
            Assign(media, "_titleFont", "Fonts/ZhiMangXing-Regular.ttf");
            Assign(media, "_menuFont", "Fonts/ZCOOLXiaoWei-Regular.ttf");
            Assign(media, "_panel", "Art/UI/ImmortalGlassPanel.png");
            Assign(media, "_choice", "Art/UI/ImmortalGlassChoice.png");
            Assign(media, "_music", "Audio/BGM/AsianOriental2_Tozan.ogg");
            Assign(media, "_rain", "Audio/Ambience/Rain_Window_Ylmir.ogg");
            Images(media.FindProperty("_backgrounds"),
                new[] { "gate", "ridge", "night", "stele", "quartersNight", "mountainHeart" },
                new[]
                {
                    "Art/Backgrounds/CloudGate_Premium_1080.jpg", "Art/Backgrounds/PineRidge_Premium_1080.jpg",
                    "Art/Backgrounds/PineRidge_Premium_Night_1080.jpg",
                    "Art/Backgrounds/SteleCorridor_Premium_1080.jpg",
                    "Art/Backgrounds/DiscipleQuartersNight_Premium_1080.jpg",
                    "Art/Backgrounds/MountainHeartGate_Premium_1080.jpg"
                });
            Portraits(media.FindProperty("_portraits"), new[] { "linYue" },
                new[] { "Art/Generated/LinYue_Premium_Cutout.png" });
            media.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            catalog = AssetDatabase.LoadAssetAtPath<StoryMediaCatalog>(catalogPath);
            if (catalog == null)
            {
                throw new InvalidOperationException("Story media catalog failed to import.");
            }

            const string scenePath = Root + "Scenes/Game.unity";
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) != null)
            {
                EditorSceneManager.OpenScene(scenePath);
                StoryApplication existing = UnityEngine.Object.FindAnyObjectByType<StoryApplication>();
                if (existing == null)
                {
                    throw new InvalidOperationException("The existing Game scene has no StoryApplication.");
                }

                ConnectApplication(existing, catalog);
                EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
                EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(scenePath, true) };
                Debug.Log("Campaign media and existing Game scene updated.");
                return;
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.08f, 0.18f, 0.16f);
            camera.orthographic = true;
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            var application = new GameObject("StoryApplication").AddComponent<StoryApplication>();
            ConnectApplication(application, catalog);
            EditorSceneManager.SaveScene(scene, scenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(scenePath, true) };
            Debug.Log("Campaign scene created. Prototype is preserved and excluded from builds.");
        }

        private static void ConnectApplication(StoryApplication application, StoryMediaCatalog catalog)
        {
            var serialized = new SerializedObject(application);
            serialized.Update();
            Assign(serialized, "_campaign", "Data/Campaign.json");
            serialized.FindProperty("_media").objectReferenceValue = catalog;
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(application);
        }

        private static void Assign(SerializedObject target, string property, string relativePath)
        {
            UnityEngine.Object asset = AssetDatabase.LoadMainAssetAtPath(Root + relativePath);
            if (asset == null)
            {
                throw new InvalidOperationException("Missing campaign asset: " + relativePath);
            }

            target.FindProperty(property).objectReferenceValue = asset;
        }

        private static void Images(SerializedProperty array, string[] ids, string[] paths)
        {
            array.arraySize = ids.Length;
            for (int index = 0; index < ids.Length; index++)
            {
                SerializedProperty entry = array.GetArrayElementAtIndex(index);
                entry.FindPropertyRelative("id").stringValue = ids[index];
                entry.FindPropertyRelative("texture").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + paths[index]);
            }
        }

        private static void Portraits(SerializedProperty array, string[] ids, string[] paths)
        {
            array.arraySize = ids.Length;
            for (int index = 0; index < ids.Length; index++)
            {
                SerializedProperty entry = array.GetArrayElementAtIndex(index);
                entry.FindPropertyRelative("_id").stringValue = ids[index];
                entry.FindPropertyRelative("_texture").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<Texture2D>(Root + paths[index]);
                entry.FindPropertyRelative("_width").floatValue = 1050f;
                entry.FindPropertyRelative("_top").floatValue = 140f;
                entry.FindPropertyRelative("_right").floatValue = 30f;
            }
        }
    }
}