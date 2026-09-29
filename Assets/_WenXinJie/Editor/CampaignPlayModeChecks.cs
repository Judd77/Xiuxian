using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using WenXinJie.Presentation;
using WenXinJie.UI;
using Object = UnityEngine.Object;

namespace WenXinJie.Editor
{
    public static class CampaignPlayModeChecks
    {
        private static Task _runningTask = Task.CompletedTask;
        public static bool IsRunning { get; private set; }
        public static string LastReport { get; private set; } = "Not run";

        [MenuItem("WenXinJie/Validation/Run Campaign Play Mode Checks")]
        public static void Run()
        {
            Require(EditorApplication.isPlaying, "Enter Play Mode first.");
            Require(_runningTask.IsCompleted, "Checks are already running.");
            var application = Object.FindAnyObjectByType<StoryApplication>();
            Require(application != null, "StoryApplication is missing.");
            var reader = application.GetComponent<DialogueReader>();
            Require(reader != null, "DialogueReader is missing.");
            _runningTask = RunAsync(application, reader);
        }

        private static async Task RunAsync(StoryApplication application, DialogueReader reader)
        {
            var previousDirectory = application.TestSaveDirectory;
            var previousRunInBackground = Application.runInBackground;
            application.TestSaveDirectory = Path.Combine(Application.temporaryCachePath,
                "WenXinJieCampaignPlayTests", Guid.NewGuid().ToString("N"));
            Application.runInBackground = true;
            IsRunning = true;
            LastReport = "Running";

            try
            {
                Require(application.Session == null, "Campaign session should not start behind the main menu.");
                VerifyActive("MainMenu", true);
                VerifyTextMeshProUsage();
                await Capture("Docs/Validation/campaign-main-menu-1080.png");
                await Click("MainMenuSettings");
                VerifyActive("SettingsModal", true);
                VerifyModalIsFrontmost("SettingsModal");
                await VerifyDisplaySettings();
                await Capture("Docs/Validation/campaign-settings-modal-1080.png");
                await Click("SettingsClose");
                await Click("MainMenuCredits");
                VerifyActive("CreditsModal", true);
                VerifyModalIsFrontmost("CreditsModal");
                VerifyTextContains("CreditsText", "OpenAI 图像生成工具");
                VerifyTextContains("CreditsText", "髡残《春景图》");
                VerifyTextContains("CreditsText", "立即下架并替换");
                await Capture("Docs/Validation/campaign-credits-modal-1080.png");
                await Click("CreditsClose");
                await Click("StartGame");
                await VerifyCaseSlice(application, reader);
                Require(application.Session.Current.id == "c01.gate", "Campaign did not start at the gate.");
                VerifyTextContains("Status", "线索 0/6");
                await Click("ClueJournal");
                VerifyActive("ClueJournalOverlay", true);
                VerifyText("ClueJournalCount", "0 / 6");
                await Click("ClueJournalClose");
                reader.Complete();
                application.AdvanceStory();
                Require(application.Session.Current.id == "c01.arrival", "Arrival advance failed.");
                VerifyPicture("Portrait", "LinYue_Premium", true);
                VerifyPortraitFraming();
                reader.Complete();
                await Capture("Docs/Validation/campaign-lin-yue-1080.png");
                application.AdvanceStory();
                Require(application.Session.Current.id == "c01.warning", "Warning advance failed.");
                reader.Complete();
                application.AdvanceStory();
                Require(application.Session.Current.id == "c01.response", "Opening response was not reached.");
                reader.Complete();
                application.SelectChoice("watch");
                Require(application.Session.Current.id == "c01.response.watch", "Opening response choice failed.");
                reader.Complete();
                application.AdvanceStory();
                Require(application.Session.Current.id == "c01.hub", "Could not reach action hub.");
                VerifyChoiceAvailability(3);
                reader.Complete();
                await Capture("Docs/Validation/campaign-choices-1080.png");

                await Click("Choice0");
                Require(application.Session.Current.id == "c01.register", "Actual choice click did not enter register node.");
                VerifyPicture("Portrait", string.Empty, false);
                reader.Complete();
                application.AdvanceStory();
                reader.Complete();
                application.AdvanceStory();
                Require(application.Session.Current.id == "c01.register.method", "Register method choice was not reached.");
                reader.Complete();
                application.SelectChoice("copyPage");
                Require(application.Session.GetValue("clue.register") == 1, "Register clue was not applied.");
                VerifyText("Notice", "新线索已收录：外门名册抄页");
                reader.Complete();
                application.AdvanceStory();
                VerifyText("Notice", string.Empty);
                reader.Complete();
                application.SelectChoice("visit");
                Require(application.Session.Current.id == "c01.meeting", "Visit choice failed.");
                VerifyPicture("Background", "PineRidge", true);
                VerifyPicture("Portrait", "LinYue_Premium", true);
                reader.Complete();
                application.AdvanceStory();
                reader.Complete();
                application.AdvanceStory();
                Require(application.Session.Current.id == "c01.meeting.answer", "Meeting answer choice was not reached.");
                reader.Complete();
                application.SelectChoice("confide");
                Require(application.Session.GetValue("clue.jade") == 1, "Jade clue was not applied.");
                VerifyText("Notice", "新线索已收录：青玉扣");
                await Click("ClueJournal");
                VerifyText("ClueJournalCount", "2 / 6");
                VerifyTextContains("Status", "线索 2/6");
                await Click("ClueEntry1");
                VerifyText("ClueDetailTitle", "青玉扣");
                VerifyText("ClueDetailStatus", "物证 · 松风岭 · 已收录");
                await Click("ClueJournalClose");
                reader.Complete();
                application.AdvanceStory();
                reader.Complete();
                application.SelectChoice("night");
                Require(application.Session.Current.id == "c01.night", "Night transition failed.");
                VerifyPicture("Background", "PineRidge_Premium_Night", true);
                reader.Complete();
                application.SelectChoice("shared");
                Require(application.Session.Current.id == "c01.shared", "Shared-secret outcome failed.");
                reader.Complete();
                application.AdvanceStory();
                reader.Complete();
                application.AdvanceStory();
                Require(application.Session.Current.id == "c01.shared.vow", "Alliance vow was not reached.");
                Require(application.Session.GetValue("route.alliance") == 1, "Alliance flag was not set.");

                application.SaveGame();
                Require(File.Exists(application.SavePath), "Play-mode save was not written.");
                await Click("MainMenuButton");
                VerifyActive("MainMenu", true);
                VerifyButton("ContinueGame", true);
                VerifyButton("LoadGame", true);
                await Click("ContinueGame");
                Require(application.Session.Current.id == "c01.shared.vow", "Continue did not resume the active session.");
                reader.Complete();
                application.AdvanceStory();
                Require(application.Session.GetValue("day") == 2, "Could not advance to day two.");
                await Click("MainMenuButton");
                await Click("LoadGame");
                Require(application.Session.Current.id == "c01.shared.vow", "Load did not restore story cursor.");
                Require(application.Session.GetValue("route.alliance") == 1, "Load did not restore world state.");
                reader.Complete();
                await Capture("Docs/Validation/campaign-shared-secret-1080.png");
                await VerifyAdditionalRoutes(application, reader);
                await VerifyGuInspection(application, reader);
                await VerifyShenSurvey(application, reader);
                await VerifyPublicSealAudit(application, reader);
                LastReport = "Campaign Play Mode checks PASS: case inference, spell, premature confrontation, "
                             + "case save/load, main menu start/continue/load, actual choice click, "
                             + "Gu Heng's supervised inspection, Shen Yan's field survey, public seal audit, four first-night routes, day-seven continuation and new clues, background fades, "
                             + "isolated save/load, and 1920x1080 capture.";
                Debug.Log(LastReport);
            }
            catch (Exception exception)
            {
                LastReport = "FAIL: " + exception;
                Debug.LogException(exception);
            }
            finally
            {
                application.TestSaveDirectory = previousDirectory;
                Application.runInBackground = previousRunInBackground;
                Directory.CreateDirectory("Docs/Validation");
                await File.WriteAllTextAsync("Docs/Validation/campaign-playmode-checks.txt", LastReport);
                IsRunning = false;
            }
        }

