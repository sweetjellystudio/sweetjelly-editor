using UnityEditor;

namespace SWeetJelly.Editor
{
    public class EditorFoldoutBool
    {
        public string Key { get; private set; }

        private bool value;

        public bool Value
        {
            get => value;
            set
            {
                this.value = value;

                EditorPrefs.SetBool(Key, value);
            }
        }

        public EditorFoldoutBool(string key, bool defaultValue = false)
        {
            Key = key;
            value = EditorPrefs.GetBool(Key, defaultValue);
        }
    }
}