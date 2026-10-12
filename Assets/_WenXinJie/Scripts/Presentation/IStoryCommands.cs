namespace WenXinJie.Presentation
{
    internal interface IStoryCommands
    {
        void AdvanceStory();

        void SelectChoice(string choiceId);

        void SaveGame();

        void LoadGame();

        void StartNewGame();

        void ContinueGame();

        void LoadGameFromMenu();

        void ReturnToMainMenu();

        void ShowDemoCompletion();

        void QuitGame();
    }
}
