using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using WenXinJie.Persistence;

namespace WenXinJie.UI
{
    // Reading history records only what this saved playthrough actually saw.
    public sealed class DialogueReader : MonoBehaviour
    {
        private const int MaximumHistoryEntries = CampaignSaveStore.MaximumHistoryEntries;
        private const string TextSpeedPreference = "WenXinJie.TextSpeed";
        private static readonly float[] TextSpeeds = { 16f, 32f, 64f, 0f };
        private static readonly string[] TextSpeedNames = { "慢", "常", "快", "即时" };

        private readonly List<string> _history = new();
        private Action _advanceStory;
        private int[] _boundaries = Array.Empty<int>();
        private float _elapsed;
        private TMP_Text _historyText;
        private TMP_Text _historyCount;
        private Button _historyPrevious;
        private Button _historyNext;
        private Button[] _historyItems;
        private Image[] _historyMarks;
        private int _historyWindowStart;
        private Func<bool> _inputBlocked;
        private int _page;
        private GameObject _panel;
        private int _revealed;
        private TMP_Text _target;
        private bool _automatic;
        private bool _canAutoAdvance;
        private float _autoElapsed;
        private int _textSpeed;

        public event Action ReadingModeChanged;
        public bool IsRevealing => _revealed < _boundaries.Length;
        public bool IsHistoryOpen => _panel != null && _panel.activeSelf;
        public int HistoryCount => _history.Count;
        public string FullText { get; private set; } = string.Empty;
        public string TextSpeedLabel => "文字：" + TextSpeedNames[_textSpeed];
        public bool IsAutomatic => _automatic;
        public string AutomaticLabel => !_automatic ? "自动：关" : _canAutoAdvance ? "自动：开" : "自动：待选";

        private void Update()
        {
            if (_inputBlocked == null || _target == null)
            {
                return;
            }

            if (_inputBlocked())
            {
                _autoElapsed = 0f;
                return;
            }

            if (IsHistoryOpen)
            {
                _autoElapsed = 0f;
            }

            if (IsRevealing && !IsHistoryOpen)
            {
                _elapsed += Time.unscaledDeltaTime;
                _revealed = Math.Min(_boundaries.Length, (int)(_elapsed * TextSpeeds[_textSpeed]));
                _target.text = _revealed == _boundaries.Length
                    ? FullText
                    : FullText.Substring(0, _boundaries[_revealed]);
            }

            var keyboard = Keyboard.current;
            if (keyboard?.escapeKey.wasPressedThisFrame == true && IsHistoryOpen)
            {
                CloseHistory();
                return;
            }

            // Focused buttons submit through the UI module, preventing a double advance.
            var events = EventSystem.current;
            if (keyboard != null && !IsHistoryOpen
                && (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame))
            {
                if (events == null || events.currentSelectedGameObject == null)
                {
                    _advanceStory();
                }

                return;
            }

            if (Mouse.current?.leftButton.wasPressedThisFrame == true)
            {
                _autoElapsed = 0f;
                return;
            }

            if (_automatic && _canAutoAdvance && !IsRevealing && !IsHistoryOpen)
            {
                _autoElapsed += Time.unscaledDeltaTime;
                var readingPause = Mathf.Clamp(1.5f + _boundaries.Length * 0.06f, 2.5f, 15f);
                if (_autoElapsed >= readingPause)
                {
                    _autoElapsed = 0f;
                    _advanceStory();
                }
            }
        }

        public void Initialize(TMP_Text target, Action advanceStory, Func<bool> inputBlocked)
        {
            _target = target ?? throw new ArgumentNullException(nameof(target));
            _advanceStory = advanceStory ?? throw new ArgumentNullException(nameof(advanceStory));
            _inputBlocked = inputBlocked ?? throw new ArgumentNullException(nameof(inputBlocked));
            _textSpeed = Mathf.Clamp(PlayerPrefs.GetInt(TextSpeedPreference, 1), 0, TextSpeeds.Length - 1);
        }

        public void CycleTextSpeed()
        {
            _textSpeed = (_textSpeed + 1) % TextSpeeds.Length;
            PlayerPrefs.SetInt(TextSpeedPreference, _textSpeed);
            PlayerPrefs.Save();
            if (TextSpeeds[_textSpeed] == 0f)
            {
                Complete();
            }
            else
            {
                _elapsed = _revealed / TextSpeeds[_textSpeed];
            }

            ReadingModeChanged?.Invoke();
        }

