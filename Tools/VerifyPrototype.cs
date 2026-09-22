using UnityEngine;
using WenXinJie.Editor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        PrototypePlayModeChecks.Run();
        result.Log("Started cross-frame Play Mode checks; query CheckVerification for completion.");
    }
}
