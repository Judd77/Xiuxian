using System;
using System.Collections;
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
        private readonly Button[] _choices = new Button[4];
        private readonly string[] _choiceIds = new string[4];
        private StoryUiFactory _ui;
        private DialogueReader _reader;
        private RawImage _background;
        private RawImage _previousBackground;
        private RawImage _portrait;
        private CanvasGroup _portraitGroup;
        private Text _chapter;
        private Text _location;
        private Text _speaker;
        private Text _status;
        private Text _notice;
        private Button _next;
        private GameObject _settings;
        private GameObject _credits;
        private RectTransform _gameplay;
        private MainMenuView _mainMenu;
        private ClueJournalView _journal;
        private Font _menuFont;
        private Coroutine _transition;
        private Coroutine _portraitTransition;

        public event Action AdvanceRequested;
        public event Action<string> ChoiceRequested;
        public event Action SaveRequested;
        public event Action LoadRequested;
        public event Action StartGameRequested;
        public event Action ContinueGameRequested;
        public event Action MainMenuLoadRequested;
        public event Action MainMenuRequested;
        public event Action QuitRequested;

        public bool IsModalOpen => (_settings != null && _settings.activeSelf)
                                   || (_credits != null && _credits.activeSelf)
                                   || (_journal != null && _journal.IsOpen);

        public bool IsMainMenuOpen => _mainMenu != null && _mainMenu.IsOpen;

        public void Initialize(StoryMediaCatalog media, DialogueReader reader, GameAudio audio)
        {
            _ui = new StoryUiFactory(media);
            _menuFont = media.MenuFont;
            _reader = reader;
            RectTransform frame = CreateCanvas();
            _gameplay = StoryUiFactory.Box(frame, "Gameplay", 0f, 0f, 1920f, 1080f);
            _ui.Picture(_gameplay, "HeaderPanel", media.Panel, 48f, 24f, 1824f, 116f);
            Text gameTitle = _ui.Label(_gameplay, "GameTitle", "问心劫", 86f, 32f, 280f, 74f, 42);
            gameTitle.font = media.TitleFont;
            gameTitle.color = new Color(0.93f, 0.80f, 0.50f);
            gameTitle.alignment = TextAnchor.MiddleLeft;
            Text subtitle = _ui.Label(_gameplay, "Subtitle", "三十日登仙", 390f, 55f, 300f, 38f, 21);
            subtitle.font = _menuFont;
            subtitle.color = new Color(0.78f, 0.84f, 0.82f);
            _chapter = _ui.Label(_gameplay, "Chapter", string.Empty, 1110f, 43f, 710f, 46f, 27);
            _chapter.font = _menuFont;
            _chapter.alignment = TextAnchor.MiddleRight;
            _status = _ui.Label(_gameplay, "Status", string.Empty, 86f, 102f, 1734f, 30f, 19);
            _status.font = _menuFont;
            _status.color = new Color(0.84f, 0.88f, 0.85f);
            _ui.Frame(_gameplay, "LocationPanel", 80f, 166f, 620f, 74f);
            _location = _ui.Label(_gameplay, "Location", string.Empty, 110f, 178f, 560f, 50f, 28);
            _location.font = _menuFont;
            _location.alignment = TextAnchor.MiddleLeft;
            _portrait = _ui.Picture(_gameplay, "Portrait", null, 0f, 0f, 1f, 1f);
            _portraitGroup = _portrait.gameObject.AddComponent<CanvasGroup>();
            _portrait.transform.SetAsFirstSibling();
            _portrait.gameObject.SetActive(false);

            for (int index = 0; index < _choices.Length; index++)
            {
                int slot = index;
                _choices[index] = _ui.Button(_gameplay, "Choice" + index, string.Empty, 96f, 286f + index * 94f, 700f, 78f,
                    () => ChoiceRequested?.Invoke(_choiceIds[slot]));
                _choices[index].GetComponentInChildren<Text>().alignment = TextAnchor.MiddleLeft;
                _choices[index].gameObject.AddComponent<ChoiceHint>();
            }

            _ui.Picture(_gameplay, "DialoguePanel", media.Panel, 80f, 748f, 1760f, 246f);
            _ui.Frame(_gameplay, "SpeakerPanel", 112f, 724f, 330f, 62f);
            _speaker = _ui.Label(_gameplay, "Speaker", string.Empty, 138f, 731f, 280f, 48f, 29);
            _speaker.font = _menuFont;
            _speaker.color = new Color(0.92f, 0.78f, 0.48f);
            _speaker.alignment = TextAnchor.MiddleLeft;
            _reader.target = _ui.Label(_gameplay, "DialogueBody", string.Empty, 124f, 808f, 1490f, 164f, 28);
            _reader.target.raycastTarget = true;
            var dialogueButton = _reader.target.gameObject.AddComponent<Button>();
            dialogueButton.transition = Selectable.Transition.None;
            dialogueButton.navigation = new Navigation { mode = Navigation.Mode.None };
            dialogueButton.onClick.AddListener(() => AdvanceRequested?.Invoke());
            _next = _ui.Button(_gameplay, "Advance", "继续  ▷", 1632f, 906f, 172f, 56f, () => AdvanceRequested?.Invoke());
            _ui.Button(_gameplay, "Save", "存档", 80f, 1008f, 146f, 54f, () => SaveRequested?.Invoke());
            _ui.Button(_gameplay, "Load", "读档", 242f, 1008f, 146f, 54f, () => LoadRequested?.Invoke());
            _ui.Button(_gameplay, "History", "前尘", 404f, 1008f, 146f, 54f, reader.OpenHistory);
            _ui.Button(_gameplay, "ClueJournal", "线索", 566f, 1008f, 146f, 54f, () => _journal.Show());
            _ui.Button(_gameplay, "MainMenuButton", "主界面", 1630f, 1008f, 210f, 54f,
                () => MainMenuRequested?.Invoke());
            var noticePanel = _ui.Frame(_gameplay, "NoticePanel", 736f, 1008f, 870f, 54f);
            noticePanel.color = new(0.82f, 0.88f, 0.82f);
            _notice = _ui.Label(_gameplay, "Notice", string.Empty, 756f, 1010f, 830f, 50f, 22);
            _notice.font = _menuFont;
            _notice.color = new(0.94f, 0.91f, 0.78f);
            _notice.alignment = TextAnchor.MiddleCenter;
            var noticeShadow = _notice.gameObject.AddComponent<Shadow>();
            noticeShadow.effectColor = new(0f, 0f, 0f, 0.92f);
            noticeShadow.effectDistance = new Vector2(1.5f, -1.5f);
            _mainMenu = new MainMenuView(frame, _ui, media.TitleFont, media.MenuFont,
                () => StartGameRequested?.Invoke(), () => ContinueGameRequested?.Invoke(),
                () => MainMenuLoadRequested?.Invoke(),
                () => Open(_settings), () => Open(_credits), () => QuitRequested?.Invoke());
            BuildOverlays(frame, audio);
            _reader.inputBlocked = () => IsModalOpen || IsMainMenuOpen;
            EnsureEventSystem();
        }

        public void ShowMainMenu(Texture2D background, bool canContinue, bool canLoad, string notice)
        {
            ChangeBackground(background);
            ChangePortrait(null);
            _journal.Hide();
            _gameplay.gameObject.SetActive(false);
            _mainMenu.Show(canContinue, canLoad, notice);
        }

        public void HideMainMenu()
        {
            _mainMenu.Hide();
            _gameplay.gameObject.SetActive(true);
        }

        public void ShowMainMenuNotice(string message)
        {
            _mainMenu.ShowNotice(message);
        }

        public void Render(StoryViewState state)
        {
            _chapter.text = state.Chapter;
            _location.text = state.Location;
            _speaker.text = state.Speaker;
            _status.text = state.Status;
            _journal.Render(state.Journal);
            ChangePortrait(state.Portrait);
            ChangeBackground(state.Background);
            _reader.Present(state.Text, true);
            _next.gameObject.SetActive(state.Choices.Length == 0);
            _next.interactable = !state.Terminal;
            _next.GetComponentInChildren<Text>().text = state.NextLabel;
            for (int index = 0; index < _choices.Length; index++)
            {
                bool visible = index < state.Choices.Length;
                _choices[index].gameObject.SetActive(visible);
                if (!visible)
                {
                    continue;
                }

                ChoiceViewState choice = state.Choices[index];
                _choiceIds[index] = choice.Id;
                _choices[index].interactable = choice.Available;
                Text choiceLabel = _choices[index].GetComponentInChildren<Text>();
                choiceLabel.text = choice.Label;
                choiceLabel.color = choice.Available
                    ? new Color(0.96f, 0.94f, 0.82f)
                    : new(0.70f, 0.73f, 0.68f);
                _choices[index].GetComponent<ChoiceHint>().Configure(_notice, choice.Available ? string.Empty : choice.LockedReason);
            }
        }

        public void ShowNotice(string message)
        {
            _notice.text = message;
        }

        private RectTransform CreateCanvas()
        {
            var canvasObject = new GameObject("StoryCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            _background = CreateBackdrop(canvasObject.transform, "Background");
            _previousBackground = CreateBackdrop(canvasObject.transform, "PreviousBackground");
            _previousBackground.color = Color.clear;
            RectTransform frame = StoryUiFactory.Box(canvasObject.transform, "DesignFrame", 0f, 0f, 1920f, 1080f);
            frame.anchorMin = frame.anchorMax = frame.pivot = new Vector2(0.5f, 0.5f);
            return frame;
        }

        private RawImage CreateBackdrop(Transform parent, string name)
        {
            RawImage image = _ui.Picture(parent, name, null, 0f, 0f, 1920f, 1080f);
            image.rectTransform.anchorMin = image.rectTransform.anchorMax = image.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            var fitter = image.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = 16f / 9f;
            return image;
        }

        private void ChangeBackground(Texture2D texture)
        {
            if (_background.texture == texture)
            {
                return;
            }

            if (_transition != null)
            {
                StopCoroutine(_transition);
            }

            _previousBackground.texture = _background.texture;
            _background.texture = texture;
            _transition = StartCoroutine(FadeBackground());
        }

        private IEnumerator FadeBackground()
        {
            float elapsed = _previousBackground.texture == null ? 0.45f : 0f;
            while (elapsed < 0.45f)
            {
                _previousBackground.color = new Color(1f, 1f, 1f, 1f - elapsed / 0.45f);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            _previousBackground.color = Color.clear;
            _transition = null;
        }

        private void ChangePortrait(PortraitMedia portrait)
        {
            Texture2D texture = portrait?.Texture;
            if (_portrait.texture == texture && _portrait.gameObject.activeSelf == (texture != null))
            {
                return;
            }

            if (_portraitTransition != null)
            {
                StopCoroutine(_portraitTransition);
            }

            _portrait.texture = texture;
            if (texture == null)
            {
                _portrait.gameObject.SetActive(false);
                _portraitTransition = null;
                return;
            }

            float height = portrait.Width * texture.height / texture.width;
            float x = 1920f - portrait.Right - portrait.Width;
            _portrait.rectTransform.anchoredPosition = new Vector2(x, -portrait.Top);
            _portrait.rectTransform.sizeDelta = new Vector2(portrait.Width, height);
            _portrait.uvRect = new Rect(0f, 0f, 1f, 1f);
            _portrait.gameObject.SetActive(true);
            _portraitGroup.alpha = 0f;
            _portraitTransition = StartCoroutine(FadePortrait());
        }

        private IEnumerator FadePortrait()
        {
            float elapsed = 0f;
            while (elapsed < 0.3f)
            {
                _portraitGroup.alpha = elapsed / 0.3f;
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            _portraitGroup.alpha = 1f;
            _portraitTransition = null;
        }

        private void BuildOverlays(Transform frame, GameAudio audio)
        {
            _journal = new(frame, _ui, _menuFont);

            RectTransform history = Overlay(frame, "HistoryOverlay", "前尘 · 已读记事");
            Text record = _ui.Label(history, "HistoryText", string.Empty, 180f, 260f, 1560f, 520f, 32);
            _ui.Button(history, "HistoryPrevious", "上一条", 180f, 870f, 260f, 64f, _reader.Previous);
            _ui.Button(history, "HistoryNext", "下一条", 470f, 870f, 260f, 64f, _reader.Next);
            _ui.Button(history, "HistoryClose", "返回", 1480f, 870f, 260f, 64f, _reader.CloseHistory);
            _reader.ConfigureHistory(history.gameObject, record);

            RectTransform settings = Overlay(frame, "SoundSettings", "声音");
            _settings = settings.gameObject;
            _ui.Volume(settings, "MusicVolume", "乐声", 340f, audio.MusicVolume, audio.SetMusicVolume);
            _ui.Volume(settings, "RainVolume", "雨声", 500f, audio.AmbienceVolume, audio.SetAmbienceVolume);
            _ui.Button(settings, "SettingsClose", "返回", 1480f, 870f, 260f, 64f, CloseModals);
            _settings.SetActive(false);

            RectTransform credits = Overlay(frame, "CreditsOverlay", "鸣谢 · 创作工具与开放资源");
            _credits = credits.gameObject;
            _ui.Label(credits, "CreditsText",
                "主场景与林月立绘：本项目原创提示词，通过 OpenAI 图像生成工具制作并人工筛选、透明化与调色。\n未使用现有游戏角色、标志、界面或拆包资源。\n\n音乐：Tozan / Asianoriental2，CC0。\n雨声：Ylmir / Rain (loopable)，CC0。\n字体：Noto Serif SC，Copyright 2012 Google Inc.，SIL OFL 1.1。\n\n早期开放馆藏美术来源与完整加工记录见项目授权台账。",
                180f, 250f, 1560f, 550f, 30);
            _ui.Button(credits, "CreditsClose", "返回", 1480f, 870f, 260f, 64f, CloseModals);
            _credits.SetActive(false);
        }

        private RectTransform Overlay(Transform parent, string name, string title)
        {
            RectTransform overlay = StoryUiFactory.Box(parent, name, 0f, 0f, 1920f, 1080f);
            var image = overlay.gameObject.AddComponent<Image>();
            image.color = new Color(0.055f, 0.14f, 0.13f, 1f);
            Text heading = _ui.Label(overlay, "Heading", title, 180f, 100f, 1560f, 90f, 44);
            heading.font = _menuFont;
            return overlay;
        }

        private void Open(GameObject window)
        {
            _reader.Complete();
            window.SetActive(true);
        }

        private void CloseModals()
        {
            _settings.SetActive(false);
            _credits.SetActive(false);
            _journal.Hide();
            PlayerPrefs.Save();
        }

        private void Update()
        {
            if (IsModalOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                CloseModals();
            }
        }

        private void EnsureEventSystem()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            events.transform.SetParent(transform, false);
            events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }
    }
}