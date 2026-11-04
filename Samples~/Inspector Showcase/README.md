#   Inspector showcase

Import this sample from Package Manager, then use **Assets > Create > SWeetJelly > Editor > Inspector Showcase**. Select the asset to edit native UI Toolkit tabs, groups, conditions, sliders and buttons. Duplicate the asset and select both to exercise multi-object actions and Undo. Give the two assets different `advanced` values: conditional fields stay visible but disabled.

Changing `speed` invokes `OnSpeedChanged` only for assets whose value changed. Buttons record serialized object state for Undo; arbitrary file or external side effects in your own methods are your responsibility.

The sample assembly references only runtime attributes. It can be included in a Player without referencing the Editor implementation. TMP conversion and atlas generation remain Editor tools; use them on disposable assets when trying them out.