        private static async Task VerifyCaseSlice(StoryApplication application, DialogueReader reader)
        {
            Require(application.Session.Current.id == "case.intro", "New game did not start at the case.");
            VerifyTextContains("Status", "辨气 未习得");
            await Click("ClueJournal");
            VerifyText("ClueJournalCount", "2 / 27");
            await Click("ClueJournalClose");
            Choose(application, reader, "post");
            Advance(application, reader);
            Choose(application, reader, "person");
            Advance(application, reader);
            Choose(application, reader, "ink");
            Advance(application, reader);
            Require(application.Session.GetValue("case.learned") == 1, "The spell was not learned.");
            VerifyTextContains("Status", "辨气 已习得");
            Choose(application, reader, "challenge");
            Require(application.Session.GetValue("case.sealLost") == 1,
                "Premature confrontation did not seal the receipt.");
            Advance(application, reader);
            Advance(application, reader);
            Choose(application, reader, "sample");
            Advance(application, reader);
            Require(application.Session.GetValue("case.verified") == 1,
                "Spell did not verify living breath after the receipt was sealed.");
            await Click("ClueJournal");
            VerifyText("ClueJournalCount", "7 / 27");
            await Click("ClueEntry4");
            VerifyText("ClueDetailTitle", "炉灰内侧活息");
            await Click("ClueJournalClose");
            application.SaveGame();
            Require(File.Exists(application.SavePath), "Case save was not written.");
            await Click("MainMenuButton");
            await Click("LoadGame");
            Require(application.Session.Current.id == "case.meet"
                    && application.Session.GetValue("case.sealLost") == 1
                    && application.Session.GetValue("case.verified") == 1,
                "Case save did not restore evidence and consequence.");
            Choose(application, reader, "inside");
            Advance(application, reader);
            Choose(application, reader, "lin");
            Advance(application, reader);
            Choose(application, reader, "cart");
            Advance(application, reader);
            Choose(application, reader, "verify");
            Advance(application, reader);
            Choose(application, reader, "window");
            VerifyChoiceAvailability(3);
            reader.Complete();
            await Capture("Docs/Validation/lin-rescue-window-1080.png");
            Choose(application, reader, "slit");
            Advance(application, reader);
            Advance(application, reader);
            Require(application.Session.Current.id == "case.boundary"
                    && application.Session.GetValue("case.action") == 1
                    && application.Session.GetValue("case.sideDoorMeasured") == 1,
                "Confrontation did not change the next action.");
            reader.Complete();
            await Capture("Docs/Validation/case-boundary-1080.png");
            Choose(application, reader, "legacy");
        }

