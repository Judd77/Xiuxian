using System;
using System.IO;
using System.Linq;
using UnityEngine;
using WenXinJie.Narrative;
using WenXinJie.Persistence;
using WenXinJie.UI;

namespace WenXinJie.Presentation
{
    public sealed class StoryApplication : MonoBehaviour
    {
        [SerializeField] private TextAsset _campaign;
        [SerializeField] private StoryMediaCatalog _media;

        private readonly CampaignSaveStore _saves = new CampaignSaveStore();
        private StoryGraph _graph;
        private StorySession _session;
        private StoryView _view;
        private DialogueReader _reader;
        private GameAudio _audio;

#if UNITY_EDITOR
        [NonSerialized] public string TestSaveDirectory;
#endif

        public StorySession Session => _session;

        public string SavePath
        {
            get
            {
#if UNITY_EDITOR
                if (!string.IsNullOrEmpty(TestSaveDirectory))
                {
                    return Path.Combine(TestSaveDirectory, "campaign-v2.json");
                }
#endif
                return Path.Combine(Application.persistentDataPath, "WenXinJie", "campaign-v2.json");
            }
        }

        private bool InputBlocked => _view.IsModalOpen || _view.IsMainMenuOpen || _reader.IsHistoryOpen;

        private void Awake()
        {
            _graph = new StoryGraph(JsonUtility.FromJson<StoryDefinition>(_campaign.text));
            _media.Validate(_graph);
            _reader = gameObject.AddComponent<DialogueReader>();
            _reader.advance = Advance;
            _audio = gameObject.AddComponent<GameAudio>();
            _audio.Initialize(_media.Music, _media.Rain);
            _view = gameObject.AddComponent<StoryView>();
            _view.Initialize(_media, _reader, _audio);
            _view.AdvanceRequested += Advance;
            _view.ChoiceRequested += Choose;
            _view.SaveRequested += Save;
            _view.LoadRequested += Load;
            _view.StartGameRequested += StartGame;
            _view.ContinueGameRequested += ContinueGame;
            _view.MainMenuLoadRequested += LoadFromMainMenu;
            _view.MainMenuRequested += ReturnToMainMenu;
            _view.QuitRequested += Quit;
            ShowMainMenu(string.Empty);
        }

        public void Advance()
        {
            if (InputBlocked || _session == null)
            {
                return;
            }

            if (_reader.IsRevealing)
            {
                _reader.Complete();
                return;
            }

            if (_session.Advance())
            {
                Render();
            }
        }

        public void Choose(string id)
        {
            if (InputBlocked || _session == null)
            {
                return;
            }

            if (_reader.IsRevealing)
            {
                _reader.Complete();
                return;
            }

            if (_session.Choose(id))
            {
                Render();
            }
        }

        public void Save()
        {
            if (InputBlocked || _session == null)
            {
                return;
            }

            try
            {
                _saves.Save(SavePath, _session);
                _view.ShowNotice("进度已保存");
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                _view.ShowNotice("存档失败，当前进度仍保留");
                Debug.LogWarning(exception.Message);
            }
        }

        public void Load()
        {
            if (InputBlocked || _session == null)
            {
                return;
            }

            if (TryLoad(out bool recovered, out string message))
            {
                _reader.ResetHistory();
                Render();
                _view.ShowNotice(recovered ? "已从备份恢复进度" : "进度已读取");
            }
            else
            {
                _view.ShowNotice(message);
            }
        }

        private void StartGame()
        {
            _session = new StorySession(_graph);
            _reader.ResetHistory();
            _view.HideMainMenu();
            Render();
        }

        private void ContinueGame()
        {
            if (_session != null)
            {
                _view.HideMainMenu();
                Render();
                return;
            }

            LoadFromMainMenu();
        }

        private void LoadFromMainMenu()
        {
            if (!TryLoad(out bool recovered, out string message))
            {
                _view.ShowMainMenuNotice(message);
                return;
            }

            _reader.ResetHistory();
            _view.HideMainMenu();
            Render();
            _view.ShowNotice(recovered ? "已从备份恢复进度" : "进度已读取");
        }

