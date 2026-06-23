# MyFlowChart Project Summary

Last reviewed: 2026-06-23

## Purpose

`MyFlowChart` is a .NET Framework 4.8 WPF desktop flowchart editor. The current workflow model is block-based: users add operator blocks to the canvas, then drag individual operators into a selected operator block from the palette.

## Key Files

- `MainWindow.xaml` / `MainWindow.xaml.cs`: main shell, toolbar, right-side property panel, start/stop buttons.
- `Controls/FlowChartControl.xaml` / `Controls/FlowChartControl.xaml.cs`: canvas rendering, block insertion, branch layout, mini-map, drag/drop, run simulation, context menus.
- `Controls/OperatorPaletteControl.xaml` / `.cs`: operator palette drag source.
- `ViewModels/MainWindowViewModel.cs`: selected-node state, right property panel visibility, Goto target list, branch add/remove commands.
- `Models/FlowNode.cs`: flow block model and factory methods.
- `Models/FlowBranch.cs`: branch lane model for Switch/Thread blocks.
- `Models/FlowOperator.cs`: operator item inside an operator block.
- `Models/FlowBlockKind.cs`: block types: `Start`, `OperatorBlock`, `Goto`, `Switch`, `Thread`, `End`.

## Current Flowchart Behavior

- The canvas uses a large fixed world size (`2600 x 1800`) so the user has usable space from initialization.
- Default `Start` and `End` blocks are always maintained and are fixed/non-deletable.
- Operators are no longer direct top-level flow nodes. A top-level operator block contains a list of operators.
- Dragging an operator from the palette onto an operator block appends it into that block.
- Hovering a connector shows a plus button; clicking it opens the insertion menu.
- The connector menu supports adding:
  - operator block
  - Goto block
  - Switch block
  - Thread block
  - pasted copied/cut block
- Goto blocks do not show a dashed link until `GotoTargetNodeId` is selected in the right property panel.
- Goto target options are recursively collected from top-level and branch-contained operator blocks.

## Switch And Thread Layout

Switch and Thread are modeled as one `FlowNode` with multiple `FlowBranch` lanes.

Visual rule:

- The main node on the original vertical flow line represents lane 1 (`Switch1` or `Thread1` behavior).
- Additional lanes are displayed as parallel blocks to the right (`Switch2`, `Thread2`, etc.).
- Every lane has its own vertical connector with plus-button insertion points.
- Lanes merge before the next main flow block.
- Adding a branch from the right property panel appends a new lane to the right.
- Removing a branch is allowed only while more than one branch exists.

Run rule:

- Thread/Switch branch node collections run via `Task.WhenAll`.
- Disabled nodes are skipped.
- Empty branch lanes still participate visually; branch-contained nodes can be inserted through lane connectors.

## Mini-map

- The mini-map is visible by default.
- The lower-right toggle button hides/shows the mini-map.
- The blue ROI rectangle can be dragged directly to pan the main viewport.
- Clicking outside the ROI inside the mini-map jumps the main view to that location.

## Runtime Display

- Blocks display elapsed runtime in milliseconds.
- Running blocks display an additional green corner outline.
- The context menu on a block includes:
  - run block
  - enable/disable block
  - copy block
  - cut block
  - delete block
- Start and End blocks are fixed, so destructive context-menu operations are disabled for them.

## Build And Verification

Use Visual Studio MSBuild:

```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe" D:\Aopencv\MyFlowChart\MyFlowChart.sln /t:Build /p:Configuration=Debug /p:Platform="Any CPU" /m
```

When the app is running, `bin\Debug\MyFlowChart.exe` may be locked. Use a temporary output directory for verification:

```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe" D:\Aopencv\MyFlowChart\MyFlowChart.sln /t:Build /p:Configuration=Debug /p:Platform="Any CPU" /p:OutDir=D:\Aopencv\MyFlowChart\obj\CodexVerify\ /m
```

Latest verification in this session:

- Temporary output build succeeded with `0 warnings, 0 errors`.
- `git diff --check` passed; Git may still warn that LF will be replaced by CRLF.

## Development Notes

- This is an older .NET Framework WPF project; prefer Visual Studio MSBuild over `dotnet build`.
- Keep comments and XML docs in Chinese to match project requirements.
- Do not remove existing user-visible behavior unless explicitly requested.
- The largest and riskiest file is `Controls/FlowChartControl.xaml.cs`; change it surgically and re-run MSBuild.
- The view model recursively subscribes to branch/node changes so Goto target lists refresh when nested branch nodes change.
- The control also recursively subscribes to model changes so branch edits, Goto binding, status, and elapsed time repaint the canvas.
