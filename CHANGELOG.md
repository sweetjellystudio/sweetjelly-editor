# CHANGELOG

## v1.0.0

* [Added] Added `SWeetJelly.Editor` as an independent Unity package extracted from the previous Core module.
* [Added] Added shared editor utility workflow for common Unity Editor tooling.
* [Added] Added editor custom style support through `EditorCustomStyles` and `EditorCustomStylesConfig`.
* [Added] Added editor style resources for icons, themes, GUI skins, and default style configuration.
* [Changed] Moved shared editor tooling from the unclassified Core module into a dedicated Editor package.
* [Changed] Changed editor style config loading to use a fixed project asset path instead of project-wide asset search.
* [Changed] Separated editor-only code into dedicated editor assemblies.
* [Fixed] Reduced ambiguous editor asset loading caused by searching the whole project for the first matching config asset.
