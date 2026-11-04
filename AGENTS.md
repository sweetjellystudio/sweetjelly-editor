#   Editor package instructions

Use the installed `unity-package` coordinator for package implementation and validation. Read [architecture][architecture] before changing Inspector extension points, attributes, dependencies, asset generation or lifecycle; read the related [decision][decision] when changing UI technology or compatibility. Read [migration][migration] for caller-visible behavior changes.

Keep Runtime/Attributes usable in Players. The Editor implementation and bundled styles belong under Editor. Preserve public/protected extensions, serialized configuration fields and existing meta GUIDs; moving assets requires moving their meta files. The packaged style config and the optional imported project override use different GUIDs and are distinct assets.

Default Inspectors use native UI Toolkit. `SweetJellyEditor` inheritance and `GUIRenderer.OnGUI` remain IMGUI extension points. Preserve consumer-specific custom Inspector priority. Exercise multi-selection, conditions, callback failures, Undo/Redo and prefab overrides when changing editing behavior.

Correctness tests belong in Tests/EditMode. Run them in both a development host and a minimal consumer with only declared dependencies. TMP atlas tests need TMP Essential Resources and temporarily enable Sprite Atlas V2, restoring the previous mode. Verify actual Inspector/style/TMP/atlas behavior and ordinary Windows Mono/IL2CPP consumption before claiming release readiness. Separate local source validation from public Git tag installation. The host owns publication policy and temporary validation artifacts.

[architecture]: ARCHITECTURE.md
[decision]: docs/adr/0001-native-inspectors-and-imgui-compatibility.md
[migration]: Documentation~/migration-2.0.md
