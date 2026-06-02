using UnityEditor;
using SWeetJelly.Editor.Attributes;

namespace SWeetJelly.Editor
{
    [CustomPropertyGrouperDrawer(typeof(GroupAttribute))]
    public class GroupPropertyGrouperDrawer : PropertyGrouperDrawer
    {
        public override void BeginGroup(SweetJellyEditor editor, string groupID, string label)
        {
            EditorGUILayout.BeginVertical();
        }

        public override void EndGroup()
        {
            EditorGUILayout.EndVertical();
        }
    }
}
