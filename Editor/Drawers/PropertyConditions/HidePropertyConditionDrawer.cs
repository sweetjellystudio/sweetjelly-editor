using System.Reflection;
using System;
using System.Collections.Generic;
using SWeetJelly.Editor.Attributes;

namespace SWeetJelly.Editor
{
    [CustomPropertyConditionDrawer(typeof(HideAttribute))]
    public class HidePropertyConditionDrawer : PropertyConditionDrawer
    {
        public override bool CanBeDrawn(SweetJellyEditor editor, FieldInfo fieldInfo, object targetObject, List<Type> nestedTypes)
        {
            return false;
        }
    }
}