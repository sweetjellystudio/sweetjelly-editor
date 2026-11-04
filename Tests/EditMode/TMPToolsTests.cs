using System;
using System.IO;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;
using UnityEngine.TestTools;

namespace SWeetJelly.Editor.Tests
{
    public sealed class TMPToolsTests
    {
        private const string Folder = "Assets/__SWeetJellyEditorTests";
        private UnityEngine.Object[] selection;
        private GameObject gameObject;
        private bool ownsFolder;
        private int undoGroup;
        [SetUp] public void SetUp()
        {
            selection = Selection.objects;
            ownsFolder = false;
            Undo.IncrementCurrentGroup();
            undoGroup = Undo.GetCurrentGroup();
            Assert.That(AssetDatabase.IsValidFolder(Folder), Is.False, "Do not overwrite an existing consumer folder.");
            AssetDatabase.CreateFolder("Assets", "__SWeetJellyEditorTests");
            ownsFolder = true;
            // Unity Test Runner already owns the active test scene, including an untitled scene.
        }
        [TearDown] public void TearDown()
        {
            Selection.objects = selection;
            Undo.RevertAllDownToGroup(undoGroup);
            if (gameObject) UnityEngine.Object.DestroyImmediate(gameObject);
            if (ownsFolder) AssetDatabase.DeleteAsset(Folder);
        }
        [TestCase(TextAnchor.UpperLeft, TextAlignmentOptions.TopLeft)]
        [TestCase(TextAnchor.UpperCenter, TextAlignmentOptions.Top)]
        [TestCase(TextAnchor.UpperRight, TextAlignmentOptions.TopRight)]
        [TestCase(TextAnchor.MiddleLeft, TextAlignmentOptions.MidlineLeft)]
        [TestCase(TextAnchor.MiddleCenter, TextAlignmentOptions.Midline)]
        [TestCase(TextAnchor.MiddleRight, TextAlignmentOptions.MidlineRight)]
        [TestCase(TextAnchor.LowerLeft, TextAlignmentOptions.BottomLeft)]
        [TestCase(TextAnchor.LowerCenter, TextAlignmentOptions.Bottom)]
        [TestCase(TextAnchor.LowerRight, TextAlignmentOptions.BottomRight)]
        public void TextConversionCopiesBeforeDestroyAndUndoes(TextAnchor alignment, TextAlignmentOptions expected)
        {
            gameObject = new GameObject("Legacy Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            var source = gameObject.GetComponent<Text>();
            source.text = "Keep this text";
            source.fontSize = 27;
            source.alignment = alignment;
            source.fontStyle = FontStyle.BoldAndItalic;
            source.resizeTextForBestFit = true;
            source.resizeTextMinSize = 8;
            source.resizeTextMaxSize = 30;
            source.color = Color.cyan;
            source.raycastTarget = false;
            source.rectTransform.sizeDelta = new Vector2(219, 57);
            Selection.activeGameObject = gameObject;
            typeof(TMPUtils).GetMethod("ReplaceSelectedObjects", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
            var converted = gameObject.GetComponent<TextMeshProUGUI>();
            Assert.That(gameObject.GetComponent<Text>(), Is.Null);
            Assert.That(converted.text, Is.EqualTo("Keep this text"));
            Assert.That(converted.fontSize, Is.EqualTo(27));
            Assert.That(converted.alignment, Is.EqualTo(expected));
            Assert.That(converted.fontStyle, Is.EqualTo(FontStyles.Bold | FontStyles.Italic));
            Assert.That(converted.color, Is.EqualTo(Color.cyan));
            Assert.That(converted.raycastTarget, Is.False);
            Assert.That(converted.rectTransform.sizeDelta, Is.EqualTo(new Vector2(219, 57)));
            Undo.FlushUndoRecordObjects();
            Undo.PerformUndo();
            Assert.That(gameObject.GetComponent<Text>(), Is.Not.Null);
            Assert.That(gameObject.GetComponent<TextMeshProUGUI>(), Is.Null);
            Undo.PerformRedo();
            Assert.That(gameObject.GetComponent<TextMeshProUGUI>().text, Is.EqualTo("Keep this text"));
        }
        [Test] public void InvalidSelectionIsRejectedWithoutAddingTMP()
        {
            gameObject = new GameObject("No Text");
            Selection.activeGameObject = gameObject;
            var error = Assert.Throws<TargetInvocationException>(() => typeof(TMPUtils).GetMethod("ReplaceSelectedObjects", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null));
            Assert.That(error.InnerException, Is.TypeOf<InvalidOperationException>());
            Assert.That(gameObject.GetComponent<TextMeshProUGUI>(), Is.Null);
        }
        [Test] public void EmptyAtlasInputDoesNotWriteAsset()
        {
            string path = Folder + "/empty.spriteatlasv2";
            Assert.Throws<ArgumentException>(() => new TMPAtlasGenerator(path).Save());
            Assert.That(File.Exists(path), Is.False);
        }
        [TestCase("../outside.spriteatlasv2")]
        [TestCase("Assets/../outside.spriteatlasv2")]
        [TestCase("Assets/invalid.asset")]
        public void InvalidAtlasPathDoesNotWrite(string path)
        {
            var generator = new TMPAtlasGenerator(path);
            generator.Add(new TMPAtlasGenerator.SpriteData(CreateSprite(), "valid"));
            Assert.Throws<ArgumentException>(generator.Save);
        }
        [Test] public void DuplicateCharacterNamesAreRejectedBeforeWrite()
        {
            var sprite = CreateSprite();
            var generator = new TMPAtlasGenerator(Folder + "/duplicate.spriteatlasv2");
            generator.Add(new TMPAtlasGenerator.SpriteData(sprite, "duplicate"));
            generator.Add(new TMPAtlasGenerator.SpriteData(sprite, "duplicate"));
            Assert.Throws<ArgumentException>(generator.Save);
            Assert.That(Directory.GetFiles(Folder, "*.spriteatlasv2"), Is.Empty);
        }
        [Test] public void UpdatingAtlasPreservesAssetAndMaterialIdentity()
        {
            var originalPacker = EditorSettings.spritePackerMode;
            try
            {
            EditorSettings.spritePackerMode = SpritePackerMode.SpriteAtlasV2;
            string path = Folder + "/icons.spriteatlasv2";
            var generator = new TMPAtlasGenerator(path);
            generator.Add(new TMPAtlasGenerator.SpriteData(CreateSprite(), "icon"));
            generator.Save();
            var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path);
            Assert.That(atlas.spriteCount, Is.GreaterThan(0));
            generator.CreateSpriteAsset(atlas);
            string output = Folder + "/icons.asset";
            string atlasGuid = AssetDatabase.AssetPathToGUID(path);
            string spriteGuid = AssetDatabase.AssetPathToGUID(output);
            var asset = AssetDatabase.LoadAssetAtPath<TMP_SpriteAsset>(output);
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset.material, out string materialGuid, out long materialId);
            generator.Save();
            generator.CreateSpriteAsset(AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path));
            var updated = AssetDatabase.LoadAssetAtPath<TMP_SpriteAsset>(output);
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(updated.material, out string updatedMaterialGuid, out long updatedMaterialId);
            Assert.That(AssetDatabase.AssetPathToGUID(path), Is.EqualTo(atlasGuid));
            Assert.That(AssetDatabase.AssetPathToGUID(output), Is.EqualTo(spriteGuid));
            Assert.That(updatedMaterialGuid, Is.EqualTo(materialGuid));
            Assert.That(updatedMaterialId, Is.EqualTo(materialId));
            Assert.That(updated.spriteCharacterTable.Count, Is.EqualTo(1));
            Assert.That(updated.spriteCharacterTable[0].name, Is.EqualTo("icon"));
            Assert.That(EditorUtility.IsPersistent(updated.spriteGlyphTable[0].sprite), Is.True, "Store source sprites, not transient atlas clones.");
            Assert.That(EditorUtility.IsPersistent(updated.spriteSheet), Is.True, "The material sheet must survive asset reload.");
            }
            finally { EditorSettings.spritePackerMode = originalPacker; }
        }
        private static Sprite CreateSprite()
        {
            var texture = new Texture2D(16, 16);
            var colors = new Color[256];
            for (int i = 0; i < colors.Length; i++) colors[i] = Color.cyan;
            texture.SetPixels(colors);
            texture.Apply();
            string path = Folder + "/icon.png";
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        [UnityTest] public IEnumerator FullAtlasFlowDeclinesGlobalSettingsAndKeepsCallerData()
        {
            var mode = EditorSettings.spritePackerMode;
            var confirm = TMPAtlasGenerator.Confirm;
            var item = new TMPAtlasGenerator.SpriteData(CreateSprite(), "full-flow");
            var input = new List<TMPAtlasGenerator.SpriteData> { item };
            var settings = TMP_Settings.instance;
            string settingsBefore = settings == null ? null : EditorJsonUtility.ToJson(settings);
            var generator = new TMPAtlasGenerator(Folder + "/flow.spriteatlasv2");
            var execution = generator.AtlasCoroutine(input);
            try
            {
                EditorSettings.spritePackerMode = SpritePackerMode.SpriteAtlasV2;
                TMPAtlasGenerator.Confirm = (title, message, accept, decline) => false;
                int frames = 0;
                while (execution.MoveNext())
                {
                    Assert.That(++frames, Is.LessThan(100));
                    yield return null;
                }
                var asset = AssetDatabase.LoadAssetAtPath<TMP_SpriteAsset>(Folder + "/flow.asset");
                Assert.That(asset, Is.Not.Null);
                Assert.That(asset.spriteCharacterTable[0].name, Is.EqualTo("full-flow"));
                Assert.That(input.Count, Is.EqualTo(1));
                Assert.That(item.OverrideDataSet, Is.False);
                Assert.That(settings == null ? null : EditorJsonUtility.ToJson(settings), Is.EqualTo(settingsBefore));
            }
            finally
            {
                (execution as IDisposable)?.Dispose();
                TMPAtlasGenerator.Confirm = confirm;
                EditorSettings.spritePackerMode = mode;
            }
        }

        [Test] public void StoppingAtlasCoroutineDeletesOnlyNewOutputs()
        {
            var mode = EditorSettings.spritePackerMode;
            string path = Folder + "/cancel.spriteatlasv2";
            var generator = new TMPAtlasGenerator(path);
            var execution = generator.AtlasCoroutine(new List<TMPAtlasGenerator.SpriteData> { new TMPAtlasGenerator.SpriteData(CreateSprite(), "cancel") });
            try
            {
                EditorSettings.spritePackerMode = SpritePackerMode.SpriteAtlasV2;
                Assert.That(execution.MoveNext(), Is.True);
                Assert.That(File.Exists(path), Is.True);
                (execution as IDisposable).Dispose();
                Assert.That(File.Exists(path), Is.False);
                Assert.That(File.Exists(Folder + "/cancel.asset"), Is.False);
            }
            finally { (execution as IDisposable)?.Dispose(); EditorSettings.spritePackerMode = mode; }
        }

        [Test] public void DisabledPackerIsRejectedWithoutChangingProjectSettings()
        {
            var mode = EditorSettings.spritePackerMode;
            string path = Folder + "/disabled.spriteatlasv2";
            var generator = new TMPAtlasGenerator(path);
            generator.Add(new TMPAtlasGenerator.SpriteData(CreateSprite(), "disabled"));
            try
            {
                EditorSettings.spritePackerMode = SpritePackerMode.Disabled;
                Assert.Throws<InvalidOperationException>(generator.Save);
                Assert.That(EditorSettings.spritePackerMode, Is.EqualTo(SpritePackerMode.Disabled));
                Assert.That(File.Exists(path), Is.False);
            }
            finally { EditorSettings.spritePackerMode = mode; }
        }
    }
}
