using UnityEngine;
using UnityEditor;
using SWeetJelly.Editor.Attributes;

namespace SWeetJelly.Editor
{
    [CustomPropertyDrawer(typeof(MinMaxSliderAttribute))]
    public class MinMaxSliderDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            if (property.propertyType == SerializedPropertyType.Vector2)
            {
                Vector2 value = property.vector2Value;

                if (DrawSlider(position, label, ref value.x, ref value.y))
                {
                    property.vector2Value = value;
                }
            }
            else if(property.propertyType == SerializedPropertyType.Generic && property.type == "DuoFloat")
            {
                SerializedProperty firstSerializedProperty = property.FindPropertyRelative("firstValue");
                SerializedProperty secondSerializedProperty = property.FindPropertyRelative("secondValue");

                float valueX = firstSerializedProperty.floatValue;
                float valueY = secondSerializedProperty.floatValue;

                if (DrawSlider(position, label, ref valueX, ref valueY))
                {
                    firstSerializedProperty.floatValue = valueX;
                    secondSerializedProperty.floatValue = valueY;
                }
            }
            else
            {
                string warning = "[MinMaxSlider] can be used only on Vector2 fields";

                EditorGUI.HelpBox(position, warning, MessageType.Warning);

                Debug.LogWarning(warning);
            }

            EditorGUI.EndProperty();
        }

        private bool DrawSlider(Rect controlRect, GUIContent label, ref float valueX, ref float valueY)
        {
            MinMaxSliderAttribute minMaxSliderAttribute = (MinMaxSliderAttribute)attribute;

            float labelWidth = EditorGUIUtility.labelWidth;
            float floatFieldWidth = EditorGUIUtility.fieldWidth;
            float sliderWidth = controlRect.width - labelWidth - 2f * floatFieldWidth;
            float sliderPadding = 4f;

            Rect labelRect = new Rect(
                controlRect.x,
                controlRect.y,
                labelWidth,
                controlRect.height);

            Rect sliderRect = new Rect(
                controlRect.x + labelWidth + floatFieldWidth + sliderPadding,
                controlRect.y,
                sliderWidth - (2f * sliderPadding),
                controlRect.height);

            Rect minFloatFieldRect = new Rect(
                controlRect.x + labelWidth,
                controlRect.y,
                floatFieldWidth,
                controlRect.height);

            Rect maxFloatFieldRect = new Rect(
                controlRect.x + controlRect.width - floatFieldWidth,
                controlRect.y,
                floatFieldWidth,
                controlRect.height);

            // Draw the label
            EditorGUI.LabelField(labelRect, label);

            // Update Input Field and Slider
            EditorGUI.BeginChangeCheck();

            // Draw Min Field
            valueX = EditorGUI.FloatField(minFloatFieldRect, valueX);
            valueX = Mathf.Clamp(valueX, minMaxSliderAttribute.MinValue, Mathf.Min(minMaxSliderAttribute.MaxValue, valueY));

            // Draw Slider
            EditorGUI.MinMaxSlider(sliderRect, ref valueX, ref valueY, minMaxSliderAttribute.MinValue, minMaxSliderAttribute.MaxValue);

            // Draw Max Field
            valueY = EditorGUI.FloatField(maxFloatFieldRect, valueY);
            valueY = Mathf.Clamp(valueY, Mathf.Max(minMaxSliderAttribute.MinValue, valueX), minMaxSliderAttribute.MaxValue);

            return EditorGUI.EndChangeCheck();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return 16;
        }
    }
}
