using UnityEditor;
using UnityEngine;

namespace SWeetJelly.Editor
{
    [InitializeOnLoad]
    public static class EditorCustomStyles
    {
        private const string EditorResourcesPath = "Assets/sweetjelly-editor/Resources/";

        private const string ProjectConfigAssetPath = EditorResourcesPath + "EditorCustomStylesConfig.asset";

        private const string ImportMenuPath = "SWeetJelly/Tools/Import Editor Custom Styles";

        private const int MaxStylesLoadAttempt = 5;

        private const int RequiredConfigVersion = 1;

        public const string ICON_SPACE = "  ";

        private static EditorCustomStylesConfig editorCustomStylesConfig;

        private static GUISkin guiSkin;
        public static GUISkin Skin => guiSkin;

        public static GUIStyle Tab;

        public static GUIStyle Box;
        public static GUIStyle BoxHeader;
        public static GUIStyle BoxBottomPanel;

        public static GUIStyle BoxGroupBackground;
        public static GUIStyle BoxContent;

        public static GUIStyle Label;
        public static GUIStyle LabelBold;
        public static GUIStyle LabelCentered;

        public static GUIStyle LabelSmall;
        public static GUIStyle LabelSmallBold;

        public static GUIStyle LabelMedium;
        public static GUIStyle LabelMediumBold;

        public static GUIStyle LabelLarge;
        public static GUIStyle LabelLargeBold;

        public static GUIStyle Button;
        public static GUIStyle ButtonMini;
        public static GUIStyle ButtonHover;

        public static GUIStyle ButtonBlue;
        public static GUIStyle ButtonGreen;
        public static GUIStyle ButtonRed;

        public static GUIStyle Padding00;
        public static GUIStyle Padding05;
        public static GUIStyle Padding10;

        public static GUIStyle BoxCompiling;

        public static GUIStyle WindowSpacedContent;

        public static GUIContent FoldoutArrowDown;
        public static GUIContent FoldoutArrowUp;
        public static GUIContent FoldoutArrowRight;

        public static GUIContent MenuContent;

        private static int stylesLoadAttempt;
        private static bool isLoadScheduled;
        private static bool hasInitialisedEmergencyStyles;

        static EditorCustomStyles()
        {
            LoadStylesData();
        }

        public static void CheckStyles()
        {
            if (editorCustomStylesConfig == null)
            {
                LoadStylesData();
            }
        }

        private static EditorCustomStylesConfig LoadProjectConfig()
        {
            return AssetDatabase.LoadAssetAtPath<EditorCustomStylesConfig>(
                ProjectConfigAssetPath);
        }

        private static void LoadStylesData()
        {
            isLoadScheduled = false;

            if (editorCustomStylesConfig != null)
            {
                return;
            }

            InitEmergencyStylesIfNeeded();

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                ScheduleLoadStylesData();
                return;
            }

            EditorCustomStylesConfig loadedConfig = LoadProjectConfig();

            if (loadedConfig == null)
            {
                stylesLoadAttempt++;

                if (stylesLoadAttempt <= MaxStylesLoadAttempt)
                {
                    ScheduleLoadStylesData();
                    return;
                }

                Debug.LogError(
                    $"EditorCustomStylesConfig was not found at '{ProjectConfigAssetPath}'. " +
                    $"Run '{ImportMenuPath}' to install the editor custom styles resources.");

                return;
            }

            if (loadedConfig.Version < RequiredConfigVersion)
            {
                Debug.LogError(
                    $"EditorCustomStylesConfig is outdated. " + $"Required version: {RequiredConfigVersion}, " + $"found version: {loadedConfig.Version}. " +
                    $"Check for updates or run '{ImportMenuPath}' to re-import the editor custom styles resources.");

                return;
            }

