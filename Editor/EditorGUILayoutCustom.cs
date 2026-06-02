using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SWeetJelly.Editor
{
    public static class EditorGUILayoutCustom
    {
        /// <summary>
        /// Displays a file selection field in the Unity Editor GUI.
        /// </summary>
        /// <param name="content">The label content to display next to the file field.</param>
        /// <param name="value">The current file path value.</param>
        /// <param name="directory">The initial directory to open in the file panel. Default is an empty string.</param>
        /// <param name="extension">The file extension filter for the file panel. Default is an empty string.</param>
        /// <returns>Returns the selected file path or the original value if no file is selected.</returns>
        public static string FileField(GUIContent content, string value, string directory = "", string extension = "")
        {
            string tempValue = value;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(content);
            EditorGUILayout.LabelField(new GUIContent(value, value), GUILayout.MaxWidth(128));
            if (GUILayout.Button("•", EditorStyles.miniButton, GUILayout.Width(16)))
            {
                tempValue = EditorUtility.OpenFilePanel("Select file path", directory, extension);

                if(string.IsNullOrEmpty(tempValue))
                {
                    Debug.LogWarning("No file selected.");
                    tempValue = value;
                }
            }
            EditorGUILayout.EndHorizontal();

            return tempValue;
        }

        /// <summary>
        /// Displays a folder selection field in the Unity Editor GUI.
        /// </summary>
        /// <param name="content">The label content to display next to the folder field.</param>
        /// <param name="value">The current folder path value.</param>
        /// <param name="folder">The initial folder to open in the folder panel. Default is an empty string.</param>
        /// <param name="defaultName">The default name for the folder. Default is an empty string.</param>
        /// <returns>Returns the selected folder path or the original value if no folder is selected.</returns>
        public static string FolderField(GUIContent content, string value, string folder = "", string defaultName = "")
        {
            string tempValue = value;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(content);
            EditorGUILayout.LabelField(new GUIContent(value, value), GUILayout.MaxWidth(128));
            if (GUILayout.Button("•", EditorStyles.miniButton, GUILayout.Width(16)))
            {
                tempValue = EditorUtility.OpenFolderPanel("Select folder path", folder, defaultName);
            }
            EditorGUILayout.EndHorizontal();

            return tempValue;
        }

        /// <summary>
        /// Creates a toggle (checkbox) in the Unity Editor GUI and checks if its state has changed.
        /// </summary>
        /// <param name="variable">A reference to the boolean variable representing the current state of the toggle.</param>
        /// <param name="content">The label and tooltip content for the toggle.</param>
        /// <returns>Returns true if the toggle's state has changed, otherwise false.</returns>
        public static bool ChangedToggle(ref bool variable, GUIContent content)
        {
            bool value = EditorGUILayout.Toggle(content, variable);
            if (value != variable)
            {
                variable = value;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Creates a foldout in the Unity Editor GUI and checks if its state has changed.
        /// </summary>
        /// <param name="variable">A reference to the boolean variable representing the current state of the foldout.</param>
        /// <param name="content">The label and tooltip content for the foldout.</param>
        /// <returns>Returns true if the foldout's state has changed, otherwise false.</returns>
        public static bool ChangedFoldout(ref bool variable, GUIContent content)
        {
            bool value = EditorGUILayout.Foldout(variable, content, true);
            if (value != variable)
            {
                variable = value;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Creates a dropdown field in the Unity Editor GUI for selecting a type from a specified assembly.
        /// </summary>
        /// <param name="content">The label content for the dropdown field.</param>
        /// <param name="type">The currently selected type.</param>
        /// <param name="assemblyType">The type used to determine the assembly to search for types. Default is MonoBehaviour.</param>
        /// <returns>Returns the selected type from the dropdown.</returns>
        public static Type TypeField(string content, Type type, Type assemblyType = null)
        {
            if (assemblyType == null)
                assemblyType = typeof(MonoBehaviour);
        
            Assembly assembly = Assembly.GetAssembly(assemblyType);
            Type[] types = assembly.GetTypes();
            string[] variableNames = types.Select(x => x.Name).ToArray();
        
            int selectedItem = Array.FindIndex(variableNames, x => x == type.Name);
        
            selectedItem = EditorGUILayout.Popup(content, selectedItem, variableNames);
        
            return types[selectedItem];
        }

        /// <summary>
        /// Creates a dropdown field in the Unity Editor GUI for selecting a field name from a specified type.
        /// </summary>
        /// <param name="type">The type from which to retrieve field names.</param>
        /// <param name="popupIndex">A reference to the index of the currently selected field name.</param>
        /// <param name="label">The label content for the dropdown field.</param>
        /// <returns>Returns the selected field name from the dropdown.</returns>
        public static string FieldNameLayout(Type type, ref int popupIndex, string label)
        {
            string[] variableNames = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Select(x => x.Name).ToArray();
        
            popupIndex = EditorGUILayout.Popup(label, popupIndex, variableNames);
        
            return variableNames[popupIndex];
        }

        /// <summary>
        /// Creates a dropdown field in the Unity Editor GUI for selecting a field name from a specified type, within a specified rectangle area.
        /// </summary>
        /// <param name="rect">The rectangle area within which to draw the dropdown field.</param>
        /// <param name="type">The type from which to retrieve field names.</param>
        /// <param name="popupIndex">A reference to the index of the currently selected field name.</param>
        /// <param name="label">The label content for the dropdown field.</param>
        /// <returns>Returns the selected field name from the dropdown.</returns>
        public static string FieldName(Rect rect, Type type, ref int popupIndex, string label)
        {
            string[] variableNames = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Select(x => x.Name).ToArray();
        
            popupIndex = EditorGUI.Popup(rect, label, popupIndex, variableNames);
        
            return variableNames[popupIndex];
        }

        /// <summary>
        /// Displays a serialized list in the Unity Editor GUI and allows for custom actions on each element.
        /// </summary>
        /// <param name="list">The serialized property representing the list or array.</param>
        /// <param name="action">The action to perform on each element of the list.</param>
        public static void ShowList(SerializedProperty list, Action<SerializedProperty> action)
        {
            if (!list.isArray)
            {
                EditorGUILayout.HelpBox(list.name + " is neither an array nor a list!", MessageType.Error);
                return;
            }
        
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        
            EditorGUILayout.PropertyField(list.FindPropertyRelative("Array.size"));
        
            for (int i = 0; i < list.arraySize; i++)
            {
                action(list.GetArrayElementAtIndex(i));
            }
        
            if (GUILayout.Button("Add", EditorStyles.miniButton))
            {
                list.arraySize += 1;
            }
        }

        /// <summary>
        /// Displays a field in the Unity Editor GUI for a given value and type, supporting various data types.
        /// </summary>
        /// <param name="value">The current value to be displayed and edited.</param>
        /// <param name="type">The type of the value to be displayed and edited.</param>
        /// <param name="title">The label content for the field. Default is an empty string.</param>
        /// <returns>Returns the edited value.</returns>
        public static object UniversalField(object value, Type type, string title = "")
        {
            if (type == typeof(string))
            {
                return EditorGUILayout.TextField(new GUIContent(title), (string)value);
            }
            else if (type == typeof(bool))
            {
                return EditorGUILayout.Toggle(new GUIContent(title), (bool)value);
            }
            else if (type == typeof(int))
            {
                return EditorGUILayout.IntField(new GUIContent(title), (int)value);
            }
            else if (type == typeof(float))
            {
                return EditorGUILayout.FloatField(new GUIContent(title), (float)value);
            }
            else if (type == typeof(Type))
            {
                return EditorGUILayoutCustom.TypeField(title, (Type)value);
            }
            else if (type == typeof(Vector2))
            {
                return EditorGUILayout.Vector2Field(new GUIContent(title), (Vector2)value);
            }
            else if (type == typeof(Vector3))
            {
                return EditorGUILayout.Vector3Field(new GUIContent(title), (Vector3)value);
            }
            else if (type.IsEnum)
            {
                return EditorGUILayout.EnumPopup(new GUIContent(title), (Enum)value);
            }
            else if (type.IsSubclassOf(typeof(Object)))
            {
                return EditorGUILayout.ObjectField(new GUIContent(title), (Object)value, type, true);
            }
            else if (type.IsSerializable && !type.IsArray && !type.IsGenericType && type != typeof(object))
            {
                foreach (var property in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where(x => x.IsPublic || x.GetCustomAttributes(typeof(SerializeField), false).Length > 0))
                {
                    EditorGUILayout.LabelField(ObjectNames.NicifyVariableName(property.Name) + ": ", GUILayout.ExpandWidth(true));
        
                    try
                    {
                        property.SetValue(value, Convert.ChangeType(UniversalField(property.GetValue(value), property.FieldType), property.FieldType));
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning(e);
                    }
                }
        
                return value;
            }
        
            return null;
        }

        /// <summary>
        /// Draws a compile window in the Unity Editor GUI indicating that the project is compiling.
        /// </summary>
        /// <param name="editorRect">The rectangle area within which to draw the compile window.</param>
        public static void DrawCompileWindow(Rect editorRect)
        {
            if (EditorApplication.isCompiling)
            {
                Rect rect = new Rect(editorRect.x, editorRect.y, editorRect.width, editorRect.height);
                GUI.Box(rect, "Compiling..", EditorCustomStyles.BoxCompiling);
            }
        }

        /// <summary>
        /// Draws a read-only script field in the Unity Editor GUI for a given MonoBehaviour script.
        /// </summary>
        /// <typeparam name="T">The type of the MonoBehaviour script.</typeparam>
        /// <param name="script">The instance of the MonoBehaviour script to display.</param>
        public static void DrawScript<T>(T script) where T : MonoBehaviour
        {
            GUI.enabled = false;
            EditorGUILayout.ObjectField("Script:", MonoScript.FromMonoBehaviour(script), typeof(T), false);
            GUI.enabled = true;
        }

        /// <summary>
        /// Draws a horizontal line spacer in the Unity Editor GUI.
        /// </summary>
        public static void LineSpacer()
        {
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider, GUILayout.Height(16), GUILayout.ExpandWidth(true));
        }

        /// <summary>
        /// Begins a box group in the Unity Editor GUI with an optional title.
        /// </summary>
        /// <param name="title">The title of the box group. If null or empty, no title is displayed.</param>
        /// <returns>Returns the rectangle area of the box group.</returns>
        public static Rect BeginBoxGroup(string title)
        {
            Rect rect = EditorGUILayout.BeginVertical(EditorCustomStyles.BoxGroupBackground);
        
            if (!string.IsNullOrEmpty(title))
            {
                Header(rect, title);
            }
        
            EditorGUILayout.BeginVertical(EditorCustomStyles.BoxContent);
        
            return rect;
        }

        /// <summary>
        /// Begins an expandable box group in the Unity Editor GUI with a header that can be expanded or collapsed.
        /// </summary>
        /// <param name="title">The title of the box group.</param>
        /// <param name="value">The initial value indicating whether the box group is expanded or collapsed.</param>
        /// <returns>Returns the new value indicating whether the box group is expanded or collapsed.</returns>
        public static bool BeginExpandBoxGroup(string title, bool value)
        {
            Rect rect = EditorGUILayout.BeginVertical(EditorCustomStyles.BoxGroupBackground);
        
            bool expandValue = HeaderExpand(rect, title, value, EditorCustomStyles.FoldoutArrowDown, EditorCustomStyles.FoldoutArrowRight);
        
            if (value)
            {
                EditorGUILayout.BeginVertical(EditorCustomStyles.BoxContent);
            }
            else
            {
                EditorGUILayout.BeginVertical(GUIStyle.none);
            }
        
            return expandValue;
        }

        /// <summary>
        /// Begins a toggle box group in the Unity Editor GUI with a header that can be expanded or collapsed.
        /// </summary>
        /// <param name="title">The title of the toggle box group.</param>
        /// <param name="value">The initial value indicating whether the box group is expanded or collapsed.</param>
        /// <returns>Returns the new value indicating whether the box group is expanded or collapsed.</returns>
        public static bool BeginToggleBoxGroup(string title, bool value)
        {
            Rect rect = EditorGUILayout.BeginVertical(EditorCustomStyles.BoxGroupBackground);
        
            bool expandValue = HeaderToggle(rect, title, value);
        
            if (value)
            {
                EditorGUILayout.BeginVertical(EditorCustomStyles.BoxContent);
            }
            else
            {
                EditorGUILayout.BeginVertical(GUIStyle.none);
            }
        
            return expandValue;
        }

        /// <summary>
        /// Begins a button box group in the Unity Editor GUI with a header that contains a button.
        /// </summary>
        /// <param name="title">The title of the button box group.</param>
        /// <param name="buttonContent">The content of the button.</param>
        /// <param name="buttonStyle">The style of the button.</param>
        /// <param name="disableState">The state indicating whether the button is disabled.</param>
        /// <returns>Returns true if the button is clicked, otherwise false.</returns>
        public static bool BeginButtonBoxGroup(string title, GUIContent buttonContent, GUIStyle buttonStyle, bool disableState)
        {
            Rect rect = EditorGUILayout.BeginVertical(EditorCustomStyles.BoxGroupBackground);
        
            bool isClicked = HeaderButton(rect, title, buttonContent, buttonStyle, disableState);
        
            EditorGUILayout.BeginVertical(EditorCustomStyles.BoxContent);
        
            return isClicked;
        }

        /// <summary>
        /// Begins a menu box group in the Unity Editor GUI with a header that contains a generic menu.
        /// </summary>
        /// <param name="title">The title of the menu box group.</param>
        /// <param name="genericMenu">The generic menu to be displayed in the header.</param>
        public static void BeginMenuBoxGroup(string title, GenericMenu genericMenu)
        {
            Rect rect = EditorGUILayout.BeginVertical(EditorCustomStyles.BoxGroupBackground);
        
            GUILayout.Space(32);
        
            Rect headerRect = new Rect(rect.x, rect.y, rect.width, 32);
        
            EditorGUI.LabelField(headerRect, title, EditorCustomStyles.BoxHeader);
        
            GUIStyle buttonStyle = EditorCustomStyles.ButtonHover;
            GUIContent buttonContent = EditorCustomStyles.MenuContent;
        
            Vector2 buttonSize = buttonStyle.CalcSize(buttonContent);
            float sizeMultiplier = buttonSize.y / 16;
        
            buttonSize /= sizeMultiplier;
        
            float yOffset = (headerRect.height - buttonSize.y) / 2;
        
            Rect buttonRect = new Rect(headerRect.x + headerRect.width - buttonSize.x - 8, headerRect.y + yOffset, buttonSize.x, buttonSize.y);
        
            if (GUI.Button(buttonRect, buttonContent, buttonStyle))
            {
                genericMenu.ShowAsContext();
            }
        
            EditorGUILayout.BeginVertical(EditorCustomStyles.BoxContent);
        }

        /// <summary>
        /// Ends a box group in the Unity Editor GUI.
        /// </summary>
        public static void EndBoxGroup()
        {
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Begins a foldout box group in the Unity Editor GUI with a header that can be expanded or collapsed.
        /// </summary>
        /// <param name="value">The initial value indicating whether the box group is expanded or collapsed.</param>
        /// <param name="title">The title of the box group.</param>
        /// <returns>Returns the new value indicating whether the box group is expanded or collapsed.</returns>
        public static bool BeginFoldoutBoxGroup(bool value, string title)
        {
            GUIStyle guiStyle = EditorCustomStyles.Box;
        
            Rect blockRect;
            if (value)
            {
                blockRect = EditorGUILayout.BeginVertical(guiStyle, GUILayout.MinHeight(32));
            }
            else
            {
                blockRect = EditorGUILayout.BeginVertical(GUIStyle.none, GUILayout.MinHeight(32));
            }
        
            Rect headerRect = new Rect(blockRect.x, blockRect.y, blockRect.width, 32);
        
            EditorGUI.LabelField(headerRect, title, EditorCustomStyles.BoxHeader);
        
            if (value)
            {
                EditorGUI.LabelField(new Rect(headerRect.x + headerRect.width - 16, headerRect.y + 8, 16, 16), EditorCustomStyles.FoldoutArrowDown);
            }
            else
            {
                EditorGUI.LabelField(new Rect(headerRect.x + headerRect.width - 16, headerRect.y + 8, 16, 16), EditorCustomStyles.FoldoutArrowRight);
            }
        
            GUILayout.Space(32);
        
            if (GUI.Button(headerRect, GUIContent.none, GUIStyle.none))
            {
                value = !value;
            }
        
            return value;
        }

        /// <summary>
        /// Begins a foldout box group in the Unity Editor GUI with a header that can be expanded or collapsed, and outputs the rectangle area of the foldout.
        /// </summary>
        /// <param name="value">The initial value indicating whether the box group is expanded or collapsed.</param>
        /// <param name="title">The title of the box group.</param>
        /// <param name="foldoutRect">The rectangle area of the foldout.</param>
        /// <returns>Returns the new value indicating whether the box group is expanded or collapsed.</returns>
        public static bool BeginFoldoutBoxGroup(bool value, string title, out Rect foldoutRect)
        {
            Rect blockRect = EditorGUILayout.BeginVertical(EditorCustomStyles.Box, GUILayout.Height(32));
        
            GUI.Box(new Rect(blockRect.x, blockRect.y, blockRect.width, 32), GUIContent.none);
        
            foldoutRect = EditorGUILayout.BeginHorizontal(GUILayout.Height(16));
        
            bool expandValue = EditorGUILayout.Foldout(value, title);
        
            EditorGUILayout.EndHorizontal();
        
            if (value)
            {
                GUILayout.Space(16);
            }
        
            if (GUI.Button(foldoutRect, GUIContent.none, GUIStyle.none))
            {
                expandValue = !expandValue;
            }
        
            return expandValue;
        }

        /// <summary>
        /// Ends a foldout box group in the Unity Editor GUI.
        /// </summary>
        public static void EndFoldoutBoxGroup()
        {
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Creates a header with a toggle in the Unity Editor GUI.
        /// </summary>
        /// <param name="title">The title of the header.</param>
        /// <param name="value">The initial value of the toggle.</param>
        /// <returns>Returns the new value of the toggle after the header is clicked.</returns>
        public static bool HeaderToggle(string title, bool value)
        {
            GUILayout.Space(4);
        
            Rect headerRect = EditorGUILayout.BeginHorizontal(GUILayout.ExpandHeight(true), GUILayout.Height(32));
        
            EditorGUI.LabelField(headerRect, title, EditorCustomStyles.BoxHeader);
        
            value = EditorGUI.Toggle(new Rect(headerRect.x + headerRect.width - 32, headerRect.y + 4, 32, 16), value, EditorCustomStyles.Skin.toggle);
        
            GUILayout.Space(32);
        
            EditorGUILayout.EndHorizontal();
        
            GUILayout.Space(4);
        
            if (GUI.Button(headerRect, GUIContent.none, GUIStyle.none))
            {
                value = !value;
            }
        
            return value;
        }

        /// <summary>
        /// Creates a header with a toggle in the Unity Editor GUI within a specified rectangle area.
        /// </summary>
        /// <param name="rect">The rectangle area within which to draw the header.</param>
        /// <param name="title">The title of the header.</param>
        /// <param name="value">The initial value of the toggle.</param>
        /// <returns>Returns the new value of the toggle after the header is clicked.</returns>
        public static bool HeaderToggle(Rect rect, string title, bool value)
        {
            GUILayout.Space(32);
        
            Rect headerRect = new Rect(rect.x, rect.y, rect.width, 32);
        
            EditorGUI.LabelField(headerRect, title, EditorCustomStyles.BoxHeader);
        
            value = EditorGUI.Toggle(new Rect(headerRect.x + headerRect.width - 32, headerRect.y + 4, 32, 16), value, EditorCustomStyles.Skin.toggle);
        
            if (GUI.Button(headerRect, GUIContent.none, GUIStyle.none))
            {
                value = !value;
            }
        
            return value;
        }

        /// <summary>
        /// Creates a header in the Unity Editor GUI within a specified rectangle area.
        /// </summary>
        /// <param name="rect">The rectangle area within which to draw the header.</param>
        /// <param name="title">The title of the header.</param>
        public static void Header(Rect rect, string title)
        {
            EditorGUI.LabelField(new Rect(rect.x, rect.y, rect.width, 32), title, EditorCustomStyles.BoxHeader);
        
            GUILayout.Space(32);
        }

        /// <summary>
        /// Creates a header in the Unity Editor GUI with a specified title.
        /// </summary>
        /// <param name="title">The title of the header.</param>
        public static void Header(string title)
        {
            GUILayout.Space(4);
        
            EditorGUILayout.LabelField(title, EditorCustomStyles.BoxHeader, GUILayout.ExpandHeight(true), GUILayout.Height(32));
        
            GUILayout.Space(4);
        }

        /// <summary>
        /// Creates a header in the Unity Editor GUI with an expandable arrow, indicating whether the section is expanded or collapsed.
        /// </summary>
        /// <param name="rect">The rectangle area within which to draw the header.</param>
        /// <param name="title">The title of the header.</param>
        /// <param name="value">The initial value indicating whether the section is expanded or collapsed.</param>
        /// <param name="activeContent">The content to display when the section is expanded.</param>
        /// <param name="disableContent">The content to display when the section is collapsed.</param>
        /// <returns>Returns the new value indicating whether the section is expanded or collapsed.</returns>
        public static bool HeaderExpand(Rect rect, string title, bool value, GUIContent activeContent, GUIContent disableContent)
        {
            GUILayout.Space(32);
        
            Rect headerRect = new Rect(rect.x, rect.y, rect.width, 32);
        
            if (value)
            {
                EditorGUI.LabelField(headerRect, new GUIContent(title, EditorCustomStyles.FoldoutArrowDown.image), EditorCustomStyles.BoxHeader);
            }
            else
            {
                EditorGUI.LabelField(headerRect, new GUIContent(title, EditorCustomStyles.FoldoutArrowRight.image), EditorCustomStyles.BoxHeader);
            }
        
            if (GUI.Button(headerRect, GUIContent.none, GUIStyle.none))
            {
                value = !value;
            }
        
            return value;
        }

        /// <summary>
        /// Creates a header in the Unity Editor GUI with an expandable arrow, indicating whether the section is expanded or collapsed.
        /// </summary>
        /// <param name="title">The title of the header.</param>
        /// <param name="value">The initial value indicating whether the section is expanded or collapsed.</param>
        /// <param name="activeContent">The content to display when the section is expanded.</param>
        /// <param name="disableContent">The content to display when the section is collapsed.</param>
        /// <returns>Returns the new value indicating whether the section is expanded or collapsed.</returns>
        public static bool HeaderExpand(string title, bool value, GUIContent activeContent, GUIContent disableContent)
        {
            GUILayout.Space(4);
        
            Rect headerRect = EditorGUILayout.BeginHorizontal(GUILayout.ExpandHeight(true), GUILayout.Height(32));
        
            EditorGUI.LabelField(headerRect, title, EditorCustomStyles.BoxHeader);
        
            float offsetY = (headerRect.height - 16) / 2;
        
            if (value)
            {
                EditorGUI.LabelField(new Rect(headerRect.x + headerRect.width - 16, headerRect.y + offsetY, 16, 16), EditorCustomStyles.FoldoutArrowDown, activeContent);
            }
            else
            {
                EditorGUI.LabelField(new Rect(headerRect.x + headerRect.width - 16, headerRect.y + offsetY, 16, 16), EditorCustomStyles.FoldoutArrowRight, disableContent);
            }
        
            GUILayout.Space(16);
        
            EditorGUILayout.EndHorizontal();
        
            GUILayout.Space(4);
        
            if (GUI.Button(headerRect, GUIContent.none, GUIStyle.none))
            {
                value = !value;
            }
        
            return value;
        }

        /// <summary>
        /// Creates a header with a button in the Unity Editor GUI within a specified rectangle area.
        /// </summary>
        /// <param name="rect">The rectangle area within which to draw the header.</param>
        /// <param name="title">The title of the header.</param>
        /// <param name="buttonContent">The content of the button.</param>
        /// <param name="buttonStyle">The style of the button.</param>
        /// <param name="disableState">The state indicating whether the button is disabled.</param>
        /// <returns>Returns true if the button is clicked, otherwise false.</returns>
        public static bool HeaderButton(Rect rect, string title, GUIContent buttonContent, GUIStyle buttonStyle, bool disableState)
        {
            bool isClicked = false;
        
            GUILayout.Space(32);
        
            Rect headerRect = new Rect(rect.x, rect.y, rect.width, 32);
        
            EditorGUI.LabelField(headerRect, title, EditorCustomStyles.BoxHeader);
        
            using (new EditorGUI.DisabledScope(disableState))
            {
                Vector2 buttonSize = buttonStyle.CalcSize(buttonContent);
                float sizeMultiplier = buttonSize.y / 16;
        
                buttonSize /= sizeMultiplier;
        
                float yOffset = (headerRect.height - buttonSize.y) / 2;
        
                Rect buttonRect = new Rect(headerRect.x + headerRect.width - buttonSize.x - 8, headerRect.y + yOffset, buttonSize.x, buttonSize.y);
        
                if (GUI.Button(buttonRect, buttonContent, buttonStyle))
                {
                    isClicked = true;
                }
            }
        
            return isClicked;
        }

        /// <summary>
        /// Creates a header with a button in the Unity Editor GUI.
        /// </summary>
        /// <param name="title">The title of the header.</param>
        /// <param name="buttonContent">The content of the button.</param>
        /// <param name="buttonStyle">The style of the button.</param>
        /// <param name="disableState">The state indicating whether the button is disabled.</param>
        /// <returns>Returns true if the button is clicked, otherwise false.</returns>
        public static bool HeaderButton(string title, GUIContent buttonContent, GUIStyle buttonStyle, bool disableState)
        {
            bool isClicked = false;
        
            Rect headerRect = EditorGUILayout.BeginHorizontal(GUILayout.ExpandHeight(true), GUILayout.Height(32));
        
            EditorGUI.LabelField(headerRect, title, EditorCustomStyles.BoxHeader);
        
            using (new EditorGUI.DisabledScope(disableState))
            {
                Vector2 buttonSize = buttonStyle.CalcSize(buttonContent);
                float sizeMultiplier = buttonSize.y / 16;
        
                buttonSize /= sizeMultiplier;
        
                float yOffset = (headerRect.height - buttonSize.y) / 2;
        
                Rect buttonRect = new Rect(headerRect.x + headerRect.width - buttonSize.x - 8, headerRect.y + yOffset, buttonSize.x, buttonSize.y);
        
                if (GUI.Button(buttonRect, buttonContent, buttonStyle))
                {
                    isClicked = true;
                }
            }
        
            GUILayout.Space(16);
        
            EditorGUILayout.EndHorizontal();
        
            GUILayout.Space(4);
        
            return isClicked;
        }

        /// <summary>
        /// Draws all properties of a serialized object in the Unity Editor GUI.
        /// </summary>
        /// <param name="serializedObject">The serialized object whose properties are to be drawn.</param>
        /// <param name="skipFirst">Indicates whether to skip the first property (usually the script field). Default is true.</param>
        public static void DrawAllProperties(SerializedObject serializedObject, bool skipFirst = true)
        {
            var prop = serializedObject.GetIterator();
        
            if (skipFirst)
            {
                // Ignore script field
                prop.NextVisible(true);
            }
        
            while (prop.NextVisible(false))
            {
                EditorGUILayout.PropertyField(prop, true);
            }
        }
    }
}