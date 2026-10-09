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

            try
            {
                _saves.Save(SavePath, Session);
                _view.ShowNotice("进度已保存");
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                _view.ShowNotice("存档失败，当前进度仍保留");
                Debug.LogWarning(exception.Message);
            }
        }

        public void LoadGame()
        {
            if (_view.IsInteractionBlocked || Session == null)
            {
                return;
            }

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
            Session = new(_graph);
            _lastJournal = null;
            _reader.ResetHistory();
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
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
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
                message = importDemo
                    ? hadFullSave ? "已接续试玩进度，原正式版存档保留；请手动存档" : "试玩进度已继承"
                    : string.Empty;
                if (importDemo && !hadFullSave)
                {
                    try
                    {
                        _saves.Save(SavePath, Session);
                    }
                    catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
                    {
                        message = "试玩进度已接续，请稍后手动存档";
                        Debug.LogWarning("Could not save imported demo progress: " + exception.Message);
                    }
                }

                _lastJournal = null;
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
            var hasSave = _saves.Exists(SavePath) || !isDemo && _saves.Exists(DemoSavePath);
            _view.ShowMainMenu(storyAssets.GetBackground("gate"), Session != null || hasSave, hasSave, notice);
        }

        private string DemoSavePath => Path.Combine(Path.GetDirectoryName(SavePath), "campaign-demo-run.json");

        private void Render()
        {
            if (isDemo && Session.Current.id == "case.boundary")
            {
                try
                {
                    _saves.Save(SavePath, Session);
                }
                catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
                {
                    Debug.LogWarning("Could not save demo handoff: " + exception.Message);
                }
            }

            var state = _viewStateFactory.Create(Session);
            var newClue = FindNewClue(_lastJournal, state.Journal);
            _audio.SetAmbienceActive(Session.Current.rain);
            _view.Render(state);
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
