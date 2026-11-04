using System;
using System.Collections.Generic;
using System.Reflection;
using SWeetJelly.Editor.Attributes;

namespace SWeetJelly.Editor
{
    public sealed class UnwrapNestedGUIRenderer : GUIRenderer
    {
        private GUIRenderer[] renderers;

        private FieldInfo fieldInfo;
        private object targetObject;
        private SweetJellyEditor editor;

        private PropertyConditionDrawer propertyCondition;

        private List<Type> nestedTypes;
        private string propertyPath;
        private bool conditionFailed;
        private readonly HashSet<string> diagnostics = new HashSet<string>();

        public UnwrapNestedGUIRenderer(SweetJellyEditor editor, FieldInfo fieldInfo, GUIRenderer[] renderers, object targetObject, List<Type> nestedTypes)
            : this(editor, fieldInfo, renderers, targetObject, nestedTypes, null) { }

        internal UnwrapNestedGUIRenderer(SweetJellyEditor editor, FieldInfo fieldInfo, GUIRenderer[] renderers, object targetObject, List<Type> nestedTypes, string propertyPath)
        {
            this.propertyPath = propertyPath;
            this.editor = editor;
            this.targetObject = targetObject;
            this.fieldInfo = fieldInfo;
            this.nestedTypes = nestedTypes;
            this.renderers = renderers;

            TabAttribute = (TabAttribute)Attribute.GetCustomAttribute(fieldInfo, typeof(TabAttribute));
            GroupAttribute = (GroupAttribute)Attribute.GetCustomAttribute(fieldInfo, typeof(GroupAttribute));

            // Check if visible
            ConditionAttribute showIfAttribute = PropertyUtility.GetAttribute<ConditionAttribute>(fieldInfo);

            if (showIfAttribute != null)
            {
                propertyCondition = CustomAttributesDatabase.GetConditionAttribute(showIfAttribute.GetType());

                UpdateVisabilityState();
            }
            else
            {
                IsVisible = renderers.IsAnyObjectVisible();
            }
        }

        public override void OnGUI()
        {
            UpdateVisabilityState();
            if (!IsVisible) return;

            using (new UnityEditor.EditorGUI.DisabledScope(conditionFailed))
            {
            foreach (GUIRenderer renderer in renderers)
            {
                renderer.OnGUI();
            }
            }
        }

        public override void OnGUIChanged()
        {
            foreach (GUIRenderer renderer in renderers)
            {
                renderer.OnGUIChanged();
            }

            UpdateVisabilityState();
        }

        private void UpdateVisabilityState()
        {
            conditionFailed = false;
            try
            {
            if (propertyCondition != null)
            {
                bool? first = null;
                bool mixed = false;
                foreach (var root in editor.targets)
                {
                    var owner = propertyPath == null ? ReferenceEquals(targetObject, editor.target) ? root : targetObject : InspectorReflection.Owner(root, propertyPath);
                    bool value = propertyCondition.CanBeDrawn(editor, fieldInfo, owner, PropertyUtility.GetClassNestedTypes(owner.GetType()));
                    if (first.HasValue && first.Value != value) mixed = true;
                    if (!first.HasValue) first = value;
                }
                conditionFailed = mixed;
                if (mixed || first == true)
                {
                    IsVisible = renderers.IsAnyObjectVisible();
                }
                else
                {
                    IsVisible = false;
                }
            }
            else
            {
                IsVisible = renderers.IsAnyObjectVisible();
            }
            }
            catch (Exception exception)
            {
                conditionFailed = true;
                IsVisible = true;
                exception = InspectorReflection.Unwrap(exception);
                if (diagnostics.Add(exception.GetType().Name + ":" + exception.Message)) UnityEngine.Debug.LogException(exception, editor.target);
            }
        }
    }
}