        private bool TryLoad(out bool recovered, out string message)
        {
            recovered = false;
            message = string.Empty;
            try
            {
                _session = _saves.Load(SavePath, _graph, out recovered);
                return true;
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException
                                                                       || exception is ArgumentException)
            {
                message = "未找到匹配当前剧情版本的有效存档";
                return false;
            }
        }

        private void ReturnToMainMenu()
        {
            _reader.Complete();
            _reader.CloseHistory();
            _audio.SetAmbienceActive(false);
            ShowMainMenu(string.Empty);
        }

        private void ShowMainMenu(string notice)
        {
            bool hasSave = _saves.Exists(SavePath);
            _view.ShowMainMenu(_media.Background("gate"), _session != null || hasSave, hasSave, notice);
        }

        private static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void Render()
        {
            StoryNode node = _session.Current;
            _audio.SetAmbienceActive(node.rain);
            _view.ShowNotice(string.Empty);
            _view.Render(new StoryViewState
            {
                Chapter = node.chapter,
                Location = node.location,
                Speaker = node.speaker,
                Text = node.text,
                Background = _media.Background(node.background),
                Portrait = _media.Portrait(node.portrait),
                Status = BuildStatus(),
                Journal = BuildJournal(),
                NextLabel = node.terminal ? "未完待续" : "继续  ▷",
                Terminal = node.terminal,
                Choices = node.choices.Select(choice => new ChoiceViewState
                {
                    Id = choice.id,
                    Label = choice.text,
                    Available = _session.IsAvailable(choice),
                    LockedReason = choice.lockedReason
                }).ToArray()
            });
        }

        private string BuildStatus()
        {
            int clues = _session.GetValue("clue.register") + _session.GetValue("clue.jade");
            string attention = _session.GetValue("risk.attention") > 0 ? "已惊动" : "未惊动";
            return "第 " + _session.GetValue("day") + " 日    /    余暇 " + _session.GetValue("actions")
                   + "    /    修为 " + _session.GetValue("cultivation")
                   + "    /    林月信任 " + _session.GetValue("trust.linYue")
                   + "    /    线索 " + clues + "/2    /    宗门 " + attention;
        }

        private ClueJournalState BuildJournal()
        {
            return new()
            {
                Entries = new[]
                {
                    new ClueEntryViewState
                    {
                        Title = "外门名册抄页",
                        Category = "物证 · 旧碑廊",
                        Description = "残页上的名字被朱笔圈过，部分人名又遭刻意刮除。页脚的归档记号指向药炉，而非外门执事堂。",
                        Unlocked = _session.GetValue("clue.register") > 0
                    },
                    new ClueEntryViewState
                    {
                        Title = "青玉扣",
                        Category = "物证 · 松风岭",
                        Description = "玉扣内侧刻着“沈砚”二字，边缘残留暗褐血痕。它与名册中被抹去的姓名能够相互印证。",
                        Unlocked = _session.GetValue("clue.jade") > 0
                    },
                    new ClueEntryViewState
                    {
                        Title = "山腹共鸣",
                        Category = "异象 · 山腹石门",
                        Description = "运转引气诀时，山腹深处会传来与经脉同频的回响。那并非自然灵脉，更像许多人的呼吸被困在石壁之后。",
                        Unlocked = _session.GetValue("mark.resonance") > 0
                    }
                }
            };
        }

        private void OnDestroy()
        {
            if (_view == null)
            {
                return;
            }

            _view.AdvanceRequested -= Advance;
            _view.ChoiceRequested -= Choose;
            _view.SaveRequested -= Save;
            _view.LoadRequested -= Load;
            _view.StartGameRequested -= StartGame;
            _view.ContinueGameRequested -= ContinueGame;
            _view.MainMenuLoadRequested -= LoadFromMainMenu;
            _view.MainMenuRequested -= ReturnToMainMenu;
            _view.QuitRequested -= Quit;
        }
    }
}
