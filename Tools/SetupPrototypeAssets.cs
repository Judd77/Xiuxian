using WenXinJie.Editor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        PrototypeAssetSetup.Run();
        CoreSmokeTests.Run();
        result.Log("Assets connected; core regression passed.");
    }
}
