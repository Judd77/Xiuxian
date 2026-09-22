using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        EditorApplication.isPlaying = false;
        result.Log("Requested exit from Play Mode.");
    }
}
