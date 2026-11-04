using System;
using System.Reflection;
using SWeetJelly.Editor.Attributes;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace SWeetJelly.Editor
{
    internal static class NativePropertyControls
    {
        internal static VisualElement Create(SerializedProperty property, string label, FieldInfo field, Action onEdit = null)
        {
            var width = field?.GetCustomAttribute<LabelWidthAttribute>();
            var indent = field?.GetCustomAttribute<IndentAttribute>();
            if (width != null || indent != null)
            {
                return new IMGUIContainer(() =>
                {
                    if (property.serializedObject.targetObject == null) return;
                    property.serializedObject.UpdateIfRequiredOrScript();
                    EditorGUI.BeginChangeCheck();
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (indent != null) GUILayout.Space(indent.Space);
                        if (width != null)
                        {
                            using (new LabelWidthScope(width.Width)) EditorGUILayout.PropertyField(property, new GUIContent(label), true);
                        }
                        else EditorGUILayout.PropertyField(property, new GUIContent(label), true);
                    }
                    if (EditorGUI.EndChangeCheck()) onEdit?.Invoke();
                    property.serializedObject.ApplyModifiedProperties();
                });
            }
            var slider = field?.GetCustomAttribute<SliderAttribute>();
            if (slider != null && property.propertyType == SerializedPropertyType.Float)
            {
                var control = new UnityEngine.UIElements.Slider(label, slider.MinValue, slider.MaxValue) { showInputField = true };
                control.BindProperty(property);
                return control;
            }
            if (slider != null && property.propertyType == SerializedPropertyType.Integer)
            {
                var control = new SliderInt(label, (int)slider.MinValue, (int)slider.MaxValue) { showInputField = true };
                control.BindProperty(property);
                return control;
            }
            if (property.propertyType == SerializedPropertyType.Boolean &&
                (field?.GetCustomAttribute<ToggleAttribute>() != null || field?.GetCustomAttribute<OnOffAttribute>() != null))
            {
                var control = new Toggle(label);
                control.BindProperty(property);
                return control;
            }
            if (property.propertyType == SerializedPropertyType.Enum && field?.GetCustomAttribute<EnumFlagsAttribute>() != null)
            {
                var control = new EnumFlagsField(label, (Enum)Enum.ToObject(field.FieldType, property.intValue));
                control.BindProperty(property);
                return control;
            }
            var range = field?.GetCustomAttribute<MinMaxSliderAttribute>();
            if (range != null && property.propertyType == SerializedPropertyType.Vector2)
            {
                var control = new MinMaxSlider(label, property.vector2Value.x, property.vector2Value.y, range.MinValue, range.MaxValue);
                control.BindProperty(property);
                return control;
            }
            var result = new VisualElement();
            var preview = field?.GetCustomAttribute<ShowAssetPreviewAttribute>();
            if (property.propertyType == SerializedPropertyType.ObjectReference && (preview != null || field?.GetCustomAttribute<DrawReferenceAttribute>() != null))
            {
                var input = new ObjectField(label) { objectType = field.FieldType, allowSceneObjects = true };
                input.BindProperty(property);
                result.Add(input);
            }
            else result.Add(new PropertyField(property, label));
            if (preview != null && property.propertyType == SerializedPropertyType.ObjectReference)
            {
                // Preserve the legacy attribute's exact pixel dimensions at this compatibility leaf.
                result.Add(new IMGUIContainer(() =>
                {
                    if (property.serializedObject.targetObject == null) return;
                    var asset = property.objectReferenceValue;
                    if (asset != null) GUILayout.Label(AssetPreview.GetAssetPreview(asset) ?? AssetPreview.GetMiniThumbnail(asset), GUILayout.Width(preview.Width), GUILayout.Height(preview.Height));
                }));
            }
            if (field?.GetCustomAttribute<DrawReferenceAttribute>() != null && property.propertyType == SerializedPropertyType.ObjectReference)
            {
                var foldout = new Foldout { text = label + " Details" };
                result.Add(foldout);
                SerializedObject nested = null;
                UnityEngine.Object previous = null;
                void UpdateReference()
                {
                    var next = property.objectReferenceValue;
                    if (next == previous) return;
                    previous = next;
                    foldout.Clear();
                    nested?.Dispose();
                    nested = null;
                    if (next == null || next == property.serializedObject.targetObject) return;
                    nested = new SerializedObject(next);
                    InspectorElement.FillDefaultInspector(foldout, nested, null);
                }
                result.TrackPropertyValue(property, _ => UpdateReference());
                result.RegisterCallback<DetachFromPanelEvent>(_ => { nested?.Dispose(); nested = null; previous = null; });
                result.RegisterCallback<AttachToPanelEvent>(_ => UpdateReference());
            }
            return result;
        }
    }
}
