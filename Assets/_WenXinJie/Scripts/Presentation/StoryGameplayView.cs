using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WenXinJie.UI;

namespace WenXinJie.Presentation
{
    internal sealed class StoryGameplayView
    {
        private const int ChoiceCount = 4;
        private readonly TMP_Text _chapter;
        private readonly string[] _choiceIds = new string[ChoiceCount];

        private readonly Button[] _choices = new Button[ChoiceCount];
        private readonly TMP_Text _location;
        private readonly Button _next;
        private readonly TMP_Text _notice;
        private readonly GameObject _root;
        private readonly TMP_Text _speaker;
        private readonly TMP_Text _status;
        private readonly StoryObservationView _observation;
        private readonly DialogueReader _reader;
        private readonly Button _automatic;
        private readonly TMP_Text _automaticLabel;
        private readonly TMP_Text _textSpeedLabel;
        private readonly TMP_Text _saveStatus;
        private readonly Button _demoCompletion;
        private readonly TMP_Text _journalLabel;
        private readonly GameObject _endingPanel;
        private readonly TMP_Text _endingTitle;
        private readonly TMP_Text[] _endingConsequences = new TMP_Text[3];
        private readonly CanvasGroup _endingGroup;
        private readonly MonoBehaviour _coroutineRunner;
        private Coroutine _endingTransition;

