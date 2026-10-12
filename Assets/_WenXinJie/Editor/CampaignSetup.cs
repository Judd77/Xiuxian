using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using WenXinJie.Presentation;
using Object = UnityEngine.Object;

namespace WenXinJie.Editor
{
    public static class CampaignSetup
    {
        private const string Root = "Assets/_WenXinJie/";

        [InitializeOnLoadMethod]
        private static void ScheduleGeneratedFontSetup()
        {
            EditorApplication.delayCall += EnsureGeneratedFonts;
        }

        [MenuItem("WenXinJie/Setup/Create Campaign Scene")]
        public static void Run()
        {
            if (EditorApplication.isPlaying || SceneManager.GetActiveScene().isDirty)
            {
                throw new InvalidOperationException("Exit Play Mode and save scene edits before setup.");
            }

            AssetDatabase.Refresh();
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { Root + "Art" }))
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

            const string catalogPath = Root + "Data/StoryAssets.asset";
            var catalog = AssetDatabase.LoadAssetAtPath<StoryAssetCatalog>(catalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<StoryAssetCatalog>();
                AssetDatabase.CreateAsset(catalog, catalogPath);
            }

            var assets = new SerializedObject(catalog);
            AssignFonts(assets);
            Assign(assets, "panel", "Art/UI/ImmortalGlassPanel.png");
            Assign(assets, "choice", "Art/UI/ImmortalGlassChoice.png");
            Assign(assets, "music", "Audio/BGM/AsianOriental2_Tozan.ogg");
            Assign(assets, "rain", "Audio/Ambience/Rain_Window_Ylmir.ogg");
            Assign(assets, "menuParticleShader", "Art/UI/MenuParticles.shader");
            Images(assets.FindProperty("backgrounds"),
                new[] { "gate", "ridge", "night", "stele", "quartersNight", "mountainHeart" },
                new[]
                {
                    "Art/Backgrounds/CloudGate_Premium_1080.jpg", "Art/Backgrounds/PineRidge_Premium_1080.jpg",
                    "Art/Backgrounds/PineRidge_Premium_Night_1080.jpg",
                    "Art/Backgrounds/SteleCorridor_Premium_1080.jpg",
                    "Art/Backgrounds/DiscipleQuartersNight_Premium_1080.jpg",
                    "Art/Backgrounds/MountainHeartGate_Premium_1080.jpg"
                });
            Portraits(assets.FindProperty("portraits"), new[] { "linYue" },
                new[] { "Art/Generated/LinYue_Premium_Cutout.png" });
            assets.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            catalog = AssetDatabase.LoadAssetAtPath<StoryAssetCatalog>(catalogPath);
            if (catalog == null)
            {
                throw new InvalidOperationException("Story asset catalog failed to import.");
            }

