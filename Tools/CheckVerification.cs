using WenXinJie.Editor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        result.Log("Running: " + PrototypePlayModeChecks.IsRunning);
        result.Log(PrototypePlayModeChecks.LastReport);
    }
}
