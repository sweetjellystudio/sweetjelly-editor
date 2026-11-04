#   Migrating to Editor 2.0

Requires Unity 6.3 LTS (6000.3) or later, Utils 2.0.0 and uGUI 2.0.0. The separate com.unity.textmeshpro dependency is replaced by uGUI's TMP integration. Existing TMP tools and annotations remain available. Root LICENSE.md remains MIT.

##  Git installation

Add both custom packages to the consumer project's Packages/manifest.json. Once the 2.0.0 tag is published:

```json
{
  "dependencies": {
    "com.sweetjelly.editor": "https://github.com/sweetjellystudio/sweetjelly-editor.git#v2.0.0",
    "com.sweetjelly.utils": "https://github.com/sweetjellystudio/sweetjelly-utils.git#v2.0.0"
  }
}
```

UPM package dependencies declare versions, not Git URLs. Local source checks do not prove the public tag installs; verify the published tag separately after publication.

##  Inspector behavior

The generic Inspector now uses UI Toolkit. Existing consumer-specific custom Inspectors take priority. Existing `SweetJellyEditor` subclasses and `GUIRenderer` IMGUI extensions remain supported, including a compatibility route for custom group/condition/meta annotations. Annotation types and GUIDs remain unchanged.

Conditions are checked across all selected objects. Conflicting results display a disabled item. Buttons and InlineButtons run once per allowed target. OnValueChanged runs after applying edits from that Inspector, only for objects whose value changed. Other Inspector bindings and Undo/Redo do not invoke it again, and the callback joins the edit's Undo group. Actions stop on the first exception and report its cause. Undo covers recorded serialized object state; custom method side effects outside that state require consumer handling. Invalid conditions block editing rather than silently enabling it.

##  Styles

Import is no longer required for bundled defaults. Existing Assets/sweetjelly-editor/Resources/EditorCustomStylesConfig.asset takes priority. The explicit import menu remains available. Bundled resources move to Editor/Resources with meta GUIDs preserved. Existing imported project resources are not moved or deleted; review those consumer-owned Resources separately if excluding all legacy Editor payload from a game Player.

##  TMP tools

Text conversion copies supported settings before destroying the old component and supports selection-wide Undo/Redo. Select scene objects with legacy Text and no existing TMP component; persistent asset selections are rejected.

Before atlas generation, enable Sprite Atlas V2 in Project Settings > Editor > Sprite Packer and import TMP Essential Resources through the TMP menu. Inputs need imported sprites, unique sprite/character names and an existing Assets folder with a .spriteatlasv2 output path. Empty/invalid inputs fail before output creation. The import wait is limited to 30 seconds. A multi-page atlas is rejected because one TMP sprite asset uses one texture sheet.

Successful updates preserve atlas/TMP asset GUIDs and existing owned material subasset identities. Shared external materials remain untouched; the output receives an owned material when needed. Metrics/scale reuse and TMP Settings default/fallback linking are explicit choices. Failed runs remove only newly created outputs; existing outputs are preserved rather than deleted. Review existing outputs after an interrupted overwrite, as generation does not promise an atomic transaction across Unity's importer.

##  Editor coroutines

Execute and StopAll signatures remain unchanged. Nested iterators are disposed on completion, failure, cancellation by StopAll, assembly reload and Editor shutdown. Work added during update begins next update. OnUpdate(true) is normal completion only; failure/StopAll never reports success. The first execution/cleanup error is logged once, the failed execution ends, and independent work resumes next update.
