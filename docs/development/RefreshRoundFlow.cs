using UnityEditor;
using UnityEditor.Compilation;
public static class RefreshRoundFlow
{
    public static object Main()
    {
        AssetDatabase.ImportAsset("Assets/Game/Tests/EditMode/CoreGuardianRoundFlowTests.cs",ImportAssetOptions.ForceUpdate);
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        CompilationPipeline.RequestScriptCompilation();
        return "Requested round flow compilation";
    }
}
