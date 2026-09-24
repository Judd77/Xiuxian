using UnityEditor;

internal sealed class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        PlayModeWindow.SetCustomRenderingResolution(1920, 1080, "WenXinJie Validation");
        EditorApplication.ExecuteMenuItem("Window/General/Game");
        result.Log("Requested 1920x1080 Game View.");
    }
}