            const string scenePath = Root + "Scenes/GameFull.unity";
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) != null)
            {
                EditorSceneManager.OpenScene(scenePath);
                var existing = Object.FindAnyObjectByType<StoryApplication>();
                if (existing == null)
                {
                    throw new InvalidOperationException("The existing full game scene has no StoryApplication.");
                }

                ConnectApplication(existing, catalog);
                EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
                EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(scenePath, true) };
                Debug.Log("Campaign assets and existing Game scene updated.");
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new(0.08f, 0.18f, 0.16f);
            camera.orthographic = true;
            cameraObject.transform.position = new(0f, 0f, -10f);
            var application = new GameObject("StoryApplication").AddComponent<StoryApplication>();
            ConnectApplication(application, catalog);
            EditorSceneManager.SaveScene(scene, scenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(scenePath, true) };
            Debug.Log("Campaign scene created. Prototype is preserved and excluded from builds.");
        }

        private static void ConnectApplication(StoryApplication application, StoryAssetCatalog catalog)
        {
            var serialized = new SerializedObject(application);
            serialized.Update();
            Assign(serialized, "campaign", "Data/Campaign.json");
            serialized.FindProperty("storyAssets").objectReferenceValue = catalog;
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(application);
        }

        private static void EnsureGeneratedFonts()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            const string catalogPath = Root + "Data/StoryAssets.asset";
            var catalog = AssetDatabase.LoadAssetAtPath<StoryAssetCatalog>(catalogPath);
            if (catalog == null)
            {
                return;
            }

            var assets = new SerializedObject(catalog);
            AssignFonts(assets);
            assets.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
        }

        private static void AssignFonts(SerializedObject assets)
        {
            var characters = CollectProjectCharacters();
            AssignFont(assets, "font", "Fonts/NotoSerifSC-Regular.ttf",
                "Fonts/TMP/NotoSerifSC-Regular SDF.asset", characters);
            AssignFont(assets, "titleFont", "Fonts/ZhiMangXing-Regular.ttf",
                "Fonts/TMP/ZhiMangXing-Regular SDF.asset", characters);
            AssignFont(assets, "menuFont", "Fonts/WenXinJieXiaoWei-Regular.ttf",
                "Fonts/TMP/WenXinJieXiaoWei-Regular SDF.asset", characters);
        }

        private static void Assign(SerializedObject target, string property, string relativePath)
        {
            var asset = AssetDatabase.LoadMainAssetAtPath(Root + relativePath);
            if (asset == null)
            {
                throw new InvalidOperationException("Missing campaign asset: " + relativePath);
            }

            target.FindProperty(property).objectReferenceValue = asset;
        }

        private static void AssignFont(SerializedObject target, string property, string sourcePath,
            string assetPath, string characters)
        {
            var fullAssetPath = Root + assetPath;
            var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fullAssetPath);
            if (fontAsset == null)
            {
                var source = AssetDatabase.LoadAssetAtPath<Font>(Root + sourcePath);
                if (source == null)
                {
                    throw new InvalidOperationException("Missing campaign font: " + sourcePath);
                }

                EnsureFolder(Root + "Fonts/TMP");
                fontAsset = TMP_FontAsset.CreateFontAsset(
                    source,
                    90,
                    9,
                    GlyphRenderMode.SDFAA,
                    2048,
                    2048);
                if (fontAsset == null)
                {
                    throw new InvalidOperationException("Could not create TMP font asset from " + sourcePath);
                }

                fontAsset.name = Path.GetFileNameWithoutExtension(fullAssetPath);
                fontAsset.TryAddCharacters(characters, out var missingCharacters, true);
                AssetDatabase.CreateAsset(fontAsset, fullAssetPath);

                if (!string.IsNullOrEmpty(missingCharacters))
                {
                    Debug.LogWarning(source.name + " does not contain some project characters: "
                                                 + missingCharacters);
                }
            }

            AddSubAsset(fontAsset.material, fontAsset);
            foreach (var texture in fontAsset.atlasTextures)
            {
                AddSubAsset(texture, fontAsset);
            }

            target.FindProperty(property).objectReferenceValue = fontAsset;
        }

        private static void AddSubAsset(Object asset, TMP_FontAsset fontAsset)
        {
            if (asset != null && !AssetDatabase.Contains(asset))
            {
                AssetDatabase.AddObjectToAsset(asset, fontAsset);
            }
        }

        private static string CollectProjectCharacters()
        {
            var characters = new HashSet<char>();
            var extensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".cs",
                ".json",
                ".txt"
            };
            foreach (var path in Directory.EnumerateFiles(Root, "*.*", SearchOption.AllDirectories)
                         .Where(path => extensions.Contains(Path.GetExtension(path))))
            {
                foreach (var character in File.ReadAllText(path))
                {
                    characters.Add(character);
                }
            }

            return new(characters.OrderBy(character => character).ToArray());
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            var name = Path.GetFileName(path);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(name))
            {
                throw new InvalidOperationException("Invalid asset folder: " + path);
            }

            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        private static void Images(SerializedProperty array, string[] ids, string[] paths)
        {
            for (var index = 0; index < ids.Length; index++)
            {
                var entry = FindOrAppend(array, ids[index]);
                entry.FindPropertyRelative("id").stringValue = ids[index];
                entry.FindPropertyRelative("texture").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + paths[index]);
            }
        }

        private static void Portraits(SerializedProperty array, string[] ids, string[] paths)
        {
            for (var index = 0; index < ids.Length; index++)
            {
                var entry = FindOrAppend(array, ids[index]);
                entry.FindPropertyRelative("id").stringValue = ids[index];
                entry.FindPropertyRelative("texture").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<Texture2D>(Root + paths[index]);
                entry.FindPropertyRelative("width").floatValue = 1050f;
                entry.FindPropertyRelative("top").floatValue = 140f;
                entry.FindPropertyRelative("right").floatValue = 30f;
            }
        }

        private static SerializedProperty FindOrAppend(SerializedProperty array, string id)
        {
            for (var index = 0; index < array.arraySize; index++)
            {
                var entry = array.GetArrayElementAtIndex(index);
                if (entry.FindPropertyRelative("id").stringValue == id)
                {
                    return entry;
                }
            }

            var newIndex = array.arraySize;
            array.InsertArrayElementAtIndex(newIndex);
            return array.GetArrayElementAtIndex(newIndex);
        }
    }
}
