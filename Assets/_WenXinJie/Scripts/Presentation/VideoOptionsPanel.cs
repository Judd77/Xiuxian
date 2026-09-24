using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WenXinJie.UI;

namespace WenXinJie.Presentation
{
    internal sealed class VideoOptionsPanel
    {
        private static readonly Color ActiveText = new(0.96f, 0.96f, 0.88f);
        private static readonly Color DisabledText = new(0.58f, 0.61f, 0.56f);
        private readonly DisplayModeService _displayModes;
        private readonly Toggle _fullScreen;

        private readonly Button _nextResolution;
        private readonly Button _previousResolution;
        private readonly TMP_Text _resolutionTitle;
        private readonly TMP_Text _resolutionValue;
        private Image _fullScreenKnob;
        private TMP_Text _fullScreenValue;

        public VideoOptionsPanel(Transform parent, StoryUiFactory ui, TMP_FontAsset menuFont,
            DisplayModeService displayModes)
        {
            _displayModes = displayModes;
            var heading = ui.Label(parent, "DisplaySettingsHeading", "显示", 90f, 370f, 300f, 54f, 25);
            heading.font = menuFont;
            heading.color = new(0.72f, 0.76f, 0.62f);

            var fullScreenTitle = ui.Label(parent, "FullScreenLabel", "全屏显示", 90f, 430f, 260f, 56f, 28);
            fullScreenTitle.font = menuFont;
            fullScreenTitle.alignment = TextAlignmentOptions.Left;
            _fullScreen = CreateFullScreenToggle(parent, ui, menuFont);

            _resolutionTitle = ui.Label(parent, "WindowResolutionLabel", "窗口分辨率", 90f, 520f, 260f, 56f, 28);
            _resolutionTitle.font = menuFont;
            _resolutionTitle.alignment = TextAlignmentOptions.Left;
            _previousResolution = CreateArrowButton(ui, parent, "ResolutionPrevious", "<", 470f,
                SelectPreviousResolution);
            var valueFrame = ui.Frame(parent, "WindowResolutionFrame", 550f, 520f, 300f, 56f);
            _resolutionValue = ui.Label(valueFrame.transform, "WindowResolutionValue", string.Empty, 20f, 0f,
                260f, 56f, 25);
            _resolutionValue.font = menuFont;
            _resolutionValue.alignment = TextAlignmentOptions.Center;
            _nextResolution = CreateArrowButton(ui, parent, "ResolutionNext", ">", 870f,
                SelectNextResolution);

            Refresh();
        }

        private Toggle CreateFullScreenToggle(Transform parent, StoryUiFactory ui, TMP_FontAsset menuFont)
        {
            var root = StoryUiFactory.Box(parent, "FullScreenToggle", 700f, 430f, 250f, 56f);
            var hitArea = root.gameObject.AddComponent<Image>();
            hitArea.color = Color.clear;
            var track = ui.Frame(root, "Track", 0f, 8f, 120f, 40f);
            track.color = new(0.72f, 0.77f, 0.64f, 0.92f);
            _fullScreenKnob = StoryUiFactory.Box(track.transform, "Knob", 5f, 5f, 30f, 30f)
                .gameObject.AddComponent<Image>();
            _fullScreenKnob.raycastTarget = false;
            _fullScreenValue = ui.Label(root, "Value", string.Empty, 140f, 0f, 110f, 56f, 25);
            _fullScreenValue.font = menuFont;
            _fullScreenValue.alignment = TextAlignmentOptions.Left;

            var toggle = root.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = hitArea;
            toggle.graphic = null;
            toggle.navigation = new() { mode = Navigation.Mode.None };
            toggle.SetIsOnWithoutNotify(_displayModes.IsFullScreen);
            toggle.onValueChanged.AddListener(SetFullScreen);
            return toggle;
        }

        private static Button CreateArrowButton(StoryUiFactory ui, Transform parent, string name, string label,
            float x, Action callback)
        {
            var button = ui.Button(parent, name, label, x, 520f, 64f, 56f, callback);
            button.GetComponentInChildren<TMP_Text>().fontSize = 24;
            return button;
        }

        private void SetFullScreen(bool enabled)
        {
            _displayModes.SetFullScreen(enabled);
            Refresh();
        }

        private void SelectPreviousResolution()
        {
            var count = _displayModes.WindowResolutions.Count;
            var index = (_displayModes.SelectedResolutionIndex - 1 + count) % count;
            _displayModes.SetWindowResolution(index);
            Refresh();
        }

        private void SelectNextResolution()
        {
            var count = _displayModes.WindowResolutions.Count;
            var index = (_displayModes.SelectedResolutionIndex + 1) % count;
            _displayModes.SetWindowResolution(index);
            Refresh();
        }

        private void Refresh()
        {
            var isFullScreen = _displayModes.IsFullScreen;
            _fullScreen.SetIsOnWithoutNotify(isFullScreen);
            _fullScreenKnob.rectTransform.anchoredPosition = new(isFullScreen ? 85f : 5f, -5f);
            _fullScreenKnob.color = isFullScreen
                ? new(0.74f, 0.67f, 0.39f)
                : new(0.37f, 0.43f, 0.40f);
            _fullScreenValue.text = isFullScreen ? "开启" : "关闭";

            var resolutionEnabled = !isFullScreen && _displayModes.WindowResolutions.Count > 1;
            _previousResolution.interactable = resolutionEnabled;
            _nextResolution.interactable = resolutionEnabled;
            _resolutionTitle.color = isFullScreen ? DisabledText : ActiveText;
            _resolutionValue.color = isFullScreen ? DisabledText : ActiveText;
            _resolutionValue.text = _displayModes.WindowResolutions[_displayModes.SelectedResolutionIndex].Label;
        }
    }
}