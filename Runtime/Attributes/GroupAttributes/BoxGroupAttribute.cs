using System;

namespace SWeetJelly.Editor.Attributes
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class BoxGroupAttribute : GroupAttribute
    {
        public BoxGroupAttribute(string id = "")
            : base(id)
        {
        }
    }
}
