using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
using WenXinJie.Core;
using WenXinJie.Persistence;
using WenXinJie.UI;

namespace WenXinJie.Editor
{
    public static class PrototypePlayModeChecks
    {
        public static bool IsRunning { get; private set; }
        public static string LastReport { get; private set; } = "Not run";

        [MenuItem("WenXinJie/Validation/Run Prototype Play Mode Checks")]
        public static async void Run()
        {
            Require(!IsRunning, "Checks are already running.");
            Require(EditorApplication.isPlaying, "Enter Play Mode first.");
            EditorApplication.ExecuteMenuItem("Window/General/Game");
            FirstDayPrototype prototype = UnityEngine.Object.FindAnyObjectByType<FirstDayPrototype>();
            Require(prototype != null, "Prototype is missing.");
            var reader = prototype.GetComponent<DialogueReader>();
            string previousTestPath = prototype.testSaveDirectory;
            prototype.testSaveDirectory = Path.Combine(Application.temporaryCachePath,
                "WenXinJieTests", Guid.NewGuid().ToString("N"));

            IsRunning = true;
            LastReport = "Running";
            try
            {
                CoreSmokeTests.Run();
                VerifyStory();
                await VerifyRoutes(prototype, reader);
                await VerifyDiskSaves(prototype, reader);
                await VerifyModals(prototype, reader);
                await VerifyInputPipeline(prototype, reader);
                VerifyAudioAssets();
                VerifyVisibleButtonRaycasts();
                LastReport = "WenXinJie Play Mode checks PASS: 12 UI routes, JSON outcomes, reading history, "
                    + "isolated disk saves and backup recovery, modal blocking, audio assets, actual button-center raycasts. "
                    + "Pointer events and synthetic Input System mouse/keyboard passed; physical hardware input is not claimed.";
                Debug.Log(LastReport);
            }
            catch (Exception exception)
            {
                LastReport = "FAIL: " + exception;
                Debug.LogException(exception);
            }
            finally
            {
                prototype.testSaveDirectory = previousTestPath;
                reader.CloseHistory();
                try
                {
                    await Click("Title");
                    ScreenCapture.CaptureScreenshot("Docs/Validation/prototype-title-1080.png");
                }
                finally
                {
                    IsRunning = false;
                    Directory.CreateDirectory("Docs/Validation");
                    File.WriteAllText("Docs/Validation/playmode-checks.txt", LastReport);
                }
            }
        }

        private static void VerifyStory()
        {
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_WenXinJie/Data/FirstDayIntro.json");
            FirstDayStory story = FirstDayStory.Parse(asset.text);
            Require(story.lines.Length == 5, "Opening lines changed unexpectedly.");
            bool rejected = false;
            try
            {
                FirstDayStory.Parse("{\"lines\":[\"test\"],\"endings\":[]}");
            }
            catch (ArgumentException)
            {
                rejected = true;
            }

            Require(rejected, "Missing outcome data was accepted.");
        }

        private static async Task VerifyRoutes(FirstDayPrototype prototype, DialogueReader reader)
        {
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_WenXinJie/Data/FirstDayIntro.json");
            FirstDayStory story = FirstDayStory.Parse(asset.text);
            foreach (DayAction first in Enum.GetValues(typeof(DayAction)))
            {
                foreach (DayAction second in Enum.GetValues(typeof(DayAction)))
                {
                    if (first == second)
                    {
                        continue;
                    }

                    await ReachActions(prototype, reader, story.lines.Length);
                    await Click("Action_" + first);
                    Require(prototype.RemainingActions == 1, "First UI action was not applied.");
                    reader.Complete();
                    await Click("Action_" + second);
                    reader.Complete();
                    await Click("Advance");
                    reader.Complete();

                    var expected = new FirstDaySession();
                    expected.TryChoose(first, out _);
                    expected.TryChoose(second, out _);
                    Require(reader.FullText == story.GetEnding(expected.ResolveNight()), "Wrong UI outcome text.");
                    Require(prototype.RemainingActions == 0, "Night reset action count.");
                }
            }
        }

        private static async Task ReachActions(FirstDayPrototype prototype, DialogueReader reader, int introLength = 5)
        {
            prototype.Begin();
            string firstLine = reader.FullText;
            prototype.Advance();
            Require(reader.FullText == firstLine && !reader.IsRevealing, "Complete skipped the opening line.");
            for (int index = 0; index < introLength; index++)
            {
                reader.Complete();
                await Click("Advance");
            }

            reader.Complete();
        }

        private static async Task VerifyDiskSaves(FirstDayPrototype prototype, DialogueReader reader)
        {
            await ReachActions(prototype, reader);
            await Click("Action_Investigate");
            await Click("Save");
            Require(File.Exists(prototype.SavePath), "UI did not create the test save.");
            reader.Complete();
            await Click("Action_VisitSister");
            await Click("Load");
            Require(prototype.RemainingActions == 1, "Load did not restore remaining actions.");
            Require(reader.HistoryCount == 1, "Load did not reset session history.");
            reader.Complete();
            prototype.Choose(DayAction.Investigate);
            Require(prototype.RemainingActions == 1, "Load allowed duplicate rewards.");
            await Click("Save");
            Require(File.Exists(prototype.SavePath + ".bak"), "Overwrite did not create a backup.");
            File.WriteAllText(prototype.SavePath, "corrupt test save");
            await Click("Load");
            Require(prototype.RemainingActions == 1, "Backup recovery failed.");
            Require(ContainsVisibleText("已恢复备份"), "Backup recovery notice is missing.");

            File.WriteAllText(prototype.SavePath + ".bak", "corrupt backup");
            string previousLine = reader.FullText;
            await Click("Load");
            Require(prototype.RemainingActions == 1 && reader.FullText == previousLine,
                "Invalid save changed the live session.");

            var store = new FirstDaySaveStore();
            string legacyPath = Path.Combine(prototype.testSaveDirectory, "legacy.json");
            File.WriteAllText(legacyPath, JsonUtility.ToJson(new FirstDaySave
            {
                version = 1,
                session = new FirstDaySession().ToJson(),
                line = 2,
                message = string.Empty
            }));
            FirstDaySave legacy = store.Load(legacyPath, 5, out bool recovered);
            Require(legacy.line == 2 && !recovered, "Version-1 save compatibility failed.");
        }