        private static async Task Click(string name)
        {
            var target = GameObject.Find(name);
            Require(target != null && target.activeInHierarchy, "Visible UI target is missing: " + name);
            var pointer = new PointerEventData(EventSystem.current)
            {
                button = PointerEventData.InputButton.Left
            };
            ExecuteEvents.Execute(target, pointer, ExecuteEvents.pointerClickHandler);
            await WaitForRender();
        }

        private static async Task VerifyAdditionalRoutes(StoryApplication application, DialogueReader reader)
        {
            ReachActionHub(application, reader);
            Choose(application, reader, "investigate");
            await VerifyBackgroundTransition(reader, "SteleCorridor", "Docs/Validation/campaign-stele-1080.png");
            FinishAction(application, reader, "copyPage");
            PerformAction(application, reader, "cultivate", "steadyBreath");
            Choose(application, reader, "night");
            Choose(application, reader, "alone");
            Advance(application, reader);
            Require(application.Session.Current.id == "c01.alone.search", "Alone route did not reach the quarters.");
            await VerifyBackgroundTransition(reader, "DiscipleQuartersNight",
                "Docs/Validation/campaign-quarters-night-1080.png");

            ReachActionHub(application, reader);
            PerformAction(application, reader, "visit", "question");
            PerformAction(application, reader, "cultivate", "steadyBreath");
            Choose(application, reader, "night");
            Choose(application, reader, "guarded");
            Require(application.Session.Current.id == "c01.guarded", "Guarded route was not reached.");
            Advance(application, reader);
            Require(application.Session.Current.id == "c01.guarded.escape", "Guarded route did not continue.");

            ReachActionHub(application, reader);
            Choose(application, reader, "cultivate");
            Advance(application, reader);
            Advance(application, reader);
            Choose(application, reader, "followEcho");
            VerifyText("Notice", "新线索已收录：山腹共鸣");
            VerifyTextContains("Status", "线索 1/6");
            await Click("ClueJournal");
            VerifyText("ClueJournalCount", "1 / 6");
            await Click("ClueEntry2");
            VerifyText("ClueDetailTitle", "山腹共鸣");
            await Click("ClueJournalClose");
            Advance(application, reader);
            Require(application.Session.Current.id == "c01.hub", "Training did not return to the hub.");
            PerformAction(application, reader, "cultivate", "steadyBreath");
            Choose(application, reader, "night");
            Choose(application, reader, "uneasy");
            Require(application.Session.Current.id == "c01.uneasy", "Uneasy route was not reached.");
            Advance(application, reader);
            Advance(application, reader);
            Advance(application, reader);
            Advance(application, reader);
            Choose(application, reader, "resonanceLead");
            Require(application.Session.Current.id == "c02.resonance", "Resonance lead was not reached.");
            await VerifyBackgroundTransition(reader, "MountainHeartGate",
                "Docs/Validation/campaign-mountain-heart-1080.png");
            Advance(application, reader);
            Require(application.Session.Current.id == "c02.evening", "Day-two evening was not reached.");
            Choose(application, reader, "rescue");
            Require(application.Session.GetValue("mark.rescued") == 1, "The witness was not rescued.");
            Advance(application, reader);
            Choose(application, reader, "conceal");
            Advance(application, reader);
            Choose(application, reader, "traceList");
            VerifyText("Notice", "新线索已收录：药炉账册");
            await Click("ClueJournal");
            VerifyText("ClueJournalCount", "2 / 6");
            await Click("ClueEntry3");
            VerifyText("ClueDetailTitle", "药炉账册");
            await Click("ClueJournalClose");
            Advance(application, reader);
            Choose(application, reader, "escort");
            Advance(application, reader);
            Choose(application, reader, "witness");
            VerifyText("Notice", "新线索已收录：阿澈的证词");
            Advance(application, reader);
            Choose(application, reader, "shen");
            Advance(application, reader);
            Choose(application, reader, "protect");
            Require(application.Session.Current.id == "c05.protect"
                    && application.Session.GetValue("day") == 5
                    && application.Session.GetValue("actions") == 0,
                "The second act did not finish at the day-five chapter boundary.");
            Advance(application, reader);
            Advance(application, reader);
            Require(application.Session.Current.id == "c06.dawn"
                    && application.Session.GetValue("day") == 6,
                "The third act did not start after day five.");
            Choose(application, reader, "followGu");
            Advance(application, reader);
            Choose(application, reader, "steal");
            VerifyText("Notice", "新线索已收录：替阵测绘");
            await Click("ClueJournal");
            VerifyText("ClueJournalCount", "4 / 6");
            await Click("ClueEntry5");
            VerifyText("ClueDetailTitle", "替阵测绘");
            await Click("ClueJournalClose");
            Advance(application, reader);
            Require(application.Session.Current.id == "c07.dawn"
                    && application.Session.GetValue("day") == 7,
                "Day seven was not reached from the third-act opening.");
        }

