using UnityEditor;
using UnityEngine;
using WenXinJie.UI;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        EditorApplication.ExecuteMenuItem("Window/General/Game");
        FirstDayPrototype prototype = Object.FindFirstObjectByType<FirstDayPrototype>();
        var reader = prototype.GetComponent<DialogueReader>();
        prototype.Begin();
        for (int index = 0; index < 5; index++)
        {
            reader.Complete();
            prototype.Advance();
        }

        reader.Complete();
        ScreenCapture.CaptureScreenshot("Docs/Validation/prototype-actions-1080.png");
        result.Log("Showing action selection and requesting a screenshot.");
    }
}
