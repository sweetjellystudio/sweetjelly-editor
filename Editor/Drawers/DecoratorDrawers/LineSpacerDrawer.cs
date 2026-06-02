using UnityEngine;
using UnityEditor;
using SWeetJelly.Editor.Attributes;

namespace SWeetJelly.Editor
{
    [CustomPropertyDrawer(typeof(LineSpacerAttribute))]
    public class LineSpacerDrawer : DecoratorDrawer
    {
        public override void OnGUI(Rect position)
        {
            LineSpacerAttribute lineSpacer = (LineSpacerAttribute)attribute;

            if(!string.IsNullOrEmpty(lineSpacer.Title))
            {
                EditorGUI.LabelField(new Rect(position.x, position.y + lineSpacer.Height - 12, position.width, 18), lineSpacer.Title, EditorStyles.boldLabel);
                EditorGUI.LabelField(new Rect(position.x, position.y + lineSpacer.Height, position.width, lineSpacer.Height), "", GUI.skin.horizontalSlider);
            }
            else
            {
                EditorGUI.LabelField(new Rect(position.x, position.y, position.width, lineSpacer.Height), "", GUI.skin.horizontalSlider);
            }
        }

        public override float GetHeight()
        {
            LineSpacerAttribute lineSpacer = (LineSpacerAttribute)attribute;

            float height = base.GetHeight();
            if (!string.IsNullOrEmpty(lineSpacer.Title))
            {
                height += lineSpacer.Height;
            }

            return height;
        }
    }
}