        private static async Task VerifyGuInspection(StoryApplication application, DialogueReader reader)
        {
            application.StartNewGame();
            Choose(application, reader, "roster");
            Advance(application, reader);
            Choose(application, reader, "borrowed");
            Advance(application, reader);
            Choose(application, reader, "ink");
            Advance(application, reader);
            Choose(application, reader, "sense");
            Advance(application, reader);
            Choose(application, reader, "sample");
            Advance(application, reader);
            Choose(application, reader, "uncertain");
            Advance(application, reader);
            Choose(application, reader, "gu");
            Advance(application, reader);
            Choose(application, reader, "workshop");
            Advance(application, reader);
            Choose(application, reader, "inspect");
            Require(application.Session.Current.id == "route.gu.inspectionBrief",
                "Supervised furnace inspection did not open.");
            VerifyActive("Choice0", true);
            VerifyActive("Choice1", true);
            VerifyActive("Choice2", true);
            VerifyActive("Choice3", false);
            reader.Complete();
            await Capture("Docs/Validation/gu-supervised-inspection-1080.png");
            Choose(application, reader, "ash");
            Require(application.Session.GetValue("case.controlSample") == 1
                    && application.Session.GetValue("case.villageWitness") == 1,
                "The supervised ash comparison did not leave a signed sample.");
        }

