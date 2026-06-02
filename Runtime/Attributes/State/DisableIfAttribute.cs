using System;

namespace SWeetJelly.Editor.Attributes
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class DisableIfAttribute : Attribute
    {
        public string ConditionName { get; private set; }

        public DisableIfAttribute(string conditionName)
        {
            ConditionName = conditionName;
        }
    }
}
