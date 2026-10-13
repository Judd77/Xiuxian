using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WenXinJie.Presentation
{
    internal sealed class MainMenuView
    {
        private readonly Button _continue;
        private readonly Button _load;
        private readonly Button _loadAutomatic;
        private readonly TMP_Text _notice;
        private readonly GameObject _root;
        private readonly TMP_Text _progress;

        public MainMenuView(Transform parent, StoryUiFactory ui, TMP_FontAsset titleFont, TMP_FontAsset menuFont,
            IStoryCommands commands, Action openSettings, Action openCredits, bool isDemo, Shader menuParticleShader)
        {
            var root = StoryUiFactory.Box(parent, "MainMenu", 0f, 0f, 1920f, 1080f);
            _root = root.gameObject;
            var veil = _root.AddComponent<Image>();
            veil.color = new(0.025f, 0.075f, 0.08f, 0.30f);
            MainMenuAtmosphere.Create(root, menuParticleShader);

            var title = ui.Label(root, "MainMenuTitle", "问心劫", 118f, 48f, 720f, 166f, 126);
            title.font = titleFont;
            title.color = new(0.045f, 0.12f, 0.115f, 0.98f);
            title.alignment = TextAlignmentOptions.Left;
            var titleShadow = title.gameObject.AddComponent<Shadow>();
            titleShadow.effectColor = new(0.78f, 0.82f, 0.68f, 0.32f);
            titleShadow.effectDistance = new(2f, -2f);

            var subtitle = ui.Label(root, "MainMenuSubtitle", isDemo ? "修仙悬疑 AVG · 免费试玩" : "修仙悬疑 AVG",
                154f, 242f, 620f, 54f, 32);
            subtitle.font = menuFont;
            subtitle.color = new(0.06f, 0.15f, 0.13f);
            var chapter = ui.Label(root, "MainMenuChapter", "旧号疑云", 154f, 310f, 420f, 42f, 23);
            chapter.font = menuFont;
            chapter.color = new(0.09f, 0.18f, 0.16f);

            CreateButton(ui, root, menuFont, "StartGame", "开始游戏", 384f, commands.StartNewGame);
            _continue = CreateButton(ui, root, menuFont, "ContinueGame", "继续游戏", 462f,
                commands.ContinueGame);
            _load = CreateButton(ui, root, menuFont, "LoadGame", "读取手动存档", 540f,
                commands.LoadGameFromMenu);
            _loadAutomatic = CreateButton(ui, root, menuFont, "LoadAutomaticGame", "恢复自动进度", 618f,
                commands.LoadAutomaticGameFromMenu);
            CreateButton(ui, root, menuFont, "MainMenuSettings", "设置", 696f, openSettings);
            CreateButton(ui, root, menuFont, "MainMenuCredits", "鸣谢", 774f, openCredits);
            CreateButton(ui, root, menuFont, "QuitGame", "退出游戏", 852f, commands.QuitGame);

            ui.Frame(root, "MainMenuNoticeFrame", 148f, 940f, 850f, 66f);
            _notice = ui.Label(root, "MainMenuNotice", string.Empty, 174f, 950f, 798f, 46f, 21);
            _notice.font = menuFont;
            _notice.color = new(0.86f, 0.88f, 0.82f);

            var progressFrame = ui.Frame(root, "MenuProgressFrame", 640f, 400f, 1040f, 480f);
            progressFrame.color = new(0.86f, 0.94f, 0.89f);
            _progress = ui.Label(root, "MenuProgress", string.Empty, 684f, 428f, 952f, 424f, 24);
            _progress.font = menuFont;
            _progress.lineSpacing = 4f;
            _progress.color = new(0.96f, 0.95f, 0.85f);
        }

        public bool IsOpen => _root.activeSelf;

        public void Show(bool canContinue, bool canLoad, bool canLoadAutomatic, string notice, string progress)
        {
            _continue.interactable = canContinue;
            _load.interactable = canLoad;
            _loadAutomatic.interactable = canLoadAutomatic;
            _notice.text = notice;
            _progress.text = progress;
            _root.SetActive(true);
        }

        public void Hide()
        {
            _root.SetActive(false);
        }

        public void ShowNotice(string message)
        {
            _notice.text = message;
        }

        private static Button CreateButton(StoryUiFactory ui, Transform parent, TMP_FontAsset font,
            string objectName,
            string text, float y, Action callback)
        {
            var button = ui.Button(parent, objectName, text, 148f, y, 430f, 70f, callback);
            var label = button.GetComponentInChildren<TMP_Text>();
            label.font = font;
            label.fontSize = 29;
            label.color = new(0.94f, 0.95f, 0.90f);
            label.alignment = TextAlignmentOptions.Left;
            return button;
        }
    }
}