            if (loadedConfig.Skin == null)
            {
                Debug.LogError(
                    $"EditorCustomStylesConfig was found at '{ProjectConfigAssetPath}', " +
                    "but its GUISkin reference is missing. " +
                    $"Check the config asset or run '{ImportMenuPath}' again.");

                return;
            }

            editorCustomStylesConfig = loadedConfig;
            guiSkin = editorCustomStylesConfig.Skin;

            stylesLoadAttempt = 0;
            hasInitialisedEmergencyStyles = false;

            InitStyles();
        }

        private static void ScheduleLoadStylesData()
        {
            if (isLoadScheduled)
            {
                return;
            }

            isLoadScheduled = true;
            EditorApplication.delayCall += LoadStylesData;
        }

        private static void InitEmergencyStylesIfNeeded()
        {
            if (hasInitialisedEmergencyStyles)
            {
                return;
            }

            InitEmergencyStyles();
            hasInitialisedEmergencyStyles = true;
        }

        private static void InitEmergencyStyles()
        {
            guiSkin = EditorGUIUtility.GetBuiltinSkin(EditorSkin.Game);

            Tab = guiSkin.button;

            Box = guiSkin.box;
            BoxHeader = guiSkin.box;
            BoxBottomPanel = guiSkin.box;

            BoxGroupBackground = new GUIStyle(guiSkin.box)
            {
                padding = new RectOffset(4, 4, 0, 0),
                margin = new RectOffset(0, 0, 4, 4)
            };

            BoxContent = new GUIStyle
            {
                padding = new RectOffset(2, 2, 4, 8)
            };

            BoxCompiling = guiSkin.box;

            Label = guiSkin.label;

            LabelBold = new GUIStyle(Label)
            {
                fontStyle = FontStyle.Bold
            };

            LabelCentered = new GUIStyle(Label)
            {
                alignment = TextAnchor.MiddleCenter
            };

            PrepareLabelStyle(ref LabelSmall, ref LabelSmallBold, 8);
            PrepareLabelStyle(ref LabelMedium, ref LabelMediumBold, 12);
            PrepareLabelStyle(ref LabelLarge, ref LabelLargeBold, 16);

            Button = guiSkin.button;

            ButtonMini = new GUIStyle(Button)
            {
                padding = new RectOffset(4, 4, 4, 4),
                margin = new RectOffset(1, 1, 2, 1),
                fontSize = 8,
                alignment = TextAnchor.MiddleCenter
            };

            ButtonHover = guiSkin.button;
            ButtonBlue = guiSkin.button;
            ButtonGreen = guiSkin.button;
            ButtonRed = guiSkin.button;

            Padding00 = new GUIStyle
            {
                padding = new RectOffset(0, 0, 0, 0)
            };

            Padding05 = new GUIStyle
            {
                padding = new RectOffset(0, 0, 5, 5)
            };

            Padding10 = new GUIStyle
            {
                padding = new RectOffset(0, 0, 10, 10)
            };

            WindowSpacedContent = new GUIStyle
            {
                padding = new RectOffset(16, 4, 4, 4)
            };

            FoldoutArrowDown = new GUIContent("↓");
            FoldoutArrowUp = new GUIContent("↑");
            FoldoutArrowRight = new GUIContent("→");

            MenuContent = new GUIContent(
                "",
                EditorGUIUtility.IconContent("_Menu@2x").image,
                "Menu");
        }

