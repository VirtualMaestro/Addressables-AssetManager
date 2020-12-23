using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Asset_Manager.Editor
{
    public class Il2CPPCustomFlag : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
#if UNITY_IOS || UNITY_ANDROID
        UnityEditor.ScriptingImplementation backend = UnityEditor.PlayerSettings.GetScriptingBackend(UnityEditor.BuildPipeline.GetBuildTargetGroup(UnityEditor.EditorUserBuildSettings.activeBuildTarget));
        if (backend == UnityEditor.ScriptingImplementation.IL2CPP)
        {
            UnityEditor.PlayerSettings.SetAdditionalIl2CppArgs("--maximum-recursive-generic-depth=50");
        }
#endif
        }
    }
}