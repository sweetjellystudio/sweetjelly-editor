using System;
using System.Collections;
using Object = UnityEngine.Object;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.TextCore;
using UnityEngine.U2D;

namespace SWeetJelly.Editor
{
    public class TMPAtlasGenerator
    {
        internal static System.Func<string, string, string, string, bool> Confirm = EditorUtility.DisplayDialog;
        private const string FILE_PATH_SAVE = "atlas_generator_file_path";
        private List<SpriteData> elements;

        private string filePath;
        
        public TMPAtlasGenerator(string path)
        {
            this.filePath = path;

            elements = new List<SpriteData>();
        }

        public void Add(SpriteData element)
        {
            elements.Add(element);
        }

        public void Save()
        {
            Validate(elements, filePath);
            if (EditorSettings.spritePackerMode != SpritePackerMode.SpriteAtlasV2)
                throw new InvalidOperationException("Enable Sprite Atlas V2 in Project Settings > Editor > Sprite Packer before generating a TMP atlas.");
            SpriteAtlasAsset spriteAtlasAsset = new SpriteAtlasAsset();
            try
            {
                spriteAtlasAsset.Add(elements.Select(x => (Object)x.Sprite).ToArray());
                SpriteAtlasAsset.Save(spriteAtlasAsset, filePath);
            }
            finally { Object.DestroyImmediate(spriteAtlasAsset); }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            SpriteAtlasImporter spriteAtlasImporter = (SpriteAtlasImporter)SpriteAtlasImporter.GetAtPath(filePath);
            spriteAtlasImporter.packingSettings = new SpriteAtlasPackingSettings()
            {
                enableTightPacking = false,
                enableAlphaDilation = true
            };

            spriteAtlasImporter.SaveAndReimport();

            var importedAtlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(filePath);
            if (importedAtlas != null)
                SpriteAtlasUtility.PackAtlases(new[] { importedAtlas }, EditorUserBuildSettings.activeBuildTarget);

            AssetDatabase.Refresh();
        }

        public IEnumerator<SpriteAtlas> GetSpriteAtlas()
        {
            SpriteAtlas spriteAtlas = null;

            double deadline = EditorApplication.timeSinceStartup + 30;
            do
            {
                if (EditorApplication.timeSinceStartup >= deadline)
                    throw new TimeoutException("Sprite atlas import did not complete within 30 seconds: " + filePath);
                yield return null;

                spriteAtlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(filePath);
            }
            while (spriteAtlas == null);

            yield return spriteAtlas;
        }

        public bool IsEmpty()
        {
            return elements.IsNullOrEmpty();
        }

