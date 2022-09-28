using UnityEngine;

namespace ProjectContextSwitcher
{
    public abstract class ContextSwitchHandler : ScriptableObject
    {
        public abstract void Switch(out string report);
        public abstract void Unswitch(out string report);
        public abstract bool ValidateProject();
    }
}