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

        public MainMenuView(Transform parent, StoryUiFactory ui, Action start, Action continueGame, Action load,
            Action settings, Action credits, Action quit)
        {
            RectTransform root = StoryUiFactory.Box(parent, "MainMenu", 0f, 0f, 1920f, 1080f);
            _root = root.gameObject;
            var veil = _root.AddComponent<Image>();
            veil.color = new Color(0.025f, 0.075f, 0.08f, 0.34f);

            Text title = ui.Label(root, "MainMenuTitle", "问心劫", 138f, 86f, 700f, 164f, 96);
            title.color = new Color(0.94f, 0.80f, 0.48f);
            title.alignment = TextAnchor.MiddleLeft;
            var titleOutline = title.gameObject.AddComponent<Outline>();
            titleOutline.effectColor = new Color(0.02f, 0.08f, 0.08f, 0.95f);
            titleOutline.effectDistance = new Vector2(3f, -3f);
            Text subtitle = ui.Label(root, "MainMenuSubtitle", "三十日登仙", 148f, 252f, 500f, 54f, 30);
            subtitle.color = new Color(0.86f, 0.90f, 0.86f);
            var subtitleOutline = subtitle.gameObject.AddComponent<Outline>();
            subtitleOutline.effectColor = new Color(0.02f, 0.08f, 0.08f, 0.9f);
            subtitleOutline.effectDistance = new Vector2(2f, -2f);
            ui.Label(root, "MainMenuChapter", "卷一 · 雨叩山门", 150f, 316f, 500f, 42f, 22)
                .color = new Color(0.72f, 0.80f, 0.78f);

            CreateButton(ui, root, "StartGame", "开始游戏", 400f, start);
            _continue = CreateButton(ui, root, "ContinueGame", "继续游戏", 490f, continueGame);
            _load = CreateButton(ui, root, "LoadGame", "读取存档", 580f, load);
            CreateButton(ui, root, "MainMenuSettings", "设置", 670f, settings);
            CreateButton(ui, root, "MainMenuCredits", "鸣谢", 760f, credits);
            CreateButton(ui, root, "QuitGame", "退出游戏", 850f, quit);

            _notice = ui.Label(root, "MainMenuNotice", string.Empty, 150f, 950f, 760f, 54f, 21);
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

        private static Button CreateButton(StoryUiFactory ui, Transform parent, string name, string text, float y,
            Action callback)
        {
            Button button = ui.Button(parent, name, text, 148f, y, 430f, 70f, callback);
            button.GetComponentInChildren<Text>().fontSize = 30;
            button.GetComponentInChildren<Text>().alignment = TextAnchor.MiddleLeft;
            return button;
        }
    }
}
