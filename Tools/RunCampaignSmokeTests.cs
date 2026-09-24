using WenXinJie.Editor;

internal sealed class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        CampaignSmokeTests.Run();
        result.Log("Campaign smoke tests completed.");
    }
}
