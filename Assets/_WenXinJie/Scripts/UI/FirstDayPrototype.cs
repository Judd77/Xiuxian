using System;
using System.IO;
using UnityEngine;
using UnityEngine.Serialization;
using WenXinJie.Core;
using WenXinJie.Persistence;

namespace WenXinJie.UI
{
    public sealed class FirstDayPrototype : MonoBehaviour
    {
        [FormerlySerializedAs("introAsset")]
        [SerializeField] private TextAsset _introAsset;
        [SerializeField] private Texture2D _landscape;
        [SerializeField] private AudioClip _music;
        [SerializeField] private AudioClip _rain;

        private readonly FirstDaySaveStore _saveStore = new FirstDaySaveStore();
        private FirstDaySession _session;
        private FirstDayStory _story;
        private DialogueReader _reader;
        private FirstDayView _view;
        private FirstDayAudio _audio;
        private Font _font;
        private int _line;
        private bool _isMenu = true;
        private string _message = string.Empty;

#if UNITY_EDITOR
        [NonSerialized] public string testSaveDirectory;
#endif

        public int RemainingActions => _session.Snapshot().actionsLeft;

        public string SavePath
        {
            get
            {
#if UNITY_EDITOR
                if (!string.IsNullOrEmpty(testSaveDirectory))
                {
                    return Path.Combine(testSaveDirectory, "quick.json");
                }
#endif
                return Path.Combine(Application.persistentDataPath, "WenXinJiePrototype", "quick.json");
            }
        }

        private bool IsInputBlocked => _reader.IsHistoryOpen || _view.IsModalOpen;

        private void Awake()
        {
            _story = FirstDayStory.Parse(_introAsset.text);
            _session = new FirstDaySession();
            _font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "SimHei", "Arial" }, 28);

            _reader = gameObject.AddComponent<DialogueReader>();
            _reader.advance = Advance;
            _audio = gameObject.AddComponent<FirstDayAudio>();
            _audio.Initialize(_music, _rain);
            _view = gameObject.AddComponent<FirstDayView>();
            _view.Initialize(_font, _landscape, _reader, _audio);
            _view.AdvanceRequested += Advance;
            _view.ActionSelected += Choose;
            _view.SaveRequested += Save;
            _view.LoadRequested += Load;
            _view.TitleRequested += ReturnToTitle;
            Render();
        }

        public void Begin()
        {
            _reader.ResetHistory();
            _session = new FirstDaySession();
            _line = 0;
            _message = string.Empty;
            _isMenu = false;
            _view.ShowNotice(string.Empty);
            Render();
        }

        public void Advance()
        {
            if (IsInputBlocked)
            {
                return;
            }

            if (_reader.IsRevealing)
            {
                _reader.Complete();
                return;
            }

            if (_isMenu)
            {
                Begin();
                return;
            }

            if (_line < _story.lines.Length)
            {
                _line++;
                Render();
                return;
            }

            FirstDayState state = _session.Snapshot();
            if (state.resolved)
            {
                Begin();
            }
            else if (state.actionsLeft == 0)
            {
                _session.ResolveNight();
                Render();
            }
        }

        public void Choose(DayAction action)
        {
            if (IsInputBlocked || _isMenu || _line < _story.lines.Length)
            {
                return;
            }

            if (_reader.IsRevealing)
            {
                _reader.Complete();
                return;
            }

            if (_session.TryChoose(action, out string feedback))
            {
                _message = feedback;
                Render();
            }
            else
            {
                _view.ShowNotice(feedback);
            }
        }

        public void Save()
        {
            if (IsInputBlocked)
            {
                return;
            }

            if (_isMenu)
            {
                _view.ShowNotice("请先开始游戏。");
                return;
            }

            try
            {
                var data = new FirstDaySave
                {
                    session = _session.ToJson(),
                    line = _line,
                    message = _message
                };
                _saveStore.Save(SavePath, data);
                _view.ShowNotice("已保存当前进度。");
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                _view.ShowNotice("保存失败：" + exception.Message);
            }
        }

        public void Load()
        {
            if (IsInputBlocked)
            {
                return;
            }

            try
            {
                FirstDaySave data = _saveStore.Load(SavePath, _story.lines.Length, out bool recoveredBackup);
                FirstDaySession restored = FirstDaySession.Restore(data.session);
                _reader.ResetHistory();
                _session = restored;
                _line = data.line;
                _message = data.message ?? string.Empty;
                _isMenu = false;
                Render();
                _view.ShowNotice(recoveredBackup ? "主存档不可读，已恢复备份。" : "已读取存档。");
            }
            catch (Exception exception) when (exception is IOException
                || exception is UnauthorizedAccessException || exception is ArgumentException)
            {
                _view.ShowNotice("没有有效存档；当前进度未改变。");
            }
        }

        private void ReturnToTitle()
        {
            _isMenu = true;
            _view.ShowNotice(string.Empty);
            Render();
        }

        private void Render()
        {
            FirstDayState state = _session.Snapshot();
            _audio.SetMenu(_isMenu);
            _view.Render(CreateViewState(state));
        }

        private FirstDayViewState CreateViewState(FirstDayState state)
        {
            var viewState = new FirstDayViewState
            {
                IsMenu = _isMenu,
                Status = "剩余行动 " + state.actionsLeft + "/2    修为 " + state.cultivation
                    + "    林月信任 " + state.trust + "    灵石 " + state.spiritStones
                    + "    证据：" + (state.hasEvidence ? "旧名册" : "无")
            };

            if (_isMenu)
            {
                viewState.Title = "距离登仙大典，还有三十日";
                viewState.Body = "你可以成为仙人，也可能成为代价。\n\n雨落青云山，有人等你赴约。";
                viewState.NextLabel = "开始新游戏";
            }
            else if (_line < _story.lines.Length)
            {
                viewState.Title = "第一日 / 山门";
                viewState.Body = _story.lines[_line];
            }
            else if (state.resolved)
            {
                viewState.Title = "第一日 / 夜";
                viewState.Body = _story.GetEnding(state.outcome);
                viewState.NextLabel = "重玩第一日";
            }
            else
            {
                viewState.Title = state.actionsLeft == 2 ? "白昼 / 你打算去哪里？" : "暮色 / 每个选择都有代价";
                viewState.Body = string.IsNullOrEmpty(_message)
                    ? "林月今夜离山，名册也将在夜里移交。\n你只够去两处地方。"
                    : _message;
                viewState.ShowActions = state.actionsLeft > 0;
                viewState.NextLabel = "入夜 / 查看结果";
                viewState.AvailableActions = new bool[4];

                for (int index = 0; index < viewState.AvailableActions.Length; index++)
                {
                    viewState.AvailableActions[index] = _session.CanChoose((DayAction)index, out _);
                }
            }

            return viewState;
        }

        private void OnDestroy()
        {
            if (_view != null)
            {
                _view.AdvanceRequested -= Advance;
                _view.ActionSelected -= Choose;
                _view.SaveRequested -= Save;
                _view.LoadRequested -= Load;
                _view.TitleRequested -= ReturnToTitle;
            }

            if (_font != null)
            {
                Destroy(_font);
            }
        }
    }
}
