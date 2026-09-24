using WenXinJie.Editor;

internal sealed class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        CampaignPlayModeChecks.Run();
        result.Log("Campaign Play Mode checks started.");
    }
}
