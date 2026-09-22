using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace WenXinJie.UI
{
    // Session history is intentionally separate from persistent read/unlock data.
    public sealed class DialogueReader : MonoBehaviour
    {
        public Text target;
        public Action advance;
        public Func<bool> inputBlocked;
        public float charactersPerSecond = 32f;

        private readonly List<string> _history = new List<string>();
        private string _fullText = string.Empty;
        private int[] _boundaries = Array.Empty<int>();
        private float _elapsed;
        private int _revealed;
        private GameObject _panel;
        private Text _historyText;
        private int _page;

        public bool IsRevealing => _revealed < _boundaries.Length;
        public bool IsHistoryOpen => _panel != null && _panel.activeSelf;
        public int HistoryCount => _history.Count;
        public string FullText => _fullText;

        public void ConfigureHistory(GameObject window, Text text)
        {
            _panel = window;
            _historyText = text;
            _panel.SetActive(false);
        }

        public void Present(string text, bool animate)
        {
            _fullText = text ?? string.Empty;
            _boundaries = StringInfo.ParseCombiningCharacters(_fullText);
            _elapsed = 0f;
            _revealed = 0;

            if (!animate || string.IsNullOrWhiteSpace(_fullText))
            {
                Complete();
                return;
            }

            if (_history.Count == 0 || _history[_history.Count - 1] != _fullText)
            {
                _history.Add(_fullText);
            }

            if (_history.Count > 200)
            {
                _history.RemoveAt(0);
            }

            target.text = string.Empty;
        }

        public void Complete()
        {
            _revealed = _boundaries.Length;
            target.text = _fullText;
        }

        public void ResetHistory()
        {
            _history.Clear();
            CloseHistory();
        }

        public void OpenHistory()
        {
            if (inputBlocked?.Invoke() == true)
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

        private void Update()
        {
            if (inputBlocked?.Invoke() == true)
            {
                return;
            }

            if (IsRevealing && !IsHistoryOpen)
            {
                _elapsed += Time.unscaledDeltaTime;
                _revealed = Math.Min(_boundaries.Length, (int)(_elapsed * Math.Max(1f, charactersPerSecond)));
                target.text = _revealed == _boundaries.Length
                    ? _fullText
                    : _fullText.Substring(0, _boundaries[_revealed]);
            }

            Keyboard keyboard = Keyboard.current;
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
            EventSystem events = EventSystem.current;
            if (events != null && events.currentSelectedGameObject != null)
            {
                return;
            }

            if (!IsHistoryOpen && (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame))
            {
                advance?.Invoke();
            }
        }
    }
}