        private static async Task VerifyShenSurvey(StoryApplication application, DialogueReader reader)
        {
            application.StartNewGame();
            Choose(application, reader, "post");
            Advance(application, reader);
            Choose(application, reader, "borrowed");
            Advance(application, reader);
            Choose(application, reader, "ink");
            Advance(application, reader);
            Choose(application, reader, "sense");
            Advance(application, reader);
            Choose(application, reader, "sample");
            Advance(application, reader);
            Choose(application, reader, "uncertain");
            Advance(application, reader);
            Choose(application, reader, "shen");
            Advance(application, reader);
            Choose(application, reader, "paper");
            Advance(application, reader);
            Choose(application, reader, "signature");
            Advance(application, reader);
            Require(application.Session.Current.id == "route.shen.trial",
                "Shen Yan's field trial was not reached.");
            VerifyChoiceAvailability(4);
            Choose(application, reader, "survey");
            reader.Complete();
            await Capture("Docs/Validation/shen-field-survey-1080.png");
            Choose(application, reader, "witness");
            Require(application.Session.GetValue("case.returnMeasured") == 1
                    && application.Session.GetValue("case.publicRecord") == 1,
                "Witnessed field survey did not retain its measurement and public record.");
        }

        private static async Task VerifyPublicSealAudit(StoryApplication application, DialogueReader reader)
        {
            application.StartNewGame();
            Choose(application, reader, "post");
            Advance(application, reader);
            Choose(application, reader, "borrowed");
            Advance(application, reader);
            Choose(application, reader, "ink");
            Advance(application, reader);
            Choose(application, reader, "sense");
            Advance(application, reader);
            Choose(application, reader, "sample");
            Advance(application, reader);
            Choose(application, reader, "uncertain");
            Advance(application, reader);
            Choose(application, reader, "public");
            Advance(application, reader);
            Choose(application, reader, "copy");
            Advance(application, reader);
            Choose(application, reader, "audit");
            Require(application.Session.Current.id == "route.public.sealAudit",
                "Public seal audit did not open.");
            VerifyChoiceAvailability(3);
            reader.Complete();
            await Capture("Docs/Validation/public-seal-audit-1080.png");
            Choose(application, reader, "form");
            Require(application.Session.GetValue("case.archiveWitness") == 1,
                "The original seal comparison did not retain the signed underleaf.");
        }

