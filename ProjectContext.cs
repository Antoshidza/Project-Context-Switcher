using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ProjectContextSwitcher
{
    [CreateAssetMenu(menuName = "ProjectContext", fileName = "NewProjectContext")]
    public class ProjectContext : ScriptableObject
    {
        [SerializeField] public string Name;
        [SerializeField] private List<ContextSwitchHandler> _switchHandlers = new List<ContextSwitchHandler>();

        public IReadOnlyCollection<ContextSwitchHandler> SwitchHandlers => _switchHandlers;

        private struct HandlerComparer : IComparer<ContextSwitchHandler>
        {
            public int Compare(ContextSwitchHandler first, ContextSwitchHandler second)
            {
                return GetHandlerOrder(first).CompareTo(GetHandlerOrder(second));
            }
        }

        public void Switch(out string report)
        {
            report = $"<b>{Name} switch report:</b>\n";
            var handlersOrdered = GetContextSwitchHandlersOrdered();
            foreach (var handlerData in handlersOrdered)
            {
                handlerData.Switch(out var switchReport);
                if(switchReport != string.Empty)
                    report += $"{switchReport}\n\n";
            }
        }
        public void Unswitch(out string report)
        {
            report = $"<b>{Name} unswitch report:</b>\n";
            var handlersOrdered = GetContextSwitchHandlersOrdered();
            foreach (var handler in handlersOrdered)
            {
                handler.Unswitch(out var unswitchReport);
                if (unswitchReport != string.Empty)
                    report += $"{unswitchReport}\n\n";
            }
        }
        private IEnumerable<ContextSwitchHandler> GetContextSwitchHandlersOrdered()
        {
            _switchHandlers.Sort(new HandlerComparer());
            return _switchHandlers;
        }
        private static int GetHandlerOrder(ContextSwitchHandler handler) => handler.GetType().GetCustomAttributes(true).Where(attribute => attribute is SwitchHandlerAttribute).Select(attribute => attribute as SwitchHandlerAttribute).First().Order;
        public void AddHandler(ContextSwitchHandler handler) => _switchHandlers.Add(handler);
        public void RemoveHandler(ContextSwitchHandler handler) => _switchHandlers.Remove(handler);
    }
}