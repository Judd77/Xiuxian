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
            Require(graph.Nodes.Count() == 41, "Unexpected node count.");
            VerifyRoute(graph, "investigate", "copyPage", "visit", "confide", "shared", "c01.shared", true);
            VerifyRoute(graph, "investigate", "takePage", "cultivate", "steadyBreath", "alone", "c01.alone", false);
            VerifyRoute(graph, "visit", "question", "cultivate", "steadyBreath", "guarded", "c01.guarded", false);
            VerifyRoute(graph, "cultivate", "followEcho", "cultivate", "steadyBreath", "uneasy", "c01.uneasy", false);
            VerifySaveRoundTrip(graph);
            VerifyInvalidContentRejected();
            VerifyAssets(graph);
            Debug.Log("Campaign smoke tests PASS: 41-node graph, four first-night outcomes, second-day state, save round-trip, media and licenses.");
        }

        private static StoryGraph LoadGraph()
        {
            TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(CampaignPath);
            Require(asset != null, "Campaign JSON is missing.");
            return new StoryGraph(JsonUtility.FromJson<StoryDefinition>(asset.text));
        }

        private static void VerifyRoute(StoryGraph graph, string firstAction, string firstResolution,
            string secondAction, string secondResolution, string night, string expectedOutcome, bool alliance)
        {
            var session = new StorySession(graph);
            ReachFirstDayHub(session);
            PerformAction(session, firstAction, firstResolution);
            PerformAction(session, secondAction, secondResolution);
            Require(session.GetValue("actions") == 0, "Actions were not consumed.");
            Require(session.Choose("night"), "Night transition was unavailable.");
            Require(session.Choose(night), "Expected night outcome was unavailable: " + night);
            Require(session.Current.id == expectedOutcome, "Wrong first-night outcome.");
            Require(session.Advance() && session.Advance() && session.Advance(), "Outcome did not advance to day two.");
            Require(session.Current.id == "c02.dawn" && session.GetValue("day") == 2, "Day two state is incorrect.");
            Require(session.GetValue("route.alliance") == (alliance ? 1 : 0), "Alliance route flag is incorrect.");
            Require(session.Advance() && session.Current.id == "c02.decision", "Could not reach day-two decision.");
            StoryChoice allianceChoice = session.Current.choices.Single(choice => choice.id == "alliance");
            Require(session.IsAvailable(allianceChoice) == alliance, "Day-two choice did not inherit first-day state.");
        }

        private static void ReachFirstDayHub(StorySession session)
        {
            Require(session.Advance() && session.Advance() && session.Advance(), "Could not reach the opening response.");
            Require(session.Choose("watch"), "Opening response was rejected.");
            Require(session.Advance() && session.Current.id == "c01.hub", "Could not reach first-day hub.");
        }

        private static void PerformAction(StorySession session, string action, string resolution)
        {
            Require(session.Choose(action), "Action rejected: " + action);
            Require(session.Advance() && session.Advance(), "Action did not reach its internal decision: " + action);
            Require(session.Choose(resolution), "Action resolution rejected: " + resolution);
            Require(session.Advance() && session.Current.id == "c01.hub", "Action did not return to hub: " + action);
        }

        private static void VerifySaveRoundTrip(StoryGraph graph)
        {
            var session = new StorySession(graph);
            ReachFirstDayHub(session);
            PerformAction(session, "investigate", "copyPage");
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
                _ = new StoryGraph(new()
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

            rejected = false;
            try
            {
                new StoryGraph(new StoryDefinition
                {
                    id = "unreachable",
                    revision = 1,
                    start = "start",
                    variables = Array.Empty<VariableDefinition>(),
                    nodes = new[]
                    {
                        TerminalNode("start"),
                        TerminalNode("orphan")
                    }
                });
            }
            catch (ArgumentException)
            {
                rejected = true;
            }

            Require(rejected, "Graph accepted an unreachable node.");
        }

        private static StoryNode TerminalNode(string id)
        {
            return new()
            {
                id = id,
                text = id,
                background = "gate",
                terminal = true
            };
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