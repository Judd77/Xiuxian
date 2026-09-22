using UnityEditor;
using UnityEngine;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        result.Log("Focused=" + Application.isFocused + "; run in background=" + Application.runInBackground);
        SessionState.SetBool("WenXinJie.Validation.PreviousRunInBackground", Application.runInBackground);
        Application.runInBackground = true;
        result.Log("Editor muted=" + EditorUtility.audioMasterMute + "; editor paused=" + EditorApplication.isPaused + "; listener paused=" + AudioListener.pause
            + "; listener volume=" + AudioListener.volume + "; DSP time=" + AudioSettings.dspTime
            + "; output rate=" + AudioSettings.outputSampleRate);
        foreach (AudioSource source in Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
        {
            result.Log(source.name + ": enabled=" + source.isActiveAndEnabled + "; loop=" + source.loop
                + "; clip state=" + source.clip.loadState + "; time=" + source.time);
        }
    }
}