        public void ToggleAutomatic()
        {
            SetAutomatic(!_automatic);
        }

        public void StopAutomatic()
        {
            SetAutomatic(false);
        }

        public void ConfigurePage(bool canAdvance, bool terminal)
        {
            _canAutoAdvance = canAdvance && !terminal;
            _autoElapsed = 0f;
            if (terminal)
            {
                SetAutomatic(false);
            }

            ReadingModeChanged?.Invoke();
        }

        private void SetAutomatic(bool enabled)
        {
            _automatic = enabled;
            _autoElapsed = 0f;
            ReadingModeChanged?.Invoke();
        }

        public void ConfigureHistory(GameObject window, TMP_Text text, TMP_Text count,
            Button previous, Button next, Button[] items, Image[] marks)
        {
            _panel = window;
            _historyText = text;
            _historyCount = count;
            _historyPrevious = previous;
            _historyNext = next;
            _historyItems = items;
            _historyMarks = marks;
            _panel.SetActive(false);
        }

        public void Present(string text, bool animate)
        {
            FullText = text ?? string.Empty;
            _boundaries = StringInfo.ParseCombiningCharacters(FullText);
            _elapsed = 0f;
            _revealed = 0;
            _autoElapsed = 0f;

            if (animate && !string.IsNullOrWhiteSpace(FullText)
                && (_history.Count == 0 || _history[_history.Count - 1] != FullText))
            {
                AddHistory(FullText);
            }

            if (!animate || TextSpeeds[_textSpeed] == 0f || string.IsNullOrWhiteSpace(FullText))
            {
                Complete();
            }
            else
            {
                _target.text = string.Empty;
            }
        }

        public void Complete()
        {
            _revealed = _boundaries.Length;
            _autoElapsed = 0f;
            _target.text = FullText;
        }

        public void ResetHistory()
        {
            _history.Clear();
            StopAutomatic();
            CloseHistory();
        }

        public string[] CaptureHistory() => _history.ToArray();

        public void RestoreHistory(string[] entries)
        {
            ResetHistory();
            if (entries == null) return;
            foreach (var entry in entries)
            {
                if (!string.IsNullOrWhiteSpace(entry)) AddHistory(entry);
            }
        }

        public void RecordChoice(string text)
        {
            AddHistory("你选择：" + text);
        }

        private void AddHistory(string text)
        {
            _history.Add(text);
            if (_history.Count > MaximumHistoryEntries) _history.RemoveAt(0);
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

        public void SelectVisibleHistory(int slot)
        {
            var index = _historyWindowStart + slot;
            if (index < 0 || index >= _history.Count)
            {
                return;
            }

            _page = index;
            DrawHistory();
        }

        private void DrawHistory()
        {
            _historyText.text = _history.Count == 0
                ? "尚无阅读记录。"
                : _history[_page];
            _historyCount.text = _history.Count == 0
                ? "0 / 0"
                : (_page + 1) + " / " + _history.Count;
            _historyPrevious.interactable = _page > 0;
            _historyNext.interactable = _page < _history.Count - 1;
            _historyWindowStart = Math.Max(0, Math.Min(_page - 2,
                _history.Count - _historyItems.Length));
            for (var slot = 0; slot < _historyItems.Length; slot++)
            {
                var index = _historyWindowStart + slot;
                var visible = index < _history.Count;
                _historyItems[slot].gameObject.SetActive(visible);
                _historyMarks[slot].gameObject.SetActive(visible && index == _page);
                if (!visible)
                {
                    continue;
                }

                var label = _historyItems[slot].GetComponentInChildren<TMP_Text>();
                var excerpt = _history[index].Replace('\n', ' ');
                if (excerpt.Length > 16)
                {
                    excerpt = excerpt.Substring(0, 16) + "…";
                }

                label.text = "第 " + (index + 1).ToString("00") + " 条\n" + excerpt;
                label.color = index == _page
                    ? new(1f, 0.88f, 0.62f)
                    : new(0.90f, 0.92f, 0.85f);
            }
        }
    }
}
