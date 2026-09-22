using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using WenXinJie.Narrative;
using WenXinJie.Persistence;
using WenXinJie.Presentation;

namespace WenXinJie.Editor
{
    public static class CampaignSmokeTests
    {
        private const string CampaignPath = "Assets/_WenXinJie/Data/Campaign.json";

        [MenuItem("WenXinJie/Validation/Run Campaign Smoke Tests")]
        public static void Run()
        {
            StoryGraph graph = LoadGraph();
            Require(graph.Nodes.Count() == 15, "Unexpected node count.");
            VerifyRoute(graph, "investigate", "visit", "shared", "c01.shared", true);
            VerifyRoute(graph, "investigate", "cultivate", "alone", "c01.alone", false);
            VerifyRoute(graph, "visit", "cultivate", "guarded", "c01.guarded", false);
            VerifyRoute(graph, "cultivate", "cultivate", "uneasy", "c01.uneasy", false);
            VerifySaveRoundTrip(graph);
            VerifyInvalidContentRejected();
            VerifyAssets(graph);
            Debug.Log("Campaign smoke tests PASS: graph validation, four first-night outcomes, second-day state, save round-trip, media and licenses.");
        }

        private static StoryGraph LoadGraph()
        {
            TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(CampaignPath);
            Require(asset != null, "Campaign JSON is missing.");
            return new StoryGraph(JsonUtility.FromJson<StoryDefinition>(asset.text));
        }

        private static void VerifyRoute(StoryGraph graph, string first, string second, string night,
            string expectedOutcome, bool alliance)
        {
            var session = new StorySession(graph);
            Require(session.Advance() && session.Advance(), "Could not reach first-day hub.");
            Require(session.Choose(first), "First action rejected: " + first);
            Require(session.Advance(), "First action did not return to hub.");
            Require(session.Choose(second), "Second action rejected: " + second);
            Require(session.Advance(), "Second action did not return to hub.");
            Require(session.GetValue("actions") == 0, "Actions were not consumed.");
            Require(session.Choose("night"), "Night transition was unavailable.");
            Require(session.Choose(night), "Expected night outcome was unavailable: " + night);
            Require(session.Current.id == expectedOutcome, "Wrong first-night outcome.");
            Require(session.Advance(), "Outcome did not advance to day two.");
            Require(session.Current.id == "c02.dawn" && session.GetValue("day") == 2, "Day two state is incorrect.");
            Require(session.GetValue("route.alliance") == (alliance ? 1 : 0), "Alliance route flag is incorrect.");
            Require(session.Advance() && session.Current.id == "c02.decision", "Could not reach day-two decision.");
            StoryChoice allianceChoice = session.Current.choices.Single(choice => choice.id == "alliance");
            Require(session.IsAvailable(allianceChoice) == alliance, "Day-two choice did not inherit first-day state.");
        }

        private static void VerifySaveRoundTrip(StoryGraph graph)
        {
            var session = new StorySession(graph);
            session.Advance();
            session.Advance();
            session.Choose("investigate");
            Require(session.GetValue("clue.register") == 1, "Entry effect was not applied.");
            StorySnapshot snapshot = session.Snapshot();
            StorySession restored = StorySession.Restore(graph, snapshot);
            Require(restored.Current.id == session.Current.id, "Saved cursor was not restored.");
            Require(restored.GetValue("clue.register") == 1, "Restore lost state.");

            string folder = Path.Combine(Application.temporaryCachePath, "WenXinJieCampaignTests", Guid.NewGuid().ToString("N"));
            string path = Path.Combine(folder, "save.json");
            var store = new CampaignSaveStore();
            store.Save(path, session);
            StorySession disk = store.Load(path, graph, out bool recovered);
            Require(!recovered && disk.GetValue("clue.register") == 1, "Disk round-trip failed.");
            store.Save(path, session);
            File.WriteAllText(path, "corrupt");
            StorySession backup = store.Load(path, graph, out recovered);
            Require(recovered && backup.Current.id == session.Current.id, "Backup recovery failed.");
        }

        private static void VerifyInvalidContentRejected()
        {
            bool rejected = false;
            try
            {
                new StoryGraph(new StoryDefinition
                {
                    id = "bad",
                    revision = 1,
                    start = "missing",
                    variables = Array.Empty<VariableDefinition>(),
                    nodes = Array.Empty<StoryNode>()
                });
            }
            catch (ArgumentException)
            {
                rejected = true;
            }

            Require(rejected, "Graph accepted a missing start node.");
        }

        private static void VerifyAssets(StoryGraph graph)
        {
            StoryMediaCatalog catalog = AssetDatabase.LoadAssetAtPath<StoryMediaCatalog>("Assets/_WenXinJie/Data/StoryMedia.asset");
            Require(catalog != null, "Media catalog is missing.");
            catalog.Validate(graph);
            Require(File.Exists("Assets/_WenXinJie/Fonts/OFL.txt"), "Font license is missing.");
            Require(File.Exists("Docs/AssetResearch/campaign-assets.json"), "Asset checksum inventory is missing.");
            Require(EditorBuildSettings.scenes.Length == 1
                && EditorBuildSettings.scenes[0].path == "Assets/_WenXinJie/Scenes/Game.unity", "Game scene is not the build entry.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}
