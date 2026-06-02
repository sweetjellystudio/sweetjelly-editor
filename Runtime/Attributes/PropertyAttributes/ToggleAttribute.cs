using System;
using UnityEngine;

namespace SWeetJelly.Editor.Attributes
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class ToggleAttribute : PropertyAttribute
    {
    }
}