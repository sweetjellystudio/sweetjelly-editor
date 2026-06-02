using UnityEditor;
using SWeetJelly.Editor.Attributes;

namespace SWeetJelly.Editor
{
    [CustomPropertyGrouperDrawer(typeof(HorizontalGroupAttribute))]
    public class HorizontalGroupPropertyGrouperDrawer : PropertyGrouperDrawer
    {
        public override void BeginGroup(SweetJellyEditor editor, string groupID, string label)
        {
            EditorGUILayout.BeginHorizontal();
        }

        public override void EndGroup()
        {
            EditorGUILayout.EndHorizontal();
        }
    }
}
