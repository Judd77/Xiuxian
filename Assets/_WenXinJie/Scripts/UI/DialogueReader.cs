using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace WenXinJie.UI
{
    // Session history is intentionally separate from persistent read/unlock data.
    public sealed class DialogueReader : MonoBehaviour
    {
        private const int MaximumHistoryEntries = 200;
        private const float DefaultCharactersPerSecond = 32f;

        private readonly List<string> _history = new();
        private Action _advanceStory;
        private int[] _boundaries = Array.Empty<int>();
        private float _elapsed;
        private TMP_Text _historyText;
        private Func<bool> _inputBlocked;
        private int _page;
        private GameObject _panel;
        private int _revealed;
        private TMP_Text _target;

        public bool IsRevealing => _revealed < _boundaries.Length;
        public bool IsHistoryOpen => _panel != null && _panel.activeSelf;
        public int HistoryCount => _history.Count;
        public string FullText { get; private set; } = string.Empty;

        private void Update()
        {
            if (_inputBlocked == null || _target == null)
            {
                return;
            }

            if (_inputBlocked())
            {
                return;
            }

            if (IsRevealing && !IsHistoryOpen)
            {
                _elapsed += Time.unscaledDeltaTime;
                _revealed = Math.Min(_boundaries.Length, (int)(_elapsed * DefaultCharactersPerSecond));
                _target.text = _revealed == _boundaries.Length
                    ? FullText
                    : FullText.Substring(0, _boundaries[_revealed]);
            }

            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.escapeKey.wasPressedThisFrame && IsHistoryOpen)
            {
                CloseHistory();
                return;
            }

            // Focused buttons submit through the UI module, preventing a double advance.
            var events = EventSystem.current;
            if (events != null && events.currentSelectedGameObject != null)
            {
                return;
            }

            if (!IsHistoryOpen && (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame))
            {
                _advanceStory();
            }
        }

        public void Initialize(TMP_Text target, Action advanceStory, Func<bool> inputBlocked)
        {
            _target = target ?? throw new ArgumentNullException(nameof(target));
            _advanceStory = advanceStory ?? throw new ArgumentNullException(nameof(advanceStory));
            _inputBlocked = inputBlocked ?? throw new ArgumentNullException(nameof(inputBlocked));
        }

        public void ConfigureHistory(GameObject window, TMP_Text text)
        {
            _panel = window;
            _historyText = text;
            _panel.SetActive(false);
        }

        public void Present(string text, bool animate)
        {
            FullText = text ?? string.Empty;
            _boundaries = StringInfo.ParseCombiningCharacters(FullText);
            _elapsed = 0f;
            _revealed = 0;

            if (!animate || string.IsNullOrWhiteSpace(FullText))
            {
                Complete();
                return;
            }

            if (_history.Count == 0 || _history[_history.Count - 1] != FullText)
            {
                _history.Add(FullText);
            }

            if (_history.Count > MaximumHistoryEntries)
            {
                _history.RemoveAt(0);
            }

            _target.text = string.Empty;
        }

        public void Complete()
        {
            _revealed = _boundaries.Length;
            _target.text = FullText;
        }

        public void ResetHistory()
        {
            _history.Clear();
            CloseHistory();
        }

        public void OpenHistory()
        {
            if (_inputBlocked())
            {
                return;
            }

            Complete();
            _page = Math.Max(0, _history.Count - 1);
            _panel.SetActive(true);
            DrawHistory();
        }

        public void CloseHistory()
        {
            if (_panel != null)
            {
                _panel.SetActive(false);
            }
        }

        public void Previous()
        {
            _page = Math.Max(0, _page - 1);
            DrawHistory();
        }

        public void Next()
        {
            _page = Math.Min(Math.Max(0, _history.Count - 1), _page + 1);
            DrawHistory();
        }

        private void DrawHistory()
        {
            _historyText.text = _history.Count == 0
                ? "尚无阅读记录。"
                : "阅读记录 " + (_page + 1) + " / " + _history.Count + "\n\n" + _history[_page];
        }
    }
}