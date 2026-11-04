using System;
using SWeetJelly.Editor.Attributes;
using UnityEngine;

namespace SWeetJelly.Editor.Samples
{
    [CreateAssetMenu(menuName = "SWeetJelly/Editor/Inspector Showcase")]
    public sealed class InspectorShowcase : ScriptableObject
    {
        [Tab("Settings"), BoxGroup("General"), Label("Enable advanced settings")]
        public bool advanced = true;

        [Tab("Settings"), BoxGroup("General"), Slider(0, 10), OnValueChanged(nameof(OnSpeedChanged))]
        public float speed = 3;

        [Tab("Settings"), Foldout("Advanced", defaultState: true), ShowIf(nameof(advanced))]
        public string description = "Select two assets to verify mixed conditions and Undo.";

        [Tab("Settings"), Foldout("Advanced"), EnableIf(nameof(advanced)), InlineButton(nameof(ResetCount), "Reset")]
        public int count;

        [Tab("Details"), ReadOnly] public int speedChanges;
        [Tab("Details"), UnpackNested] public Options options = new Options();

        [Button("Increment"), Tab("Settings"), Group("General")]
        public void Increment() { count++; }

        private void ResetCount() { count = 0; }
        private void OnSpeedChanged() { speedChanges++; }

        [Serializable]
        public sealed class Options
        {
            [Label("Nested label")] public string note = "This example has no UnityEditor dependency.";
        }
    }
}
