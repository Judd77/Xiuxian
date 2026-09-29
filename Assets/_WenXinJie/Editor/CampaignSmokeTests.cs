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
        private const string CampaignPath = "Assets/_WenXinJie/Data/Campaign.json";

        [MenuItem("WenXinJie/Validation/Run Campaign Smoke Tests")]
        public static void Run()
        {
            var graph = LoadGraph();
            Require(graph.Nodes.Count() == 220, "Unexpected node count.");
            VerifyCaseSlice(graph);
            VerifyNewStory(graph);
            VerifyRoute(graph, "investigate", "copyPage", "visit", "confide", "shared", "c01.shared", true);
            VerifyRoute(graph, "investigate", "takePage", "cultivate", "steadyBreath", "alone", "c01.alone", false);
            VerifyRoute(graph, "visit", "question", "cultivate", "steadyBreath", "guarded", "c01.guarded", false);
            VerifyRoute(graph, "cultivate", "followEcho", "cultivate", "steadyBreath", "uneasy", "c01.uneasy", false);
            VerifySaveRoundTrip(graph);
            VerifySeededOutcomes();
            VerifySecondAct(graph);
            VerifyInvalidContentRejected();
            VerifyAssets(graph);
            Debug.Log(
                "Campaign smoke tests PASS: 220-node graph, evidence-dependent openings, four investigation routes, three-party hearing, endings, save round-trip and assets.");
        }

        private static StoryGraph LoadGraph()
        {
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(CampaignPath);
            Require(asset != null, "Campaign JSON is missing.");
            return new(JsonUtility.FromJson<StoryDefinition>(asset.text));
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
            var allianceChoice = session.Current.choices.Single(choice => choice.id == "alliance");
            Require(session.IsAvailable(allianceChoice) == alliance, "Day-two choice did not inherit first-day state.");
        }

        private static void ReachFirstDayHub(StorySession session)
        {
            ReachLegacyGate(session);
            Require(session.Advance() && session.Advance() && session.Advance(), "Could not reach the opening response.");
            Require(session.Choose("watch"), "Opening response was rejected.");
            Require(session.Advance() && session.Current.id == "c01.hub", "Could not reach first-day hub.");
        }

        private static void ReachLegacyGate(StorySession session)
        {
            ReachCaseMeet(session, "roster", "borrowed", "ink", "sense", "proceed");
            Require(session.Choose("number") && session.Advance(), "Could not reach the investigation choice.");
            Require(session.Choose("lin") && session.Advance()
                    && session.Choose("clerk") && session.Advance() && session.Choose("verify")
                    && session.Advance() && session.Choose("witness") && session.Advance(),
                "Could not finish the Lin Yue investigation.");
            Require(session.Choose("legacy") && session.Current.id == "c01.gate", "Legacy material was not reachable.");
        }

        private static void VerifyCaseSlice(StoryGraph graph)
        {
            var mistaken = new StorySession(graph);
            ReachCaseMeet(mistaken, "post", "person", "ink", "challenge", "sample");
            Require(mistaken.GetValue("case.guess") == 1 && mistaken.GetValue("case.learned") == 1
                    && mistaken.GetValue("case.sealLost") == 1 && mistaken.GetValue("case.verified") == 1
                    && mistaken.GetValue("case.controlSample") == 1,
                "Opening evidence, training or early confrontation was not retained.");
            var restored = StorySession.Restore(graph, mistaken.Snapshot());
            Require(restored.Current.id == "case.meet" && restored.GetValue("case.sealLost") == 1,
                "Case consequences were not saved.");
            Require(restored.Choose("inside") && restored.Advance() && restored.Choose("public")
                    && restored.Advance() && restored.Choose("copy") && restored.Advance()
                    && restored.Choose("scope") && restored.Advance() && restored.Advance()
                    && restored.Choose("finish") && restored.Current.terminal,
                "Mistaken conclusion did not reach a consequential ending.");
            Require(restored.GetValue("case.action") == 3 && restored.GetValue("risk.attention") == 3,
                "Public disclosure lost its consequences.");

            var careful = new StorySession(graph);
            ReachCaseMeet(careful, "roster", "error", "ink", "sense", "proceed");
            Require(careful.Choose("uncertain") && careful.Advance() && careful.Choose("gu")
                    && careful.Advance()
                    && careful.Choose("charm") && careful.Advance() && careful.Choose("prepare")
                    && careful.Advance() && careful.Advance() && careful.Choose("finish")
                    && careful.Current.terminal,
                "Cautious route could not finish after verifying its initial error.");
            Require(careful.GetValue("case.guess") == 3 && careful.GetValue("case.conclusion") == 3
                                                        && careful.GetValue("case.action") == 2 && careful.GetValue("case.sealLost") == 0,
                "Cautious route state was incorrect.");
        }

        private static void VerifyNewStory(StoryGraph graph)
        {
            var session = new StorySession(graph);
            ReachCaseMeet(session, "clerk", "borrowed", "ink", "sense", "sample");
            Require(session.Choose("number") && session.Advance() && session.Choose("lin")
                    && session.Advance() && session.Choose("ache") && session.Advance()
                    && session.Choose("verify") && session.Advance() && session.Choose("witness")
                    && session.Advance() && session.Choose("continue"),
                "The rescue route did not enter the rift investigation.");
            Require(session.Choose("village") && session.Advance() && session.Choose("guardFurnace")
                    && session.Advance() && session.Choose("ache") && session.Advance()
                    && session.Advance() && session.Choose("pressure") && session.Choose("consent")
                    && session.Advance() && session.Choose("proceed") && session.Choose("solo") && session.Advance()
                    && session.Choose("replace") && session.Choose("prepared") && session.Current.terminal,
                "The replacement ending was not playable.");
            Require(session.GetValue("case.ache") == 1 && session.GetValue("case.arrayMeasured") == 1,
                "Investigation evidence was not retained for the ending.");

            var drafts = new StorySession(graph);
            ReachCaseMeet(drafts, "post", "borrowed", "ink", "sense", "sample");
            Require(drafts.Choose("number") && drafts.Advance() && drafts.Choose("shen")
                    && drafts.Advance() && drafts.Choose("paper") && drafts.Advance()
                    && drafts.Choose("signature") && drafts.Advance() && drafts.Choose("village")
                    && drafts.Advance() && drafts.Advance() && drafts.Choose("continue")
                    && drafts.Choose("draft") && drafts.Advance() && drafts.Choose("guardVillage")
                    && drafts.Advance() && drafts.Choose("shenRecord") && drafts.Advance()
                    && drafts.Choose("proceed") && drafts.Choose("solo") && drafts.Advance()
                    && drafts.Choose("hold") && drafts.Choose("open") && drafts.Current.terminal,
                "Shen Yan's investigation did not reach its evidence-specific hearing and ending.");
            Require(drafts.GetValue("case.shenAccount") == 1 && drafts.GetValue("case.publicRecord") == 1,
                "The signed draft did not survive to the ending.");

            var family = new StorySession(graph);
            ReachCaseMeet(family, "roster", "borrowed", "ink", "sense", "sample");
            Require(family.Choose("number") && family.Advance() && family.Choose("lin")
                    && family.Advance() && family.Choose("bowl") && family.Advance()
                    && family.Choose("dose") && family.Advance() && family.Choose("family")
                    && family.Choose("consent") && family.Advance() && family.Advance()
                    && family.Choose("continue") && family.Choose("village") && family.Advance()
                    && family.Choose("guardFurnace") && family.Advance()
                    && family.Choose("records") && family.Advance() && family.Advance()
                    && family.Choose("names") && family.Choose("matched")
                    && family.Advance() && family.Choose("pressure") && family.Choose("consent")
                    && family.Advance() && family.Choose("draft") && family.Choose("archive")
                    && family.Advance() && family.Choose("proceed") && family.Choose("joint")
                    && family.Advance()
                    && family.Choose("leave") && family.Choose("family") && family.Current.terminal,
                "Family-verified rescue did not reach its distinct ending.");
            Require(family.GetValue("case.sedative") == 1 && family.GetValue("case.familyConsent") == 1
                    && family.GetValue("case.jointProtocol") == 1,
                "The drug check, family consent or joint protocol was lost.");

            var measured = new StorySession(graph);
            ReachCaseMeet(measured, "post", "borrowed", "ink", "sense", "sample");
            Require(measured.Choose("number") && measured.Advance() && measured.Choose("shen")
                    && measured.Advance() && measured.Choose("paper") && measured.Advance()
                    && measured.Choose("signature") && measured.Advance() && measured.Choose("pause")
                    && measured.Choose("witness") && measured.Advance() && measured.Advance()
                    && measured.Choose("continue") && measured.Choose("village") && measured.Advance()
                    && measured.Choose("guardVillage") && measured.Advance()
                    && measured.Choose("records") && measured.Advance() && measured.Advance()
                    && measured.Choose("names") && measured.Choose("matched") && measured.Advance()
                    && measured.Choose("pressure") && measured.Choose("consent") && measured.Advance()
                    && measured.Choose("proceed") && measured.Choose("joint") && measured.Advance()
                    && measured.Choose("replace") && measured.Choose("timed") && measured.Current.terminal,
                "Two-sided return measurement did not affect replacement execution.");
            Require(measured.GetValue("case.returnMeasured") == 1 && measured.GetValue("case.publicRecord") == 1,
                "The witnessed measurement was lost.");
        }

        private static void ReachCaseMeet(StorySession session, string entry, string guess,
            string practice, string method, string control)
        {
            Require(session.Current.id == "case.intro" && session.Choose(entry) && session.Advance()
                    && session.Choose(guess) && session.Advance() && session.Choose(practice)
                    && session.Advance() && session.Choose(method),
                "Could not choose the opening evidence and learn辨气.");
            if (method == "challenge")
            {
                Require(session.Advance(), "Early challenge did not lead to the ash sample.");
            }

            Require(session.Advance() && session.Current.id == "case.control"
                    && session.Choose(control) && session.Advance() && session.Current.id == "case.meet",
                "Could not verify the ash source and compare evidence.");
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
            var snapshot = session.Snapshot();
            var restored = StorySession.Restore(graph, snapshot);
            Require(restored.Current.id == session.Current.id, "Saved cursor was not restored.");
            Require(restored.GetValue("clue.register") == 1, "Restore lost state.");

            var folder = Path.Combine(Application.temporaryCachePath, "WenXinJieCampaignTests", Guid.NewGuid().ToString("N"));
            var path = Path.Combine(folder, "save.json");
            var store = new CampaignSaveStore();
            store.Save(path, session);
            var disk = store.Load(path, graph, out var recovered);
            Require(!recovered && disk.GetValue("clue.register") == 1, "Disk round-trip failed.");
            store.Save(path, session);
            File.WriteAllText(path, "corrupt");
            var backup = store.Load(path, graph, out recovered);
            Require(recovered && backup.Current.id == session.Current.id, "Backup recovery failed.");
        }

        private static void VerifySeededOutcomes()
        {
            var graph = new StoryGraph(new()
            {
                id = "seeded-check",
                revision = 1,
                start = "draw",
                variables = Array.Empty<VariableDefinition>(),
                nodes = new[]
                {
                    new StoryNode
                    {
                        id = "draw",
                        text = "Draw an encounter.",
                        background = "gate",
                        choices = new[]
                        {
                            new StoryChoice
                            {
                                id = "explore",
                                text = "Explore",
                                outcomes = new[]
                                {
                                    new StoryOutcome { target = "quiet", weight = 3 },
                                    new StoryOutcome { target = "danger", weight = 1 }
                                }
                            }
                        }
                    },
                    new StoryNode { id = "quiet", text = "Quiet road.", background = "gate", next = "draw" },
                    new StoryNode { id = "danger", text = "Patrol ahead.", background = "gate", next = "draw" }
                }
            });

            var original = new StorySession(graph, 173);
            var sameSeed = new StorySession(graph, 173);
            Require(original.Choose("explore") && sameSeed.Choose("explore"), "Seeded choice failed.");
            Require(original.Current.id == sameSeed.Current.id, "Identical seeds produced different outcomes.");

            var restored = StorySession.Restore(graph, original.Snapshot());
            Require(original.Advance() && restored.Advance(), "Could not return to the encounter draw.");
            Require(original.Choose("explore") && restored.Choose("explore"), "Restored encounter choice failed.");
            Require(original.Current.id == restored.Current.id, "Loading a save rerolled the next encounter.");
            Require(original.Snapshot().randomState == restored.Snapshot().randomState,
                "Restored random state diverged after an identical action.");
        }

        private static void VerifySecondAct(StoryGraph graph)
        {
            var allied = VerifySecondActRoute(graph, "rescue", "excel", "findShen", "enter", "witness", "shen", "protect");
            var exposed = VerifySecondActRoute(graph, "ledger", "conceal", "traceList", "escort", "records", "linYue", "signal");
            VerifyThirdActRoute(allied, true, "c10.preserve", new[]
            {
                "followGu", "negotiate", "measure", "studySeal", "counter", "askGu", "visitGu", "warn",
                "holdArray", "preserve"
            });
            VerifyThirdActRoute(exposed, false, "c10.publish", new[]
            {
                "auditGu", "steal", "intercept", "compare", "infiltrate", "goIn", "freeLin", "train",
                "openPath", "publish"
            });
        }

        private static StorySession VerifySecondActRoute(StoryGraph graph, string dayTwo, string exam,
            string dayThree, string dayFour, string evening, string dayFive, string finale)
        {
            var session = new StorySession(graph);
            ReachFirstDayHub(session);
            PerformAction(session, "investigate", "copyPage");
            PerformAction(session, "visit", "confide");
            Require(session.Choose("night") && session.Choose("shared"), "Could not reach alliance outcome.");
            while (session.Current.id != "c02.decision")
            {
                Require(session.Advance(), "Could not reach day-two decision.");
            }

            Require(session.Choose("alliance") && session.Advance(), "Could not reach day-two evening.");
            Require(session.Current.id == "c02.evening" && session.GetValue("actions") == 1,
                "Day-two lead did not consume an action.");
            Require(session.GetValue("clue.ledger") == 0,
                "A partial lead must not count as the complete ledger.");
            Require(session.Choose(dayTwo) && session.Advance(), "Could not reach day-three exam.");
            Require(session.Current.id == "c03.dawn" && session.GetValue("day") == 3, "Day three is incorrect.");
            Require(session.Choose(exam) && session.Advance() && session.Choose(dayThree) && session.Advance(),
                "Could not finish day three.");
            Require(session.Current.id == "c04.dawn" && session.GetValue("day") == 4, "Day four is incorrect.");
            Require(session.Choose(dayFour) && session.Advance(), "Could not reach day-four evening.");
            var witness = session.Current.choices.Single(choice => choice.id == "witness");
            Require(session.IsAvailable(witness) == (session.GetValue("mark.rescued") == 1),
                "Witness choice ignored the rescue outcome.");
            Require(session.Choose(evening) && session.Advance(), "Could not finish day four.");
            Require(session.Current.id == "c05.dawn" && session.GetValue("day") == 5, "Day five is incorrect.");
            Require(session.Choose(dayFive) && session.Advance() && session.Choose(finale),
                "Could not finish day five.");
            Require(session.GetValue("actions") == 0, "Day five did not consume both actions.");
            return session;
        }

        private static void VerifyThirdActRoute(StorySession session, bool trustsGu, string terminal,
            string[] choices)
        {
            Require(choices.Length == 10, "Third-act route needs two choices per day.");
            for (var day = 6; day <= 10; day++)
            {
                AdvanceToChoices(session);
                Require(session.GetValue("day") == day && session.GetValue("actions") == 2,
                    "New day did not refresh the two-action budget: " + day);
                if (day == 9)
                {
                    var visitGu = session.Current.choices.Single(choice => choice.id == "visitGu");
                    Require(session.IsAvailable(visitGu) == trustsGu, "Gu's day-nine route ignored prior trust.");
                }

                Require(session.Choose(choices[(day - 6) * 2]), "First action was blocked on day " + day);
                AdvanceToChoices(session);
                Require(session.GetValue("actions") == 1, "First action was not consumed on day " + day);
                if (day == 8)
                {
                    var askGu = session.Current.choices.Single(choice => choice.id == "askGu");
                    Require(session.IsAvailable(askGu) == trustsGu, "Gu's observatory ignored prior trust.");
                }

                Require(session.Choose(choices[(day - 6) * 2 + 1]), "Evening action was blocked on day " + day);
                Require(session.GetValue("actions") == 0, "Evening action was not consumed on day " + day);
            }

            Require(session.Current.id == terminal && session.Current.terminal,
                "Third act did not reach its expected chapter boundary.");
            Require(session.GetValue("clue.array") == 1, "The substitute-array measurement was lost.");
            Require(session.GetValue("mark.village") == (trustsGu ? 1 : 0), "Trial outcome ignored the defended side.");
            Require(session.GetValue("mark.evacuated") == (trustsGu ? 0 : 1),
                "Trial outcome ignored the evacuated side.");
            Require(session.GetValue("mark.arrayKey") == (trustsGu ? 1 : 0),
                "The array key ignored Gu's day-nine route.");
        }

        private static void AdvanceToChoices(StorySession session)
        {
            while (session.Current.choices.Length == 0)
            {
                Require(session.Advance(), "Route ended before the next decision.");
            }
        }

        private static void VerifyInvalidContentRejected()
        {
            var rejected = false;
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
                _ = new StoryGraph(new()
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
            var catalog = AssetDatabase.LoadAssetAtPath<StoryAssetCatalog>(
                "Assets/_WenXinJie/Data/StoryAssets.asset");
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
