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
            Require(full.Definition.revision == 34 && demo.Definition.revision == 34,
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
            Require(full.Nodes.Count(node => node.terminal) == 2, "Expected distinct story outcomes.");
            VerifyPaths(full, demo);
            VerifyOpeningRecovery(full);
            VerifyGuRoute(full);
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
                         "ending.oldPublic", "ending.newRoad"
                     })
            {
                Require(CanReach(full, ending), "Cannot reach story outcome: " + ending);
            }

            var demoSession = new StorySession(demo, 42);
            AdvanceTo(demoSession, "case.boundary");
            var fullSession = StorySession.Restore(full, demoSession.Snapshot());
            Require(fullSession.Current.id == "case.boundary", "Demo save did not import.");
            Require(fullSession.Choose("village") && fullSession.Current.id == "g01.intro",
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

        private static void VerifyGuRoute(StoryGraph full)
        {
            var run = new StorySession(full, 42);
            AdvanceTo(run, "case.boundary");
            Require(run.Choose("village"), "Gu Heng route is unavailable.");
            StorySnapshot beforeArchive = null;
            StorySnapshot beforeTest = null;
            StorySnapshot councilInference = null;
            for (var step = 0; step < 250 && run.Current.id != "ending.newRoad"; step++)
            {
                var id = run.Current.id;
                if (id == "g03.keep") beforeArchive = run.Snapshot();
                if (id == "g07.test") beforeTest = run.Snapshot();
                if (id == "h04.infer") councilInference = run.Snapshot();
                var choiceId = id switch
                {
                    "g01.table" when run.GetValue("g.oldCharm") == 0 => "charm",
                    "g01.table" when run.GetValue("g.bellBook") == 0 => "book",
                    "g01.table" => "compare",
                    "g02.marks" => "compare",
                    "g03.keep" => "give",
                    "g04.ask" => "time",
                    "g05.answer" => "listen",
                    "g06.order" => "slow",
                    "g07.test" => "measure",
                    "g08.question" => "share",
                    "g09.record" => "separate",
                    "h01.infer" => "three",
                    "h02.choice" => "preserved",
                    "h04.table" when run.GetValue("h.shen") == 0 => "shen",
                    "h04.table" when run.GetValue("h.qi") == 0 => "qi",
                    "h04.table" when run.GetValue("h.gu") == 0 => "gu",
                    "h04.table" => "joint",
                    "h04.infer" => "separate",
                    "h05.assign" => "split",
                    "h05.stop" => "stop",
                    "h05.decision" => "prepare",
                    "h06.table" when run.GetValue("h.house1") == 0 => "elder",
                    "h06.table" when run.GetValue("h.house2") == 0 => "father",
                    "h06.table" when run.GetValue("h.house3") == 0 => "carpenter",
                    "h06.table" => "proceed",
                    "e01.order" => "slow",
                    "e02.bell" => "place",
                    "e03.choice" => "new",
                    "e04.third" => "stop",
                    _ => string.Empty
                };
                Require(string.IsNullOrEmpty(choiceId) ? run.Advance() : run.Choose(choiceId),
                    "Gu Heng route stopped at " + id);
            }

            Require(run.Current.id == "ending.newRoad" && run.GetValue("g.corrected") == 1
                    && run.GetValue("g.walked") == 1 && run.GetValue("g.thirdPillar") == 1
                    && run.GetValue("g.villageRecord") == 1 && run.GetValue("h.roles") == 1
                    && run.GetValue("e.flowMeasured") == 1,
                "Gu Heng route did not carry its evidence through the diversion.");
            Require(beforeArchive != null && beforeTest != null && councilInference != null,
                "Gu Heng branch snapshots are missing.");
            var wrongCouncil = StorySession.Restore(full, councilInference);
            Require(wrongCouncil.Choose("blame") && wrongCouncil.CurrentText.Contains("周望指着初试本")
                    && wrongCouncil.Advance() && wrongCouncil.Current.id == "h04.infer"
                    && wrongCouncil.Choose("separate") && wrongCouncil.Advance()
                    && wrongCouncil.Current.id == "h05.roles",
                "The council did not correct a merged accusation before agreeing on test roles.");

            var custody = StorySession.Restore(full, beforeArchive);
            Require(custody.Choose("take"), "Taking the original book was unavailable.");
            AdvanceTo(custody, "g04.ask");
            Require(!custody.IsAvailable(custody.Current.choices.First(choice => choice.id == "time"))
                    && custody.Choose("return") && custody.Advance()
                    && custody.GetValue("g.archiveHeld") == 1
                    && custody.Choose("time"),
                "Taking the book did not require returning it to the family before questioning.");

            var oldMap = StorySession.Restore(full, beforeTest);
            Require(oldMap.Choose("old"), "Testing the old map was unavailable.");
            AdvanceTo(oldMap, "g08.question");
            Require(oldMap.GetValue("g.repairOwed") == 1
                    && !oldMap.IsAvailable(oldMap.Current.choices.First(choice => choice.id == "share")),
                "Cracking the village charm did not delay another test.");
            var publicHold = StorySession.Restore(full, oldMap.Snapshot());
            Require(publicHold.Choose("hold") && publicHold.Choose("warnFirst") && publicHold.Advance()
                    && publicHold.Current.id == "ending.oldPublic",
                "The village could not decline another test and keep the old array public.");
            Require(oldMap.Choose("repair") && oldMap.CurrentText.Contains("小试裂开的村符")
                    && oldMap.Advance() && oldMap.GetValue("g.repairOwed") == 0
                    && oldMap.IsAvailable(oldMap.Current.choices.First(choice => choice.id == "share")),
                "Repairing the charm did not reopen the village decision.");
        }

        private static void AdvanceTo(StorySession session, string target)
        {
            for (var step = 0; step < 1000 && session.Current.id != target; step++)
            {
                if (session.Current.choices.Length > 0)
                {
                    Require(session.Choose(session.Current.choices.First(session.IsAvailable).id), "Choice failed.");
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
