using UnityEngine;
using WenXinJie.Editor;

internal sealed class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        CampaignSmokeTests.Run();
        if (Application.isPlaying)
        {
            CampaignPlayModeChecks.Run();
            result.Log("Campaign smoke tests finished; Play Mode checks started.");
        }
        else
        {
            result.Log("Campaign smoke tests finished; enter Play Mode for UI checks.");
        }
    }
}
