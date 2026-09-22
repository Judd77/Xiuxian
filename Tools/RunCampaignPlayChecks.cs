using WenXinJie.Editor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        CampaignPlayModeChecks.Run();
        result.Log("Campaign Play Mode checks started.");
    }
}
