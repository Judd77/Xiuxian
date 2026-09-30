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
            Require(full.Definition.revision == 22 && demo.Definition.revision == 22,
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
            VerifyLinRoute(full);
            VerifyGuRoute(full);
            VerifyShenRoute(full);
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
                         "ending.oldPublic", "ending.oldHidden", "ending.newRoad"
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

        private static void VerifyLinRoute(StoryGraph full)
        {
            var run = new StorySession(full, 42);
            AdvanceTo(run, "case.boundary");
            Require(run.Choose("rescue"), "Lin Yue route is unavailable.");
            StorySnapshot beforeLetters = null;
            StorySnapshot beforeTestimony = null;
            for (var step = 0; step < 150 && run.Current.id != "ending.leave"; step++)
            {
                var id = run.Current.id;
                if (id == "l03.plan") beforeLetters = run.Snapshot();
                if (id == "l09.record") beforeTestimony = run.Snapshot();
                if (id == "l05.cart")
                    Require(run.CurrentText.Contains("桂花糕"), "Borrowed letter was not used at the door.");
                if (id == "l08.road")
                    Require(run.CurrentText.Contains("守卫没认出姐姐"), "Covered approach did not carry forward.");
                var choiceId = id switch
                {
                    "l01.table" when run.GetValue("l.medical") == 0 => "medicine",
                    "l01.table" when run.GetValue("l.shift") == 0 => "shift",
                    "l01.table" => "ready",
                    "l02.test" => "ache",
                    "l03.plan" => "borrow",
                    "l04.select" => "signed",
                    "l05.choice" => "pulse",
                    "l07.warning" => "return",
                    "l08.answer" => "rescued",
                    "l08.names" => "bed",
                    "l09.record" => "full",
                    "l10.reply" => "joint",
                    "l.hall.ask" => "read",
                    "l.hall.choice" => "leave",
                    "l.depart.choice" => "injured",
                    _ => string.Empty
                };
                Require(string.IsNullOrEmpty(choiceId) ? run.Advance() : run.Choose(choiceId),
                    "Lin Yue route stopped at " + id);
            }

            Require(run.Current.id == "ending.leave" && run.GetValue("l.danqi") == 1
                    && run.GetValue("l.protocol") == 1,
                "Lin Yue rescue did not carry its outcomes to the ending.");
            Require(beforeLetters != null && beforeTestimony != null,
                "Lin Yue branch snapshots were not reached.");

            var otherApproach = StorySession.Restore(full, beforeLetters);
            Require(otherApproach.Choose("nickname"), "The family could not keep its letter.");
            AdvanceTo(otherApproach, "l05.cart");
            Require(otherApproach.CurrentText.Contains("信还在姐姐怀里"),
                "Keeping the letter did not change the door scene.");
            Require(otherApproach.Advance() && otherApproach.Choose("sister"),
                "The sister could not question the person behind the door.");
            AdvanceTo(otherApproach, "l08.road");
            Require(otherApproach.CurrentText.Contains("盘问的人已往许家"),
                "The guard seeing the sister had no follow-up.");

            var correctedTestimony = StorySession.Restore(full, beforeTestimony);
            Require(correctedTestimony.Choose("simple"), "The short testimony was unavailable.");
            AdvanceTo(correctedTestimony, "l.hall.ask");
            Require(correctedTestimony.GetValue("l.testimony") == 0
                    && correctedTestimony.GetValue("l.protocol") == 0
                    && !correctedTestimony.IsAvailable(correctedTestimony.Current.choices.First(choice => choice.id == "read")),
                "A corrected draft must not become permission to speak for Xu Cheng.");
            Require(correctedTestimony.Choose("ask") && correctedTestimony.Advance(),
                "Xu Cheng could not testify in her own words.");
            Require(correctedTestimony.Choose("holdLate") && correctedTestimony.Current.id == "l.hold.plan"
                    && correctedTestimony.Advance() && correctedTestimony.GetValue("l.protocol") == 1,
                "Late public agreement did not repair the missing stop protocol.");
        }

        private static void VerifyGuRoute(StoryGraph full)
        {
            var run = new StorySession(full, 42);
            AdvanceTo(run, "case.boundary");
            Require(run.Choose("village"), "Gu Heng route is unavailable.");
            for (var step = 0; step < 250 && run.Current.id != "ending.newRoad"; step++)
            {
                var id = run.Current.id;
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
                    "h05.assign" => "split",
                    "h05.stop" => "stop",
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
        }

        private static void VerifyShenRoute(StoryGraph full)
        {
            var run = new StorySession(full, 42);
            AdvanceTo(run, "case.boundary");
            Require(run.Choose("shen"), "Shen Yan route is unavailable.");
            StorySnapshot hallSnapshot = null;
            for (var step = 0; step < 200 && run.Current.id != "ending.seizeSelf"; step++)
            {
                var id = run.Current.id;
                if (id == "s.hall.choice") hallSnapshot = run.Snapshot();
                var choiceId = id switch
                {
                    "s02.first" => "left",
                    "s03.bells" => "fit",
                    "s04.choice" => "small",
                    "s05.reply" => "ask",
                    "s06.map" => "homes",
                    "s07.stop" => "now",
                    "s08.choice" => "split",
                    "s09.map" => "include",
                    "s.hall.choice" => "seize",
                    "e20.symbol" => "side",
                    "e20.choose" => "self",
                    "e20.bell" => "stop",
                    "e20.record" => "public",
                    _ => string.Empty
                };
                Require(string.IsNullOrEmpty(choiceId) ? run.Advance() : run.Choose(choiceId),
                    "Shen Yan route stopped at " + id);
            }

            Require(run.Current.id == "ending.seizeSelf" && run.GetValue("s.firstBed") == 1
                    && run.GetValue("s.zhouWords") == 1 && run.GetValue("s.newMap") == 1
                    && run.GetValue("e.seizeRecord") == 1,
                "Shen Yan route did not carry its evidence through self-sacrifice.");
            Require(hallSnapshot != null, "Shen Yan council snapshot is missing.");
            var hiddenRun = StorySession.Restore(full, hallSnapshot);
            Require(hiddenRun.Choose("hold") && hiddenRun.Advance()
                    && hiddenRun.Choose("hide") && hiddenRun.Advance()
                    && hiddenRun.Choose("sign") && hiddenRun.Advance()
                    && hiddenRun.Current.id == "ending.oldHidden"
                    && hiddenRun.GetValue("e.hiddenName") == 1,
                "The concealment ending was not an explicit player action.");
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
