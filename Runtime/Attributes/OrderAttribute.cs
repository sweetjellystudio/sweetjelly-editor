using System;

namespace SWeetJelly.Editor.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class OrderAttribute : Attribute
    {
        public int Order { get; private set; }

        public OrderAttribute(int order)
        {
            Order = order;
        }
    }
}
