using System;
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
        private const string Data = "Assets/_WenXinJie/Data/";

        [MenuItem("WenXinJie/Validation/Run Campaign Smoke Tests")]
        public static void Run()
        {
            var full = Load("Campaign.json");
            var demo = Load("DemoCampaign.json");
            Require(full.Definition.revision == 16 && demo.Definition.revision == 16,
                "Full and Demo revisions differ.");
            Require(full.Definition.id == demo.Definition.id, "Campaign identities differ.");
            Require(full.Nodes.Count() > demo.Nodes.Count(), "Demo is not shorter than Full.");
            Require(demo.GetNode("case.boundary").terminal, "Demo boundary is not terminal.");
            Require(!full.GetNode("case.boundary").terminal, "Full boundary must continue.");
            Require(full.GetNode("case.intro").text.Contains("阿澈"), "New story is not the entry point.");
            Require(full.GetNode("c06.infer").choices.Length == 3,
                "The player must make the ash inference.");
            Require(full.GetNode("c11.verified").choices.Length == 2,
                "The player must choose what to preserve during rescue.");
            Require(full.Nodes.Count(node => node.terminal) >= 5, "Expected distinct story outcomes.");
            VerifyPaths(full, demo);
            VerifyOpeningRecovery(full);
            VerifyAssets(full);
            Debug.Log("Campaign smoke tests PASS: new story graph, endings, Demo handoff and assets.");
        }

        private static StoryGraph Load(string filename)
        {
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(Data + filename);
            Require(asset != null, "Missing campaign: " + filename);
            return new StoryGraph(JsonUtility.FromJson<StoryDefinition>(asset.text));
        }

        private static void VerifyPaths(StoryGraph full, StoryGraph demo)
        {
            foreach (var ending in new[]
                     {
                         "ending.leave", "ending.seizeSelf", "ending.seizeForce",
                         "ending.oldPublic", "ending.oldHidden", "story.six.06.0"
                     })
            {
                Require(CanReach(full, ending), "Cannot reach story outcome: " + ending);
            }

            var demoSession = new StorySession(demo, 42);
            AdvanceTo(demoSession, "case.boundary");
            var fullSession = StorySession.Restore(full, demoSession.Snapshot());
            Require(fullSession.Current.id == "case.boundary", "Demo save did not import.");
            Require(fullSession.Choose("village") && fullSession.Current.id == "story.three.00.0",
                "Full campaign did not continue after Demo.");
        }

        private static void VerifyOpeningRecovery(StoryGraph full)
        {
            var run = new StorySession(full, 42);
            AdvanceTo(run, "c05.test");
            Require(run.Choose("right") && run.Current.id == "c05.retry",
                "Wrong training comparison has no response.");
            Require(run.Advance() && run.Choose("left") && run.GetValue("story.skill") == 1,
                "Training correction did not teach the skill.");
            AdvanceTo(run, "c06.infer");
            Require(run.Choose("person") && run.Current.id == "c06.person",
                "Overstated inference has no consequence.");
            AdvanceTo(run, "c06.revise");
            Require(run.Choose("correct") && run.GetValue("case.ashCorrect") == 1,
                "The player could not correct an overstated inference.");
        }

        private static void AdvanceTo(StorySession session, string target)
        {
            for (var step = 0; step < 1000 && session.Current.id != target; step++)
            {
                if (session.Current.choices.Length > 0)
                {
                    Require(session.Choose(session.Current.choices[0].id), "Choice failed.");
                }
                else
                {
                    Require(session.Advance(), "Advance failed before " + target);
                }
            }

            Require(session.Current.id == target, "Could not reach " + target);
        }

        private static bool CanReach(StoryGraph graph, string target)
        {
            var visited = new System.Collections.Generic.HashSet<string>();
            var pending = new System.Collections.Generic.Stack<string>();
            pending.Push(graph.Definition.start);
            while (pending.Count > 0)
            {
                var id = pending.Pop();
                if (!visited.Add(id)) continue;
                if (id == target) return true;
                var node = graph.GetNode(id);
                if (!string.IsNullOrEmpty(node.next)) pending.Push(node.next);
                foreach (var choice in node.choices) pending.Push(choice.target);
            }
            return false;
        }

        private static void VerifyAssets(StoryGraph graph)
        {
            var catalog = AssetDatabase.FindAssets("t:StoryAssetCatalog")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<StoryAssetCatalog>)
                .FirstOrDefault(asset => asset != null);
            Require(catalog != null, "Story asset catalog is missing.");
            catalog.Validate(graph);
        }

        private static void Require(bool valid, string message)
        {
            if (!valid) throw new InvalidOperationException(message);
        }
    }
}
