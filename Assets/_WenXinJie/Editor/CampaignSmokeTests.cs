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
            Require(full.Definition.revision == 29 && demo.Definition.revision == 29,
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
            StorySnapshot councilEvidence = null;
            StorySnapshot beforeRescue = null;
            for (var step = 0; step < 150 && run.Current.id != "ending.leave"; step++)
            {
                var id = run.Current.id;
                if (id == "l03.plan") beforeLetters = run.Snapshot();
                if (id == "l09.record") beforeTestimony = run.Snapshot();
                if (id == "l.hall.infer") councilEvidence = run.Snapshot();
                if (id == "l07.warning") beforeRescue = run.Snapshot();
                if (id == "l.depart.choice")
                    Require(run.IsAvailable(run.Current.choices.First(choice => choice.id == "injured"))
                            && !run.IsAvailable(run.Current.choices.First(choice => choice.id == "ache")),
                        "The saved Dan Qi did not receive a place on the wagon.");
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
                    "l.hall.infer" => "separate",
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
            Require(run.CurrentText.Contains("段七与阿澈都下了山"),
                "The saved Dan Qi disappeared from the departure ending.");
            Require(beforeLetters != null && beforeTestimony != null && councilEvidence != null
                    && beforeRescue != null,
                "Lin Yue branch snapshots were not reached.");

            var retreat = StorySession.Restore(full, beforeRescue);
            Require(retreat.Choose("retreat"), "The agreed retreat was unavailable.");
            for (var step = 0; step < 130 && retreat.Current.id != "l.depart.choice"; step++)
            {
                var id = retreat.Current.id;
                var choiceId = id switch
                {
                    "l08.answer" => "inside",
                    "l09.record" => "full",
                    "l10.reply" => "joint",
                    "l.hall.ask" => "read",
                    "l.hall.infer" => "separate",
                    "l.hall.choice" => "leave",
                    _ => string.Empty
                };
                Require(!string.IsNullOrEmpty(choiceId) ? retreat.Choose(choiceId)
                    : retreat.Current.choices.Length > 0
                        ? retreat.Choose(retreat.Current.choices.First(retreat.IsAvailable).id)
                        : retreat.Advance(), "Retreat route stopped at " + id);
            }
            Require(retreat.Current.id == "l.depart.choice" && retreat.GetValue("l.danqi") == 0
                    && retreat.CurrentText.Contains("段七被留在内榻")
                    && retreat.IsAvailable(retreat.Current.choices.First(choice => choice.id == "ache"))
                    && !retreat.IsAvailable(retreat.Current.choices.First(choice => choice.id == "injured")),
                "The wagon still counted Dan Qi after leaving him inside.");
            Require(retreat.Choose("boyFirst") && retreat.CurrentText.Contains("段七仍在里榻")
                    && retreat.Advance() && retreat.CurrentText.Contains("仍在炉内")
                    && retreat.Advance() && retreat.CurrentText.Contains("段七仍在里榻"),
                "The retreat ending did not preserve Dan Qi's actual whereabouts.");

            var overreach = StorySession.Restore(full, councilEvidence);
            Require(overreach.Choose("overreach") && overreach.CurrentText.Contains("没证明谷口能接住冲力")
                    && overreach.GetValue("l.councilDelay") == 1
                    && overreach.Advance() && overreach.Current.id == "l.hall.infer"
                    && overreach.Choose("separate") && overreach.Advance()
                    && overreach.Current.id == "l.hall.choice",
                "Lin Yue council did not distinguish the witness from the village warning.");
            var delayedHold = StorySession.Restore(full, overreach.Snapshot());
            Require(delayedHold.Choose("hold") && delayedHold.CurrentText.Contains("顾衡当众保管钟槌"),
                "Council overreach did not change village control of the bell.");
            Require(overreach.Choose("leave") && overreach.CurrentText.Contains("药车等到守卫换班"),
                "Council overreach did not delay the departure convoy.");

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
            Require(correctedTestimony.Choose("ask") && correctedTestimony.Advance()
                    && correctedTestimony.Choose("separate") && correctedTestimony.Advance(),
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
            Require(publicHold.Choose("hold") && publicHold.Advance()
                    && publicHold.Current.id == "ending.oldPublic",
                "The village could not decline another test and keep the old array public.");
            Require(oldMap.Choose("repair") && oldMap.CurrentText.Contains("小试裂开的村符")
                    && oldMap.Advance() && oldMap.GetValue("g.repairOwed") == 0
                    && oldMap.IsAvailable(oldMap.Current.choices.First(choice => choice.id == "share")),
                "Repairing the charm did not reopen the village decision.");
        }

        private static void VerifyShenRoute(StoryGraph full)
        {
            var run = new StorySession(full, 42);
            AdvanceTo(run, "case.boundary");
            Require(run.Choose("shen"), "Shen Yan route is unavailable.");
            StorySnapshot hallSnapshot = null;
            StorySnapshot councilEvidence = null;
            StorySnapshot beforeZhou = null;
            StorySnapshot beforeStop = null;
            for (var step = 0; step < 200 && run.Current.id != "ending.seizeSelf"; step++)
            {
                var id = run.Current.id;
                if (id == "s.hall.choice") hallSnapshot = run.Snapshot();
                if (id == "s.hall.infer") councilEvidence = run.Snapshot();
                if (id == "s05.reply") beforeZhou = run.Snapshot();
                if (id == "s07.stop") beforeStop = run.Snapshot();
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
                    "s.hall.infer" => "source",
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
            Require(!run.CurrentText.Contains("谷口小试你晚了一息"),
                "An on-time stop was narrated as a late stop in the ending.");
            Require(hallSnapshot != null, "Shen Yan council snapshot is missing.");
            var forced = StorySession.Restore(full, hallSnapshot);
            Require(forced.Choose("seize"), "The seizure route was unavailable.");
            AdvanceTo(forced, "e20.choose");
            Require(forced.Choose("force") && forced.CurrentText.Contains("合上榻锁")
                    && forced.Advance() && forced.Current.id == "e21.question",
                "The forced seat had no visible action or answer from Xu Cheng.");
            Require(councilEvidence != null, "Shen Yan evidence inference was not reached.");
            var overclaim = StorySession.Restore(full, councilEvidence);
            Require(overclaim.Choose("tear") && overclaim.CurrentText.Contains("剩下的记录没写是谁撕的")
                    && overclaim.GetValue("s.councilOverclaim") == 1
                    && overclaim.Advance() && overclaim.Current.id == "s.hall.infer"
                    && overclaim.Choose("source") && overclaim.Advance()
                    && overclaim.Current.id == "s.hall.choice",
                "The Shen Yan council did not separate the missing page from signed orders.");
            var publicAfterOverclaim = StorySession.Restore(full, overclaim.Snapshot());
            Require(publicAfterOverclaim.Choose("hold") && publicAfterOverclaim.Advance()
                    && publicAfterOverclaim.Choose("tell")
                    && publicAfterOverclaim.CurrentText.Contains("周望自己拿着初试原簿"),
                "The unsupported accusation did not change who presented the original book.");
            Require(overclaim.Choose("seize") && overclaim.CurrentText.Contains("周望不借初试原簿"),
                "The unsupported accusation was still treated as warrant for seizure.");
            var hiddenRun = StorySession.Restore(full, hallSnapshot);
            Require(hiddenRun.Choose("hold") && hiddenRun.Advance()
                    && hiddenRun.Choose("hide") && hiddenRun.Advance()
                    && hiddenRun.Choose("sign") && hiddenRun.Advance()
                    && hiddenRun.Current.id == "ending.oldHidden"
                    && hiddenRun.GetValue("e.hiddenName") == 1,
                "The concealment ending was not an explicit player action.");

            Require(beforeZhou != null && beforeStop != null, "Shen Yan branch snapshots are missing.");
            var speakingForZhou = StorySession.Restore(full, beforeZhou);
            Require(speakingForZhou.Choose("apologise") && speakingForZhou.Advance()
                    && speakingForZhou.Current.id == "s05.reply"
                    && speakingForZhou.Choose("ask") && speakingForZhou.Current.id == "s05.words",
                "Zhou Wang's words must be requested from him, not supplied by the player.");

            var lateStop = StorySession.Restore(full, beforeStop);
            Require(lateStop.Choose("wait"), "Waiting past the stop call was unavailable.");
            AdvanceTo(lateStop, "s08.choice");
            Require(lateStop.GetValue("s.bellTrusted") == 0
                    && !lateStop.IsAvailable(lateStop.Current.choices.First(choice => choice.id == "split")),
                "Ignoring the stop call did not remove control of the next test.");
            var publicHoldAfterStop = StorySession.Restore(full, lateStop.Snapshot());
            Require(publicHoldAfterStop.Choose("hold") && publicHoldAfterStop.Advance()
                    && publicHoldAfterStop.Current.id == "ending.oldPublic",
                "The player could not stop further testing after a late bell.");
            Require(lateStop.Choose("account") && lateStop.CurrentText.Contains("钟响后你仍等了一息")
                    && lateStop.Advance() && lateStop.GetValue("s.bellTrusted") == 1
                    && lateStop.Choose("split"),
                "Publicly recording the late stop did not return the bell to shared control.");
            AdvanceTo(lateStop, "s.hall");
            Require(lateStop.CurrentText.Contains("你在谷口迟停"),
                "The late stop disappeared from the council record.");
            var lateForce = StorySession.Restore(full, lateStop.Snapshot());
            AdvanceTo(lateForce, "e20.choose");
            Require(lateForce.Choose("force") && lateForce.Advance()
                    && lateForce.CurrentText.Contains("谷口那回有人喊停"),
                "Xu Cheng did not connect the forced seat to the earlier ignored stop call.");
            AdvanceTo(lateStop, "e20.self");
            Require(lateStop.CurrentText.Contains("谷口那次你晚了一息"),
                "The late stop did not inform the later seat decision.");
            AdvanceTo(lateStop, "e20.bell");
            Require(lateStop.Choose("endure") && lateStop.GetValue("e.extraInjury") == 1,
                "Overriding the third stop call did not cause extra injury.");
            AdvanceTo(lateStop, "ending.seizeSelf");
            Require(lateStop.CurrentText.Contains("两次迟疑和加重的伤情"),
                "The final record forgot both ignored stop calls and the added injury.");
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
