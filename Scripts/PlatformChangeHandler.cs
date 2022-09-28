using UnityEditor;
using UnityEngine;

namespace ProjectContextSwitcher
{
    [SwitchHandler(Label = "Platform Change", Order = int.MinValue)]
    [CreateAssetMenu(menuName = "Project Context Switcher/Platform Change Handler", fileName = "NewPlatformChangeHandler")]
    public class PlatformChangeHandler : ContextSwitchHandler
    {
        [SerializeField] private BuildTargetGroup _buildTargetGroup;
        [SerializeField] private BuildTarget _buildTarget;

        private bool SameBuildTargetGroup => EditorUserBuildSettings.activeBuildTarget == _buildTarget;
        private bool SameBuildTarget => EditorUserBuildSettings.selectedBuildTargetGroup == _buildTargetGroup;

        public override void Switch(out string report)
        {
            report = $"Build target group {(SameBuildTargetGroup ? $"remain {EditorUserBuildSettings.selectedBuildTargetGroup}" : $"changed {EditorUserBuildSettings.selectedBuildTargetGroup} -> {_buildTargetGroup}")}" +
                $"\nBuild target {(SameBuildTarget ? $"remain {EditorUserBuildSettings.activeBuildTarget}" : $"changed {EditorUserBuildSettings.activeBuildTarget} -> {_buildTarget}")}";
            EditorUserBuildSettings.SwitchActiveBuildTarget(_buildTargetGroup, _buildTarget);
        }
        public override void Unswitch(out string report) { report = string.Empty; }
        public override bool ValidateProject() => SameBuildTargetGroup && SameBuildTarget;
    }
}