using UnityEngine;
using WenXinJie.Editor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        CampaignSetup.Run();
        result.Log("Campaign scene and media catalog ready.");
    }
}
