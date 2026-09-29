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
                _view.ShowNotice(recovered ? "已从备份恢复进度" :
                    string.IsNullOrEmpty(message) ? "进度已读取" : message);
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
            _view.ShowNotice(recovered ? "已从备份恢复进度" :
                string.IsNullOrEmpty(message) ? "进度已读取" : message);
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
                var importDemo = !isDemo && !_saves.Exists(SavePath) && _saves.Exists(DemoSavePath);
                Session = _saves.Load(importDemo ? DemoSavePath : SavePath, _graph, out recovered);
                if (importDemo)
                {
                    _saves.Save(SavePath, Session);
                }

                _lastJournal = null;
                message = importDemo ? "试玩进度已继承" : string.Empty;
                return true;
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException
                                                                       || exception is ArgumentException)
            {
                message = "未找到匹配当前剧情版本的有效存档";
                return false;
            }
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
