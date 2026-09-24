using UnityEditor;

internal sealed class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        EditorApplication.isPlaying = false;
        result.Log("Requested exit from Play Mode.");
    }
}
