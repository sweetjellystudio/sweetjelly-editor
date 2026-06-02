using UnityEditor;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using SWeetJelly.Editor.Attributes;

namespace SWeetJelly.Editor
{
    public static class PropertyUtility
    {
        public static readonly Type TYPE_BOOL = typeof(bool);

        public static GUIRenderer[] GroupRenderers(SweetJellyEditor editor, IEnumerable<GUIRenderer> baseRenderers)
        {
            List<GroupGUIRenderer> groupGUIRenderers = new List<GroupGUIRenderer>();

            IGrouping<GroupAttribute, GUIRenderer>[] groupRenderers = baseRenderers
                .Where(x => x.GroupAttribute != null)
                .GroupBy(x => x.GroupAttribute).ToArray();
            
            foreach (IGrouping<GroupAttribute, GUIRenderer> groupRenderer in groupRenderers)
            {
                groupGUIRenderers.Add(new GroupGUIRenderer(editor, groupRenderer.Key, groupRenderer.ToList()));
            }

            Dictionary<string, GroupGUIRenderer> groupDictionary = groupGUIRenderers.ToDictionary(g => g.GroupID, g => g);

            for(int i=0; i<groupGUIRenderers.Count;i++)
            {
                GroupGUIRenderer groupGUIRenderer = groupGUIRenderers[i];

                if(!string.IsNullOrEmpty(groupGUIRenderer.ParentPath))
                {
                    if(groupDictionary.TryGetValue(groupGUIRenderer.ParentPath, out var parentGroup))
                    {
                        parentGroup.AddRenderer(groupGUIRenderer);
                        groupGUIRenderers.Remove(groupGUIRenderer);

                        i--;
                    }
                }
            }

            GUIRenderer[] orderedRenderers = groupGUIRenderers.Concat(baseRenderers.Where(x => x.GroupAttribute == null)).OrderBy(x => x.Order).ToArray();

            groupGUIRenderers = null;
            groupDictionary = null;

            return orderedRenderers;
        }

        public static T GetAttribute<T>(FieldInfo fieldInfo) where T : Attribute
        {
            if(fieldInfo != null)
                return fieldInfo.GetCustomAttribute<T>();

            return null;
        }

        public static T GetAttribute<T>(PropertyInfo propertyInfo) where T : Attribute
        {
            if(propertyInfo != null)
                return propertyInfo.GetCustomAttribute<T>();

            return null;
        }

        public static T GetAttribute<T>(SerializedProperty property) where T : Attribute
        {
            T[] attributes = GetAttributes<T>(property);

            if(attributes != null && attributes.Length > 0)
                return attributes[0];

            return null;
        }

        public static T[] GetAttributes<T>(SerializedProperty property) where T : Attribute
        {
            Type targetType = GetTargetObject(property).GetType();

            foreach(Type type in GetClassNestedTypes(targetType))
            {
                FieldInfo fieldInfo = targetType.GetField(property.name, ReflectionUtils.FLAGS_INSTANCE);

                if(fieldInfo != null)
                    return (T[])fieldInfo.GetCustomAttributes(typeof(T), true);

            }

            return null;
        }

        public static UnityEngine.Object GetTargetObject(SerializedProperty property)
        {
            return property.serializedObject.targetObject;
        }

        public static List<Type> GetClassNestedTypes(Type type)
        {
            List<Type> typesList = new List<Type> { type };

            Type lastAddedType = type;

            while(lastAddedType.BaseType != null)
            {
                lastAddedType = lastAddedType.BaseType;
                typesList.Add(lastAddedType);
            }

            return typesList;
        }

        public static string GetSubstringBeforeLastSlash(string text)
        {
            if(string.IsNullOrEmpty(text))
                return text;
            
            int lastSlashIndex = text.LastIndexOf('/');

            if(lastSlashIndex == -1 || lastSlashIndex == text.Length - 1)
            {
                return string.Empty;
            }

            return text.Substring(0, lastSlashIndex);
        }
    }
}