        public void CreateSpriteAsset(SpriteAtlas spriteAtlas)
        {
            Validate(elements, filePath);
            if (spriteAtlas == null || spriteAtlas.spriteCount == 0)
                throw new InvalidOperationException("The imported sprite atlas is empty.");
            string assetPath = Path.ChangeExtension(filePath, ".asset").Replace('\\', '/');
            var existing = AssetDatabase.LoadMainAssetAtPath(assetPath);
            if (existing != null && !(existing is TMP_SpriteAsset))
                throw new InvalidOperationException("The output path contains a different asset type: " + assetPath);
            TMP_SpriteAsset spriteAsset = existing as TMP_SpriteAsset;
            bool create = spriteAsset == null;
            if (create) spriteAsset = ScriptableObject.CreateInstance<TMP_SpriteAsset>();
            spriteAsset.name = Path.GetFileNameWithoutExtension(assetPath);

            // Compute the hash code for the sprite asset.
            spriteAsset.hashCode = TMP_TextUtilities.GetSimpleHashCode(spriteAsset.name);

            List<TMP_SpriteGlyph> spriteGlyphTable = new List<TMP_SpriteGlyph>();
            List<TMP_SpriteCharacter> spriteCharacterTable = new List<TMP_SpriteCharacter>();

            // Get number of sprites contained in the sprite atlas.
            int spriteCount = spriteAtlas.spriteCount;
            Sprite[] sprites = new Sprite[spriteCount];

            // Get all the sprites
            spriteAtlas.GetSprites(sprites);

            for (int i = 0; i < sprites.Length; i++)
            {
                Sprite sprite = sprites[i];
                string realName = sprite.name.EndsWith("(Clone)", StringComparison.Ordinal) ? sprite.name.Substring(0, sprite.name.Length - 7) : sprite.name;
                SpriteData linkedElement = GetAtlasElement(realName);

                TMP_SpriteGlyph spriteGlyph = new TMP_SpriteGlyph();
                spriteGlyph.index = (uint)i;

                if ((linkedElement != null) && (linkedElement.OverrideDataSet))
                {
                    spriteGlyph.metrics = linkedElement.GlyphMetrics;
                    spriteGlyph.scale = linkedElement.Scale;
                }
                else
                {
                    spriteGlyph.metrics = new GlyphMetrics(sprite.rect.width, sprite.rect.height, -sprite.pivot.x, sprite.rect.height - sprite.pivot.y, sprite.rect.width);
                    spriteGlyph.scale = 1.0f;
                }

                spriteGlyph.glyphRect = new GlyphRect(sprite.textureRect);
                // Atlas.GetSprites returns transient clones; keep the serialized source Sprite reference stable.
                spriteGlyph.sprite = linkedElement != null ? linkedElement.Sprite : sprite;
                spriteGlyphTable.Add(spriteGlyph);

                TMP_SpriteCharacter spriteCharacter = new TMP_SpriteCharacter(0xFFFE, spriteGlyph);
                
                if (linkedElement != null)
                {
                    spriteCharacter.name = linkedElement.Name;
                }
                else
                {
                    spriteCharacter.name = realName;
                }

                spriteCharacter.scale = 1.0f;

                spriteCharacterTable.Add(spriteCharacter);
            }

            if (sprites.Select(sprite => sprite.texture).Distinct().Count() != 1)
            {
                if (create) Object.DestroyImmediate(spriteAsset);
                throw new InvalidOperationException("TMP sprite assets require a single atlas texture page.");
            }
            if (Shader.Find("TextMeshPro/Sprite") == null)
            {
                if (create) Object.DestroyImmediate(spriteAsset);
                throw new InvalidOperationException("Import TMP essential resources before generating a sprite asset.");
            }
            if (create) AssetDatabase.CreateAsset(spriteAsset, assetPath);
            Undo.RecordObject(spriteAsset, "Update TMP Sprite Asset");
            spriteAsset.spriteCharacterTable.Clear();
            spriteAsset.spriteCharacterTable.AddRange(spriteCharacterTable);
            spriteAsset.spriteGlyphTable.Clear();
            spriteAsset.spriteGlyphTable.AddRange(spriteGlyphTable);
            using (var serialized = new SerializedObject(spriteAsset))
            {
                var version = serialized.FindProperty("m_Version");
                if (version != null) version.stringValue = "1.1.0";
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            spriteAsset.spriteSheet = sprites[0].texture;

            // Add new default material for sprite asset.
            AddDefaultMaterial(spriteAsset);

            // Update Lookup tables.
            spriteAsset.UpdateLookupTables();

            // Get the Sprites contained in the Sprite Sheet
            EditorUtility.SetDirty(spriteAsset);

            AssetDatabase.SaveAssets();

            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(spriteAsset));
            foreach (var sprite in sprites)
                if (sprite != null && !EditorUtility.IsPersistent(sprite)) Object.DestroyImmediate(sprite);
        }

        private void AddDefaultMaterial(TMP_SpriteAsset spriteAsset)
        {
            UnityEngine.Shader shader = UnityEngine.Shader.Find("TextMeshPro/Sprite");
            Material material = spriteAsset.material;
            bool create = material == null || AssetDatabase.GetAssetPath(material) != AssetDatabase.GetAssetPath(spriteAsset);
            if (create) material = new Material(shader) { name = spriteAsset.name + " Material" };
            else Undo.RecordObject(material, "Update TMP Sprite Material");
            material.SetTexture(ShaderUtilities.ID_MainTex, spriteAsset.spriteSheet);

            spriteAsset.material = material;
            material.hideFlags = HideFlags.HideInHierarchy;
            if (create) AssetDatabase.AddObjectToAsset(material, spriteAsset);
            EditorUtility.SetDirty(material);
        }

