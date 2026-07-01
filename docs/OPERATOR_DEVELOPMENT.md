# 算子开发规范

当前阶段先使用内置算子注册，不启用 MEF 或 DLL 自动扫描。等算子数量、部署方式、版本隔离需求明确后，再把同一套执行器契约迁移到插件目录。

除早期验证用的 `占位检测` 外，新增真实算子必须配套标准编辑窗体。窗体类型先按算子独立实现，等通用窗体形态明确后再抽公共壳。

## 新增一个标准算子

1. 在 `Models\OperatorDefinition.cs` 增加算子名称常量。
2. 在 `OperatorRuntimeKind` 增加运行类型。
3. 在 `CreateKnownOperators()` 增加 `OperatorDefinition`。
   - 需要显示在左侧算子库时设置 `IsVisible = true`。
   - 真实算子必须设置对应 `OperatorEditorKind`。
4. 增加参数模型，放在 `Models`。
5. 在 `OperatorDefinition.CreateDefaultParameters()` 按 `OperatorRuntimeKind` 返回默认参数。
6. 在 `FlowOperator.CloneParameters()` 增加参数克隆，避免 UI 参数对象被后台线程直接读取。
7. 在 `Services\Vision` 增加执行器，并实现 `IVisionOperatorExecutor`。
   - 执行开始调用 `cancellationToken.ThrowIfCancellationRequested()`。
   - 成功返回 `VisionOperatorResult.Ok(...)`。
   - 失败返回 `VisionOperatorResult.Fail(...)`，不要抛给 UI。
   - 执行器只读取 `VisionOperatorWorkItem.Parameters`，不要直接访问 WPF 绑定对象。
8. 在 `Services\Vision\OperatorCatalog.cs` 的 `CreateFactory()` 映射运行类型到执行器。
9. 增加编辑窗体和编辑 ViewModel。
   - 在 `OperatorEditorKind` 增加编辑器类型。
   - 在 `MainWindow.OpenOperatorEditor(...)` 增加分发。
   - 窗体只负责配置参数、预览、运行一次和回写结果，不写流程调度逻辑。
10. 在 `MyFlowChart.csproj` 增加模型、执行器、ViewModel、XAML 和 code-behind。
11. 增加或更新 `tools\Verify*.ps1`，覆盖注册、编译、执行器和窗体关键行为。

可参考 `图像采集`：

- `ImageInputOperatorParameters`
- `ImageInputOperatorExecutor`
- `ImageInputOperatorEditorViewModel`
- `ImageInputOperatorEditorWindow`
- `tools\VerifyImageInputOperatorEditor.ps1`

## 图像传输规则

- 算子之间通过 `VisionRunContext.Items` 传递轻量数据。
- 图像使用 `ImageFrameToken` 或上下文中的共享引用，不复制 `Mat`。
- 需要图像的算子读取 `VisionDataKeys.CurrentImage`。
- 不需要图像的算子不得因为没有图像而 NG。

## 验证命令

```powershell
Get-ChildItem .\tools -Filter 'Verify*.ps1' | Sort-Object Name | ForEach-Object { powershell -NoProfile -ExecutionPolicy Bypass -File $_.FullName }
& "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe" D:\Aopencv\MyFlowChart\MyFlowChart.sln /t:Build /p:Configuration=Debug /p:Platform="Any CPU" /p:OutDir=D:\Aopencv\MyFlowChart\obj\CodexVerify\ /m
git diff --check
```
