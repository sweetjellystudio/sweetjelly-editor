using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SWeetJelly.Editor.Attributes;
using UnityEditor;
using UnityEngine;

namespace SWeetJelly.Editor
{
    public sealed class SerializedPropertyGUIRenderer : GUIRenderer
    {
        private readonly SweetJellyEditor editor;
        private readonly SerializedProperty property;
        private readonly FieldInfo field;
        private readonly GUIContent label;
        private readonly HashSet<string> diagnostics = new HashSet<string>();
        private bool conditionEnabled = true;

        public SerializedPropertyGUIRenderer(SweetJellyEditor editor, SerializedProperty serializedProperty, FieldInfo fieldInfo, object targetObject, List<Type> nestedTypes)
        {
            this.editor = editor; property = serializedProperty; field = fieldInfo;
            TabAttribute = PropertyUtility.GetAttribute<TabAttribute>(field);
            GroupAttribute = PropertyUtility.GetAttribute<GroupAttribute>(field);
            Order = PropertyUtility.GetAttribute<OrderAttribute>(field)?.Order ?? 0;
            label = new GUIContent(PropertyUtility.GetAttribute<LabelAttribute>(field)?.Label ?? property.displayName);
            Refresh();
        }

        private void Report(Exception exception)
        {
            exception = InspectorReflection.Unwrap(exception);
            if (diagnostics.Add(exception.GetType().Name + ":" + exception.Message)) Debug.LogException(exception, editor.target);
        }

        private bool Evaluate(Func<object, bool> condition, bool visibility)
        {
            try
            {
                var results = editor.targets.Select(root => condition(InspectorReflection.Owner(root, property.propertyPath))).ToArray();
                bool mixed = results.Any(value => value != results[0]);
                if (mixed) { conditionEnabled = false; return visibility; }
                return results[0];
            }
            catch (Exception exception) { Report(exception); conditionEnabled = false; return visibility; }
        }

        private void Refresh()
        {
            conditionEnabled = true;
            IsVisible = true;
            var condition = PropertyUtility.GetAttribute<ConditionAttribute>(field);
            if (condition is HideAttribute) IsVisible = false;
            else if (condition is ShowIfAttribute show) IsVisible = Evaluate(owner => InspectorReflection.Condition(owner, show.ConditionName), true);
            else if (condition is HideIfAttribute hide) IsVisible = Evaluate(owner => !InspectorReflection.Condition(owner, hide.ConditionName), true);
            else if (condition != null)
            {
                var drawer = CustomAttributesDatabase.GetConditionAttribute(condition.GetType());
                if (drawer != null) IsVisible = Evaluate(owner => drawer.CanBeDrawn(editor, field, owner, PropertyUtility.GetClassNestedTypes(owner.GetType())), true);
                else { conditionEnabled = false; Report(new InvalidOperationException("No condition drawer for " + condition.GetType().Name)); }
            }
        }

        public bool IsEnabled()
        {
            if (PropertyUtility.GetAttribute<ReadOnlyAttribute>(field) != null) return false;
            var enable = PropertyUtility.GetAttribute<EnableIfAttribute>(field);
            var disable = PropertyUtility.GetAttribute<DisableIfAttribute>(field);
            bool allowed = true;
            if (enable != null) allowed &= Evaluate(owner => InspectorReflection.Condition(owner, enable.ConditionName), false);
            if (disable != null) allowed &= Evaluate(owner => !InspectorReflection.Condition(owner, disable.ConditionName), false);
            return allowed && conditionEnabled;
        }

        public override void OnGUIChanged() { Refresh(); }

        public override void OnGUI()
        {
            Refresh();
            if (!IsVisible) return;
            var callback = PropertyUtility.GetAttribute<OnValueChangedAttribute>(field);
            var snapshots = callback == null ? null : editor.targets.Select(root => new SerializedObject(root)).ToArray();
            try
            {
                using (new EditorGUILayout.HorizontalScope())
                using (new EditorGUI.DisabledScope(!IsEnabled()))
                {
                    var indent = PropertyUtility.GetAttribute<IndentAttribute>(field);
                    if (indent != null) GUILayout.Space(indent.Space);
                    EditorGUI.BeginChangeCheck();
                    var width = PropertyUtility.GetAttribute<LabelWidthAttribute>(field);
                    if (width != null)
                    {
                        using (new LabelWidthScope(width.Width)) EditorGUILayout.PropertyField(property, label, true);
                    }
                    else EditorGUILayout.PropertyField(property, label, true);
                    bool changed = EditorGUI.EndChangeCheck();
                    if (changed && callback != null)
                    {
                        property.serializedObject.ApplyModifiedProperties();
                        var changedTargets = new List<UnityEngine.Object>();
                        for (int i = 0; i < snapshots.Length; i++)
                        {
                            using (var fresh = new SerializedObject(editor.targets[i]))
                                if (!SerializedProperty.DataEquals(snapshots[i].FindProperty(property.propertyPath), fresh.FindProperty(property.propertyPath))) changedTargets.Add(editor.targets[i]);
                        }
                        if (changedTargets.Count > 0) Invoke(changedTargets.ToArray(), callback.CallbackName, true);
                    }
                    var inline = PropertyUtility.GetAttribute<InlineButtonAttribute>(field);
                    if (inline != null && GUILayout.Button(inline.Title, EditorCustomStyles.ButtonMini)) Invoke(editor.targets, inline.MethodName, false);
                }
            }
            finally { if (snapshots != null) foreach (var snapshot in snapshots) snapshot.Dispose(); }
        }

        private void Invoke(UnityEngine.Object[] targets, string name, bool requireVoid)
        {
            try { InspectorReflection.Invoke(targets, property.propertyPath, name, null, requireVoid); }
            catch (Exception exception) { Report(exception); }
            finally { property.serializedObject.Update(); }
        }
    }
}
