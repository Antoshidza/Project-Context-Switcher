using System;

namespace ProjectContextSwitcher
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class SwitchHandlerAttribute : Attribute
    {
        public string Label;
        public int Order;
    }
}