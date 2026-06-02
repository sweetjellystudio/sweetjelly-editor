using System;
using UnityEngine;

namespace SWeetJelly.Editor.Attributes
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class LineSpacerAttribute : PropertyAttribute
    {
        public string Title {get; private set;}
        public int Height {get; private set;}

        public LineSpacerAttribute(string title = "", int height = 16)
        {
            Title = title;
            Height = height;
        }
    }
}
