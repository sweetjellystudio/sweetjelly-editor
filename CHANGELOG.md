# CHANGELOG

## v2.0.0

* [Changed] Require Unity 6.3 LTS, Utils 2.0.0 and uGUI 2.0.0; retain TMP tooling and the root MIT license.
* [Changed] Render generic fallback Inspectors with native UI Toolkit while retaining SweetJellyEditor/GUIRenderer IMGUI extension compatibility.
* [Fixed] Resolve inherited/nested/list metadata, guard invalid conditions, and handle multi-selection actions, actual-change callbacks and Undo/prefab recording.
* [Changed] Load bundled Editor-only styles by default, preserving existing project overrides and resource GUIDs.
* [Fixed] Snapshot legacy Text before destruction and group selection-wide TMP conversion Undo/Redo.
* [Fixed] Validate atlas inputs, bound import waits, preserve existing output/material identities and clean up only new failed outputs; require Sprite Atlas V2 and TMP Essential Resources explicitly.
* [Fixed] Dispose nested Editor coroutines on terminal paths and preserve independent work after failures or list mutation.
* [Added] Package maintenance/architecture/migration documents, EditMode regression tests and an Inspector Showcase sample.

## v1.0.0

* [Added] Added `SWeetJelly.Editor` as an independent Unity package extracted from the previous Core module.
* [Added] Added shared editor utility workflow for common Unity Editor tooling.
* [Added] Added editor custom style support through `EditorCustomStyles` and `EditorCustomStylesConfig`.
* [Added] Added editor style resources for icons, themes, GUI skins, and default style configuration.
* [Changed] Moved shared editor tooling from the unclassified Core module into a dedicated Editor package.
* [Changed] Changed editor style config loading to use a fixed project asset path instead of project-wide asset search.
* [Changed] Separated editor-only code into dedicated editor assemblies.
* [Fixed] Reduced ambiguous editor asset loading caused by searching the whole project for the first matching config asset.
