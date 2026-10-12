using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WenXinJie.Presentation
{
    internal sealed class ClueJournalView
    {
        private const int EntryCount = 6;
        private readonly TMP_Text _category;
        private readonly TMP_Text _count;
        private readonly TMP_Text _description;
        private readonly Button[] _entryButtons = new Button[EntryCount];
        private readonly Image[] _selectionMarks = new Image[EntryCount];
        private readonly Button _previousPage;
        private readonly Button _nextPage;
        private readonly TMP_Text _pageCount;
        private readonly Button _nextUnread;
        private readonly TMP_Text _nextUnreadLabel;
        private readonly RawImage _backdrop;

        private readonly GameObject _root;
        private readonly TMP_Text _title;
        private int _selectedIndex;
        private int _page;
        private ClueJournalState _state = new();
        private readonly List<int> _unread = new();
        private bool _hasState;

        public event Action<int> UnreadCountChanged;

        public ClueJournalView(Transform parent, StoryUiFactory ui, TMP_FontAsset font)
        {
            var root = ui.ArchiveOverlay(parent, "ClueJournalOverlay", "见闻", "线索簿",
                out _backdrop);
            _root = root.gameObject;
            _count = ui.Label(root, "ClueJournalCount", "已收录 0 / 0",
                1320f, 178f, 390f, 52f, 28);
            _count.font = font;
            _count.alignment = TextAlignmentOptions.Right;
            _count.color = new(0.86f, 0.78f, 0.55f);
            _count.enableAutoSizing = true;
            _count.fontSizeMin = 20;
            _count.fontSizeMax = 28;

            var listFrame = ui.Frame(root, "ClueListFrame", 192f, 290f, 500f, 532f);
            listFrame.color = new(0.86f, 0.94f, 0.89f);
            var detailFrame = ui.Frame(root, "ClueDetailFrame", 724f, 290f, 1004f, 532f);
            detailFrame.color = new(0.91f, 0.96f, 0.92f);

            for (var index = 0; index < _entryButtons.Length; index++)
            {
                var entryIndex = index;
                _entryButtons[index] = ui.Button(root, "ClueEntry" + index, string.Empty,
                    218f, 314f + index * 80f, 448f, 72f,
                    () => Select(_page * EntryCount + entryIndex));
                var label = _entryButtons[index].GetComponentInChildren<TMP_Text>();
                label.alignment = TextAlignmentOptions.Left;
                label.font = font;
                label.fontSize = 25;
                _selectionMarks[index] = StoryUiFactory.Rule(root, "ClueSelected" + index,
                    228f, 326f + index * 80f, 4f, 46f,
                    new(0.95f, 0.75f, 0.38f, 1f));
            }

            _previousPage = ui.Button(root, "CluePreviousPage", "上一页",
                192f, 880f, 182f, 64f,
                () => SetPage(_page - 1));
            _nextPage = ui.Button(root, "ClueNextPage", "下一页",
                396f, 880f, 182f, 64f,
                () => SetPage(_page + 1));
            _pageCount = ui.Label(root, "CluePageCount", string.Empty,
                604f, 888f, 310f, 46f, 23);
            _pageCount.font = font;
            _pageCount.color = new(0.71f, 0.80f, 0.75f);

            _nextUnread = ui.Button(root, "ClueNextUnread", "暂无未读",
                984f, 880f, 310f, 64f, ShowNextUnread);
            _nextUnreadLabel = _nextUnread.GetComponentInChildren<TMP_Text>();
            _nextUnreadLabel.fontSize = 24;

            _title = ui.Label(root, "ClueDetailTitle", string.Empty,
                780f, 334f, 900f, 70f, 40);
            _title.font = font;
            _title.color = new(0.96f, 0.84f, 0.55f);
            _category = ui.Label(root, "ClueDetailStatus", string.Empty,
                780f, 416f, 870f, 44f, 24);
            _category.font = font;
            _category.color = new(0.70f, 0.82f, 0.76f);
            StoryUiFactory.Rule(root, "ClueDetailRule", 780f, 470f, 888f, 2f,
                new(0.72f, 0.63f, 0.43f, 0.63f));
            _description = ui.Label(root, "ClueDetailBody", string.Empty,
                780f, 510f, 878f, 264f, 31);
            _description.font = ui.Font;
            _description.color = new(0.95f, 0.94f, 0.85f);
            _description.lineSpacing = 8f;
            _description.overflowMode = TextOverflowModes.Truncate;

            ui.Button(root, "ClueJournalClose", "继续",
                1518f, 880f, 210f, 64f, Hide);
            _root.SetActive(false);
        }

        public bool IsOpen => _root.activeSelf;

        public void SetBackdrop(Texture2D texture)
        {
            _backdrop.texture = texture;
        }

        public void Render(ClueJournalState state)
        {
            var current = state ?? new();
            _unread.RemoveAll(index => index >= current.Entries.Length || !current.Entries[index].Unlocked);
            for (var index = 0; _hasState && index < current.Entries.Length; index++)
            {
                var entry = current.Entries[index];
                var previous = index < _state.Entries.Length ? _state.Entries[index] : null;
                if (entry.Unlocked && (previous == null || !previous.Unlocked
                    || entry.Description != previous.Description || entry.Category != previous.Category
                    || entry.Title != previous.Title))
                {
                    _unread.Remove(index);
                    _unread.Add(index);
                }
            }

            _state = current;
            _hasState = true;
            RefreshUnreadCount();
            if (_state.Entries.Length == 0)
            {
                ClearDetails();
                RenderPage();
                return;
            }

            _selectedIndex = FindSelection();
            _page = _selectedIndex / EntryCount;
            RenderPage();
            Select(_selectedIndex);
        }

        public void Show()
        {
            _root.SetActive(true);
            Select(_unread.Count > 0 ? _unread[_unread.Count - 1] : _selectedIndex);
        }

        public void Reset()
        {
            Hide();
            _state = new();
            _hasState = false;
            _selectedIndex = 0;
            _page = 0;
            _unread.Clear();
            RefreshUnreadCount();
        }

        private void ShowNextUnread()
        {
            if (_unread.Count > 0)
            {
                Select(_unread[_unread.Count - 1]);
            }
        }

        private void RefreshUnreadCount()
        {
            var unlocked = 0;
            foreach (var entry in _state.Entries)
            {
                if (entry.Unlocked) unlocked++;
            }

            _count.text = "已收录 " + unlocked + " / " + _state.Entries.Length
                + (_unread.Count > 0 ? " · 未读 " + _unread.Count : string.Empty);
            _nextUnread.interactable = _unread.Count > 0;
            _nextUnreadLabel.text = _unread.Count > 0 ? "下一条未读（" + _unread.Count + "）" : "暂无未读";
            UnreadCountChanged?.Invoke(_unread.Count);
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
            if (_page != index / EntryCount)
            {
                _page = index / EntryCount;
                RenderPage();
            }
            var entry = _state.Entries[index];
            _title.text = entry.Unlocked ? entry.Title : "尚未发现的线索";
            _category.text = entry.Unlocked ? entry.Category + " · 已收录" : "尚未收录";
            _description.text = entry.Unlocked
                ? entry.Description
                : "继续调查后，亲见的事、当事人的说法与术法所测会记在这里。尚未取得的内容不会提前显示。";
            // Hidden renders update the detail pane without counting it as read.
            if (IsOpen && _unread.Remove(index))
            {
                RefreshUnreadCount();
                RenderPage();
            }
            RenderSelection();
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

        private void SetPage(int page)
        {
            var pageCount = (_state.Entries.Length + EntryCount - 1) / EntryCount;
            if (page < 0 || page >= pageCount)
            {
                return;
            }

            _page = page;
            RenderPage();
            Select(page * EntryCount);
        }

        private void RenderPage()
        {
            for (var slot = 0; slot < EntryCount; slot++)
            {
                var index = _page * EntryCount + slot;
                var visible = index < _state.Entries.Length;
                _entryButtons[slot].gameObject.SetActive(visible);
                if (!visible)
                {
                    continue;
                }

                var entry = _state.Entries[index];
                var label = _entryButtons[slot].GetComponentInChildren<TMP_Text>();
                label.text = entry.Unlocked
                    ? entry.Title + (_unread.Contains(index) ? "\n未读" : "\n已收录")
                    : "尚未发现\n未收录";
            }

            _previousPage.gameObject.SetActive(_page > 0);
            _nextPage.gameObject.SetActive((_page + 1) * EntryCount < _state.Entries.Length);
            _pageCount.text = "第 " + (_page + 1) + " / "
                              + ((_state.Entries.Length + EntryCount - 1) / EntryCount) + " 页";
            RenderSelection();
        }

        private void RenderSelection()
        {
            for (var slot = 0; slot < EntryCount; slot++)
            {
                var index = _page * EntryCount + slot;
                var visible = index < _state.Entries.Length;
                var selected = visible && index == _selectedIndex;
                _selectionMarks[slot].gameObject.SetActive(selected);
                if (!visible)
                {
                    continue;
                }

                var entry = _state.Entries[index];
                _entryButtons[slot].GetComponentInChildren<TMP_Text>().color = selected
                    ? new(1f, 0.88f, 0.62f)
                    : _unread.Contains(index) ? new(0.98f, 0.82f, 0.53f)
                    : entry.Unlocked ? new(0.94f, 0.94f, 0.86f) : new(0.68f, 0.74f, 0.70f);
            }
        }

        private void ClearDetails()
        {
            _title.text = string.Empty;
            _category.text = string.Empty;
            _description.text = string.Empty;
        }
    }
}
