using WenXinJie.Editor;

internal sealed class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        CampaignSetup.Run();
        result.Log("Campaign scene and story asset catalog ready.");
    }
}
