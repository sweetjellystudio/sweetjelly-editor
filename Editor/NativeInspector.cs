using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SWeetJelly.Editor.Attributes;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace SWeetJelly.Editor
{
    internal sealed class NativeInspector
    {
        private readonly SweetJellyEditor editor;
        private readonly VisualElement root = new VisualElement { name = "sweetJellyInspector" };
        private readonly List<Action> refreshers = new List<Action>();
        private readonly HashSet<string> diagnostics = new HashSet<string>();
        private readonly Dictionary<string, VisualElement> groups = new Dictionary<string, VisualElement>();
        private readonly Dictionary<string, VisualElement> tabs = new Dictionary<string, VisualElement>();
        private readonly Dictionary<string, Button> tabButtons = new Dictionary<string, Button>();
        private readonly VisualElement common = new VisualElement();
        private readonly VisualElement tabBar = new VisualElement();
        private string selectedTab;

        internal NativeInspector(SweetJellyEditor editor) { this.editor = editor; }

        internal static VisualElement Create(SweetJellyEditor editor)
        {
            bool customMetadata = InspectorReflection.Hierarchy(editor.target.GetType())
                .SelectMany(type => type.GetMembers(InspectorReflection.Flags))
                .SelectMany(member => member.GetCustomAttributes(true).OfType<Attribute>())
                .Any(attribute => (attribute is GroupAttribute || attribute is ConditionAttribute || attribute is MetaAttribute) &&
                    attribute.GetType().Assembly != typeof(GroupAttribute).Assembly);
            return customMetadata ? new IMGUIContainer(editor.OnInspectorGUI) : new NativeInspector(editor).Build();
        }

        internal VisualElement Build()
        {
            var sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Packages/com.sweetjelly.editor/Editor/NativeInspector.uss");
            if (sheet != null) root.styleSheets.Add(sheet);
            root.AddToClassList("sj-inspector");
            tabBar.AddToClassList("sj-horizontal");
            root.Add(common);
            root.Add(tabBar);
            using (var iterator = editor.serializedObject.GetIterator())
            {
                bool enter = true;
                var fields = new List<(string path, FieldInfo field, int order)>();
                while (iterator.NextVisible(enter))
                {
                    enter = false;
                    if (iterator.name == "m_Script")
                    {
                        if (editor.target.GetType().GetCustomAttribute<HideScriptFieldAttribute>(true) == null)
                        {
                            var script = new PropertyField(iterator.Copy());
                            script.SetEnabled(false);
                            common.Add(script);
                        }
                        continue;
                    }
                    var field = InspectorReflection.ResolveField(editor.target.GetType(), iterator.propertyPath);
                    fields.Add((iterator.propertyPath, field, field?.GetCustomAttribute<OrderAttribute>()?.Order ?? 0));
                }
                foreach (var item in fields.OrderBy(f => f.order)) AddField(item.path, item.field, 0);
            }
            var names = new HashSet<string>();
            foreach (var type in InspectorReflection.Hierarchy(editor.target.GetType()))
            {
                foreach (var method in type.GetMethods(InspectorReflection.Flags).OrderBy(m => m.GetCustomAttribute<OrderAttribute>()?.Order ?? GUIRenderer.ORDER_METHOD))
                {
                    foreach (var button in method.GetCustomAttributes<ButtonAttribute>(true))
                    {
                        string key = method.ToString() + ":" + button.Title;
                        if (!names.Add(key)) continue;
                        AddButton(method, button);
                    }
                }
                foreach (var field in type.GetFields(InspectorReflection.Flags))
                {
                    if (field.GetCustomAttribute<ShowNonSerializedFieldAttribute>() == null) continue;
                    AddReadOnly(field, () => field.GetValue(editor.target));
                }
                foreach (var property in type.GetProperties(InspectorReflection.Flags))
                {
                    if (property.GetCustomAttribute<ShowNonSerializedFieldAttribute>() == null) continue;
                    AddReadOnly(property, () => property.GetValue(editor.target));
                }
                foreach (var help in type.GetCustomAttributes<HelpButtonAttribute>(false))
                    common.Add(new Button(() => Application.OpenURL(help.URL)) { text = help.Name });
            }
            foreach (var container in groups.Values.Concat(tabs.Values).Concat(new[] { common }).Distinct())
            {
                foreach (var child in container.Children().OrderBy(element => element.userData is int order ? order : int.MinValue).ToArray()) child.BringToFront();
            }
            // Unity's InspectorElement owns binding the returned tree.
            root.schedule.Execute(Refresh).Every(200);
            Refresh();
            return root;
        }

        private void AddReadOnly(MemberInfo member, Func<object> read)
        {
            var label = new Label();
            label.userData = member.GetCustomAttribute<OrderAttribute>()?.Order ?? GUIRenderer.ORDER_NON_SERIALIZED;
            label.AddToClassList("sj-readonly");
            Container(member).Add(label);
            refreshers.Add(() =>
            {
                try { label.text = (member.GetCustomAttribute<LabelAttribute>()?.Label ?? ObjectNames.NicifyVariableName(member.Name)) + ": " + (read()?.ToString() ?? "null"); }
                catch (Exception exception) { Report(member.Name, exception); label.text = member.Name + ": unavailable"; }
            });
        }

        private void AddField(string path, FieldInfo field, int depth, VisualElement inheritedParent = null)
        {
            var property = editor.serializedObject.FindProperty(path);
            if (property == null) return;
            if (field?.GetCustomAttribute<UnpackNestedAttribute>() != null && depth < 16 && property.hasVisibleChildren && !property.isArray)
            {
                var unpacked = new VisualElement { name = "field-" + path, userData = field.GetCustomAttribute<OrderAttribute>()?.Order ?? 0 };
                Container(field, inheritedParent).Add(unpacked);
                refreshers.Add(() =>
                {
                    var state = State(field, path);
                    SetVisible(unpacked, state.visible);
                    unpacked.SetEnabled(state.enabled);
                });
                using (var child = property.Copy())
                {
                    var end = child.GetEndProperty();
                    if (child.NextVisible(true))
                    {
                        do
                        {
                            if (SerializedProperty.EqualContents(child, end)) break;
                            AddField(child.propertyPath, InspectorReflection.ResolveField(editor.target.GetType(), child.propertyPath), depth + 1, unpacked);
                        } while (child.NextVisible(false));
                    }
                    end.Dispose();
                }
                var nested = InspectorReflection.Value(editor.target, path);
                var nestedButtons = new HashSet<string>();
                if (nested != null)
                    foreach (var type in InspectorReflection.Hierarchy(nested.GetType()))
                    foreach (var method in type.GetMethods(InspectorReflection.Flags))
                    foreach (var button in method.GetCustomAttributes<ButtonAttribute>(true))
                        if (nestedButtons.Add(method.ToString() + ":" + button.Title)) AddButton(method, button, path, unpacked);
                return;
            }
            string label = field?.GetCustomAttribute<LabelAttribute>()?.Label ?? property.displayName;
            var wrapper = new VisualElement { name = "field-" + path, userData = field?.GetCustomAttribute<OrderAttribute>()?.Order ?? 0 };
            Container(field, inheritedParent).Add(wrapper);
            if (field != null)
            {
                foreach (var info in field.GetCustomAttributes<InfoBoxAttribute>())
                {
                    var box = new HelpBox(info.Text, info.Type == InfoBoxType.Error ? HelpBoxMessageType.Error : info.Type == InfoBoxType.Warning ? HelpBoxMessageType.Warning : HelpBoxMessageType.Info);
                    wrapper.Add(box);
                    if (!string.IsNullOrEmpty(info.VisibleIf)) refreshers.Add(() => SetVisible(box, Consensus(path, info.VisibleIf, false).visible));
                }
            }
            var row = new VisualElement();
            row.AddToClassList("sj-horizontal");
            wrapper.Add(row);
            bool editedHere = false;
            var input = NativePropertyControls.Create(property.Copy(), label, field, () => editedHere = true);
            row.Add(input);
            var inline = field?.GetCustomAttribute<InlineButtonAttribute>();
            if (inline != null)
                row.Add(new Button(() => Action(path, inline.MethodName, null)) { text = inline.Title, name = "inline-" + path });
            var callback = field?.GetCustomAttribute<OnValueChangedAttribute>();
            if (callback != null)
            {
                WatchEdits(wrapper, root, (control, value) =>
                {
                    string changedPath = (control as IBindable)?.bindingPath;
                    if (string.IsNullOrEmpty(changedPath)) changedPath = path;
                    foreach (var target in editor.targets)
                    {
                        using (var fresh = new SerializedObject(target))
                        {
                            var actual = fresh.FindProperty(changedPath);
                            if (actual != null && !SameValue(actual, value)) { editedHere = true; break; }
                        }
                    }
                }, () => editedHere = true);
                var previous = editor.targets.Select(target => new SerializedObject(target)).ToArray();
                bool subscribed = false;
                Undo.UndoRedoCallback onUndoRedo = () => { if (previous != null) foreach (var snapshot in previous) snapshot.Update(); };
                root.RegisterCallback<AttachToPanelEvent>(evt =>
                {
                    if (evt.target != root || subscribed) return;
                    if (previous == null) previous = editor.targets.Select(target => new SerializedObject(target)).ToArray();
                    Undo.undoRedoPerformed += onUndoRedo;
                    subscribed = true;
                });
                input.TrackPropertyValue(property, changedProperty =>
                {
                    if (previous == null) { previous = editor.targets.Select(target => new SerializedObject(target)).ToArray(); return; }
                    var changed = new List<UnityEngine.Object>();
                    for (int i = 0; i < previous.Length; i++)
                    {
                        using (var fresh = new SerializedObject(editor.targets[i]))
                        {
                            var oldValue = previous[i].FindProperty(path);
                            var newValue = fresh.FindProperty(path);
                            if (oldValue != null && newValue != null && !SerializedProperty.DataEquals(oldValue, newValue)) changed.Add(editor.targets[i]);
                        }
                        previous[i].Update();
                    }
                    bool invoke = editedHere;
                    editedHere = false;
                    if (invoke && changed.Count > 0) Action(path, callback.CallbackName, null, changed.ToArray(), true);
                    Refresh();
                });
                root.RegisterCallback<DetachFromPanelEvent>(evt =>
                {
                    if (evt.target != root || previous == null) return;
                    Undo.undoRedoPerformed -= onUndoRedo;
                    subscribed = false;
                    foreach (var snapshot in previous) snapshot.Dispose();
                    previous = null;
                });
            }
            refreshers.Add(() =>
            {
                var state = State(field, path);
                SetVisible(wrapper, state.visible);
                wrapper.SetEnabled(state.enabled);
            });
        }

        private static bool SameValue(SerializedProperty actual, object value)
        {
            switch (actual.propertyType)
            {
                case SerializedPropertyType.Float:
                    return !(value is float || value is double) || actual.doubleValue == Convert.ToDouble(value);
                case SerializedPropertyType.Integer:
                case SerializedPropertyType.Enum:
                    if (!(value is Enum || value is int || value is long || value is uint || value is ulong || value is short || value is ushort || value is byte || value is sbyte)) return true;
                    return actual.longValue == Convert.ToInt64(value);
                case SerializedPropertyType.ArraySize: return !(value is int size) || actual.intValue == size;
                case SerializedPropertyType.String: return !(value is string text) || actual.stringValue == text;
                case SerializedPropertyType.Boolean: return !(value is bool flag) || actual.boolValue == flag;
                case SerializedPropertyType.ObjectReference: return !(value == null || value is UnityEngine.Object) || actual.objectReferenceValue == value as UnityEngine.Object;
                case SerializedPropertyType.AnimationCurve:
                    return !(value is AnimationCurve curve) || actual.animationCurveValue.keys.SequenceEqual(curve.keys) && actual.animationCurveValue.preWrapMode == curve.preWrapMode && actual.animationCurveValue.postWrapMode == curve.postWrapMode;
                case SerializedPropertyType.Gradient:
                    return !(value is Gradient gradient) || actual.gradientValue.colorKeys.SequenceEqual(gradient.colorKeys) && actual.gradientValue.alphaKeys.SequenceEqual(gradient.alphaKeys) && actual.gradientValue.mode == gradient.mode;
                case SerializedPropertyType.Generic: return true;
                default:
                    var boxed = actual.boxedValue;
                    return boxed != null && value != null && boxed.GetType() != value.GetType() || Equals(boxed, value);
            }
        }

        private static void WatchEdits(VisualElement input, VisualElement eventRoot, Action<VisualElement, object> edited, Action collectionEdited)
        {
            void Watch<T>()
            {
                eventRoot.RegisterCallback<ChangeEvent<T>>(evt =>
                {
                    if (!(evt.target is VisualElement target) || !input.Contains(target)) return;
                    if (evt.target is VisualElement element && element.ClassListContains("unity-foldout__toggle")) return;
                    edited(evt.target as VisualElement, evt.newValue);
                }, TrickleDown.TrickleDown);
            }
            Watch<bool>(); Watch<int>(); Watch<long>(); Watch<float>(); Watch<double>(); Watch<string>();
            Watch<Enum>(); Watch<UnityEngine.Object>(); Watch<Vector2>(); Watch<Vector3>(); Watch<Vector4>();
            Watch<Vector2Int>(); Watch<Vector3Int>(); Watch<Color>(); Watch<Rect>(); Watch<RectInt>();
            Watch<Bounds>(); Watch<BoundsInt>(); Watch<AnimationCurve>(); Watch<Gradient>();
            eventRoot.RegisterCallback<ClickEvent>(evt =>
            {
                var target = evt.target as VisualElement;
                if (target == null || !input.Contains(target)) return;
                var button = target as Button ?? target?.GetFirstAncestorOfType<Button>();
                if (button != null && (button.name.Contains("add-button") || button.name.Contains("remove-button"))) collectionEdited();
            }, TrickleDown.TrickleDown);
        }

        private void AddButton(MethodInfo method, ButtonAttribute attribute, string ownerPath = null, VisualElement inheritedParent = null)
        {
            string actionPath = ownerPath == null ? null : ownerPath + ".__action";
            var button = new Button(() => Action(actionPath, method.Name, attribute.Params))
            { text = string.IsNullOrEmpty(attribute.Title) ? method.Name : attribute.Title, name = "button-" + (ownerPath == null ? "" : ownerPath + ".") + method.Name, userData = method.GetCustomAttribute<OrderAttribute>()?.Order ?? GUIRenderer.ORDER_METHOD };
            Container(method, inheritedParent).Add(button);
            refreshers.Add(() =>
            {
                var state = string.IsNullOrEmpty(attribute.VisabilityConditionName) ? (visible: true, enabled: true) :
                    Consensus(actionPath, attribute.VisabilityConditionName, attribute.VisibilityOption == ButtonVisibility.HideIf);
                try
                {
                    foreach (var target in editor.targets)
                    {
                        var owner = ownerPath == null ? target : InspectorReflection.Value(target, ownerPath);
                        if (owner == null) { state.enabled = false; continue; }
                        InspectorReflection.Method(owner.GetType(), method.Name, attribute.Params);
                    }
                }
                catch (Exception exception) { Report(method.Name, exception); state.enabled = false; }
                SetVisible(button, state.visible);
                button.SetEnabled(state.enabled);
            });
        }

        private (bool visible, bool enabled) State(FieldInfo field, string path)
        {
            if (field == null) return (true, true);
            if (field.GetCustomAttribute<HideAttribute>() != null) return (false, false);
            var state = (visible: true, enabled: field.GetCustomAttribute<ReadOnlyAttribute>() == null);
            var show = field.GetCustomAttribute<ShowIfAttribute>();
            var hide = field.GetCustomAttribute<HideIfAttribute>();
            if (show != null || hide != null)
            {
                var condition = Consensus(path, show?.ConditionName ?? hide.ConditionName, hide != null);
                state.visible = condition.visible;
                state.enabled &= condition.enabled;
            }
            var enable = field.GetCustomAttribute<EnableIfAttribute>();
            var disable = field.GetCustomAttribute<DisableIfAttribute>();
            if (enable != null || disable != null)
            {
                var condition = Consensus(path, enable?.ConditionName ?? disable.ConditionName, disable != null);
                state.enabled &= condition.visible && condition.enabled;
            }
            return state;
        }

        private (bool visible, bool enabled) Consensus(string path, string name, bool invert)
        {
            try
            {
                var values = editor.targets.Select(target => InspectorReflection.Condition(string.IsNullOrEmpty(path) ? target : InspectorReflection.Owner(target, path), name) ^ invert).ToArray();
                bool mixed = values.Any(v => v != values[0]);
                return (mixed || values[0], !mixed);
            }
            catch (Exception exception) { Report((path ?? "root") + ":" + name, exception); return (true, false); }
        }

        private VisualElement Container(MemberInfo member, VisualElement inheritedParent = null)
        {
            var tab = member?.GetCustomAttribute<TabAttribute>();
            string scope = inheritedParent == null ? tab?.Title ?? "" : "nested:" + inheritedParent.GetHashCode();
            VisualElement parent = inheritedParent ?? common;
            if (tab != null && inheritedParent == null)
            {
                if (!tabs.TryGetValue(scope, out parent))
                {
                    parent = new VisualElement { name = "tab-" + scope };
                    tabs.Add(scope, parent);
                    var button = new Button(() => { selectedTab = scope; Refresh(); }) { text = scope };
                    tabButtons.Add(scope, button);
                    tabBar.Add(button);
                    root.Add(parent);
                    if (selectedTab == null) selectedTab = scope;
                    if (!string.IsNullOrEmpty(tab.ShowIf))
                    {
                        refreshers.Add(() =>
                        {
                            var condition = Consensus(null, tab.ShowIf, false);
                            SetVisible(button, condition.visible);
                            button.SetEnabled(condition.enabled);
                        });
                    }
                }
            }
            var group = member?.GetCustomAttribute<GroupAttribute>();
            if (group == null) return parent;
            bool unnamed = string.IsNullOrEmpty(group.ID);
            string accumulated = "";
            var parts = unnamed ? new[] { "" } : group.ID.Split('/');
            for (int i = 0; i < parts.Length; i++)
            {
                if (!unnamed && string.IsNullOrEmpty(parts[i])) continue;
                accumulated += "/" + parts[i];
                string key = scope + (unnamed ? ":unnamed:" : ":named:") + accumulated;
                if (!groups.TryGetValue(key, out var next))
                {
                    bool last = i == parts.Length - 1;
                    string title = last && !string.IsNullOrEmpty(group.Label) ? group.Label : parts[i];
                    if (last && group is BaseFoldoutGroupAttribute foldout)
                    {
                        string stateKey = "SWeetJelly.Editor:" + editor.target.GetType().FullName + ":" + key;
                        var fold = new Foldout { text = title, value = SessionState.GetBool(stateKey, foldout.DefaultState) };
                        fold.RegisterValueChangedCallback(evt => SessionState.SetBool(stateKey, evt.newValue));
                        next = fold;
                    }
                    else
                    {
                        next = new VisualElement();
                        if (!(last && group is HorizontalGroupAttribute) && !string.IsNullOrEmpty(title)) next.Add(new Label(title));
                    }
                    next.AddToClassList(last && group is HorizontalGroupAttribute ? "sj-horizontal" : "sj-group");
                    next.userData = group.Order;
                    parent.Add(next);
                    groups.Add(key, next);
                }
                parent = next;
            }
            return parent;
        }

        private void Action(string path, string name, object[] args, UnityEngine.Object[] targets = null, bool requireVoid = false)
        {
            try { InspectorReflection.Invoke(targets ?? editor.targets, path, name, args, requireVoid); }
            catch (Exception exception) { Report((path ?? "root") + ":" + name, exception); }
            finally { editor.serializedObject.Update(); Refresh(); }
        }

        private void Report(string key, Exception exception)
        {
            exception = InspectorReflection.Unwrap(exception);
            if (diagnostics.Add(key + ":" + exception.GetType().FullName + ":" + exception.Message))
                Debug.LogException(new InvalidOperationException("Inspector " + editor.target.GetType().Name + "." + key + ": " + exception.Message, exception), editor.target);
        }

        private void Refresh()
        {
            if (editor == null || editor.target == null) return;
            foreach (var refresh in refreshers) refresh();
            foreach (var group in groups.Values.Reverse())
                SetVisible(group, group.Children().Any(child => child.userData is int && !child.ClassListContains("sj-hidden")));
            if (selectedTab != null && tabButtons[selectedTab].ClassListContains("sj-hidden"))
                selectedTab = tabButtons.FirstOrDefault(pair => !pair.Value.ClassListContains("sj-hidden")).Key;
            foreach (var pair in tabs) SetVisible(pair.Value, pair.Key == selectedTab);
            foreach (var pair in tabButtons) pair.Value.EnableInClassList("sj-selected", pair.Key == selectedTab);
            SetVisible(tabBar, tabs.Count > 0);
        }

        private static void SetVisible(VisualElement element, bool visible) { element.EnableInClassList("sj-hidden", !visible); }
    }
}
