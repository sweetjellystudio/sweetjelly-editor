using System;

namespace SWeetJelly.Editor.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    public class InlineButtonAttribute : Attribute
    {
        public string MethodName { get; private set; }
        public string Title { get; private set; }

        public InlineButtonAttribute(string methodName = null, string title = "DO")
        {
            MethodName = methodName;
            Title = title;
        }
    }
}
