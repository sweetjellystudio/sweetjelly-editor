using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SWeetJelly.Editor.Attributes;
using UnityEditor;
using UnityEngine;

namespace SWeetJelly.Editor
{
    public sealed class MethodGUIRenderer : GUIRenderer
    {
        private readonly SweetJellyEditor editor;
        private readonly MethodInfo method;
        private readonly ButtonAttribute attribute;
        private readonly string ownerPath;
        private readonly object explicitTarget;
        private readonly HashSet<string> diagnostics = new HashSet<string>();
        private bool enabled;

        public MethodGUIRenderer(SweetJellyEditor editor, MethodInfo methodInfo, object target, ButtonAttribute buttonAttribute)
            : this(editor, methodInfo, target, buttonAttribute, null) { }

        internal MethodGUIRenderer(SweetJellyEditor editor, MethodInfo methodInfo, object target, ButtonAttribute buttonAttribute, string ownerPath)
        {
            this.editor = editor; method = methodInfo; explicitTarget = target; attribute = buttonAttribute; this.ownerPath = ownerPath;
            TabAttribute = method.GetCustomAttribute<TabAttribute>();
            GroupAttribute = method.GetCustomAttribute<GroupAttribute>();
            Order = method.GetCustomAttribute<OrderAttribute>()?.Order ?? ORDER_METHOD;
            Refresh();
        }

        private void Report(Exception exception)
        {
            exception = InspectorReflection.Unwrap(exception);
            if (diagnostics.Add(exception.GetType().Name + ":" + exception.Message)) Debug.LogException(exception, editor.target);
        }

        private void Refresh()
        {
            IsVisible = true; enabled = true;
            try
            {
                InspectorReflection.Method(method.DeclaringType, method.Name, attribute.Params);
                if (string.IsNullOrEmpty(attribute.VisabilityConditionName)) return;
                var values = editor.targets.Select(root => InspectorReflection.Condition(ownerPath == null ? explicitTarget == editor.target ? root : explicitTarget : InspectorReflection.Value(root, ownerPath), attribute.VisabilityConditionName) ^ (attribute.VisibilityOption == ButtonVisibility.HideIf)).ToArray();
                bool mixed = values.Any(value => value != values[0]);
                IsVisible = mixed || values[0]; enabled = !mixed;
            }
            catch (Exception exception) { enabled = false; Report(exception); }
        }

        public override void OnGUIChanged() { Refresh(); }

        public override void OnGUI()
        {
            Refresh();
            if (!IsVisible) return;
            using (new EditorGUI.DisabledScope(!enabled))
            {
                if (!GUILayout.Button(string.IsNullOrEmpty(attribute.Title) ? method.Name : attribute.Title, GUILayout.Height(22))) return;
                try
                {
                    if (ownerPath == null && !ReferenceEquals(explicitTarget, editor.target))
                    {
                        Undo.RecordObject(editor.target, method.Name);
                        method.Invoke(explicitTarget, attribute.Params);
                        EditorUtility.SetDirty(editor.target);
                    }
                    else InspectorReflection.Invoke(editor.targets, ownerPath == null ? null : ownerPath + ".__action", method.Name, attribute.Params);
                }
                catch (Exception exception) { Report(exception); }
                finally { editor.serializedObject.Update(); }
            }
        }
    }
}
