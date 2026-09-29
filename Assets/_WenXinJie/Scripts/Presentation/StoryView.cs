using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using WenXinJie.UI;

namespace WenXinJie.Presentation
{
    public sealed class StoryView : MonoBehaviour
    {
        private StoryGameplayView _gameplay;
        private MainMenuView _mainMenu;
        private StoryOverlayView _overlays;
        private DialogueReader _reader;
        private StorySceneView _scene;

        public bool IsInteractionBlocked => _overlays.IsModalOpen || _mainMenu.IsOpen || _reader.IsHistoryOpen;

        private void Update()
        {
            if (_overlays == null)
            {
                return;
            }

            if (_overlays.IsModalOpen && Keyboard.current?.escapeKey.wasPressedThisFrame == true)
            {
                _overlays.CloseAll();
            }
        }

        internal void Initialize(StoryAssetCatalog assets, DialogueReader reader, GameAudio audio,
            DisplayModeService displayModes, IStoryCommands commands, bool isDemo)
        {
            _reader = reader;
            var ui = new StoryUiFactory(assets);
            var frame = CreateCanvas();
            var gameplayRoot = StoryUiFactory.Box(frame, "Gameplay", 0f, 0f, 1920f, 1080f);

            _scene = new(frame.parent, gameplayRoot, ui, this);
            _overlays = new(frame, ui, assets.MenuFont, reader, audio, displayModes);
            _gameplay = new(
                gameplayRoot,
                ui,
                assets,
                reader,
                commands,
                _overlays.OpenJournal,
                IsReaderInputBlocked);
            _mainMenu = new(
                frame,
                ui,
                assets.TitleFont,
                assets.MenuFont,
                commands,
                _overlays.OpenSettings,
                _overlays.OpenCredits,
                isDemo);

            EnsureEventSystem();
        }

        public void ShowMainMenu(Texture2D background, bool canContinue, bool canLoad, string notice)
        {
            _scene.ShowBackground(background);
            _scene.ShowPortrait(null);
            _overlays.CloseAll();
            _gameplay.SetVisible(false);
            _mainMenu.Show(canContinue, canLoad, notice);
        }

        public void HideMainMenu()
        {
            _mainMenu.Hide();
            _gameplay.SetVisible(true);
        }

        public void ShowMainMenuNotice(string message)
        {
            _mainMenu.ShowNotice(message);
        }

        internal void Render(StoryViewState state)
        {
            _overlays.RenderJournal(state.Journal);
            _scene.ShowPortrait(state.Portrait);
            _scene.ShowBackground(state.Background);
            _reader.Present(state.Text, true);
            _gameplay.Render(state);
        }

        public void ShowNotice(string message)
        {
            _gameplay.ShowNotice(message);
        }

        private RectTransform CreateCanvas()
        {
            var canvasObject = new GameObject(
                "StoryCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            var frame = StoryUiFactory.Box(canvasObject.transform, "DesignFrame", 0f, 0f, 1920f, 1080f);
            frame.anchorMin = frame.anchorMax = frame.pivot = new(0.5f, 0.5f);
            return frame;
        }

        private bool IsReaderInputBlocked()
        {
            return _overlays.IsModalOpen || _mainMenu.IsOpen;
        }

        private void EnsureEventSystem()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            var eventSystem = new GameObject(
                "EventSystem",
                typeof(EventSystem),
                typeof(InputSystemUIInputModule));
            eventSystem.transform.SetParent(transform, false);
            eventSystem.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }
    }
}
