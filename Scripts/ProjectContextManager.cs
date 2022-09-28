using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectContextSwitcher
{
    [CreateAssetMenu(menuName = "Project Context Switcher/Project Context Manager", fileName = "ProjectContextManager")]
    public class ProjectContextManager : ScriptableObject
    {
        [SerializeField] private List<ProjectContext> _projectContexts = new List<ProjectContext>();
        [SerializeField][HideInInspector] private ProjectContext _activatedProjectContext;

        public IReadOnlyCollection<ProjectContext> ProjectContexts => _projectContexts;

        public void Registrate(ProjectContext projectContext) => _projectContexts.Add(projectContext);
        public void Deregistrate(ProjectContext projectContext) => _projectContexts.Remove(projectContext);
        public void SwitchTo(in int projectContextIndex)
        {
            if (projectContextIndex > _projectContexts.Count)
                throw new System.IndexOutOfRangeException($"There is no {projectContextIndex} in {name} which have only {_projectContexts.Count} contexts");

            SwitchToInternal(_projectContexts[projectContextIndex]);
        }
        public void SwitchTo(ProjectContext context)
        {
            if (!_projectContexts.Contains(context))
                throw new System.InvalidOperationException($"There is no {context.Name} in {name}");

            SwitchToInternal(context);
        }
        public void UnswitchFrom(ProjectContext context)
        {
            UnswitchFromInternal(context, out var report);
            Debug.Log(report);
        }
        private void SwitchToInternal(ProjectContext context)
        {
            var unswitchReport = string.Empty;

            if (_activatedProjectContext != null)
                UnswitchFromInternal(_activatedProjectContext, out unswitchReport);

            _activatedProjectContext = context;
            _activatedProjectContext.Switch(out var switchReport);

            if (unswitchReport != string.Empty)
                switchReport = unswitchReport + "\n" + switchReport;

            Debug.Log(switchReport);
        }
        private void UnswitchFromInternal(ProjectContext context, out string unswitchReport)
        {
            if (context == null)
                throw new ArgumentNullException($"Passed context you want unswitch from is NULL");
            if (!IsCurrent(context))
                throw new ArgumentException($"{context.Name} isn't active so can't be unswitched from");

            unswitchReport = string.Empty;

            if (_activatedProjectContext != null)
                _activatedProjectContext.Unswitch(out unswitchReport);

            _activatedProjectContext = null;
        }
        public bool IsCurrent(ProjectContext projectContext) => _activatedProjectContext == projectContext;
    }
}