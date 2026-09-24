using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WenXinJie.Presentation
{
    internal sealed class ClueJournalView
    {
        private const int EntryCount = 3;
        private readonly TMP_Text _category;
        private readonly TMP_Text _count;
        private readonly TMP_Text _description;
        private readonly Button[] _entryButtons = new Button[EntryCount];

        private readonly GameObject _root;
        private readonly TMP_Text _title;
        private int _selectedIndex;
        private ClueJournalState _state = new();

        public ClueJournalView(Transform parent, StoryUiFactory ui, TMP_FontAsset font)
        {
            var root = StoryUiFactory.Box(parent, "ClueJournalOverlay", 0f, 0f, 1920f, 1080f);
            _root = root.gameObject;
            var background = _root.AddComponent<Image>();
            background.color = new(0.045f, 0.125f, 0.115f, 1f);

            var heading = ui.Label(root, "ClueJournalHeading", "见闻 · 线索簿", 180f, 100f, 1100f, 90f, 44);
            heading.font = font;
            _count = ui.Label(root, "ClueJournalCount", "0 / 3", 1380f, 118f, 360f, 60f, 27);
            _count.font = font;
            _count.alignment = TextAlignmentOptions.Right;
            _count.color = new(0.84f, 0.76f, 0.52f);

            for (var index = 0; index < _entryButtons.Length; index++)
            {
                var entryIndex = index;
                _entryButtons[index] = ui.Button(root, "ClueEntry" + index, string.Empty,
                    180f, 270f + index * 152f, 420f, 116f, () => Select(entryIndex));
                var label = _entryButtons[index].GetComponentInChildren<TMP_Text>();
                label.alignment = TextAlignmentOptions.Left;
                label.fontSize = 28;
            }

            var detailFrame = ui.Frame(root, "ClueDetailFrame", 660f, 250f, 1080f, 560f);
            detailFrame.color = new(0.82f, 0.88f, 0.82f);
            _title = ui.Label(root, "ClueDetailTitle", string.Empty, 720f, 292f, 930f, 70f, 38);
            _title.font = font;
            _title.color = new(0.94f, 0.80f, 0.50f);
            _category = ui.Label(root, "ClueDetailStatus", string.Empty, 720f, 370f, 930f, 48f, 24);
            _category.font = font;
            _category.color = new(0.68f, 0.78f, 0.73f);
            _description = ui.Label(root, "ClueDetailBody", string.Empty, 720f, 446f, 940f, 280f, 29);
            _description.font = font;
            _description.color = new(0.92f, 0.91f, 0.82f);

            ui.Button(root, "ClueJournalClose", "返回", 1480f, 870f, 260f, 64f, Hide);
            _root.SetActive(false);
        }

        public bool IsOpen => _root.activeSelf;

        public void Render(ClueJournalState state)
        {
            _state = state ?? new();
            var unlocked = 0;
            for (var index = 0; index < _entryButtons.Length; index++)
            {
                var visible = index < _state.Entries.Length;
                _entryButtons[index].gameObject.SetActive(visible);
                if (!visible)
                {
                    continue;
                }

                var entry = _state.Entries[index];
                if (entry.Unlocked)
                {
                    unlocked++;
                }

                var label = _entryButtons[index].GetComponentInChildren<TMP_Text>();
                label.text = entry.Title + "\n" + (entry.Unlocked ? "已收录" : "未发现");
                label.color = entry.Unlocked
                    ? new(0.96f, 0.94f, 0.82f)
                    : new(0.68f, 0.72f, 0.68f);
            }

            _count.text = unlocked + " / " + _state.Entries.Length;
            if (_state.Entries.Length == 0)
            {
                ClearDetails();
                return;
            }

            _selectedIndex = FindSelection();
            Select(_selectedIndex);
        }

        public void Show()
        {
            _root.SetActive(true);
            Select(_selectedIndex);
        }

        public void Hide()
        {
            _root.SetActive(false);
        }

        private void Select(int index)
        {
            if (index < 0 || index >= _state.Entries.Length)
            {
                return;
            }

            _selectedIndex = index;
            var entry = _state.Entries[index];
            _title.text = entry.Title;
            _category.text = entry.Unlocked ? entry.Category + " · 已收录" : "尚未收录";
            _description.text = entry.Unlocked
                ? entry.Description
                : "尚未发现与此相关的确证。";
        }

        private int FindSelection()
        {
            if (_selectedIndex < _state.Entries.Length && _state.Entries[_selectedIndex].Unlocked)
            {
                return _selectedIndex;
            }

            for (var index = 0; index < _state.Entries.Length; index++)
            {
                if (_state.Entries[index].Unlocked)
                {
                    return index;
                }
            }

            return 0;
        }

        private void ClearDetails()
        {
            _title.text = string.Empty;
            _category.text = string.Empty;
            _description.text = string.Empty;
        }
    }
}