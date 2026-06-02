using System;
using UnityEngine;

namespace SWeetJelly.Editor.Attributes
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class EnumFlagsAttribute : PropertyAttribute
    {
        public EnumFlagsAttribute()
        {

        }
    }
}
