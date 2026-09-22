using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        if (SceneManager.GetActiveScene().path != "Assets/_WenXinJie/Scenes/Game.unity")
        {
            throw new InvalidOperationException("Open Game scene first.");
        }

        EditorApplication.isPlaying = true;
        result.Log("Requested campaign Play Mode.");
    }
}
