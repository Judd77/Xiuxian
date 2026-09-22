using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        if (SceneManager.GetActiveScene().path != "Assets/_WenXinJie/Scenes/Prototype.unity")
        {
            throw new System.InvalidOperationException("Open Prototype scene before running checks.");
        }
        EditorApplication.isPlaying = true;
        result.Log("Requested Play Mode for Prototype.");
    }
}