        private static void InitStyles()
        {
            if (guiSkin == null)
            {
                InitEmergencyStyles();
                return;
            }

            Tab = GetStyleOrFallback("tab", guiSkin.button);

            Box = guiSkin.box;
            BoxHeader = GetStyleOrFallback("boxHeader", guiSkin.box);
            BoxBottomPanel = GetStyleOrFallback("bottomPanel", guiSkin.box);

            BoxGroupBackground = new GUIStyle(Box)
            {
                padding = new RectOffset(4, 4, 0, 0),
                margin = new RectOffset(0, 0, 4, 4)
            };

            BoxContent = new GUIStyle
            {
                padding = new RectOffset(2, 2, 4, 8)
            };

            Label = guiSkin.label;

            LabelBold = new GUIStyle(Label)
            {
                fontStyle = FontStyle.Bold
            };

            LabelCentered = new GUIStyle(Label)
            {
                alignment = TextAnchor.MiddleCenter
            };

            PrepareLabelStyle(ref LabelSmall, ref LabelSmallBold, 8);
            PrepareLabelStyle(ref LabelMedium, ref LabelMediumBold, 12);
            PrepareLabelStyle(ref LabelLarge, ref LabelLargeBold, 16);

            Button = guiSkin.button;
            ButtonHover = GetStyleOrFallback("buttonHover", guiSkin.button);

            ButtonMini = new GUIStyle(Button)
            {
                padding = new RectOffset(4, 4, 4, 4),
                margin = new RectOffset(1, 1, 2, 1),
                fontSize = 8,
                alignment = TextAnchor.MiddleCenter
            };

            ButtonBlue = GetStyleOrFallback("buttonBlue", guiSkin.button);
            ButtonGreen = GetStyleOrFallback("buttonGreen", guiSkin.button);
            ButtonRed = GetStyleOrFallback("buttonRed", guiSkin.button);

            Padding00 = new GUIStyle
            {
                padding = new RectOffset(0, 0, 0, 0)
            };

            Padding05 = new GUIStyle
            {
                padding = new RectOffset(0, 0, 5, 5)
            };

            Padding10 = new GUIStyle
            {
                padding = new RectOffset(0, 0, 10, 10)
            };

            BoxCompiling = GetStyleOrFallback("boxCompiling", guiSkin.box);

            WindowSpacedContent = new GUIStyle
            {
                padding = new RectOffset(16, 4, 4, 4)
            };

            FoldoutArrowDown = new GUIContent(GetIcon("foldout_arrow_down"));
            FoldoutArrowUp = new GUIContent(GetIcon("foldout_arrow_up"));
            FoldoutArrowRight = new GUIContent(GetIcon("foldout_arrow_right"));

            MenuContent = new GUIContent(
                "",
                EditorGUIUtility.IconContent("_Menu@2x").image,
                "Menu");
        }

        private static GUIStyle GetStyleOrFallback(string styleName, GUIStyle fallback)
        {
            GUIStyle style = guiSkin.FindStyle(styleName);

            if (style != null)
            {
                return style;
            }

            Debug.LogError(
                $"EditorCustomStyles : GUISkin style '{styleName}' is missing. " +
                $"Check '{ProjectConfigAssetPath}' or run '{ImportMenuPath}' again.");

            return fallback;
        }

        public static Texture2D GetMissingIcon()
        {
            if (editorCustomStylesConfig == null)
            {
                return null;
            }

            return editorCustomStylesConfig.MissingIcon;
        }

        public static Texture2D GetIcon(string name)
        {
            if (editorCustomStylesConfig == null)
            {
                return null;
            }

            Texture2D icon = editorCustomStylesConfig.GetIcon(name);

            if (icon != null)
            {
                return icon;
            }

            return editorCustomStylesConfig.MissingIcon;
        }

        public static Texture2D GetIcon(string name, Color color)
        {
            Texture2D icon = GetIcon(name);

            if (icon == null)
            {
                return null;
            }

            return icon.ChangeColor(color);
        }

        private static void PrepareLabelStyle(
            ref GUIStyle labelStyle,
            ref GUIStyle boldLabelStyle,
            int fontSize)
        {
            GUIStyle baseLabelStyle = Label ?? EditorGUIUtility.GetBuiltinSkin(EditorSkin.Game).label;

            labelStyle = new GUIStyle(baseLabelStyle)
            {
                fontSize = fontSize
            };

            boldLabelStyle = new GUIStyle(labelStyle)
            {
                fontStyle = FontStyle.Bold
            };
        }
    }
}