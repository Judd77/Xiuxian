using WenXinJie.Editor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        result.Log("Running={0}; Report={1}", CampaignPlayModeChecks.IsRunning, CampaignPlayModeChecks.LastReport);
    }
}
