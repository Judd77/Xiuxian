using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using WenXinJie.Core;

namespace WenXinJie.UI
{
    public sealed class FirstDayView : MonoBehaviour
    {
        private static readonly Color Ink = new Color(0.055f, 0.085f, 0.09f, 0.96f);
        private static readonly Color Paper = new Color(0.95f, 0.94f, 0.87f);
        private static readonly Color Cinnabar = new Color(0.57f, 0.21f, 0.18f);

        private readonly Button[] _actions = new Button[4];
        private Font _font;
        private DialogueReader _reader;
        private Text _status;
        private Text _title;
        private Text _body;
        private Text _notice;
        private Button _next;
        private GameObject _settings;
        private GameObject _credits;

        public event Action AdvanceRequested;
        public event Action<DayAction> ActionSelected;
        public event Action SaveRequested;
        public event Action LoadRequested;
        public event Action TitleRequested;

        public bool IsModalOpen => (_settings != null && _settings.activeSelf)
            || (_credits != null && _credits.activeSelf);

        public void Initialize(Font font, Texture2D landscape, DialogueReader reader, FirstDayAudio audio)
        {
            _font = font;
            _reader = reader;
            _reader.inputBlocked = () => IsModalOpen;

            RectTransform frame = CreateCanvas();
            var background = CreateBox(frame, "Landscape", 0f, 0f, 1920f, 1080f).gameObject.AddComponent<RawImage>();
            background.texture = landscape;
            background.color = landscape != null ? Color.white : Ink;
            background.raycastTarget = false;

            CreateBand(frame, "HeaderShade", 0f, 180f, new Color(0.055f, 0.085f, 0.09f, 0.82f));
            CreateBand(frame, "DialogueBand", 570f, 510f, Ink);
            CreateLabel(frame, "问心劫", 84f, 30f, 520f, 78f, 58);
            Text chapter = CreateLabel(frame, "三十日登仙  /  山门夜雨", 1190f, 60f, 646f, 48f, 28);
            chapter.alignment = TextAnchor.MiddleRight;
            _status = CreateLabel(frame, string.Empty, 84f, 126f, 1752f, 48f, 26);
            _title = CreateLabel(frame, string.Empty, 84f, 592f, 1752f, 62f, 38);
            _body = CreateLabel(frame, string.Empty, 84f, 680f, 1752f, 164f, 32);
            _body.name = "DialogueBody";
            _reader.target = _body;

            _next = CreateButton(frame, "Advance", "继续", 1452f, 850f, 384f, () => AdvanceRequested?.Invoke());
            _next.targetGraphic.color = Cinnabar;
            string[] labels = { "练剑  /  修为 +2", "查名册  /  1 灵石，获得证据", "陪林月  /  信任 +2", "药庐做工  /  灵石 +2" };
            for (int index = 0; index < _actions.Length; index++)
            {
                var action = (DayAction)index;
                _actions[index] = CreateButton(frame, "Action_" + action, labels[index],
                    1116f, 194f + index * 92f, 720f, () => ActionSelected?.Invoke(action));
            }

            CreateButton(frame, "Save", "存档", 84f, 948f, 216f, () => SaveRequested?.Invoke());
            CreateButton(frame, "Load", "读档", 324f, 948f, 216f, () => LoadRequested?.Invoke());
            CreateButton(frame, "History", "阅读记录", 564f, 948f, 264f, _reader.OpenHistory);
            CreateButton(frame, "Settings", "声音", 852f, 948f, 216f, OpenSettings);
            CreateButton(frame, "Credits", "鸣谢", 1092f, 948f, 216f, OpenCredits);
            CreateButton(frame, "Title", "返回标题", 1332f, 948f, 264f, () => TitleRequested?.Invoke());
            _notice = CreateLabel(frame, string.Empty, 84f, 1040f, 1752f, 32f, 22);

            var dialogueButton = _body.gameObject.AddComponent<Button>();
            _body.raycastTarget = true;
            dialogueButton.transition = Selectable.Transition.None;
            dialogueButton.navigation = new Navigation { mode = Navigation.Mode.None };
            dialogueButton.onClick.AddListener(() => AdvanceRequested?.Invoke());

            BuildHistory(frame);
            BuildSettings(frame, audio);
            BuildCredits(frame);
            EnsureEventSystem();
        }