        public StoryGameplayView(RectTransform root, StoryUiFactory ui, StoryAssetCatalog assets,
            DialogueReader reader, IStoryCommands commands, MonoBehaviour coroutineRunner,
            Action openJournal, Func<bool> inputBlocked)
        {
            _root = root.gameObject;
            _reader = reader;
            _coroutineRunner = coroutineRunner;
            ui.Picture(root, "HeaderPanel", assets.PanelTexture, 48f, 24f, 1824f, 116f);
            var gameTitle = ui.Label(root, "GameTitle", "问心劫", 86f, 32f, 280f, 74f, 42);
            gameTitle.font = assets.TitleFont;
            gameTitle.color = new(0.93f, 0.80f, 0.50f);
            gameTitle.alignment = TextAlignmentOptions.Left;

            var subtitle = ui.Label(root, "Subtitle", "修仙悬疑 AVG", 390f, 55f, 360f, 38f, 21);
            subtitle.font = assets.MenuFont;
            subtitle.color = new(0.78f, 0.84f, 0.82f);

            _chapter = ui.Label(root, "Chapter", string.Empty, 1110f, 43f, 710f, 46f, 27);
            _chapter.font = assets.MenuFont;
            _chapter.alignment = TextAlignmentOptions.Right;
            _status = ui.Label(root, "Status", string.Empty, 86f, 102f, 1290f, 30f, 19);
            _status.font = assets.Font;
            _status.color = new(0.84f, 0.88f, 0.85f);
            _saveStatus = ui.Label(root, "SaveStatus", string.Empty, 1390f, 102f, 430f, 30f, 19);
            _saveStatus.font = assets.MenuFont;
            _saveStatus.color = new(0.94f, 0.89f, 0.67f);
            _saveStatus.alignment = TextAlignmentOptions.Right;
            _saveStatus.enableAutoSizing = true;
            _saveStatus.fontSizeMin = 15;
            _saveStatus.fontSizeMax = 19;

            ui.Frame(root, "LocationPanel", 80f, 166f, 620f, 74f);
            _location = ui.Label(root, "Location", string.Empty, 110f, 178f, 560f, 50f, 28);
            _location.font = assets.MenuFont;
            _location.alignment = TextAlignmentOptions.Left;

            CreateChoices(root, ui, commands, assets.Font);
            _demoCompletion = ui.Button(root, "DemoCompletion", "试玩结束 · 进度与接续方式",
                96f, 286f, 700f, 78f, commands.ShowDemoCompletion);
            _demoCompletion.gameObject.SetActive(false);
            _observation = new(root, ui);

            var ending = StoryUiFactory.Box(root, "EndingPanel", 80f, 252f, 1040f, 460f);
            _endingPanel = ending.gameObject;
            _endingGroup = ending.gameObject.AddComponent<CanvasGroup>();
            ui.Panel(ending, "EndingFrame", 0f, 0f, 1040f, 460f);
            var endingOverline = ui.Label(ending, "EndingOverline", "问心劫  /  本次故事已结束",
                44f, 24f, 952f, 32f, 21);
            endingOverline.font = assets.MenuFont;
            endingOverline.color = new(0.72f, 0.82f, 0.78f);
            _endingTitle = ui.Label(ending, "EndingTitle", string.Empty, 42f, 60f, 952f, 62f, 44);
            _endingTitle.font = assets.TitleFont;
            _endingTitle.color = new(0.96f, 0.86f, 0.60f);
            StoryUiFactory.Rule(ending, "EndingRule", 44f, 132f, 952f, 2f,
                new(0.74f, 0.64f, 0.40f, 0.76f));
            for (var index = 0; index < _endingConsequences.Length; index++)
            {
                _endingConsequences[index] = ui.Label(ending, "EndingConsequence" + index,
                    string.Empty, 44f, 146f + index * 70f, 952f, 68f, 25);
            }
            ui.Button(ending, "EndingMainMenu", "返回主界面", 44f, 370f, 300f, 60f,
                commands.ReturnToMainMenu);
            ui.Button(ending, "EndingRestart", "重新开始", 364f, 370f, 300f, 60f,
                commands.StartNewGame);
            _endingPanel.SetActive(false);

            ui.Picture(root, "DialoguePanel", assets.PanelTexture, 80f, 748f, 1760f, 246f);
            ui.Frame(root, "SpeakerPanel", 112f, 724f, 330f, 62f);
            _speaker = ui.Label(root, "Speaker", string.Empty, 138f, 731f, 280f, 48f, 29);
            _speaker.font = assets.MenuFont;
            _speaker.color = new(0.92f, 0.78f, 0.48f);
            _speaker.alignment = TextAlignmentOptions.Left;

            var dialogue = ui.Label(root, "DialogueBody", string.Empty, 124f, 808f, 1490f, 164f, 28);
            dialogue.raycastTarget = true;
            var dialogueButton = dialogue.gameObject.AddComponent<Button>();
            dialogueButton.transition = Selectable.Transition.None;
            dialogueButton.navigation = new() { mode = Navigation.Mode.None };
            dialogueButton.onClick.AddListener(commands.AdvanceStory);
            reader.Initialize(dialogue, commands.AdvanceStory, inputBlocked);

            _next = ui.Button(root, "Advance", "继续", 1632f, 906f, 172f, 56f, commands.AdvanceStory);
            ui.Button(root, "Save", "存档", 80f, 1008f, 146f, 54f, commands.SaveGame);
            ui.Button(root, "Load", "读档", 242f, 1008f, 146f, 54f, commands.LoadGame);
            ui.Button(root, "History", "前尘", 404f, 1008f, 146f, 54f, reader.OpenHistory);
            var journal = ui.Button(root, "ClueJournal", "线索", 566f, 1008f, 146f, 54f, openJournal);
            _journalLabel = journal.GetComponentInChildren<TMP_Text>();
            _journalLabel.enableAutoSizing = true;
            _journalLabel.fontSizeMin = 18;
            _journalLabel.fontSizeMax = 26;
            _automatic = ui.Button(root, "AutomaticReading", string.Empty, 736f, 1008f, 182f, 54f,
                reader.ToggleAutomatic);
            _automaticLabel = _automatic.GetComponentInChildren<TMP_Text>();
            _automaticLabel.fontSize = 22;
            var textSpeed = ui.Button(root, "TextSpeed", string.Empty, 934f, 1008f, 182f, 54f,
                reader.CycleTextSpeed);
            _textSpeedLabel = textSpeed.GetComponentInChildren<TMP_Text>();
            _textSpeedLabel.fontSize = 22;
            reader.ReadingModeChanged += RefreshReadingControls;
            RefreshReadingControls();
            ui.Button(root, "MainMenuButton", "主界面", 1630f, 1008f, 210f, 54f,
                commands.ReturnToMainMenu);

            var noticePanel = ui.Frame(root, "NoticePanel", 1132f, 1008f, 474f, 54f);
            noticePanel.color = new(0.82f, 0.88f, 0.82f);
            _notice = ui.Label(root, "Notice", string.Empty, 1152f, 1010f, 434f, 50f, 22);
            _notice.enableAutoSizing = true;
            _notice.fontSizeMin = 16;
            _notice.fontSizeMax = 22;
            _notice.overflowMode = TextOverflowModes.Truncate;
            _notice.font = assets.MenuFont;
            _notice.color = new(0.94f, 0.91f, 0.78f);
            _notice.alignment = TextAlignmentOptions.Center;
            var noticeShadow = _notice.gameObject.AddComponent<Shadow>();
            noticeShadow.effectColor = new(0f, 0f, 0f, 0.92f);
            noticeShadow.effectDistance = new(1.5f, -1.5f);
        }