        private static void ReachActionHub(StoryApplication application, DialogueReader reader)
        {
            application.StartNewGame();
            Choose(application, reader, "roster");
            Advance(application, reader);
            Choose(application, reader, "borrowed");
            Advance(application, reader);
            Choose(application, reader, "ink");
            Advance(application, reader);
            Choose(application, reader, "sense");
            Advance(application, reader);
            Choose(application, reader, "proceed");
            Advance(application, reader);
            Choose(application, reader, "number");
            Advance(application, reader);
            Choose(application, reader, "lin");
            Advance(application, reader);
            Choose(application, reader, "clerk");
            Advance(application, reader);
            Choose(application, reader, "verify");
            Advance(application, reader);
            Choose(application, reader, "witness");
            Advance(application, reader);
            Choose(application, reader, "legacy");
            Advance(application, reader);
            Advance(application, reader);
            Advance(application, reader);
            Choose(application, reader, "watch");
            Advance(application, reader);
            Require(application.Session.Current.id == "c01.hub", "Could not reach the action hub.");
            reader.Complete();
        }

        private static void PerformAction(StoryApplication application, DialogueReader reader, string action,
            string resolution)
        {
            Choose(application, reader, action);
            FinishAction(application, reader, resolution);
        }

        private static void FinishAction(StoryApplication application, DialogueReader reader, string resolution)
        {
            Advance(application, reader);
            Advance(application, reader);
            Choose(application, reader, resolution);
            Advance(application, reader);
            Require(application.Session.Current.id == "c01.hub", "Action did not return to the hub.");
            reader.Complete();
        }

        private static void Advance(StoryApplication application, DialogueReader reader)
        {
            reader.Complete();
            application.AdvanceStory();
        }

        private static void Choose(StoryApplication application, DialogueReader reader, string choiceId)
        {
            reader.Complete();
            application.SelectChoice(choiceId);
        }

        private static async Task VerifyBackgroundTransition(DialogueReader reader, string textureName, string screenshot)
        {
            VerifyPicture("Background", textureName, true);
            await WaitForRender();
            var previous = FindIncludingInactive("PreviousBackground").GetComponent<RawImage>();
            Require(previous.texture != null && previous.color.a > 0f,
                "Background fade did not start for " + textureName);
            await Awaitable.WaitForSecondsAsync(0.6f);
            Require(previous.color.a < 0.01f, "Background fade did not finish for " + textureName);
            reader.Complete();
            await Capture(screenshot);
        }

        private static void VerifyChoiceAvailability(int expectedAvailable)
        {
            var available = 0;
            for (var index = 0; index < 4; index++)
            {
                var choice = GameObject.Find("Choice" + index);
                Require(choice != null, "Choice button is missing.");
                if (choice.GetComponent<Button>().interactable)
                {
                    available++;
                }
            }

            Require(available == expectedAvailable, "Unexpected available choice count.");
        }

        private static void VerifyActive(string name, bool active)
        {
            var transform = FindIncludingInactive(name);
            Require(transform != null && transform.gameObject.activeSelf == active, "Object state is wrong: " + name);
        }

        private static void VerifyButton(string name, bool interactable)
        {
            var transform = FindIncludingInactive(name);
            Require(transform != null && transform.GetComponent<Button>().interactable == interactable,
                "Button state is wrong: " + name);
        }

        private static void VerifyTextMeshProUsage()
        {
            var tmpText = Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include);
            var legacyText = Object.FindObjectsByType<Text>(FindObjectsInactive.Include);
            Require(tmpText.Length > 0, "No TextMesh Pro UI was created.");
            Require(legacyText.Length == 0, "Legacy Unity UI Text components are still present.");
        }

        private static void VerifyModalIsFrontmost(string name)
        {
            var modal = FindIncludingInactive(name);
            Require(modal != null && modal.GetSiblingIndex() == modal.parent.childCount - 1,
                "Modal is not rendered above the main menu: " + name);
        }

