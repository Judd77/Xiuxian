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
                Require(application.Session.Current.id == "c01.gate", "Campaign did not start at the gate.");
                await Click("ClueJournal");
                VerifyActive("ClueJournalOverlay", true);
                VerifyText("ClueJournalCount", "0 / 3");
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
                reader.Complete();
                application.AdvanceStory();
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
                await Click("ClueJournal");
                VerifyText("ClueJournalCount", "2 / 3");
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
                LastReport = "Campaign Play Mode checks PASS: main menu start/continue/load, actual choice click, "
                             + "portrait/background changes, first-night alliance route, isolated save/load, and 1920x1080 capture.";
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