using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SWeetJelly.Editor
{
    public static class TMPUtils
    {
        [MenuItem("CONTEXT/Text/Replace Text Component With Text Mesh Pro", validate = true)]
        private static bool TextSelectedValidation()
        {
            var selectedObjects = Selection.gameObjects;
            if (selectedObjects.Length == 0) return false;

            foreach (var selectedObject in selectedObjects)
            {
                var text = selectedObject.GetComponent<Text>();
                if (!text || EditorUtility.IsPersistent(selectedObject) || selectedObject.GetComponent<TextMeshProUGUI>()) return false;
            }

            return true;
        }

        [MenuItem("CONTEXT/Text/Replace Text Component With Text Mesh Pro")]
        private static void ReplaceSelectedObjects()
        {
            var selectedObjects = Selection.gameObjects;
            if (!TextSelectedValidation())
                throw new System.InvalidOperationException("Select scene objects with legacy Text and no TMP component.");
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Replace Text Components With Text Mesh Pro");
            try
            {

            foreach (var selectedObject in selectedObjects)
            {
                var textComp = selectedObject.GetComponent<Text>();
                var textSizeDelta = textComp.rectTransform.sizeDelta;
                var saved = new TextState(textComp);
                Undo.RecordObject(textComp.rectTransform, "Preserve Text Layout");
                Undo.DestroyObjectImmediate(textComp);

                var tmp = Undo.AddComponent<TextMeshProUGUI>(selectedObject);

                tmp.text = saved.text;

                tmp.fontSize = saved.fontSize;

                var fontStyle = saved.fontStyle;
                switch (fontStyle)
                {
                    case FontStyle.Normal:
                        tmp.fontStyle = FontStyles.Normal;
                        break;
                    case FontStyle.Bold:
                        tmp.fontStyle = FontStyles.Bold;
                        break;
                    case FontStyle.Italic:
                        tmp.fontStyle = FontStyles.Italic;
                        break;
                    case FontStyle.BoldAndItalic:
                        tmp.fontStyle = FontStyles.Bold | FontStyles.Italic;
                        break;
                }

                tmp.enableAutoSizing = saved.resizeTextForBestFit;
                tmp.fontSizeMin = saved.resizeTextMinSize;
                tmp.fontSizeMax = saved.resizeTextMaxSize;

                var alignment = saved.alignment;
                switch (alignment)
                {
                    case TextAnchor.UpperLeft:
                        tmp.alignment = TextAlignmentOptions.TopLeft;
                        break;
                    case TextAnchor.UpperCenter:
                        tmp.alignment = TextAlignmentOptions.Top;
                        break;
                    case TextAnchor.UpperRight:
                        tmp.alignment = TextAlignmentOptions.TopRight;
                        break;
                    case TextAnchor.MiddleLeft:
                        tmp.alignment = TextAlignmentOptions.MidlineLeft;
                        break;
                    case TextAnchor.MiddleCenter:
                        tmp.alignment = TextAlignmentOptions.Midline;
                        break;
                    case TextAnchor.MiddleRight:
                        tmp.alignment = TextAlignmentOptions.MidlineRight;
                        break;
                    case TextAnchor.LowerLeft:
                        tmp.alignment = TextAlignmentOptions.BottomLeft;
                        break;
                    case TextAnchor.LowerCenter:
                        tmp.alignment = TextAlignmentOptions.Bottom;
                        break;
                    case TextAnchor.LowerRight:
                        tmp.alignment = TextAlignmentOptions.BottomRight;
                        break;
                }

                tmp.textWrappingMode = saved.horizontalOverflow == HorizontalWrapMode.Wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;

                tmp.color = saved.color;
                tmp.raycastTarget = saved.raycastTarget;
                tmp.richText = saved.supportRichText;

                tmp.rectTransform.sizeDelta = textSizeDelta;
                PrefabUtility.RecordPrefabInstancePropertyModifications(tmp);
                PrefabUtility.RecordPrefabInstancePropertyModifications(tmp.rectTransform);
            }
            Undo.CollapseUndoOperations(undoGroup);
            }
            catch
            {
                Undo.RevertAllDownToGroup(undoGroup);
                throw;
            }
        }

        private sealed class TextState
        {
            internal string text;
            internal int fontSize, resizeTextMinSize, resizeTextMaxSize;
            internal FontStyle fontStyle;
            internal bool resizeTextForBestFit, raycastTarget, supportRichText;
            internal TextAnchor alignment;
            internal HorizontalWrapMode horizontalOverflow;
            internal Color color;
            internal TextState(Text source)
            {
                text = source.text; fontSize = source.fontSize; fontStyle = source.fontStyle;
                resizeTextForBestFit = source.resizeTextForBestFit;
                resizeTextMinSize = source.resizeTextMinSize; resizeTextMaxSize = source.resizeTextMaxSize;
                alignment = source.alignment; horizontalOverflow = source.horizontalOverflow;
                color = source.color; raycastTarget = source.raycastTarget; supportRichText = source.supportRichText;
            }
        }
    }
}