        private SpriteData GetAtlasElement(string spriteName)
        {
            foreach (SpriteData element in elements)
            {
                if (element.Sprite.name == spriteName)
                    return element;
            }

            return null;
        }

        public static TMPAtlasGenerator Create(List<SpriteData> atlasElements, string path)
        {
            TMPAtlasGenerator atlasGenerator = new TMPAtlasGenerator(path);

            EditorCoroutines.Execute(atlasGenerator.AtlasCoroutine(atlasElements));

            return atlasGenerator;
        }

        public IEnumerator AtlasCoroutine(List<SpriteData> atlasElements)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                string savedPath = EditorPrefs.GetString(FILE_PATH_SAVE);
                if(!string.IsNullOrEmpty(savedPath))
                {
                    savedPath = Path.GetDirectoryName(savedPath);
                }
                else
                {
                    savedPath = "Assets";
                }

                filePath = EditorUtility.SaveFilePanelInProject("Generated Atlas", "GeneratedAtlas", "spriteatlasv2", "Select atlas path", savedPath);

                if (string.IsNullOrEmpty(filePath))
                {
                    yield break;
                }

                EditorPrefs.SetString(FILE_PATH_SAVE, Path.GetDirectoryName(filePath));
            }

            Validate(atlasElements, filePath);
            elements = atlasElements.Select(item => new SpriteData(item.Sprite, item.Name)
            { OverrideDataSet = item.OverrideDataSet, GlyphMetrics = item.GlyphMetrics, Scale = item.Scale }).ToList();
            bool atlasExisted = File.Exists(filePath);
            string outputPath = Path.ChangeExtension(filePath, ".asset").Replace('\\', '/');
            bool assetExisted = File.Exists(outputPath);
            bool complete = false;
            if (!activePaths.Add(filePath)) throw new InvalidOperationException("Atlas generation is already running: " + filePath);
            try
            {

            //Handle possible override
            if (!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(filePath, AssetPathToGUIDOptions.OnlyExistingAssets)))
            {
                string TMP_AssetFilePath = Path.ChangeExtension(filePath, ".asset");
                TMP_SpriteAsset TMP_AssetFile = AssetDatabase.LoadAssetAtPath<TMP_SpriteAsset>(TMP_AssetFilePath);

                if ((TMP_AssetFile != null) && (Confirm("Asset override", "Do you want to copy sprite GlyphMetrics and scale from overriden asset?", "Yes", "No")))
                {
                    for (int i = 0; i < TMP_AssetFile.spriteCharacterTable.Count; i++)
                    {
                        for (int j = 0; j < elements.Count; j++)
                        {
                            if (!elements[j].OverrideDataSet)
                            {
                                if (elements[j].Name.Equals(TMP_AssetFile.spriteCharacterTable[i].name))
                                {
                                    elements[j].GlyphMetrics = TMP_AssetFile.spriteCharacterTable[i].glyph.metrics;
                                    elements[j].Scale = TMP_AssetFile.spriteCharacterTable[i].glyph.scale;
                                    elements[j].OverrideDataSet = true;
                                }
                            }
                        }
                    }
                }
            }

            Save();

            IEnumerator<SpriteAtlas> spriteAtlasEnumerator = GetSpriteAtlas();
            while (spriteAtlasEnumerator.MoveNext())
            {
                yield return null;
            }

            SpriteAtlas spriteAtlas = spriteAtlasEnumerator.Current;
            if (spriteAtlas == null)
            {
                throw new InvalidOperationException("Failed to create sprite atlas: " + filePath);
            }

            EditorUtility.SetDirty(spriteAtlas);

            yield return null;

            CreateSpriteAsset(spriteAtlas);

            complete = true;

            //Applying Ref to TMP settings
            TMP_Settings settings = EditorUtils.GetAsset<TMP_Settings>();

