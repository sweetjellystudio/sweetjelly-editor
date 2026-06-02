using UnityEditor;
using UnityEngine;
using SWeetJelly.Editor.Attributes;

namespace SWeetJelly.Editor
{
    [CustomPropertyDrawer(typeof(ShowAssetPreviewAttribute))]
    public class ShowAssetPreviewDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // 속성을 가져옵니다.
            var previewAttribute = (ShowAssetPreviewAttribute)attribute;

            Rect fieldPositon = new Rect(position.x, position.y, position.width, EditorGUI.GetPropertyHeight(property));
            // 기본 필드 렌더링
            EditorGUI.PropertyField(fieldPositon, property, label);

            // 프리뷰를 위한 공간 계산
            position.y += EditorGUI.GetPropertyHeight(property);
            position.height = previewAttribute.Height;
            position.width = previewAttribute.Width;

            // 에셋이 Object 타입인지 확인
            if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue != null)
            {
                // 에셋 프리뷰를 그립니다.
                Texture preview = AssetPreview.GetAssetPreview(property.objectReferenceValue);
                if (preview != null)
                {
                    GUI.DrawTexture(position, preview, ScaleMode.ScaleToFit);
                }
            }
            else
            {
                string warning = "[ShowAssetPreviewAttribute] can be used only on Unity Object field";

                EditorGUI.HelpBox(position, warning, MessageType.Warning);

                Debug.LogWarning(warning);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var previewAttribute = (ShowAssetPreviewAttribute)attribute;

            // 기본 높이 + 프리뷰 높이
            return EditorGUI.GetPropertyHeight(property) + previewAttribute.Height;
        }
    }
}