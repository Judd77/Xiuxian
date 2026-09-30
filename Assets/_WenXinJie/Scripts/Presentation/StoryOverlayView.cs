using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WenXinJie.UI;

namespace WenXinJie.Presentation
{
    internal sealed class StoryOverlayView
    {
        private const string CreditsText =
            "场景与角色立绘由原创提示词驱动，使用 OpenAI 图像生成工具制作，"
            + "并经人工筛选、透明化、裁切与调色；提示词及加工记录已归档。\n\n"
            + "早期美术研究参考：髡残《春景图》（1666，CC0）。"
            + "当前主要美术均为项目专用生成内容，仅参考传统水墨的设色留白与云雾层次，"
            + "不临摹具体作品，不使用现有游戏角色、标志、界面或拆包资源。\n\n"
            + "音乐：Tozan《Asianoriental2》（CC0）\n"
            + "环境音：Ylmir《Rain (loopable)》（CC0）\n\n"
            + "免责声明：本作无意侵犯著作权、商标权及其他合法权益。"
            + "如权利人认为相关内容涉嫌侵权，请通过项目发布页联系我们；核实后将立即下架并替换。";

        private readonly GameObject _credits;
        private readonly ClueJournalView _journal;
        private readonly DialogueReader _reader;
        private readonly GameObject _settings;
        private readonly RawImage _historyBackdrop;

        public StoryOverlayView(Transform parent, StoryUiFactory ui, TMP_FontAsset menuFont,
            DialogueReader reader,
            GameAudio audio, DisplayModeService displayModes)
        {
            _reader = reader;
            _journal = new(parent, ui, menuFont);

            var history = ui.ArchiveOverlay(parent, "HistoryOverlay", "前尘", "已读记事",
                out _historyBackdrop);
            var historyCount = ui.Label(history, "HistoryCount", string.Empty,
                1360f, 178f, 350f, 52f, 28);
            historyCount.font = menuFont;
            historyCount.color = new(0.86f, 0.78f, 0.55f);
            historyCount.alignment = TextAlignmentOptions.Right;
            var historyList = ui.Frame(history, "HistoryListFrame", 192f, 290f, 500f, 532f);
            historyList.color = new(0.86f, 0.94f, 0.89f);
            var historyCard = ui.Frame(history, "HistoryPage", 724f, 290f, 1004f, 532f);
            historyCard.color = new(0.91f, 0.96f, 0.92f);
            var historyItems = new Button[5];
            var historyMarks = new Image[5];
            for (var index = 0; index < historyItems.Length; index++)
            {
                var slot = index;
                historyItems[index] = ui.Button(history, "HistoryItem" + index, string.Empty,
                    218f, 314f + index * 96f, 448f, 80f,
                    () => reader.SelectVisibleHistory(slot));
                var itemLabel = historyItems[index].GetComponentInChildren<TMP_Text>();
                itemLabel.font = ui.Font;
                itemLabel.fontSize = 22;
                itemLabel.overflowMode = TextOverflowModes.Truncate;
                itemLabel.alignment = TextAlignmentOptions.Left;
                historyMarks[index] = StoryUiFactory.Rule(history, "HistorySelected" + index,
                    228f, 328f + index * 96f, 4f, 50f,
                    new(0.95f, 0.75f, 0.38f, 1f));
            }

            var pageLabel = ui.Label(history, "HistoryPageLabel", "本周目所见",
                780f, 332f, 800f, 45f, 23);
            pageLabel.font = menuFont;
            pageLabel.color = new(0.69f, 0.80f, 0.74f);
            StoryUiFactory.Rule(history, "HistoryPageRule", 780f, 392f, 888f, 2f,
                new(0.72f, 0.63f, 0.43f, 0.63f));
            var record = ui.Label(history, "HistoryText", string.Empty,
                780f, 432f, 878f, 324f, 35);
            record.font = ui.Font;
            record.color = new(0.96f, 0.95f, 0.85f);
            record.lineSpacing = 10f;
            record.overflowMode = TextOverflowModes.Truncate;
            var previous = ui.Button(history, "HistoryPrevious", "上一条", 192f, 880f, 206f, 64f,
                reader.Previous);
            var next = ui.Button(history, "HistoryNext", "下一条", 420f, 880f, 206f, 64f,
                reader.Next);
            ui.Button(history, "HistoryClose", "继续", 1518f, 880f, 210f, 64f,
                reader.CloseHistory);
            reader.ConfigureHistory(history.gameObject, record, historyCount, previous, next,
                historyItems, historyMarks);

            var settingsHeight = displayModes.SupportsWindowMode ? 820f : 560f;
            var settings = CreateModal(parent, ui, menuFont, "SettingsModal", "设置", 1040f,
                settingsHeight);
            _settings = settings.Root;
            var settingsHint = ui.Label(settings.Content, "SettingsHint", "声音", 90f, 118f, 300f, 54f, 25);
            settingsHint.font = menuFont;
            settingsHint.color = new(0.72f, 0.76f, 0.62f);
            ui.Volume(settings.Content, "MusicVolume", "乐声", 165f, audio.MusicVolume, audio.SetMusicVolume);
            ui.Volume(settings.Content, "RainVolume", "雨声", 275f, audio.AmbienceVolume,
                audio.SetAmbienceVolume);
            if (displayModes.SupportsWindowMode)
            {
                _ = new VideoOptionsPanel(settings.Content, ui, menuFont, displayModes);
            }

            var closeY = displayModes.SupportsWindowMode ? 710f : 455f;
            ui.Button(settings.Content, "SettingsClose", "关闭", 780f, closeY, 170f, 58f, CloseAll);
            _settings.SetActive(false);

            var credits = CreateModal(
                parent,
                ui,
                menuFont,
                "CreditsModal",
                "鸣谢",
                1240f,
                720f);
            _credits = credits.Root;
            var creditsBody = ui.Label(credits.Content, "CreditsText", CreditsText, 60f, 130f, 1120f, 470f, 22);
            creditsBody.lineSpacing = 12f;
            creditsBody.alignment = TextAlignmentOptions.TopLeft;
            ui.Button(credits.Content, "CreditsClose", "关闭", 980f, 620f, 180f, 58f, CloseAll);
            _credits.SetActive(false);
        }

