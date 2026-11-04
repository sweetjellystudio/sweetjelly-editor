---
status: accepted
---

#   Native default Inspectors with IMGUI extension compatibility

Approved on 2026-11-04 for 2.0.0. Unity 6.3 is the minimum supported version. uGUI 2.0.0 supplies TMP; the obsolete standalone TMP dependency is replaced while the Unity.TextMeshPro assembly reference and TMP asset tools remain. Utils moves to 2.0.0.

The generic MonoBehaviour/ScriptableObject fallback Inspectors use native UI Toolkit fields, grouping, tabs, conditions and actions. Existing `SweetJellyEditor` subclasses and `GUIRenderer` extensions retain IMGUI behavior. We rejected wrapping the entire old Inspector as a claimed migration, and rejected removing IMGUI extensions because unknown downstream subclasses may still use them. Individual compatibility leaves preserve legacy control behavior where needed.

The package owns default Editor resources, while existing imported style configs remain consumer overrides with their own identities. Moving bundled resources under Editor preserves GUIDs and prevents those defaults from becoming Player resources. The package cannot automatically relocate existing consumer Assets/Resources without changing consumer content, so that cleanup is a separate consumer decision.
