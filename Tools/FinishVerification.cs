using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        GameObject settingsClose = GameObject.Find("SettingsClose");
        if (settingsClose != null)
        {
            settingsClose.GetComponent<Button>().onClick.Invoke();
        }

        GameObject.Find("Title").GetComponent<Button>().onClick.Invoke();
        Application.runInBackground = SessionState.GetBool("WenXinJie.Validation.PreviousRunInBackground", false);
        EditorApplication.isPlaying = false;
        result.Log("Restored background setting and requested exit from Play Mode; Prototype remains open.");
    }
}
