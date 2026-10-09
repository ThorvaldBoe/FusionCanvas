# AvaloniaVirtualDataGrid trial dependency

This directory holds a local binary snapshot for evaluating the grid in FusionCanvas. Only `FusionCanvas.App` and its integration tests reference it; Domain, Application, and Integration remain independent of the control.

- Upstream: https://github.com/tkleisas/AvaloniaVirtualDataGrid
- Source checkout commit: `2a6d6a818f120a05b56f9a3f019997175b625d7a` (clean checkout when inspected).
- Source artifact: `src/AvaloniaVirtualDataGrid/bin/Release/net10.0/AvaloniaVirtualDataGrid.dll` in the sibling checkout.
- Assembly version: `1.0.0.0`.
- Target framework: .NET 10.
- Dependencies: Avalonia and Avalonia.Themes.Fluent 12.0.5, already provided by FusionCanvas.
- SHA-256: `05FFFA9A001CF6FB9F11E594734EC4F0C2288ED1DBF51142EEEC63F216FC7CAB`.
- Upstream README states MIT licensing; the inspected checkout has no separate LICENSE file.

The checksum identifies the copied artifact. A clean source checkout does not establish that its existing build output was produced from that exact commit.

The DLL contains the compiled Avalonia resources. Do not copy the demo, its SQLite dependency, the framework DLLs, or a separate loose style file into this directory.

Load its styles after `FluentTheme`, under `Application.Styles`:

```xml
<StyleInclude Source="avares://AvaloniaVirtualDataGrid/Themes/Generic.axaml" />
```

`Generic.axaml` has a `Styles` root, so this uses `StyleInclude`, rather than a merged resource dictionary. `Private=true` on the assembly reference copies the DLL into build/publish output. The reference is relative to the repository; building FusionCanvas does not require the sibling checkout.

See [the trial analysis](../../docs/experiments/virtual-data-grid.md) for the implemented bulk-variant preview, verification, and observed integration constraints.
