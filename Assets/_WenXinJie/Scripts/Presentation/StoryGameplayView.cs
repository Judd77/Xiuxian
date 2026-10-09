using System;
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

        public StoryGameplayView(RectTransform root, StoryUiFactory ui, StoryAssetCatalog assets,
            DialogueReader reader, IStoryCommands commands, Action openJournal, Func<bool> inputBlocked)
        {
            _root = root.gameObject;
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
            _status = ui.Label(root, "Status", string.Empty, 86f, 102f, 1734f, 30f, 19);
            _status.font = assets.Font;
            _status.color = new(0.84f, 0.88f, 0.85f);

            ui.Frame(root, "LocationPanel", 80f, 166f, 620f, 74f);
            _location = ui.Label(root, "Location", string.Empty, 110f, 178f, 560f, 50f, 28);
            _location.font = assets.MenuFont;
            _location.alignment = TextAlignmentOptions.Left;

            CreateChoices(root, ui, commands, assets.Font);
            _observation = new(root, ui);

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
            ui.Button(root, "ClueJournal", "线索", 566f, 1008f, 146f, 54f, openJournal);
            ui.Button(root, "MainMenuButton", "主界面", 1630f, 1008f, 210f, 54f,
                commands.ReturnToMainMenu);

            var noticePanel = ui.Frame(root, "NoticePanel", 736f, 1008f, 870f, 54f);
            noticePanel.color = new(0.82f, 0.88f, 0.82f);
            _notice = ui.Label(root, "Notice", string.Empty, 756f, 1010f, 830f, 50f, 22);
            _notice.font = assets.MenuFont;
            _notice.color = new(0.94f, 0.91f, 0.78f);
            _notice.alignment = TextAlignmentOptions.Center;
            var noticeShadow = _notice.gameObject.AddComponent<Shadow>();
            noticeShadow.effectColor = new(0f, 0f, 0f, 0.92f);
            noticeShadow.effectDistance = new(1.5f, -1.5f);
        }

        public void SetVisible(bool visible)
        {
            _root.SetActive(visible);
        }

        public void ShowNotice(string message)
        {
            _notice.text = message;
        }

        public void Render(StoryViewState state)
        {
            _observation.Render(state.Observation);
            _chapter.text = state.Chapter;
            _location.text = state.Location;
            _speaker.text = state.Speaker;
            _status.text = state.Status;
            _next.gameObject.SetActive(state.Choices.Length == 0);
            _next.interactable = !state.Terminal;
            _next.GetComponentInChildren<TMP_Text>().text = state.NextLabel;

            for (var index = 0; index < _choices.Length; index++)
            {
                RenderChoice(index, state.Choices);
            }
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
