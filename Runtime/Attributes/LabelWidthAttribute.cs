using System;

namespace SWeetJelly.Editor.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class LabelWidthAttribute : Attribute
    {
        public float Width { get; private set; }

        public LabelWidthAttribute(float width)
        {
            Width = width;
        }
    }
}