        private static async Task VerifyDisplaySettings()
        {
            var toggleTransform = FindIncludingInactive("FullScreenToggle");
            var previousTransform = FindIncludingInactive("ResolutionPrevious");
            var nextTransform = FindIncludingInactive("ResolutionNext");
            var valueTransform = FindIncludingInactive("WindowResolutionValue");
            Require(toggleTransform != null && previousTransform != null && nextTransform != null
                    && valueTransform != null, "Desktop display settings are incomplete.");

            var toggle = toggleTransform.GetComponent<Toggle>();
            var previous = previousTransform.GetComponent<Button>();
            var next = nextTransform.GetComponent<Button>();
            var value = valueTransform.GetComponent<TMP_Text>();
            var originalFullScreen = toggle.isOn;
            var originalResolution = value.text;

            if (originalFullScreen)
            {
                await Click("FullScreenToggle");
            }

            Require(previous.interactable && next.interactable,
                "Window resolution controls should be enabled in window mode.");
            await Click("ResolutionNext");
            Require(value.text != originalResolution, "Window resolution did not change.");
            await Click("ResolutionPrevious");
            Require(value.text == originalResolution, "Window resolution was not restored.");

            await Click("FullScreenToggle");
            Require(!previous.interactable && !next.interactable,
                "Previous resolution state does not match fullscreen mode.");
            if (!originalFullScreen)
            {
                await Click("FullScreenToggle");
            }

            Require(toggle.isOn == originalFullScreen, "Fullscreen setting was not restored.");
            Require(value.text.Contains("×"),
                "Window resolution value is not formatted correctly.");
        }

        private static void VerifyText(string name, string expected)
        {
            var transform = FindIncludingInactive(name);
            Require(transform != null, "Text target is missing: " + name);
            Require(transform.GetComponent<TMP_Text>().text == expected, "Unexpected text on " + name + ".");
        }

        private static void VerifyTextContains(string name, string expected)
        {
            var transform = FindIncludingInactive(name);
            Require(transform != null, "Text target is missing: " + name);
            Require(transform.GetComponent<TMP_Text>().text.Contains(expected),
                "Expected text is missing from " + name + ".");
        }

        private static void VerifyPicture(string name, string expectedTexture, bool active)
        {
            var transform = FindIncludingInactive(name);
            Require(transform != null && transform.gameObject.activeSelf == active, "Picture state is wrong: " + name);
            if (!active)
            {
                return;
            }

            var image = transform.GetComponent<RawImage>();
            Require(image.texture?.name.Contains(expectedTexture) == true, "Wrong picture on " + name);
        }

        private static void VerifyPortraitFraming()
        {
            var transform = FindIncludingInactive("Portrait");
            var image = transform.GetComponent<RawImage>();
            var rect = image.rectTransform;
            Require(image.uvRect == new Rect(0f, 0f, 1f, 1f),
                "Portrait should display the complete source image.");
            Require(rect.sizeDelta.x >= 1000f && rect.sizeDelta.y >= 1500f,
                "Portrait is too small for the dialogue composition.");
            Require(Mathf.Abs(-rect.anchoredPosition.y - 140f) < 1f,
                "Portrait head is not aligned below the header.");
            Require(-rect.anchoredPosition.y + rect.sizeDelta.y > 1500f,
                "Portrait should extend beyond the bottom edge for a half-body composition.");
        }

        private static Transform FindIncludingInactive(string name)
        {
            return Object.FindObjectsByType<Transform>(FindObjectsInactive.Include)
                .FirstOrDefault(transform => transform.name == name);
        }

        private static async Task WaitForRender()
        {
            SceneView.RepaintAll();
            InternalEditorUtility.RepaintAllViews();
            await Awaitable.NextFrameAsync();
            await Awaitable.EndOfFrameAsync();
        }

        private static async Task Capture(string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            await Awaitable.WaitForSecondsAsync(0.6f);
            await WaitForRender();
            ScreenCapture.CaptureScreenshot(path);
            await WaitForRender();
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
