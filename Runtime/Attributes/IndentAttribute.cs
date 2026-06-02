using System;

namespace SWeetJelly.Editor.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class IndentAttribute : Attribute
    {
        public float Space { get; private set; }

        public IndentAttribute(float space = 32) 
        { 
            Space = space;
        }
    }
}
