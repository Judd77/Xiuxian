using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build.Profile;
using UnityEditor.Build.Reporting;
using UnityEngine;
using WenXinJie.Narrative;

namespace WenXinJie.Editor
{
    public static class SteamEditionBuild
    {
        private const string FullScene = "Assets/_WenXinJie/Scenes/GameFull.unity";
        private const string DemoScene = "Assets/_WenXinJie/Scenes/GameDemo.unity";
        private const string FullCampaign = "Assets/_WenXinJie/Data/Campaign.json";
        private const string DemoCampaign = "Assets/_WenXinJie/Data/DemoCampaign.json";
        private const string DemoBoundary = "case.boundary";

        [MenuItem("WenXinJie/Build/Create Windows Build Profiles")]
        public static void CreateWindowsBuildProfiles()
        {
            var platform = BuildProfile.GetInstalledPlatformModules().FirstOrDefault(info =>
                info.displayName.IndexOf("Windows", StringComparison.OrdinalIgnoreCase) >= 0
                && info.displayName.IndexOf("Server", StringComparison.OrdinalIgnoreCase) < 0);
            if (string.IsNullOrEmpty(platform.displayName))
            {
                throw new InvalidOperationException("Windows Standalone build support is not installed.");
            }

            ConfigureProfile("WenXinJie Full Windows", FullScene, platform.platformGuid);
            ConfigureProfile("WenXinJie Demo Windows", DemoScene, platform.platformGuid);
            AssetDatabase.SaveAssets();
            Debug.Log("Windows build profiles are ready: full game and demo.");
        }

        [MenuItem("WenXinJie/Build/Windows Full Game")]
        public static void BuildFull()
        {
            VerifyFullCampaign();
            Build(FullScene, FullCampaign, DemoCampaign, "Builds/SteamFull/WenXinJie.exe");
        }

        [MenuItem("WenXinJie/Build/Windows Demo")]
        public static void BuildDemo()
        {
            SyncDemoCampaign();
            Build(DemoScene, DemoCampaign, FullCampaign, "Builds/SteamDemo/WenXinJieDemo.exe");
        }

        [MenuItem("WenXinJie/Build/Sync Demo Campaign")]
        public static void SyncDemoCampaign()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException("Exit Play Mode before syncing the demo campaign.");
            }

            var full = LoadCampaign(FullCampaign);
            var demo = CreateDemoCampaign(full);
            var output = JsonUtility.ToJson(demo, true);
            var path = AbsoluteProjectPath(DemoCampaign);
            if (!File.Exists(path) || File.ReadAllText(path) != output)
            {
                File.WriteAllText(path, output, new UTF8Encoding(false));
                AssetDatabase.ImportAsset(DemoCampaign, ImportAssetOptions.ForceSynchronousImport);
            }

            var demoAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(DemoCampaign);
            if (demoAsset == null)
            {
                throw new InvalidOperationException("Demo campaign asset is missing.");
            }

            _ = new StoryGraph(JsonUtility.FromJson<StoryDefinition>(demoAsset.text));
            Debug.Log($"Demo campaign synced: {demo.nodes.Length} reachable nodes, ending at {DemoBoundary}.");
        }

        private static StoryDefinition CreateDemoCampaign(StoryDefinition source)
        {
            var demo = JsonUtility.FromJson<StoryDefinition>(JsonUtility.ToJson(source));
            var nodes = demo.nodes.ToDictionary(node => node.id);
            if (!nodes.TryGetValue(DemoBoundary, out var boundary))
            {
                throw new InvalidOperationException("Demo boundary is missing.");
            }

            boundary.text += "\n\n【试玩结束】完整版可继承本次调查进度。";
            boundary.next = string.Empty;
            boundary.terminal = true;
            boundary.choices = Array.Empty<StoryChoice>();

            var reachable = new HashSet<string>();
            var pending = new Stack<string>();
            pending.Push(demo.start);
            while (pending.Count > 0)
            {
                var id = pending.Pop();
                if (!reachable.Add(id))
                {
                    continue;
                }

                if (!nodes.TryGetValue(id, out var node))
                {
                    throw new InvalidOperationException("Demo references missing node: " + id);
                }

                if (!string.IsNullOrEmpty(node.next))
                {
                    pending.Push(node.next);
                }

                foreach (var choice in node.choices)
                {
                    if (!string.IsNullOrEmpty(choice.target))
                    {
                        pending.Push(choice.target);
                    }

                    foreach (var outcome in choice.outcomes)
                    {
                        pending.Push(outcome.target);
                    }
                }
            }

            if (!reachable.Contains(DemoBoundary))
            {
                throw new InvalidOperationException("Demo ending cannot be reached from the start.");
            }

            demo.nodes = demo.nodes.Where(node => reachable.Contains(node.id)).ToArray();
            _ = new StoryGraph(demo);
            return demo;
        }

        private static StoryDefinition LoadCampaign(string path)
        {
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
            if (asset == null)
            {
                throw new InvalidOperationException("Campaign asset is missing: " + path);
            }

            var definition = JsonUtility.FromJson<StoryDefinition>(asset.text);
            _ = new StoryGraph(definition);
            return definition;
        }

        private static void VerifyFullCampaign()
        {
            _ = LoadCampaign(FullCampaign);
        }

        private static void Build(string scene, string requiredCampaign, string forbiddenCampaign, string output)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scene) == null)
            {
                throw new InvalidOperationException("Edition scene is missing: " + scene);
            }

            var dependencies = AssetDatabase.GetDependencies(scene, true);
            if (!dependencies.Contains(requiredCampaign) || dependencies.Contains(forbiddenCampaign))
            {
                throw new InvalidOperationException("Edition scene references the wrong campaign: " + scene);
            }

            var destination = AbsoluteProjectPath(output);
            Directory.CreateDirectory(Path.GetDirectoryName(destination));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { scene },
                locationPathName = destination,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException("Windows build failed: " + report.summary.result);
            }

            Debug.Log("Windows edition built: " + destination);
        }

        private static string AbsoluteProjectPath(string relativePath)
        {
            return Path.Combine(Directory.GetParent(Application.dataPath).FullName, relativePath);
        }

        private static void ConfigureProfile(string name, string scene, GUID platformGuid)
        {
            var profile = BuildProfile.GetAllBuildProfiles().FirstOrDefault(item => item.name == name)
                          ?? BuildProfile.CreateBuildProfile(platformGuid, name, null);
            if (profile == null)
            {
                throw new InvalidOperationException("Could not create build profile: " + name);
            }

            profile.overrideGlobalScenes = true;
            profile.scenes = new[] { new EditorBuildSettingsScene(scene, true) };
            EditorUtility.SetDirty(profile);
        }
    }
}
