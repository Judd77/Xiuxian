using WenXinJie.Editor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        CampaignSmokeTests.Run();
        result.Log("Campaign smoke tests completed.");
    }
}
