using System;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using WenXinJie.Presentation;
using WenXinJie.UI;

namespace WenXinJie.Editor
{
    public static class CampaignPlayModeChecks
    {
        public static bool IsRunning { get; private set; }
        public static string LastReport { get; private set; } = "Not run";

        [MenuItem("WenXinJie/Validation/Run Campaign Play Mode Checks")]
        public static async void Run()
        {
            Require(EditorApplication.isPlaying, "Enter Play Mode first.");
            Require(!IsRunning, "Checks are already running.");
            StoryApplication application = UnityEngine.Object.FindAnyObjectByType<StoryApplication>();
            Require(application != null, "StoryApplication is missing.");
            DialogueReader reader = application.GetComponent<DialogueReader>();
            Require(reader != null, "DialogueReader is missing.");
            string previousDirectory = application.TestSaveDirectory;
            application.TestSaveDirectory = Path.Combine(Application.temporaryCachePath,
                "WenXinJieCampaignPlayTests", Guid.NewGuid().ToString("N"));
            IsRunning = true;
            LastReport = "Running";

            try
            {
                Require(application.Session == null, "Campaign session should not start behind the main menu.");
                VerifyActive("MainMenu", true);
                await Capture("Docs/Validation/campaign-main-menu-1080.png");
                await Click("StartGame");
                Require(application.Session.Current.id == "c01.gate", "Campaign did not start at the gate.");
                reader.Complete();
                application.Advance();
                Require(application.Session.Current.id == "c01.warning", "Opening advance failed.");
                VerifyPicture("Portrait", "LinYue_Premium", true);
                VerifyPortraitFraming();
                reader.Complete();
                await Capture("Docs/Validation/campaign-lin-yue-1080.png");
                application.Advance();
                Require(application.Session.Current.id == "c01.hub", "Could not reach action hub.");
                VerifyChoiceAvailability(3);
                reader.Complete();
                await Capture("Docs/Validation/campaign-choices-1080.png");

                await Click("Choice0");
                Require(application.Session.Current.id == "c01.register", "Actual choice click did not enter register node.");
                Require(application.Session.GetValue("clue.register") == 1, "Register effect was not applied.");
                VerifyPicture("Portrait", string.Empty, false);

                reader.Complete();
                application.Advance();
                reader.Complete();
                application.Choose("visit");
                Require(application.Session.Current.id == "c01.meeting", "Visit choice failed.");
                VerifyPicture("Background", "PineRidge", true);
                VerifyPicture("Portrait", "LinYue_Premium", true);

                reader.Complete();
                application.Advance();
                reader.Complete();
                application.Choose("night");
                Require(application.Session.Current.id == "c01.night", "Night transition failed.");
                VerifyPicture("Background", "PineRidge_Premium_Night", true);
                reader.Complete();
                application.Choose("shared");
                Require(application.Session.Current.id == "c01.shared", "Shared-secret outcome failed.");
                Require(application.Session.GetValue("route.alliance") == 1, "Alliance flag was not set.");

                application.Save();
                Require(File.Exists(application.SavePath), "Play-mode save was not written.");
                await Click("MainMenuButton");
                VerifyActive("MainMenu", true);
                VerifyButton("ContinueGame", true);
                VerifyButton("LoadGame", true);
                await Click("ContinueGame");
                Require(application.Session.Current.id == "c01.shared", "Continue did not resume the active session.");
                reader.Complete();
                application.Advance();
                Require(application.Session.GetValue("day") == 2, "Could not advance to day two.");
                await Click("MainMenuButton");
                await Click("LoadGame");
                Require(application.Session.Current.id == "c01.shared", "Load did not restore story cursor.");
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
                Directory.CreateDirectory("Docs/Validation");
                File.WriteAllText("Docs/Validation/campaign-playmode-checks.txt", LastReport);
                IsRunning = false;
            }
        }

        private static async Task Click(string name)
        {
            GameObject target = GameObject.Find(name);
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
            int available = 0;
            for (int index = 0; index < 4; index++)
            {
                GameObject choice = GameObject.Find("Choice" + index);
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
            Transform transform = FindIncludingInactive(name);
            Require(transform != null && transform.gameObject.activeSelf == active, "Object state is wrong: " + name);
        }

        private static void VerifyButton(string name, bool interactable)
        {
            Transform transform = FindIncludingInactive(name);
            Require(transform != null && transform.GetComponent<Button>().interactable == interactable,
                "Button state is wrong: " + name);
        }

        private static void VerifyPicture(string name, string expectedTexture, bool active)
        {
            Transform transform = FindIncludingInactive(name);
            Require(transform != null && transform.gameObject.activeSelf == active, "Picture state is wrong: " + name);
            if (active)
            {
                RawImage image = transform.GetComponent<RawImage>();
                Require(image.texture != null && image.texture.name.Contains(expectedTexture), "Wrong picture on " + name);
            }
        }

        private static void VerifyPortraitFraming()
        {
            Transform transform = FindIncludingInactive("Portrait");
            var image = transform.GetComponent<RawImage>();
            RectTransform rect = image.rectTransform;
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
            foreach (Transform transform in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
            {
                if (transform.name == name)
                {
                    return transform;
                }
            }

            return null;
        }

        private static async Task WaitForRender()
        {
            SceneView.RepaintAll();
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
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
