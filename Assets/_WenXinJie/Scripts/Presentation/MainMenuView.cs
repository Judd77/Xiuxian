using System;
using UnityEngine;
using UnityEngine.UI;

namespace WenXinJie.Presentation
{
    internal sealed class MainMenuView
    {
        private readonly GameObject _root;
        private readonly Button _continue;
        private readonly Button _load;
        private readonly Text _notice;

        public MainMenuView(Transform parent, StoryUiFactory ui, Font titleFont, Font menuFont,
            Action start, Action continueGame, Action load, Action settings, Action credits, Action quit)
        {
            RectTransform root = StoryUiFactory.Box(parent, "MainMenu", 0f, 0f, 1920f, 1080f);
            _root = root.gameObject;
            var veil = _root.AddComponent<Image>();
            veil.color = new Color(0.025f, 0.075f, 0.08f, 0.30f);
            MainMenuAtmosphere.Create(root);

            Text title = ui.Label(root, "MainMenuTitle", "问心劫", 118f, 48f, 720f, 166f, 126);
            title.font = titleFont;
            title.color = new Color(0.045f, 0.12f, 0.115f, 0.98f);
            title.alignment = TextAnchor.MiddleLeft;
            var titleShadow = title.gameObject.AddComponent<Shadow>();
            titleShadow.effectColor = new Color(0.78f, 0.82f, 0.68f, 0.32f);
            titleShadow.effectDistance = new Vector2(2f, -2f);

            Text subtitle = ui.Label(root, "MainMenuSubtitle", "三十日登仙", 154f, 242f, 420f, 54f, 32);
            subtitle.font = menuFont;
            subtitle.color = new Color(0.06f, 0.15f, 0.13f);
            Text chapter = ui.Label(root, "MainMenuChapter", "卷一 · 雨叩山门", 154f, 310f, 420f, 42f, 23);
            chapter.font = menuFont;
            chapter.color = new Color(0.09f, 0.18f, 0.16f);

            CreateButton(ui, root, menuFont, "StartGame", "开始游戏", 400f, start);
            _continue = CreateButton(ui, root, menuFont, "ContinueGame", "继续游戏", 490f, continueGame);
            _load = CreateButton(ui, root, menuFont, "LoadGame", "读取存档", 580f, load);
            CreateButton(ui, root, menuFont, "MainMenuSettings", "设置", 670f, settings);
            CreateButton(ui, root, menuFont, "MainMenuCredits", "鸣谢", 760f, credits);
            CreateButton(ui, root, menuFont, "QuitGame", "退出游戏", 850f, quit);

            _notice = ui.Label(root, "MainMenuNotice", string.Empty, 150f, 950f, 760f, 54f, 21);
            _notice.font = menuFont;
            _notice.color = new Color(0.86f, 0.88f, 0.82f);
        }

        public bool IsOpen => _root.activeSelf;

        public void Show(bool canContinue, bool canLoad, string notice)
        {
            _continue.interactable = canContinue;
            _load.interactable = canLoad;
            _notice.text = notice;
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

        private static Button CreateButton(StoryUiFactory ui, Transform parent, Font font, string name, string text,
            float y, Action callback)
        {
            Button button = ui.Button(parent, name, text, 148f, y, 430f, 70f, callback);
            Text label = button.GetComponentInChildren<Text>();
            label.font = font;
            label.fontSize = 29;
            label.color = new Color(0.94f, 0.95f, 0.90f);
            label.alignment = TextAnchor.MiddleLeft;
            return button;
        }

        private static void AddTextShadow(Text text, float distance)
        {
            var shadow = text.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.01f, 0.05f, 0.05f, 0.92f);
            shadow.effectDistance = new Vector2(distance, -distance);
        }
    }
}