        public bool IsModalOpen => _settings.activeSelf || _credits.activeSelf || _journal.IsOpen;

        public void OpenSettings()
        {
            Open(_settings);
        }

        public void OpenCredits()
        {
            Open(_credits);
        }

        public void OpenJournal()
        {
            _reader.Complete();
            _journal.Show();
        }

        public void RenderJournal(ClueJournalState state)
        {
            _journal.Render(state);
        }

        public void SetBackdrop(Texture2D texture)
        {
            _historyBackdrop.texture = texture;
            _journal.SetBackdrop(texture);
        }

        public void CloseAll()
        {
            _settings.SetActive(false);
            _credits.SetActive(false);
            _journal.Hide();
            PlayerPrefs.Save();
        }

        private static ModalWindow CreateModal(Transform parent, StoryUiFactory ui, TMP_FontAsset menuFont,
            string objectName, string title, float width, float height)
        {
            var root = StoryUiFactory.Box(parent, objectName, 0f, 0f, 1920f, 1080f);
            var blocker = root.gameObject.AddComponent<Image>();
            blocker.color = new(0.01f, 0.035f, 0.04f, 0.72f);
            blocker.raycastTarget = true;

            var x = (1920f - width) * 0.5f;
            var y = (1080f - height) * 0.5f;
            var panelFill = StoryUiFactory.Box(root, objectName + "Fill", x + 8f, y + 8f, width - 16f,
                height - 16f).gameObject.AddComponent<Image>();
            panelFill.color = new(0.035f, 0.085f, 0.095f, 0.97f);
            panelFill.raycastTarget = false;
            var panel = ui.Panel(root, objectName + "Panel", x, y, width, height);
            var heading = ui.Label(panel.transform, "Heading", title, 70f, 38f, width - 140f, 72f, 43);
            heading.font = menuFont;
            heading.color = new(0.94f, 0.89f, 0.67f);
            heading.alignment = TextAlignmentOptions.Left;
            return new(root.gameObject, panel.rectTransform);
        }

        private void Open(GameObject window)
        {
            _reader.Complete();
            _settings.SetActive(false);
            _credits.SetActive(false);
            window.SetActive(true);
            window.transform.SetAsLastSibling();
        }

        private sealed class ModalWindow
        {
            public ModalWindow(GameObject root, RectTransform content)
            {
                Root = root;
                Content = content;
            }

            public GameObject Root { get; }
            public RectTransform Content { get; }
        }
    }
}
