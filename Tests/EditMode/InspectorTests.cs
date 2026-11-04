using System;
using System.Collections.Generic;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using SWeetJelly.Editor.Attributes;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace SWeetJelly.Editor.Tests
{
    public class InspectorFixtureBase : ScriptableObject
    {
        [SerializeField, Label("Inherited value")] private int inheritedValue;
    }
    public sealed class InspectorFixture : InspectorFixtureBase
    {
        public bool allowed = true;
        [ShowIf(nameof(allowed))] public int conditional;
        [EnableIf(nameof(allowed))] public int enabledValue;
        [ShowIf("missing")] public int invalid;
        [Tab("Settings"), Foldout("Settings/Numbers"), Slider(0, 10)] public float amount;
        [Tab("Details"), UnpackNested] public Nested nested = new Nested();
        public List<Nested> items = new List<Nested> { new Nested() };
        public Counter counter;
        public List<Counter> counters = new List<Counter> { new Counter() };
        public int calls;
        [BoxGroup] public int boxed;
        [Button("First"), SWeetJelly.Editor.Attributes.Order(-50)] public void First() { }
        [Slider(0, 10), OnValueChanged(nameof(Changed))] public float observed;
        public int changes;
        private void Changed() { changes++; }
        [Button("Increment")] public void Increment() { calls++; }
        public void Throw() { calls++; throw new InvalidOperationException("action failed"); }
        public void Accept(string value) { calls++; }
        public void Ambiguous(string value) { }
        public void Ambiguous(UnityEngine.Object value) { }
        public bool Broken => throw new InvalidOperationException("getter failed");
        [Serializable] public sealed class Nested
        {
            [Label("Nested value")] public int value;
            [Button("Bump nested")] public void Bump() { value++; }
        }
        [Serializable] public struct Counter { public int calls; public void Increment() { calls++; } }
    }

    public sealed class InspectorTests
    {
        private InspectorFixture first, second;
        private UnityEditor.Editor editor;
        [SetUp] public void SetUp()
        {
            first = ScriptableObject.CreateInstance<InspectorFixture>();
            second = ScriptableObject.CreateInstance<InspectorFixture>();
        }
        [TearDown] public void TearDown()
        {
            if (editor) UnityEngine.Object.DestroyImmediate(editor);
            Undo.ClearUndo(first);
            Undo.ClearUndo(second);
            UnityEngine.Object.DestroyImmediate(first);
            UnityEngine.Object.DestroyImmediate(second);
        }
        [TestCase("inheritedValue", "Inherited value")]
        [TestCase("nested.value", "Nested value")]
        [TestCase("items.Array.data[0].value", "Nested value")]
        public void AttributeLookupResolvesInheritedNestedAndListPaths(string path, string label)
        {
            using (var serialized = new SerializedObject(first))
                Assert.That(PropertyUtility.GetAttribute<LabelAttribute>(serialized.FindProperty(path)).Label, Is.EqualTo(label));
        }
        [Test] public void NullNestedOwnerDoesNotThrowDuringResolution()
        {
            first.nested = null;
            Assert.That(InspectorReflection.Owner(first, "nested.value"), Is.Null);
        }
        [Test] public void ConditionReadsPrivateInheritedAndPublicMembers()
        {
            Assert.That(InspectorReflection.Condition(first, "allowed"), Is.True);
            first.allowed = false;
            Assert.That(InspectorReflection.Condition(first, "allowed"), Is.False);
        }
        [Test] public void MissingConditionIsExplicitError()
        {
            Assert.Throws<MissingMethodException>(() => InspectorReflection.Condition(first, "missing"));
        }
        [Test] public void WrongConditionTypeIsExplicitError()
        {
            Assert.Throws<InvalidOperationException>(() => InspectorReflection.Condition(first, "calls"));
        }
        [Test] public void NullArgumentCanMatchReferenceParameter()
        {
            InspectorReflection.Invoke(new UnityEngine.Object[] { first }, null, "Accept", new object[] { null });
            Assert.That(first.calls, Is.EqualTo(1));
        }
        [Test] public void AmbiguousNullArgumentIsRejectedBeforeMutation()
        {
            Assert.Throws<AmbiguousMatchException>(() => InspectorReflection.Invoke(new UnityEngine.Object[] { first }, null, "Ambiguous", new object[] { null }));
            Assert.That(first.calls, Is.Zero);
        }
        [Test] public void MultiTargetActionRunsOncePerTargetAndSupportsUndo()
        {
            InspectorReflection.Invoke(new UnityEngine.Object[] { first, second }, null, "Increment", null);
            Assert.That(first.calls, Is.EqualTo(1));
            Assert.That(second.calls, Is.EqualTo(1));
            Undo.FlushUndoRecordObjects();
            Undo.PerformUndo();
            Assert.That(first.calls, Is.Zero);
            Assert.That(second.calls, Is.Zero);
        }
        [Test] public void ActionExceptionStopsBeforeNextTarget()
        {
            var error = Assert.Throws<TargetInvocationException>(() => InspectorReflection.Invoke(new UnityEngine.Object[] { first, second }, null, "Throw", null));
            Assert.That(InspectorReflection.Unwrap(error).Message, Is.EqualTo("action failed"));
            Assert.That(first.calls, Is.EqualTo(1));
            Assert.That(second.calls, Is.Zero);
        }
        [Test] public void MixedConditionsDisplayButDisableField()
        {
            second.allowed = false;
            editor = UnityEditor.Editor.CreateEditor(new UnityEngine.Object[] { first, second }, typeof(ScriptableObjectEditor));
            LogAssert.Expect(LogType.Exception, new System.Text.RegularExpressions.Regex(".*InspectorFixture.missing.*"));
            var root = editor.CreateInspectorGUI();
            var field = root.Q<VisualElement>("field-conditional");
            Assert.That(field, Is.Not.Null);
            Assert.That(field.ClassListContains("sj-hidden"), Is.False);
            Assert.That(field.enabledSelf, Is.False);
            Assert.That(root.Q<Button>("button-Increment"), Is.Not.Null);
            Assert.That(root.Q<IMGUIContainer>(), Is.Null, "Plain native fields/groups/buttons must not wrap the legacy Inspector.");
        }
        [Test] public void AllFalseConditionsHideField()
        {
            first.allowed = false;
            editor = UnityEditor.Editor.CreateEditor(first, typeof(ScriptableObjectEditor));
            LogAssert.Expect(LogType.Exception, new System.Text.RegularExpressions.Regex(".*InspectorFixture.missing.*"));
            var root = editor.CreateInspectorGUI();
            Assert.That(root.Q<VisualElement>("field-conditional").ClassListContains("sj-hidden"), Is.True);
        }
        [Test] public void DefaultEditorRegistrationIsFallback()
        {
            Assert.That(typeof(ScriptableObjectEditor).GetCustomAttribute<CustomEditor>().isFallback, Is.True);
            Assert.That(typeof(MonoBehaviourEditor).GetCustomAttribute<CustomEditor>().isFallback, Is.True);
        }

        [Test] public void UnpackedChildrenRemainInsideTheirParentTab()
        {
            editor = UnityEditor.Editor.CreateEditor(first, typeof(ScriptableObjectEditor));
            LogAssert.Expect(LogType.Exception, new System.Text.RegularExpressions.Regex(".*InspectorFixture.missing.*"));
            var root = editor.CreateInspectorGUI();
            Assert.That(root.Q<VisualElement>("tab-Details").Q<VisualElement>("field-nested.value"), Is.Not.Null);
            Assert.That(root.Q<VisualElement>("tab-Details").Q<Button>("button-nested.Bump"), Is.Not.Null);
        }

        [TestCase("counter")]
        [TestCase("counters.Array.data[0]")]
        public void NestedStructActionsPersistAndUndo(string path)
        {
            InspectorReflection.Invoke(new UnityEngine.Object[] { first }, path + ".__action", "Increment", null);
            Assert.That(path == "counter" ? first.counter.calls : first.counters[0].calls, Is.EqualTo(1));
            Undo.FlushUndoRecordObjects();
            Undo.PerformUndo();
            Assert.That(path == "counter" ? first.counter.calls : first.counters[0].calls, Is.Zero);
        }

        [Test] public void UnnamedGroupsAndMethodOrderRemainMeaningful()
        {
            editor = UnityEditor.Editor.CreateEditor(first, typeof(ScriptableObjectEditor));
            LogAssert.Expect(LogType.Exception, new System.Text.RegularExpressions.Regex(".*InspectorFixture.missing.*"));
            var root = editor.CreateInspectorGUI();
            Assert.That(root.Q<VisualElement>("field-boxed").parent.ClassListContains("sj-group"), Is.True);
            var button = root.Q<Button>("button-First");
            var field = root.Q<VisualElement>("field-allowed");
            Assert.That(button.parent, Is.SameAs(field.parent));
            Assert.That(button.parent.IndexOf(button), Is.LessThan(button.parent.IndexOf(field)));
        }
        [Test] public void LegacyInheritanceKeepsOnInspectorGUIContract()
        {
            Assert.That(typeof(SweetJellyEditor).GetMethod("OnInspectorGUI").DeclaringType, Is.EqualTo(typeof(SweetJellyEditor)));
            Assert.That(typeof(SweetJellyEditor).GetMethod("CreateInspectorGUI").DeclaringType, Is.EqualTo(typeof(UnityEditor.Editor)));
        }
        [Test] public void MissingIconsAreSafe()
        {
            var config = ScriptableObject.CreateInstance<EditorCustomStylesConfig>();
            try { Assert.That(config.GetIcon("missing"), Is.Null); }
            finally { UnityEngine.Object.DestroyImmediate(config); }
        }

        [Test] public void BundledStylesResolveWithoutImportedProjectResources()
        {
            const string path = "Packages/com.sweetjelly.editor/Editor/Resources/EditorCustomStylesConfig.asset";
            var config = AssetDatabase.LoadAssetAtPath<EditorCustomStylesConfig>(path);
            Assert.That(config, Is.Not.Null);
            Assert.That(config.Skin, Is.Not.Null);
            using (var serialized = new SerializedObject(config))
            {
                Assert.That(serialized.FindProperty("defaultGUISkin").objectReferenceValue, Is.Not.Null);
                Assert.That(serialized.FindProperty("proGUISkin").objectReferenceValue, Is.Not.Null);
                var icons = serialized.FindProperty("icons");
                Assert.That(icons.arraySize, Is.GreaterThan(0));
                for (int i = 0; i < icons.arraySize; i++) Assert.That(icons.GetArrayElementAtIndex(i).objectReferenceValue, Is.Not.Null);
            }
            var dependencies = AssetDatabase.GetDependencies(path, true);
            Assert.That(dependencies.Length, Is.GreaterThan(10));
            Assert.That(System.Array.Exists(dependencies, dependency => dependency.StartsWith("Assets/")), Is.False);
        }

        [UnityTest] public IEnumerator BoundChangesNotifyOnlyTargetsWhoseValueChanged()
        {
            first.observed = 2;
            second.observed = 0;
            editor = UnityEditor.Editor.CreateEditor(new UnityEngine.Object[] { first, second }, typeof(ScriptableObjectEditor));
            LogAssert.Expect(LogType.Exception, new System.Text.RegularExpressions.Regex(".*InspectorFixture.missing.*"));
            var root = editor.CreateInspectorGUI();
            var window = ScriptableObject.CreateInstance<InspectorTestWindow>();
            var observer = UnityEditor.Editor.CreateEditor(new UnityEngine.Object[] { first, second }, typeof(ScriptableObjectEditor));
            try
            {
                window.rootVisualElement.Add(root);
                root.Bind(editor.serializedObject);
                LogAssert.Expect(LogType.Exception, new System.Text.RegularExpressions.Regex(".*InspectorFixture.missing.*"));
                var observedRoot = observer.CreateInspectorGUI();
                window.rootVisualElement.Add(observedRoot);
                observedRoot.Bind(observer.serializedObject);
                window.Show();
                for (int i = 0; i < 5; i++) yield return null;
                var input = root.Q<VisualElement>("field-observed").Q<UnityEngine.UIElements.Slider>();
                Assert.That(input, Is.Not.Null);
                input.value = 2;
                for (int i = 0; i < 10; i++) yield return null;
                Assert.That(first.changes, Is.Zero);
                Assert.That(second.changes, Is.EqualTo(1));
                for (int i = 0; i < 5; i++) yield return null;
                Assert.That(second.changes, Is.EqualTo(1), "Repaint/binding refresh must not repeat the callback.");
                Undo.FlushUndoRecordObjects();
                Undo.PerformUndo();
                for (int i = 0; i < 10; i++) yield return null;
                Assert.That(second.changes, Is.Zero, "Undo must restore the edit and callback state without invoking the callback again.");
            }
            finally { window.Close(); UnityEngine.Object.DestroyImmediate(observer); }
        }
    }
    public sealed class InspectorTestWindow : EditorWindow { }

    public sealed class LiveInspectorFixture : ScriptableObject
    {
        [Slider(0, 10), OnValueChanged(nameof(Changed))] public float observed = 3;
        public int changes;
        private void Changed() { changes++; }
    }

    [TestFixture]
    public sealed class LiveInspectorTests
    {
        [UnityTest] public IEnumerator ActualInspectorBindsSliderOnceAndUndoesTheEntireEdit()
        {
            var selection = Selection.objects;
            var fixture = ScriptableObject.CreateInstance<LiveInspectorFixture>();
            var type = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.InspectorWindow");
            var window = (EditorWindow)ScriptableObject.CreateInstance(type);
            try
            {
                Selection.activeObject = fixture;
                window.ShowUtility();
                VisualElement root = null;
                for (int i = 0; i < 100 && root == null; i++)
                {
                    yield return null;
                    root = window.rootVisualElement.Q<VisualElement>("sweetJellyInspector");
                }
                Assert.That(root, Is.Not.Null);
                var slider = root.Q<UnityEngine.UIElements.Slider>();
                for (int i = 0; i < 20; i++) yield return null;
                Assert.That(slider.value, Is.EqualTo(3), "Wait for the Inspector's initial binding before editing.");
                slider.value = 4;
                for (int i = 0; i < 30; i++) yield return null;
                Assert.That(fixture.observed, Is.EqualTo(4));
                Assert.That(fixture.changes, Is.EqualTo(1));
                Undo.FlushUndoRecordObjects();
                Undo.PerformUndo();
                for (int i = 0; i < 20; i++) yield return null;
                Assert.That(fixture.observed, Is.EqualTo(3));
                Assert.That(fixture.changes, Is.Zero);
            }
            finally
            {
                window.Close();
                Selection.objects = selection;
                Undo.ClearUndo(fixture);
                UnityEngine.Object.DestroyImmediate(fixture);
            }
        }
    }
}
