using System;
using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using WenXinJie.Narrative;
using WenXinJie.Persistence;
using WenXinJie.UI;

namespace WenXinJie.Presentation
{
    public sealed class StoryApplication : MonoBehaviour, IStoryCommands
    {
        [SerializeField] private TextAsset campaign;
        [SerializeField] private StoryAssetCatalog storyAssets;
        [SerializeField] private bool isDemo;

        private readonly CampaignSaveStore _saves = new();

#if UNITY_EDITOR
        [NonSerialized] public string TestSaveDirectory;
#endif
        private GameAudio _audio;
        private DisplayModeService _displayModes;
        private StoryGraph _graph;
        private ClueJournalState _lastJournal;
        private DialogueReader _reader;
        private StoryView _view;
        private StoryViewStateFactory _viewStateFactory;
        private string _savedState;
        private string _savedTime;

        public StorySession Session { get; private set; }

        public string SavePath
        {
            get
            {
#if UNITY_EDITOR
                if (!string.IsNullOrEmpty(TestSaveDirectory))
                {
                    return Path.Combine(TestSaveDirectory, isDemo ? "campaign-demo-run.json" : "campaign-run.json");
                }
#endif
                return Path.Combine(Application.persistentDataPath, "WenXinJie",
                    isDemo ? "campaign-demo-run.json" : "campaign-run.json");
            }
        }

        private void Awake()
        {
            _graph = new(JsonUtility.FromJson<StoryDefinition>(campaign.text));
            storyAssets.Validate(_graph);
            _reader = gameObject.AddComponent<DialogueReader>();
            _audio = gameObject.AddComponent<GameAudio>();
            _audio.Initialize(storyAssets.MusicClip, storyAssets.RainClip);
            _displayModes = new();
            _displayModes.Initialize();
            _viewStateFactory = new(storyAssets);
            _view = gameObject.AddComponent<StoryView>();
            _view.Initialize(storyAssets, _reader, _audio, _displayModes, this, isDemo);
            ShowMainMenu(string.Empty);
        }

        public void AdvanceStory()
        {
            if (_view.IsInteractionBlocked || Session == null)
            {
                return;
            }

            if (_reader.IsRevealing)
            {
                _reader.Complete();
                return;
            }

            if (Session.Advance())
            {
                Render();
            }
        }

        public void SelectChoice(string choiceId)
        {
            if (_view.IsInteractionBlocked || Session == null)
            {
                return;
            }

            if (_reader.IsRevealing)
            {
                _reader.Complete();
                return;
            }

            if (Session.Choose(choiceId))
            {
                Render();
            }
        }

        public void SaveGame()
        {
            if (_view.IsInteractionBlocked || Session == null)
            {
                return;
            }

            var saved = SaveCurrentProgress();
            UpdateProgress();
            _view.ShowNotice(saved ? "进度已保存 · " + _savedTime : "存档失败，当前进度仍保留");
        }

        public void LoadGame()
        {
            if (_view.IsInteractionBlocked || Session == null)
            {
                return;
            }

            ConfirmProgressReplacement("读取存档", LoadDestination(), LoadCurrentProgress, false);
        }

        private void LoadCurrentProgress()
        {
            if (TryLoad(out var recovered, out var message))
            {
                _reader.ResetHistory();
                Render();
                _view.ShowNotice(LoadNotice(recovered, message));
            }
            else
            {
                _view.ShowNotice(message);
            }
        }

        public void StartNewGame()
        {
            ConfirmProgressReplacement("重新开始", "从故事开场重新开始；原存档在手动保存前仍保留。", BeginNewGame);
        }

        private void BeginNewGame()
        {
            Session = new(_graph);
            _savedState = null;
            _savedTime = null;
            _lastJournal = null;
            _reader.ResetHistory();
            _view.ResetJournal();
            _view.HideMainMenu();
            Render();
        }

        public void ContinueGame()
        {
            if (Session != null)
            {
                _view.HideMainMenu();
                Render();
                return;
            }

            LoadGameFromMenu();
        }

        public void LoadGameFromMenu()
        {
            ConfirmProgressReplacement("读取存档", LoadDestination(), LoadFromMenu, false);
        }

        private void LoadFromMenu()
        {
            if (!TryLoad(out var recovered, out var message))
            {
                _view.ShowMainMenuNotice(message);
                return;
            }

            _reader.ResetHistory();
            _view.HideMainMenu();
            Render();
            _view.ShowNotice(LoadNotice(recovered, message));
        }