            if (settings != null)
            {

                if (Confirm("Linking asset to TMP Settings", "Do you want to add created \"Sprite Atlas\" to \"TMP Settings\"?", "Yes", "Cancel"))
                {
                    SerializedObject settingsSerializedObject = new SerializedObject(settings);
                    SerializedProperty defaultAssetProperty = settingsSerializedObject.FindProperty("m_defaultSpriteAsset");
                    string TMP_AssetFilePath = Path.ChangeExtension(filePath, ".asset");
                    TMP_SpriteAsset TMP_AssetFile = AssetDatabase.LoadAssetAtPath<TMP_SpriteAsset>(TMP_AssetFilePath);

                    if ((defaultAssetProperty.objectReferenceValue == null) || Confirm("Linking asset to TMP Settings", "Do you want to override \"Default Sprite Asset\" reference with created \"Sprite Atlas\" or add reference to fallback list of current \"Default Sprite Asset\" ?", "Override", "Add to fallback list"))
                    {
                        defaultAssetProperty.objectReferenceValue = TMP_AssetFile;
                        settingsSerializedObject.ApplyModifiedProperties();
                    }
                    else
                    {
                        SerializedObject spriteAssetSerializedObject = new SerializedObject(defaultAssetProperty.objectReferenceValue);
                        SerializedProperty fallbackAssetsProperty = spriteAssetSerializedObject.FindProperty("fallbackSpriteAssets");
                        for (int i = 0; i < fallbackAssetsProperty.arraySize; i++)
                            if (fallbackAssetsProperty.GetArrayElementAtIndex(i).objectReferenceValue == TMP_AssetFile) yield break;
                        fallbackAssetsProperty.arraySize++;
                        fallbackAssetsProperty.GetArrayElementAtIndex(fallbackAssetsProperty.arraySize - 1).objectReferenceValue = TMP_AssetFile;
                        spriteAssetSerializedObject.ApplyModifiedProperties();
                    }
                }
            }
                    }
            finally
            {
                activePaths.Remove(filePath);
                if (!complete)
                {
                    if (!assetExisted) AssetDatabase.DeleteAsset(outputPath);
                    if (!atlasExisted) AssetDatabase.DeleteAsset(filePath);
                }
            }
        }

        private static readonly HashSet<string> activePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private static void Validate(IList<SpriteData> input, string path)
        {
            if (input == null || input.Count == 0) throw new ArgumentException("Provide at least one sprite.", nameof(input));
            if (string.IsNullOrWhiteSpace(path) || !path.StartsWith("Assets/", StringComparison.Ordinal) ||
                path.Contains("\\") || path.Split('/').Any(segment => segment == ".." || segment == ".") ||
                !path.EndsWith(".spriteatlasv2", StringComparison.OrdinalIgnoreCase) || !Directory.Exists(Path.GetDirectoryName(path)))
                throw new ArgumentException("Save the .spriteatlasv2 inside an existing Assets folder.", nameof(path));
            var names = new HashSet<string>(StringComparer.Ordinal);
            var sprites = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in input)
            {
                if (item == null || item.Sprite == null || string.IsNullOrWhiteSpace(item.Name))
                    throw new ArgumentException("Every sprite entry needs a sprite and non-empty name.", nameof(input));
                if (!names.Add(item.Name) || !sprites.Add(item.Sprite.name))
                    throw new ArgumentException("Sprite names and output character names must be unique.", nameof(input));
                if (!AssetDatabase.Contains(item.Sprite)) throw new ArgumentException("Sprites must be imported project assets.", nameof(input));
            }
            string output = Path.ChangeExtension(path, ".asset");
            var existing = AssetDatabase.LoadMainAssetAtPath(output);
            if (existing != null && !(existing is TMP_SpriteAsset)) throw new ArgumentException("The TMP output path contains another asset type.", nameof(path));
        }

        public class SpriteData
        {
            private Sprite sprite;
            private string name;
            private bool overrideDataSet;
            private GlyphMetrics glyphMetrics;
            private float scale;

            public Sprite Sprite => sprite;
            public string Name => name;

            public bool OverrideDataSet { get => overrideDataSet; set => overrideDataSet = value; }
            public GlyphMetrics GlyphMetrics { get => glyphMetrics; set => glyphMetrics = value; }
            public float Scale { get => scale; set => scale = value; }

            public SpriteData(Sprite sprite, string name)
            {
                this.sprite = sprite;
                this.name = name;
                overrideDataSet = false;
            }
        }
    }
}
