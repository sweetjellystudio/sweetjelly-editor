using System;
using System.Collections.Generic;
using System.Linq;
using SWeetJelly.Editor.Attributes;

namespace SWeetJelly.Editor
{
    public static class CustomAttributesDatabase
    {
        private static Dictionary<Type, PropertyGrouperDrawer> groupersAttributes;
        private static Dictionary<Type, PropertyConditionDrawer> conditionAttributes;

        static CustomAttributesDatabase()
        {
            CacheDrawer(out groupersAttributes, typeof(CustomPropertyGrouperDrawerAttribute));
            CacheDrawer(out conditionAttributes, typeof(CustomPropertyConditionDrawerAttribute));
        }

        public static PropertyGrouperDrawer GetGroupAttribute(Type attributeType)
        {
            if (groupersAttributes.TryGetValue(attributeType, out PropertyGrouperDrawer grouper)) { return grouper; }

            return null;
        }

        public static PropertyConditionDrawer GetConditionAttribute(Type attributeType)
        {
            if (conditionAttributes.TryGetValue(attributeType, out PropertyConditionDrawer condition)) { return condition; }

            return null;
        }

        private static void CacheDrawer<T>(out Dictionary<Type, T> dictionary, Type attributeType)
        {
            dictionary = new Dictionary<Type, T>();

            Type drawerType = typeof(T);

            IEnumerable<Type> types = drawerType.Assembly.GetTypes().Where(x => !x.IsAbstract && x.IsSubclassOf(drawerType));
            foreach (Type t in types)
            {
                BaseDrawerAttribute attribute = (BaseDrawerAttribute)Attribute.GetCustomAttribute(t, attributeType);
                if (attribute != null)
                {
                    dictionary.Add(attribute.TargetAttributeType, (T)Activator.CreateInstance(t));
                }
            }
        }
    }
}

