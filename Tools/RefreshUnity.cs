using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        AssetDatabase.Refresh();
        result.Log("Requested asset refresh and script compilation.");
    }
}
