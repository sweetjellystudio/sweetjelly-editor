#   Editor architecture

This package supplies Inspector annotations, Editor rendering helpers and asset tools. DDD responsibility and language boundaries and Clean Architecture dependency direction guide the design. Editor UI and asset APIs are the actual product here; an artificial Domain/App hierarchy would not protect independent rules.

##  Assembly responsibilities

`SWeetJelly.Editor.Attributes` owns passive annotations and their values. It references UnityEngine where an annotation derives from PropertyAttribute, and never UnityEditor or the Editor implementation. Its name does not make it an Editor-only assembly. Consumers may use these annotations in ordinary Players.

`SWeetJelly.Editor` is restricted to the Editor platform. It references Attributes, Unity.TextMeshPro and Utils. uGUI supplies TMP in Unity 6.3. Utils supplies collection helpers. Dependencies are declared in package.json; a consumer using Git packages must also provide a Git URL for Utils in its own manifest.

`InspectorReflection` resolves inherited fields, nested owners, list/array paths and valid method overloads, and owns multi-object action invocation with Undo/prefab recording. `NativeInspector` composes UI Toolkit fields, conditions, tabs, groups and actions. `NativePropertyControls` adapts supported property types. Exact legacy IMGUI sizing and asset preview behavior can use individual compatibility leaves; the default Inspector is not a wrapper around the old Inspector.

##  Consumption and extension

Fallback custom Editors for MonoBehaviour and ScriptableObject provide the native default. A consumer's specific custom Editor takes priority. Deriving from `SweetJellyEditor` retains the IMGUI lifecycle, `OnInspectorGUI`, protected script visibility and existing public helper methods. `GUIRenderer` and drawer contracts remain available. Custom group/condition/meta annotations select the IMGUI compatibility path; ordinary external PropertyDrawers remain Unity's responsibility.

Multi-selection evaluates conditions for every selected target. A disagreement leaves an item visible but disabled. Actions validate all target methods before invoking each once, stopping on the first exception. OnValueChanged runs after applying edits from that Inspector and only for targets with an actual value change. Model-to-view binding echoes and Undo/Redo do not invoke it again; callback state shares the edit's Undo group. Undo records serialized object state; arbitrary filesystem/network/static side effects in consumer methods are not transactional. Call Editor utilities on the Editor main thread.

Invalid reflection declarations disable the affected feature and produce a diagnostic without hiding unrelated fields. Condition errors block editing. User exceptions retain their original cause. Diagnostics are deduplicated for the lifetime of an Inspector rendering instance.

##  Resources and asset ownership

Bundled configs, GUISkins, icons and the optional import archive live under Editor/Resources. The existing project override at Assets/sweetjelly-editor/Resources takes priority over the bundled default. Config serialized fields and asset GUIDs are preserved. Imported override assets are consumer-owned; this package does not silently rewrite or relocate them.

TMP text conversion snapshots source values before component destruction and records the entire selection in one Undo group, including prefab modifications. Atlas generation validates inputs before writes, uses a finite import wait and preserves existing atlas/TMP asset/material identities during successful updates. Sprite Atlas V2 and imported TMP Essential Resources are explicit prerequisites, not settings changed by the generator. A TMP sprite asset supports one texture page; a multi-page result is rejected. TMP Settings linking and reuse of existing glyph metrics/scale remain explicit choices. Failure cleanup deletes only outputs newly created by that run; existing outputs are never deleted as rollback.

`EditorCoroutines` owns its update subscription and iterator stack. Completion, StopAll, failure, reload and Editor shutdown dispose active nested iterators. Work added during an update starts on the next update. Failures terminate one execution, report its first error, and leave independent work for the next update. OnUpdate(true) means normal completion only.

##  Verification ownership

Package EditMode tests protect editing and lifecycle behavior. Samples~/Inspector Showcase is a thin consumer of runtime annotations. The development host owns cross-package checks; a separate consumer checks the declared dependency graph and Player exclusion of Editor implementation/resources. See the [migration guide][migration] and [UI compatibility decision][decision].

[migration]: Documentation~/migration-2.0.md
[decision]: docs/adr/0001-native-inspectors-and-imgui-compatibility.md
