using System;
using UnityEngine;
using UnityEngine.UI;
using WenXinJie.Editor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        PrototypePlayModeChecks.VerifyVisibleButtonRaycasts();
        foreach (Text label in UnityEngine.Object.FindObjectsByType<Text>(FindObjectsSortMode.None))
        {
            if (label.preferredHeight > label.rectTransform.rect.height + 1f)
            {
                throw new InvalidOperationException("Text exceeds its box: " + label.name);
            }
        }

        foreach (AudioSource source in UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
        {
            var samples = new float[1024];
            source.GetOutputData(samples, 0);
            float peak = 0f;
            foreach (float sample in samples)
            {
                peak = Mathf.Max(peak, Mathf.Abs(sample));
            }

            result.Log(source.name + ": playing=" + source.isPlaying + "; length=" + source.clip.length
                + "; volume=" + source.volume + "; output peak=" + peak);
        }

        string path = "Docs/Validation/prototype-" + Screen.width + "x" + Screen.height + ".png";
        ScreenCapture.CaptureScreenshot(path);
        result.Log("Raycasts and text fit passed at " + Screen.width + "x" + Screen.height
            + "; requested screenshot: " + path);
    }
}
