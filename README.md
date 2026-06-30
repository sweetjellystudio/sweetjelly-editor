# SWeetJelly Editor

`SWeetJelly Editor` is a Unity editor utility package for SWeetJelly projects. It provides inspector attributes, custom drawer infrastructure, shared editor GUI helpers, editor style resources, and small Unity Editor convenience tools.

## Features

- Inspector attributes for grouping, conditions, labels, buttons, tabs, and read-only states
- Property drawers for sliders, toggles, enum flags, references, previews, and min/max values
- Metadata drawers such as info boxes and value-changed callbacks
- `SweetJellyEditor` base inspector workflow for MonoBehaviour and ScriptableObject inspectors
- Shared editor GUI/style helpers and custom editor skin resources
- TextMeshPro helper utilities and atlas generation helpers

## Assemblies

```text
Runtime/Attributes
  SWeetJelly.Editor.Attributes

Editor
  SWeetJelly.Editor
```

Use `SWeetJelly.Editor.Attributes` from runtime code when you need attributes on serialized fields. Editor-only helpers live in `SWeetJelly.Editor` and should only be referenced by editor assemblies.

## Basic Usage

```csharp
using SWeetJelly.Editor.Attributes;
using UnityEngine;

public sealed class SampleSettings : MonoBehaviour
{
    [BoxGroup("Timing")]
    [Slider(0f, 10f)]
    [SerializeField] private float duration = 1f;

    [ShowIf(nameof(useAdvanced))]
    [SerializeField] private int advancedValue;

    [SerializeField] private bool useAdvanced;

    [Button("Reset Values")]
    private void ResetValues()
    {
        duration = 1f;
        advancedValue = 0;
    }
}
```

## Common Attributes

- `Button`, `InlineButton`
- `ShowIf`, `HideIf`, `Hide`
- `EnableIf`, `DisableIf`, `ReadOnly`
- `Group`, `BoxGroup`, `Foldout`, `BoxFoldout`, `HorizontalGroup`
- `Tab`, `Label`, `LabelWidth`, `Indent`, `Order`
- `InfoBox`, `OnValueChanged`
- `Slider`, `MinMaxSlider`, `Toggle`, `OnOff`, `EnumFlags`
- `DrawReference`, `ShowAssetPreview`, `ShowNonSerializedField`

## Notes

This package is editor-focused. Runtime gameplay code should depend only on `SWeetJelly.Editor.Attributes` when serialized-field annotations are needed, and should avoid referencing editor-only APIs.