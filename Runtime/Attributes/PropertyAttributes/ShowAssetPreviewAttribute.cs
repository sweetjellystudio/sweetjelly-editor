using System;
using UnityEngine;

namespace SWeetJelly.Editor.Attributes
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class ShowAssetPreviewAttribute : PropertyAttribute
    {
        private int width;
        private int height;

        public int Width => width;

        public int Height => height;

        public ShowAssetPreviewAttribute(int width = 64, int height = 64)
        {
            this.width = width;
            this.height = height;
        }

    }
}