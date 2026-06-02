using UnityEditor;
using UnityEngine;

namespace SWeetJelly.Editor
{
    internal static class EditorCustomStylesResourceImporter
    {
        private const string ImportMenuPath =
            "SWeetJelly/Tools/Import Editor Custom Styles";

        private const string EditorCustomStylesPackagePath =
            "Packages/com.sweetjelly.editor/Resources/EditorCustomStylesResources.unitypackage";

        [MenuItem(ImportMenuPath)]
        private static void ImportEditorCustomStyles()
        {
            if (!System.IO.File.Exists(EditorCustomStylesPackagePath))
            {
                Debug.LogError(
                    $"Editor custom styles package was not found at '{EditorCustomStylesPackagePath}'.");
                return;
            }

            AssetDatabase.ImportPackage(EditorCustomStylesPackagePath, true);
        }
    }
}