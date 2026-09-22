using System;
using UnityEngine;
using UnityEngine.UI;
using WenXinJie.UI;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        GameObject.Find("Settings").GetComponent<Button>().onClick.Invoke();
        var audio = UnityEngine.Object.FindFirstObjectByType<FirstDayAudio>();
        var music = GameObject.Find("MusicVolume").GetComponent<Slider>();
        var rain = GameObject.Find("AmbienceVolume").GetComponent<Slider>();
        float previousMusic = music.value;
        float previousRain = rain.value;
        try
        {
            music.value = 0.62f;
            rain.value = 0.44f;
            if (!Mathf.Approximately(audio.MusicVolume, 0.62f)
                || !Mathf.Approximately(audio.AmbienceVolume, 0.44f))
            {
                throw new InvalidOperationException("Volume controls did not reach the audio component.");
            }
        }
        finally
        {
            music.value = previousMusic;
            rain.value = previousRain;
        }

        ScreenCapture.CaptureScreenshot("Docs/Validation/sound-settings.png");
        result.Log("Both volume controls passed; original values restored; requested settings screenshot.");
    }
}
