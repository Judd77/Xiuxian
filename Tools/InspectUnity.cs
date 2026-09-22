using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        result.Log("Project: " + Application.dataPath + "; scene: " + SceneManager.GetActiveScene().path + "; play: " + EditorApplication.isPlaying + "; compiling: " + EditorApplication.isCompiling);
        result.Log("Screen: " + Screen.width + "x" + Screen.height);
        foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            result.Log("Canvas: " + canvas.name + "; active: " + canvas.isActiveAndEnabled + "; display: " + canvas.targetDisplay + "; rect: " + canvas.pixelRect);
        }
    }
}
