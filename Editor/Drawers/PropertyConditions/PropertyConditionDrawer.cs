using System.Collections.Generic;
using System.Reflection;
using System;

namespace SWeetJelly.Editor
{
    public abstract class PropertyConditionDrawer
    {
        public abstract bool CanBeDrawn(SweetJellyEditor editor, FieldInfo fieldInfo, object targetObject, List<Type> nestedTypes);
    }
}