        public void Render(FirstDayViewState state)
        {
            _status.text = state.Status;
            _status.gameObject.SetActive(!state.IsMenu);
            _title.text = state.Title;
            _reader.Present(state.Body, !state.IsMenu);
            _next.gameObject.SetActive(!state.ShowActions);
            _next.GetComponentInChildren<Text>().text = state.NextLabel;

            for (int index = 0; index < _actions.Length; index++)
            {
                _actions[index].gameObject.SetActive(state.ShowActions);
                _actions[index].interactable = state.ShowActions && state.AvailableActions[index];
            }
        }

        public void ShowNotice(string text)
        {
            _notice.text = text;
        }

        private RectTransform CreateCanvas()
        {
            var canvasObject = new GameObject("PrototypeCanvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            RectTransform frame = CreateBox(canvasObject.transform, "DesignFrame", 0f, 0f, 1920f, 1080f);
            frame.anchorMin = frame.anchorMax = new Vector2(0.5f, 0.5f);
            frame.pivot = new Vector2(0.5f, 0.5f);
            return frame;
        }

        private void BuildHistory(Transform frame)
        {
            RectTransform overlay = CreateOverlay(frame, "HistoryOverlay", "阅读记录");
            Text record = CreateLabel(overlay, string.Empty, 120f, 220f, 1680f, 560f, 34);
            CreateButton(overlay, "HistoryPrevious", "上一条", 120f, 880f, 300f, _reader.Previous);
            CreateButton(overlay, "HistoryNext", "下一条", 450f, 880f, 300f, _reader.Next);
            CreateButton(overlay, "HistoryClose", "关闭", 1500f, 880f, 300f, _reader.CloseHistory);
            _reader.ConfigureHistory(overlay.gameObject, record);
        }

        private void BuildSettings(Transform frame, FirstDayAudio audio)
        {
            RectTransform overlay = CreateOverlay(frame, "SoundSettings", "声音");
            _settings = overlay.gameObject;
            CreateVolumeSlider(overlay, "MusicVolume", "音乐", 320f, audio.MusicVolume, audio.SetMusicVolume);
            CreateVolumeSlider(overlay, "AmbienceVolume", "雨声", 460f, audio.AmbienceVolume, audio.SetAmbienceVolume);
            CreateButton(overlay, "SettingsClose", "返回", 1500f, 880f, 300f, CloseModals);
            _settings.SetActive(false);
        }

        private void BuildCredits(Transform frame)
        {
            RectTransform overlay = CreateOverlay(frame, "CreditsOverlay", "鸣谢与素材来源");
            _credits = overlay.gameObject;
            CreateLabel(overlay,
                "山水底图：髡残《春景图》（1666），克利夫兰艺术博物馆，1966.367。\n" +
                "本项目进行了裁切、缩放及冷色调处理。\n\n" +
                "音乐：Asianoriental2 / Tozan / OpenGameArt.org。\n" +
                "雨声：Rain (loopable) / Ylmir / OpenGameArt.org。\n\n" +
                "以上素材来源均标注 CC0 1.0。感谢作者及开放馆藏。",
                120f, 220f, 1680f, 560f, 32);
            CreateButton(overlay, "CreditsClose", "返回", 1500f, 880f, 300f, CloseModals);
            _credits.SetActive(false);
        }

        private void OpenSettings()
        {
            _reader.Complete();
            _settings.SetActive(true);
        }

        private void OpenCredits()
        {
            _reader.Complete();
            _credits.SetActive(true);
        }

        private void CloseModals()
        {
            _settings.SetActive(false);
            _credits.SetActive(false);
            PlayerPrefs.Save();
        }

        private void Update()
        {
            if (IsModalOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                CloseModals();
            }
        }

        private void CreateVolumeSlider(Transform parent, string objectName, string label, float y,
            float value, Action<float> onChanged)
        {
            CreateLabel(parent, label, 300f, y, 300f, 64f, 32);
            Text percentage = CreateLabel(parent, Mathf.RoundToInt(value * 100f) + "%", 1470f, y, 180f, 64f, 30);
            RectTransform root = CreateBox(parent, objectName, 620f, y, 780f, 64f);
            var hitArea = root.gameObject.AddComponent<Image>();
            hitArea.color = new Color(1f, 1f, 1f, 0.02f);
            var track = CreateBox(root, "Track", 0f, 28f, 780f, 8f).gameObject.AddComponent<Image>();
            track.color = new Color(0.35f, 0.45f, 0.44f);
            track.raycastTarget = false;
            RectTransform handleArea = CreateBox(root, "HandleArea", 14f, 14f, 752f, 36f);
            RectTransform handle = CreateBox(handleArea, "Handle", 0f, 0f, 28f, 0f);
            handle.pivot = new Vector2(0.5f, 0.5f);
            var handleImage = handle.gameObject.AddComponent<Image>();
            handleImage.color = Paper;
            var slider = root.gameObject.AddComponent<Slider>();
            slider.handleRect = handle;
            slider.targetGraphic = handleImage;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.SetValueWithoutNotify(value);
            slider.onValueChanged.AddListener(volume =>
            {
                percentage.text = Mathf.RoundToInt(volume * 100f) + "%";
                onChanged(volume);
            });
        }

        private RectTransform CreateOverlay(Transform parent, string objectName, string heading)
        {
            RectTransform overlay = CreateBox(parent, objectName, 0f, 0f, 1920f, 1080f);
            var shade = overlay.gameObject.AddComponent<Image>();
            shade.color = new Color(Ink.r, Ink.g, Ink.b, 1f);
            CreateLabel(overlay, heading, 120f, 80f, 1680f, 80f, 44);
            return overlay;
        }

        private static void CreateBand(Transform parent, string objectName, float y, float height, Color color)
        {
            var image = CreateBox(parent, objectName, 0f, y, 1920f, height).gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
        }

        private Text CreateLabel(Transform parent, string text, float x, float y, float width, float height, int size)
        {
            var label = CreateBox(parent, "Label", x, y, width, height).gameObject.AddComponent<Text>();
            label.font = _font;
            label.fontSize = size;
            label.text = text;
            label.color = Paper;
            label.supportRichText = false;
            label.raycastTarget = false;
            return label;
        }

        private Button CreateButton(Transform parent, string objectName, string text, float x, float y,
            float width, Action callback)
        {
            RectTransform rect = CreateBox(parent, objectName, x, y, width, 76f);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(0.12f, 0.23f, 0.23f, 0.96f);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            Text label = CreateLabel(rect, text, 24f, 6f, width - 48f, 64f, 28);
            label.alignment = TextAnchor.MiddleLeft;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.onClick.AddListener(() =>
            {
                callback();
                if (EventSystem.current != null)
                {
                    EventSystem.current.SetSelectedGameObject(null);
                }
            });
            return button;
        }

        private static RectTransform CreateBox(Transform parent, string objectName, float x, float y,
            float width, float height)
        {
            var uiObject = new GameObject(objectName, typeof(RectTransform));
            uiObject.transform.SetParent(parent, false);
            var rect = (RectTransform)uiObject.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
            return rect;
        }

        private void EnsureEventSystem()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            var eventObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventObject.transform.SetParent(transform, false);
            eventObject.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }
    }
}