        public void ReturnToMainMenu()
        {
            _reader.Complete();
            _reader.CloseHistory();
            _audio.SetAmbienceActive(false);
            ShowMainMenu(string.Empty);
        }

        public void QuitGame()
        {
            ConfirmProgressReplacement("退出游戏", "退出后，未保存的进度将无法继续。", ExitGame);
        }

        private static void ExitGame()
        {
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private bool HasUnsavedProgress => Session != null
            && JsonUtility.ToJson(Session.Snapshot()) != _savedState;

        private bool IsDemoBoundary => isDemo && Session?.Current.id == "case.boundary";

        private bool SaveCurrentProgress()
        {
            try
            {
                _saves.Save(SavePath, Session);
                MarkSaved(SavePath);
                return true;
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                Debug.LogWarning("Could not save progress: " + exception.Message);
                return false;
            }
        }

        private void MarkSaved(string path)
        {
            _savedState = JsonUtility.ToJson(Session.Snapshot());
            _savedTime = SaveTime(path);
        }

        private static string SaveTime(string path)
        {
            return File.GetLastWriteTime(path).ToString("MM-dd HH:mm");
        }

        private void UpdateProgress()
        {
            var status = HasUnsavedProgress
                ? IsDemoBoundary ? "试玩接续未保存" : "有新进度 · 未保存"
                : "已保存 · " + _savedTime;
            _view.ShowProgress(status, IsDemoBoundary);
        }

        private string LoadDestination()
        {
            try
            {
                var saved = ReadPreferredSave(out _, out _);
                return "将回到：" + DescribeProgress(saved) + "\n"
                    + "当前页：" + PageExcerpt(saved);
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException
                                               || exception is ArgumentException)
            {
                return "读取磁盘中上次保存的位置；没有有效存档时保留当前进度。";
            }
        }

        private void ConfirmProgressReplacement(string title, string destination, Action action,
            bool allowSave = true)
        {
            if (!HasUnsavedProgress)
            {
                action();
                return;
            }

            var body = "当前进度尚未保存\n" + DescribeProgress(Session) + "\n\n" + destination;
            if (!allowSave)
            {
                // Saving here would replace the very checkpoint the player intends to load.
                _view.ShowPrompt(title, body + "\n读取将放弃本次未保存的新进度，取消可继续当前游戏。",
                    "读取并放弃新进度", action);
                return;
            }

            _view.ShowPrompt(title, body + "\n请选择先保存，或放弃本次未保存的进度。",
                "先保存再继续", () =>
                {
                    if (SaveCurrentProgress())
                    {
                        UpdateProgress();
                        action();
                    }
                    else
                    {
                        _view.ShowPrompt("存档失败", "当前进度仍保留，尚未执行“" + title + "”。\n"
                            + "请确认本机存档目录可写，然后重试；也可以直接继续。",
                            "重试", () => ConfirmProgressReplacement(title, destination, action),
                            "放弃进度并继续", action);
                    }
                }, "放弃进度并继续", action);
        }

        public void ShowDemoCompletion()
        {
            if (!IsDemoBoundary)
            {
                return;
            }

            var saveNotice = HasUnsavedProgress
                ? "试玩接续存档失败：当前进度还在本次游戏中，请点击重新保存。"
                : "试玩进度已保存（" + _savedTime + "），线索与选择会一并保留。";
            _view.ShowPrompt("试玩结束", saveNotice + "\n\n"
                + "保存后，在同一台电脑启动正式版，点击“继续游戏”即可接续调查。\n"
                + "已有正式版存档时会优先读取正式进度。试玩与正式存档分别保存；"
                + "换电脑接续需要转移本机存档。",
                "主界面", ReturnToMainMenu, "重新保存", () =>
                {
                    SaveCurrentProgress();
                    UpdateProgress();
                    ShowDemoCompletion();
                }, "继续查看");
        }

        private static string DescribeProgress(StorySession session)
        {
            return session.Current.chapter + " · " + session.Current.location;
        }

        private static string PageExcerpt(StorySession session)
        {
            var text = session.CurrentText.Replace("\r", string.Empty).Replace("\n", " ");
            return text.Length > 26 ? text.Substring(0, 26) + "…" : text;
        }

        private bool TryLoad(out bool recovered, out string message)
        {
            recovered = false;
            message = string.Empty;
            try
            {
                var hadFullSave = _saves.Exists(SavePath);
                var loaded = ReadPreferredSave(out recovered, out var importDemo);
                Session = loaded;
                _savedState = null;
                _savedTime = null;
                if (!importDemo)
                {
                    MarkSaved(recovered ? SavePath + ".bak" : SavePath);
                }
                message = importDemo
                    ? hadFullSave ? "已接续试玩进度，原正式版存档保留；请手动存档" : "试玩进度已继承"
                    : string.Empty;
                if (importDemo && !hadFullSave)
                {
                    if (!SaveCurrentProgress())
                    {
                        message = "试玩进度已接续，请稍后手动存档";
                    }
                }

                _lastJournal = null;
                _view.ResetJournal();
                return true;
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException
                                                                       || exception is ArgumentException)
            {
                message = "未找到匹配当前剧情版本的有效存档";
                return false;
            }
        }

        private StorySession ReadPreferredSave(out bool recovered, out bool importDemo)
        {
            importDemo = false;
            if (_saves.Exists(SavePath))
            {
                try
                {
                    return _saves.Load(SavePath, _graph, out recovered);
                }
                catch (Exception exception) when ((exception is IOException || exception is ArgumentException)
                                                 && !isDemo && _saves.Exists(DemoSavePath))
                {
                    // A rejected full save must not block a valid Demo handoff or be overwritten by it.
                }
            }

            importDemo = !isDemo && _saves.Exists(DemoSavePath);
            return _saves.Load(importDemo ? DemoSavePath : SavePath, _graph, out recovered);
        }

        private static string LoadNotice(bool recovered, string message)
        {
            if (!string.IsNullOrEmpty(message))
            {
                return recovered ? message + "（从备份恢复）" : message;
            }

            return recovered ? "已从备份恢复进度" : "进度已读取";
        }

        private void ShowMainMenu(string notice)
        {
            var hasSave = false;
            var diskProgress = "读档：尚无存档";
            try
            {
                var saved = ReadPreferredSave(out var recovered, out var importDemo);
                hasSave = true;
                var source = importDemo ? "试玩进度" : recovered ? "备份存档" : "已保存进度";
                diskProgress = "读档：" + source + "\n" + DescribeProgress(saved)
                    + "\n“" + PageExcerpt(saved) + "”";
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException
                                               || exception is ArgumentException)
            {
                if (_saves.Exists(SavePath) || !isDemo && _saves.Exists(DemoSavePath))
                {
                    diskProgress = "读档：没有适用于当前剧情的有效存档，原文件保留";
                }
            }

            var progress = Session != null
                ? "继续：本次进度" + (HasUnsavedProgress ? "（未保存）" : "（已保存）")
                    + "\n" + DescribeProgress(Session) + "\n“" + PageExcerpt(Session) + "”\n" + diskProgress
                : hasSave ? diskProgress + "\n继续游戏将读取以上位置。" : diskProgress + "\n开始游戏后，请使用底栏存档。";
            if (string.IsNullOrEmpty(notice) && HasUnsavedProgress)
            {
                notice = "继续会保留当前进度；退出前请存档。";
            }

            _view.ShowMainMenu(storyAssets.GetBackground("gate"), Session != null || hasSave, hasSave, notice,
                progress);
        }

        private string DemoSavePath => Path.Combine(Path.GetDirectoryName(SavePath), "campaign-demo-run.json");

        private void Render()
        {
            if (IsDemoBoundary)
            {
                SaveCurrentProgress();
            }

            var state = _viewStateFactory.Create(Session);
            var newClue = FindNewClue(_lastJournal, state.Journal);
            _audio.SetAmbienceActive(Session.Current.rain);
            _view.Render(state);
            UpdateProgress();
            _view.ShowNotice(string.IsNullOrEmpty(newClue) ? string.Empty : "新线索已收录：" + newClue);
            _lastJournal = state.Journal;
        }

        private static string FindNewClue(ClueJournalState previous, ClueJournalState current)
        {
            if (previous == null)
            {
                return string.Empty;
            }

            for (var index = 0; index < current.Entries.Length; index++)
            {
                if (current.Entries[index].Unlocked
                    && (index >= previous.Entries.Length || !previous.Entries[index].Unlocked))
                {
                    return current.Entries[index].Title;
                }
            }

            return string.Empty;
        }
    }
}
