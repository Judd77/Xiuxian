using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace WenXinJie.Presentation
{
    internal sealed class StoryUiFactory
    {
        private static readonly Vector4 FrameBorder = new(30f, 18f, 30f, 18f);
        private static readonly Vector4 PanelBorder = new(24f, 18f, 24f, 18f);
        private static readonly Color Paper = new(0.96f, 0.96f, 0.88f);
        private readonly StoryAssetCatalog _assets;
        private readonly Sprite _frameSprite;
        private readonly Sprite _panelSprite;

        public StoryUiFactory(StoryAssetCatalog assets)
        {
            _assets = assets;
            var frameTexture = assets.ChoiceFrameTexture;
            _frameSprite = Sprite.Create(frameTexture, new(0f, 0f, frameTexture.width, frameTexture.height),
                new(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, FrameBorder);
            var panelTexture = assets.PanelTexture;
            _panelSprite = Sprite.Create(panelTexture, new(0f, 0f, panelTexture.width, panelTexture.height),
                new(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, PanelBorder);
        }

        public static RectTransform Box(Transform parent, string name, float x, float y, float width, float height)
        {
            var child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(parent, false);
            var rect = (RectTransform)child.transform;
            rect.anchorMin = rect.anchorMax = new(0f, 1f);
            rect.pivot = new(0f, 1f);
            rect.anchoredPosition = new(x, -y);
            rect.sizeDelta = new(width, height);
            return rect;
        }

        public RawImage Picture(Transform parent, string name, Texture texture, float x, float y, float width, float height)
        {
            var image = Box(parent, name, x, y, width, height).gameObject.AddComponent<RawImage>();
            image.texture = texture;
            image.raycastTarget = false;
            return image;
        }

        public Image Frame(Transform parent, string name, float x, float y, float width, float height)
        {
            var image = Box(parent, name, x, y, width, height).gameObject.AddComponent<Image>();
            image.sprite = _frameSprite;
            image.type = Image.Type.Sliced;
            image.raycastTarget = false;
            return image;
        }

        public Image Panel(Transform parent, string name, float x, float y, float width, float height)
        {
            var image = Box(parent, name, x, y, width, height).gameObject.AddComponent<Image>();
            image.sprite = _panelSprite;
            image.type = Image.Type.Sliced;
            image.raycastTarget = true;
            return image;
        }

        public TMP_Text Label(Transform parent, string name, string text, float x, float y, float width,
            float height, int size)
        {
            var label = Box(parent, name, x, y, width, height).gameObject.AddComponent<TextMeshProUGUI>();
            label.font = _assets.Font;
            label.fontSize = size;
            label.text = text;
            label.color = Paper;
            label.richText = false;
            label.raycastTarget = false;
            label.overflowMode = TextOverflowModes.Overflow;
            return label;
        }

        public Button Button(Transform parent, string name, string text, float x, float y, float width, float height, Action callback)
        {
            var image = Frame(parent, name, x, y, width, height);
            image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = new(0.82f, 0.88f, 0.82f);
            colors.highlightedColor = new(1.15f, 0.96f, 0.62f);
            colors.pressedColor = new(0.76f, 0.56f, 0.28f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new(0.54f, 0.60f, 0.57f, 0.90f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            button.navigation = new() { mode = Navigation.Mode.None };
            var label = Label(image.transform, "Label", text, 24f, 0f, width - 48f, height, 26);
            label.font = _assets.MenuFont;
            label.alignment = TextAlignmentOptions.Center;
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

        public void Volume(Transform parent, string name, string label, float y, float value, Action<float> changed)
        {
            var title = Label(parent, name + "Label", label, 90f, y, 180f, 64f, 28);
            title.font = _assets.MenuFont;
            title.alignment = TextAlignmentOptions.Left;
            var percentage = Label(parent, name + "Value", Mathf.RoundToInt(value * 100f) + "%", 850f, y,
                120f, 64f, 26);
            percentage.font = _assets.MenuFont;
            percentage.alignment = TextAlignmentOptions.Right;
            var root = Box(parent, name, 300f, y, 520f, 64f);
            var hitArea = root.gameObject.AddComponent<Image>();
            hitArea.color = Color.clear;

            var trackFrame = Frame(root, "TrackFrame", 0f, 15f, 520f, 34f);
            trackFrame.color = new(0.72f, 0.77f, 0.64f, 0.92f);
            var rail = Box(trackFrame.transform, "Rail", 22f, 15f, 476f, 4f).gameObject.AddComponent<Image>();
            rail.color = new(0.68f, 0.73f, 0.55f, 0.92f);
            rail.raycastTarget = false;

            var handleArea = Box(root, "HandleArea", 22f, 12f, 476f, 40f);
            var handleImage = Frame(handleArea, "Handle", 0f, 0f, 40f, 40f);
            var handle = handleImage.rectTransform;
            handle.anchorMin = handle.anchorMax = new(0f, 0.5f);
            handle.pivot = new(0.5f, 0.5f);
            handle.anchoredPosition = Vector2.zero;
            handle.sizeDelta = new(40f, 0f);
            handleImage.color = new(0.90f, 0.82f, 0.54f);

            var slider = root.gameObject.AddComponent<Slider>();
            slider.handleRect = handle;
            slider.targetGraphic = handleImage;
            slider.navigation = new() { mode = Navigation.Mode.None };
            var colors = slider.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new(1.15f, 1.08f, 0.82f);
            colors.pressedColor = new(0.82f, 0.70f, 0.42f);
            colors.selectedColor = colors.highlightedColor;
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            slider.colors = colors;
            slider.SetValueWithoutNotify(value);
            slider.onValueChanged.AddListener(volume =>
            {
                percentage.text = Mathf.RoundToInt(volume * 100f) + "%";
                changed(volume);
            });
        }
    }
}