using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using WenXinJie.UI;

namespace WenXinJie.Editor
{
    public static class PrototypeAssetSetup
    {
        private const string AssetRoot = "Assets/_WenXinJie/";

        [MenuItem("WenXinJie/Setup/Connect Licensed Prototype Assets")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                throw new InvalidOperationException("Exit Play Mode before assigning scene assets.");
            }

            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != AssetRoot + "Scenes/Prototype.unity")
            {
                throw new InvalidOperationException("Open the existing Prototype scene first.");
            }

            AssetDatabase.Refresh();
            ConfigureBackground();
            ConfigureAudio(AssetRoot + "Audio/BGM/AsianOriental2_Tozan.ogg", AudioClipLoadType.Streaming);
            ConfigureAudio(AssetRoot + "Audio/Ambience/Rain_Window_Ylmir.ogg", AudioClipLoadType.DecompressOnLoad);

            FirstDayPrototype prototype = UnityEngine.Object.FindAnyObjectByType<FirstDayPrototype>();
            if (prototype == null)
            {
                throw new InvalidOperationException("Prototype controller is missing.");
            }

            Undo.RecordObject(prototype, "Connect licensed prototype assets");
            var serialized = new SerializedObject(prototype);
            Assign(serialized, "_introAsset", AssetRoot + "Data/FirstDayIntro.json");
            Assign(serialized, "_landscape", AssetRoot + "Art/Backgrounds/Landscape_Cool_1080.jpg");
            Assign(serialized, "_music", AssetRoot + "Audio/BGM/AsianOriental2_Tozan.ogg");
            Assign(serialized, "_rain", AssetRoot + "Audio/Ambience/Rain_Window_Ylmir.ogg");
            serialized.ApplyModifiedProperties();

            if (UnityEngine.Object.FindAnyObjectByType<AudioListener>() == null)
            {
                if (Camera.main == null)
                {
                    throw new InvalidOperationException("The scene requires a main camera for its audio listener.");
                }

                Undo.AddComponent<AudioListener>(Camera.main.gameObject);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Prototype scene now references the CC0 landscape, music, and rain assets.");
        }

        private static void Assign(SerializedObject target, string propertyName, string assetPath)
        {
            UnityEngine.Object asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
            if (asset == null)
            {
                throw new InvalidOperationException("Missing asset: " + assetPath);
            }

            target.FindProperty(propertyName).objectReferenceValue = asset;
        }

        private static void ConfigureBackground()
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(AssetRoot + "Art/Backgrounds/Landscape_Cool_1080.jpg");
            importer.textureType = TextureImporterType.Default;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 2048;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.SaveAndReimport();
        }

        private static void ConfigureAudio(string path, AudioClipLoadType loadType)
        {
            var importer = (AudioImporter)AssetImporter.GetAtPath(path);
            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.loadType = loadType;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = 0.8f;
            importer.defaultSampleSettings = settings;
            importer.SaveAndReimport();
        }
    }
}
