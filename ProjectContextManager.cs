using System.Collections.Generic;
using UnityEngine;

namespace ProjectContextSwitcher
{
    public class ProjectContextManager : ScriptableObject
    {
        [SerializeField] private List<ProjectContext> _projectContexts = new List<ProjectContext>();
        [SerializeField][HideInInspector] private ProjectContext _activatedProjectContext;

        public IReadOnlyCollection<ProjectContext> ProjectContexts => _projectContexts;

        public void Registrate(ProjectContext projectContext) => _projectContexts.Add(projectContext);
        public void Deregistrate(ProjectContext projectContext) => _projectContexts.Remove(projectContext);
        public void SwitchTo(in int projectContextIndex)
        {
            var unswitchReport = string.Empty;
            if(_activatedProjectContext != null)
                _activatedProjectContext.Unswitch(out unswitchReport);
            _activatedProjectContext = _projectContexts[projectContextIndex];
            _activatedProjectContext.Switch(out var switchReport);
            if (unswitchReport != string.Empty)
                switchReport = unswitchReport + "\n" + switchReport;
            Debug.Log(switchReport);
        }
        public bool IsCurrent(ProjectContext projectContext) => _activatedProjectContext == projectContext;
    }
}