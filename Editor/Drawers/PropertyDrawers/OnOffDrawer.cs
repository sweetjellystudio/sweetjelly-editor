using UnityEditor;
using UnityEngine;

using SWeetJelly.Editor.Attributes;

namespace SWeetJelly.Editor
{
    [CustomPropertyDrawer(typeof(OnOffAttribute))]
    public class OnOffDrawer : PropertyDrawer
    {
        private static readonly string[] TOOLBAR_OPTIONS = { "On", "Off" };

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            if (property.propertyType == SerializedPropertyType.Boolean)
            {
                EditorGUI.PrefixLabel(position, label);

                OnOffAttribute onOffAttribute = (OnOffAttribute)attribute;
                if(onOffAttribute.IsWide)
                {
                    property.boolValue = GUI.Toolbar(new Rect(position.x + EditorGUIUtility.labelWidth, position.y, position.width - EditorGUIUtility.labelWidth, position.height), property.boolValue ? 0 : 1, TOOLBAR_OPTIONS) == 0;
                }
                else
                {
                    property.boolValue = GUI.Toolbar(new Rect(position.x + EditorGUIUtility.labelWidth, position.y, 64, position.height), property.boolValue ? 0 : 1, TOOLBAR_OPTIONS) == 0;
                }
            }
            else
            {
                string warning = "[ToggleAttribute] can be used only on bool fields";

                EditorGUI.HelpBox(position, warning, MessageType.Warning);

                Debug.LogWarning(warning);
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }
    }
}
