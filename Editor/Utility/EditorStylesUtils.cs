using UnityEngine;

namespace SWeetJelly.Editor
{
    public static class EditorStylesUtils
    {
        public static GUIStyle ConvertButtonToSmall(this GUIStyle parentStyle)
        {
            GUIStyle smallButton = new GUIStyle(parentStyle);
            smallButton.padding = new RectOffset(0, 0, 0, 0);
            smallButton.margin = new RectOffset(1, 1, 1, 1);
            smallButton.fontStyle = FontStyle.Bold;
            smallButton.fontSize = 12;

            return smallButton;
        }

        public static GUIStyle GetAligmentStyle(this GUIStyle style, TextAnchor textAnchor)
        {
            GUIStyle tempStyle = new GUIStyle(style);
            tempStyle.alignment = textAnchor;

            return tempStyle;
        }

        public static GUIStyle GetTextColorStyle(this GUIStyle style, Color color)
        {
            GUIStyle tempStyle = new GUIStyle(style);
            tempStyle.normal.textColor = color;

            return tempStyle;
        }

        public static GUIStyle GetTextFontSizeStyle(this GUIStyle style, int fontSize)
        {
            GUIStyle tempStyle = new GUIStyle(style);
            tempStyle.fontSize = fontSize;

            return tempStyle;
        }

        public static GUIStyle GetPaddingStyle(this GUIStyle style, RectOffset padding)
        {
            GUIStyle tempStyle = new GUIStyle(style);
            tempStyle.padding = padding;

            return tempStyle;
        }

        public static Texture2D ChangeColor(this Texture2D texture, Color color)
        {
            if (texture != null)
            {
                Texture2D tempTexture = new Texture2D(texture.width, texture.height);
                tempTexture.SetPixels(texture.GetPixels());

                for (int x = 0; x < tempTexture.width; x++)
                {
                    for (int y = 0; y < tempTexture.height; y++)
                    {
                        Color tempColor = tempTexture.GetPixel(x, y);
                        if (tempColor.a > 0)
                        {
                            tempTexture.SetPixel(x, y, color.SetAlpha(tempColor.a));
                        }
                    }
                }

                tempTexture.Apply();

                return tempTexture;
            }

            return null;
        }

        public static GUIStyle GetBoxWithColor(Color color)
        {
            Texture2D backgroundTexture = new Texture2D(1, 1);
            backgroundTexture.SetPixel(0, 0, color);
            backgroundTexture.Apply();

            GUIStyle backgroundStyle = new GUIStyle();
            backgroundStyle.normal.background = backgroundTexture;

            return backgroundStyle;
        }
    }
}