        private static async Task VerifyModals(FirstDayPrototype prototype, DialogueReader reader)
        {
            reader.Complete();
            await Click("History");
            int remaining = prototype.RemainingActions;
            prototype.Choose(DayAction.VisitSister);
            Require(reader.IsHistoryOpen && remaining == prototype.RemainingActions, "History leaked input.");
            await Click("HistoryClose");
            await Click("Settings");
            prototype.Choose(DayAction.VisitSister);
            Require(remaining == prototype.RemainingActions, "Settings leaked input.");
            await Click("SettingsClose");
            await Click("Credits");
            Require(ContainsVisibleText("CC0 1.0"), "Credits are missing.");
            await Click("CreditsClose");
        }


        private static async Task VerifyInputPipeline(FirstDayPrototype prototype, DialogueReader reader)
        {
            await Click("Title");
            var mouse = InputSystem.AddDevice<Mouse>("ValidationMouse");
            var keyboard = InputSystem.AddDevice<Keyboard>("ValidationKeyboard");

            try
            {
                var button = GameObject.Find("Advance").GetComponent<Button>();
                Vector2 point = Raycast(button).position;
                InputSystem.QueueStateEvent(mouse, new MouseState { position = point });
                await Task.Delay(100);
                InputSystem.QueueStateEvent(mouse, new MouseState { position = point }.WithButton(MouseButton.Left));
                await Task.Delay(100);
                InputSystem.QueueStateEvent(mouse, new MouseState { position = point });
                await Task.Delay(100);
                Require(reader.HistoryCount == 1 && reader.FullText.StartsWith("旁白：青云山"),
                    "Synthetic mouse click did not reach the Input System UI module.");

                string firstLine = reader.FullText;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Space));
                await Task.Delay(100);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                await Task.Delay(100);
                Require(!reader.IsRevealing && reader.FullText == firstLine,
                    "Space did not complete the current line.");

                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Enter));
                await Task.Delay(100);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                await Task.Delay(100);
                Require(reader.FullText != firstLine && prototype.RemainingActions == 2,
                    "Enter did not advance to the next line.");
            }
            finally
            {
                InputSystem.RemoveDevice(mouse);
                InputSystem.RemoveDevice(keyboard);
            }
        }

        private static void VerifyAudioAssets()
        {
            AudioSource[] sources = UnityEngine.Object.FindObjectsByType<AudioSource>();
            Require(sources.Length == 2, "Expected music and rain sources.");
            foreach (AudioSource source in sources)
            {
                Require(source.clip != null && source.clip.length > 1f && source.loop,
                    "Audio asset is missing or not configured to loop.");
            }

            Require(UnityEngine.Object.FindObjectsByType<AudioListener>().Length == 1,
                "Expected exactly one audio listener.");
            var background = GameObject.Find("Landscape").GetComponent<RawImage>();
            Require(background.texture != null && background.texture.width == 1920,
                "Landscape texture was not connected at its expected size.");
        }

        public static void VerifyVisibleButtonRaycasts()
        {
            foreach (Button button in UnityEngine.Object.FindObjectsByType<Button>())
            {
                if (button.IsInteractable())
                {
                    Raycast(button);
                }
            }
        }

        private static async Task Click(string objectName)
        {
            // Newly enabled UGUI graphics enter the raycast list after a rendered frame.
            await WaitForRender();
            Require(EditorApplication.isPlaying, "Play Mode ended during checks.");
            GameObject target = GameObject.Find(objectName);
            Require(target != null, "Missing active UI object: " + objectName);
            var button = target.GetComponent<Button>();
            Require(button != null && button.IsInteractable(), "Button unavailable: " + objectName);
            PointerEventData pointer = Raycast(button);
            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
            await WaitForRender();
        }

        private static async Task WaitForRender()
        {
            EditorApplication.QueuePlayerLoopUpdate();
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
            await Awaitable.NextFrameAsync();
            await Awaitable.EndOfFrameAsync();
        }

        private static PointerEventData Raycast(Button button)
        {
            Canvas.ForceUpdateCanvases();
            var rect = (RectTransform)button.transform;
            var canvas = button.GetComponentInParent<Canvas>();
            Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            Vector2 point = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.center));
            var pointer = new PointerEventData(EventSystem.current) { position = point };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            Require(hits.Count > 0 && hits[0].gameObject == button.gameObject,
                "Button center raycast missed or was blocked: " + button.name + " at " + point
                + "; screen=" + Screen.width + "x" + Screen.height + "; canvas=" + canvas.pixelRect
                + "; depth=" + button.targetGraphic.depth + "; culled=" + button.targetGraphic.canvasRenderer.cull
                + "; first=" + (hits.Count > 0 ? hits[0].gameObject.name : "none"));
            return pointer;
        }

        private static bool ContainsVisibleText(string value)
        {
            foreach (Text label in UnityEngine.Object.FindObjectsByType<Text>())
            {
                if (label.text.Contains(value))
                {
                    return true;
                }
            }

            return false;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException("Prototype check failed: " + message);
            }
        }
    }
}
