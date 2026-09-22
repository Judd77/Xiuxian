using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace WenXinJie.Presentation
{
    internal sealed class StoryUiFactory
    {
        private static readonly Color Paper = new Color(0.96f, 0.96f, 0.88f);
        private readonly StoryMediaCatalog _media;

        public StoryUiFactory(StoryMediaCatalog media)
        {
            _media = media;
        }

        public static RectTransform Box(Transform parent, string name, float x, float y, float width, float height)
        {
            var child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(parent, false);
            var rect = (RectTransform)child.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
            return rect;
        }

        public RawImage Picture(Transform parent, string name, Texture texture, float x, float y, float width, float height)
        {
            var image = Box(parent, name, x, y, width, height).gameObject.AddComponent<RawImage>();
            image.texture = texture;
            image.raycastTarget = false;
            return image;
        }

        public Text Label(Transform parent, string name, string text, float x, float y, float width, float height, int size)
        {
            var label = Box(parent, name, x, y, width, height).gameObject.AddComponent<Text>();
            label.font = _media.Font;
            label.fontSize = size;
            label.text = text;
            label.color = Paper;
            label.supportRichText = false;
            label.raycastTarget = false;
            label.lineSpacing = 1.15f;
            return label;
        }

        public Button Button(Transform parent, string name, string text, float x, float y, float width, float height, Action callback)
        {
            RawImage image = Picture(parent, name, _media.Choice, x, y, width, height);
            image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(1.25f, 1.25f, 1.1f);
            colors.pressedColor = new Color(0.7f, 0.85f, 0.8f);
            colors.disabledColor = new Color(0.6f, 0.66f, 0.62f);
            button.colors = colors;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            Text label = Label(image.transform, "Label", text, 24f, 0f, width - 48f, height, 26);
            label.alignment = TextAnchor.MiddleCenter;
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
            Label(parent, name + "Label", label, 300f, y, 300f, 64f, 30);
            Text percentage = Label(parent, name + "Value", Mathf.RoundToInt(value * 100f) + "%", 1470f, y, 200f, 64f, 28);
            RectTransform root = Box(parent, name, 620f, y, 780f, 64f);
            var hitArea = root.gameObject.AddComponent<Image>();
            hitArea.color = new Color(1f, 1f, 1f, 0.02f);
            var track = Box(root, "Track", 0f, 28f, 780f, 8f).gameObject.AddComponent<Image>();
            track.color = new Color(0.4f, 0.6f, 0.5f);
            track.raycastTarget = false;
            RectTransform handleArea = Box(root, "HandleArea", 14f, 14f, 752f, 36f);
            RectTransform handle = Box(handleArea, "Handle", 0f, 0f, 28f, 0f);
            handle.pivot = new Vector2(0.5f, 0.5f);
            var handleImage = handle.gameObject.AddComponent<Image>();
            handleImage.color = Paper;
            var slider = root.gameObject.AddComponent<Slider>();
            slider.handleRect = handle;
            slider.targetGraphic = handleImage;
            slider.SetValueWithoutNotify(value);
            slider.onValueChanged.AddListener(volume =>
            {
                percentage.text = Mathf.RoundToInt(volume * 100f) + "%";
                changed(volume);
            });
        }
    }
}
