using System;

namespace SWeetJelly.Editor.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public abstract class BaseDrawerAttribute : Attribute
    {
        private Type targetAttributeType;

        public Type TargetAttributeType => targetAttributeType;

        public BaseDrawerAttribute(Type targetAttributeType)
        {
            this.targetAttributeType = targetAttributeType;
        }
    }
}
