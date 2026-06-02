using System;

namespace SWeetJelly.Editor.Attributes
{
    public abstract class MetaAttribute : Attribute
    {
        public int Order { get; set; }
    }
}