        public void SetVisible(bool visible)
        {
            if (!visible)
            {
                _reader.StopAutomatic();
                FinishEndingTransition();
            }

            _root.SetActive(visible);
        }

        public void ShowNotice(string message)
        {
            _notice.text = message;
        }

        public void ShowUnreadClues(int count)
        {
            _journalLabel.text = count > 0 ? "线索 · " + count : "线索";
            _journalLabel.color = count > 0 ? new(1f, 0.85f, 0.55f) : new(0.96f, 0.94f, 0.82f);
        }

        public void ShowProgress(string message, bool demoBoundary)
        {
            _saveStatus.text = message;
            _demoCompletion.gameObject.SetActive(demoBoundary);
        }

        public void Render(StoryViewState state)
        {
            _reader.ConfigurePage(state.Choices.Length == 0, state.Terminal);
            _automatic.interactable = !state.Terminal;
            _observation.Render(state.Observation);
            _chapter.text = state.Chapter;
            _location.text = state.Location;
            _speaker.text = state.Speaker;
            _status.text = state.Status;
            var enteringEnding = state.Ending != null && !_endingPanel.activeSelf;
            if (state.Ending == null) FinishEndingTransition();
            _endingPanel.SetActive(state.Ending != null);
            if (state.Ending != null)
            {
                _endingTitle.text = state.Ending.Title;
                for (var index = 0; index < _endingConsequences.Length; index++)
                {
                    _endingConsequences[index].text = state.Ending.Consequences[index];
                }
                if (enteringEnding)
                {
                    _endingGroup.alpha = 0f;
                    _endingTransition = _coroutineRunner.StartCoroutine(FadeEnding());
                }
            }
            _next.gameObject.SetActive(state.Choices.Length == 0 && state.Ending == null);
            _next.interactable = !state.Terminal;
            _next.GetComponentInChildren<TMP_Text>().text = state.NextLabel;

            for (var index = 0; index < _choices.Length; index++)
            {
                RenderChoice(index, state.Choices);
            }
        }

        private IEnumerator FadeEnding()
        {
            var elapsed = 0f;
            while (elapsed < 0.45f)
            {
                _endingGroup.alpha = Mathf.SmoothStep(0f, 1f, elapsed / 0.45f);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            _endingGroup.alpha = 1f;
            _endingTransition = null;
        }

        private void FinishEndingTransition()
        {
            if (_endingTransition != null) _coroutineRunner.StopCoroutine(_endingTransition);
            _endingTransition = null;
            _endingGroup.alpha = 1f;
        }

        private void RefreshReadingControls()
        {
            _automaticLabel.text = _reader.AutomaticLabel;
            _automaticLabel.color = _reader.IsAutomatic
                ? new(1f, 0.85f, 0.55f)
                : new(0.96f, 0.94f, 0.82f);
            _textSpeedLabel.text = _reader.TextSpeedLabel;
        }

        private void CreateChoices(Transform parent, StoryUiFactory ui, IStoryCommands commands,
            TMP_FontAsset choiceFont)
        {
            for (var index = 0; index < _choices.Length; index++)
            {
                var slot = index;
                _choices[index] = ui.Button(parent, "Choice" + index, string.Empty, 96f,
                    286f + index * 94f, 700f, 78f, () => commands.SelectChoice(_choiceIds[slot]));
                var label = _choices[index].GetComponentInChildren<TMP_Text>();
                label.font = choiceFont;
                label.alignment = TextAlignmentOptions.Left;
                _choices[index].gameObject.AddComponent<ChoiceHint>();
            }
        }

        private void RenderChoice(int index, ChoiceViewState[] choices)
        {
            var visible = index < choices.Length;
            _choices[index].gameObject.SetActive(visible);
            if (!visible)
            {
                return;
            }

            var choice = choices[index];
            _choiceIds[index] = choice.Id;
            _choices[index].interactable = choice.Available;
            var choiceLabel = _choices[index].GetComponentInChildren<TMP_Text>();
            choiceLabel.text = choice.Label;
            choiceLabel.color = choice.Available
                ? new(0.96f, 0.94f, 0.82f)
                : new(0.70f, 0.73f, 0.68f);
            _choices[index].GetComponent<ChoiceHint>().Configure(
                _notice,
                choice.Available ? string.Empty : choice.LockedReason);
        }
    }
}
