# AGENTS.md

## Project

`MyFlowChart` is a .NET Framework 4.8 WPF flowchart editor under `D:\Aopencv\MyFlowChart`.

## Coding Rules

- Keep WPF changes MVVM-friendly: UI state belongs in `ViewModels/MainWindowViewModel.cs` unless it is strictly rendering/control behavior inside `FlowChartControl`.
- All code comments and XML documentation should be in Chinese.
- Keep edits surgical. Do not remove existing user-visible behavior unless the user explicitly asks to remove it.
- Prefer existing model types: `FlowNode`, `FlowBranch`, `FlowOperator`, `FlowBlockKind`, and `RelayCommand`.
- `Start` and `End` blocks are fixed and must remain non-deletable.

## Important Files

- `Controls/FlowChartControl.xaml.cs`: canvas rendering, connector menu, branch layout, mini-map, node context menu, run simulation.
- `ViewModels/MainWindowViewModel.cs`: selected node, right property panel visibility, Goto targets, branch add/remove.
- `Models/FlowNode.cs`: flow block factories and block capabilities.
- `docs/PROJECT_SUMMARY.md`: current architecture and handoff notes.

## Build

Use Visual Studio MSBuild:

```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe" D:\Aopencv\MyFlowChart\MyFlowChart.sln /t:Build /p:Configuration=Debug /p:Platform="Any CPU" /m
```

If `bin\Debug\MyFlowChart.exe` is locked by a running app, verify with:

```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe" D:\Aopencv\MyFlowChart\MyFlowChart.sln /t:Build /p:Configuration=Debug /p:Platform="Any CPU" /p:OutDir=D:\Aopencv\MyFlowChart\obj\CodexVerify\ /m
```
