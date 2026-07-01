# MyFlowChart

.NET Framework 4.8 WPF flowchart editor for configuring operator-block based workflows.

## Build

Use Visual Studio MSBuild for this WPF project:

```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe" D:\Aopencv\MyFlowChart\MyFlowChart.sln /t:Build /p:Configuration=Debug /p:Platform="Any CPU" /m
```

If `bin\Debug\MyFlowChart.exe` is locked by a running app instance, verify with a temporary output directory:

```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe" D:\Aopencv\MyFlowChart\MyFlowChart.sln /t:Build /p:Configuration=Debug /p:Platform="Any CPU" /p:OutDir=D:\Aopencv\MyFlowChart\obj\CodexVerify\ /m
```

## Vision Integration Verification

```powershell
.\tools\VerifyVisionIntegration.ps1
```

This verifies copied line-detection core hashes, builds the solution, and runs a startup smoke check.

## Project Notes

See [docs/PROJECT_SUMMARY.md](docs/PROJECT_SUMMARY.md) for the current architecture, implemented flowchart behaviors, and handoff notes.
