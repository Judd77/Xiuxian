using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

internal sealed class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        result.Log(
            "Project: " + Application.dataPath
            + "; scene: " + SceneManager.GetActiveScene().path
            + "; play: " + EditorApplication.isPlaying
            + "; compiling: " + EditorApplication.isCompiling);
        result.Log("Screen: " + Screen.width + "x" + Screen.height);
        foreach (Canvas canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            result.Log(
                "Canvas: " + canvas.name
                + "; active: " + canvas.isActiveAndEnabled
                + "; display: " + canvas.targetDisplay
                + "; rect: " + canvas.pixelRect);
        }
    }
}
