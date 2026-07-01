# Vision Workflow Direct OpenCV Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the first stable vision-workflow loop in `D:\Aopencv\MyFlowChart`: integrated `OpenCvWindowTool`, modern Industrial Fluent WPF shell, OpenCV viewer, background runner, and original line-detection operator execution.

**Architecture:** Keep `OpenCvWindowTool` as source inside this solution and do not alter its line-detection core files. `MyFlowChart` owns WPF UI, MVVM state, workflow execution, image-context lifetime, and operator registration. UI stays separate from algorithm execution; data flows through `VisionRunContext` references rather than per-node image clones.

**Tech Stack:** .NET Framework 4.8 WPF, WindowsFormsHost, iNKORE.UI.WPF.Modern 0.10.2.1, OpenCvSharp4 4.11.0.20250507, `OpenCvWindowTool`, Visual Studio MSBuild.

---

## File Structure

- Create: `D:\Aopencv\MyFlowChart\OpenCvWindowTool\` copied from `D:\Aopencv\OpencvMaster\OpenCvWindowTool\`, excluding `bin`, `obj`, and `.user` files.
- Modify: `D:\Aopencv\MyFlowChart\MyFlowChart.sln` to include `OpenCvWindowTool`.
- Modify: `D:\Aopencv\MyFlowChart\MyFlowChart.csproj` to reference `OpenCvWindowTool`, `WindowsFormsIntegration`, WinForms assemblies, OpenCvSharp packages, and `iNKORE.UI.WPF.Modern`.
- Modify: `D:\Aopencv\MyFlowChart\App.xaml` to merge iNKORE resources and Industrial Fluent theme dictionaries.
- Create: `D:\Aopencv\MyFlowChart\Resources\Themes\IndustrialFluent.Tokens.xaml`.
- Create: `D:\Aopencv\MyFlowChart\Resources\Themes\IndustrialFluent.Light.xaml`.
- Create: `D:\Aopencv\MyFlowChart\Resources\Themes\IndustrialFluent.Dark.xaml`.
- Modify: `D:\Aopencv\MyFlowChart\MainWindow.xaml` to use fixed non-overlapping panes and host `OpenCvImageViewer`.
- Modify: `D:\Aopencv\MyFlowChart\MainWindow.xaml.cs` only for viewer bridge, file dialogs, dispatcher handoff, and disposal.
- Modify: `D:\Aopencv\MyFlowChart\ViewModels\MainWindowViewModel.cs` for run commands, selected operator state, visual parameters, and result text.
- Create: `D:\Aopencv\MyFlowChart\Services\Vision\VisionRunContext.cs`.
- Create: `D:\Aopencv\MyFlowChart\Services\Vision\VisionOperatorResult.cs`.
- Create: `D:\Aopencv\MyFlowChart\Services\Vision\IVisionOperatorExecutor.cs`.
- Create: `D:\Aopencv\MyFlowChart\Services\Vision\OperatorCatalog.cs`.
- Create: `D:\Aopencv\MyFlowChart\Services\Vision\VisionWorkflowRunner.cs`.
- Create: `D:\Aopencv\MyFlowChart\Services\Vision\ImageInputOperatorExecutor.cs`.
- Create: `D:\Aopencv\MyFlowChart\Services\Vision\LineFindOperatorExecutor.cs`.
- Create: `D:\Aopencv\MyFlowChart\Services\Vision\ResultOutputOperatorExecutor.cs`.
- Create: `D:\Aopencv\MyFlowChart\tools\VerifyVisionIntegration.ps1`.

Do not modify these copied files except by explicit user request:

- `D:\Aopencv\MyFlowChart\OpenCvWindowTool\LineDetectionOperator.cs`
- `D:\Aopencv\MyFlowChart\OpenCvWindowTool\OptLineDetectionOperator.cs`
- `D:\Aopencv\MyFlowChart\OpenCvWindowTool\LineDetectionModels.cs`
- `D:\Aopencv\MyFlowChart\OpenCvWindowTool\LineDetectionImageContext.cs`
- `D:\Aopencv\MyFlowChart\OpenCvWindowTool\RoiItem.cs`

---

### Task 1: Import OpenCvWindowTool Without Changing Detection Logic

**Files:**
- Create: `D:\Aopencv\MyFlowChart\OpenCvWindowTool\**`
- Modify: `D:\Aopencv\MyFlowChart\MyFlowChart.sln`
- Modify: `D:\Aopencv\MyFlowChart\MyFlowChart.csproj`

- [ ] **Step 1: Verify source hashes before copy**

Run:

```powershell
Get-FileHash `
  D:\Aopencv\OpencvMaster\OpenCvWindowTool\LineDetectionOperator.cs,`
  D:\Aopencv\OpencvMaster\OpenCvWindowTool\OptLineDetectionOperator.cs,`
  D:\Aopencv\OpencvMaster\OpenCvWindowTool\LineDetectionModels.cs,`
  D:\Aopencv\OpencvMaster\OpenCvWindowTool\LineDetectionImageContext.cs,`
  D:\Aopencv\OpencvMaster\OpenCvWindowTool\RoiItem.cs `
  -Algorithm SHA256 | Format-Table -AutoSize
```

Expected hashes:

```text
8E5448262EA475B9F74B728F17FD51F922F0004EDDF0A258B4C5F0698B221C4F  LineDetectionOperator.cs
BD513931A316B39A2EBF0F5A7808BE6603931DD9ED0721335E496264E71B4981  OptLineDetectionOperator.cs
C8A1C431FA533C8E981ACE37596146CF618E3221C92BFFCF99B56FEDAE95C0FD  LineDetectionModels.cs
8D182DBF636938B01F30C119679E98641956A4E15711ED8E6289941007EAB4DB  LineDetectionImageContext.cs
A211A0A7D79A09444F8B961B94B1BE434862FF4CCDCEFB9B32455A12189E30F0  RoiItem.cs
```

- [ ] **Step 2: Copy source into current workspace**

Run:

```powershell
$source = 'D:\Aopencv\OpencvMaster\OpenCvWindowTool'
$target = 'D:\Aopencv\MyFlowChart\OpenCvWindowTool'
if (Test-Path $target) { throw "OpenCvWindowTool already exists: $target" }
robocopy $source $target /E /XD bin obj /XF *.user
if ($LASTEXITCODE -ge 8) { exit $LASTEXITCODE }
```

Expected: `OpenCvWindowTool` exists under `D:\Aopencv\MyFlowChart`; `bin`, `obj`, and `.user` files are absent.

- [ ] **Step 3: Add project to solution**

Run:

```powershell
dotnet sln D:\Aopencv\MyFlowChart\MyFlowChart.sln add D:\Aopencv\MyFlowChart\OpenCvWindowTool\OpenCvWindowTool.csproj
```

Expected: solution includes `OpenCvWindowTool\OpenCvWindowTool.csproj`.

- [ ] **Step 4: Add references to MyFlowChart.csproj**

In `D:\Aopencv\MyFlowChart\MyFlowChart.csproj`, add these references if absent:

```xml
<Reference Include="System.Drawing" />
<Reference Include="System.Windows.Forms" />
<Reference Include="WindowsFormsIntegration" />
```

Add package references:

```xml
<PackageReference Include="OpenCvSharp4" Version="4.11.0.20250507" />
<PackageReference Include="OpenCvSharp4.runtime.win" Version="4.11.0.20250507" />
<PackageReference Include="iNKORE.UI.WPF.Modern" Version="0.10.2.1" />
```

Add project reference:

```xml
<ProjectReference Include="OpenCvWindowTool\OpenCvWindowTool.csproj" />
```

- [ ] **Step 5: Build**

Run:

```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe" D:\Aopencv\MyFlowChart\MyFlowChart.sln /t:Build /p:Configuration=Debug /p:Platform="Any CPU" /p:OutDir=D:\Aopencv\MyFlowChart\obj\CodexVerify\ /m
```

Expected: build succeeds. If iNKORE or OpenCvSharp assemblies are missing, restore packages and rebuild.

- [ ] **Step 6: Commit**

```powershell
git add MyFlowChart.sln MyFlowChart.csproj OpenCvWindowTool
git commit -m "feat: integrate opencv window tool source"
```

---

### Task 2: Add Industrial Fluent Theme Resources

**Files:**
- Modify: `D:\Aopencv\MyFlowChart\App.xaml`
- Create: `D:\Aopencv\MyFlowChart\Resources\Themes\IndustrialFluent.Tokens.xaml`
- Create: `D:\Aopencv\MyFlowChart\Resources\Themes\IndustrialFluent.Light.xaml`
- Create: `D:\Aopencv\MyFlowChart\Resources\Themes\IndustrialFluent.Dark.xaml`
- Modify: `D:\Aopencv\MyFlowChart\MyFlowChart.csproj`

- [ ] **Step 1: Add theme files to project**

In `MyFlowChart.csproj`, include:

```xml
<Page Include="Resources\Themes\IndustrialFluent.Tokens.xaml">
  <Generator>MSBuild:Compile</Generator>
  <SubType>Designer</SubType>
</Page>
<Page Include="Resources\Themes\IndustrialFluent.Light.xaml">
  <Generator>MSBuild:Compile</Generator>
  <SubType>Designer</SubType>
</Page>
<Page Include="Resources\Themes\IndustrialFluent.Dark.xaml">
  <Generator>MSBuild:Compile</Generator>
  <SubType>Designer</SubType>
</Page>
```

- [ ] **Step 2: Create tokens dictionary**

Create `Resources\Themes\IndustrialFluent.Tokens.xaml`:

```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    xmlns:sys="clr-namespace:System;assembly=mscorlib">
    <!-- 工业视觉界面的紧凑间距。 -->
    <Thickness x:Key="AppShellPadding">10</Thickness>
    <Thickness x:Key="PanelPadding">12</Thickness>
    <Thickness x:Key="FieldRowMargin">0,0,0,8</Thickness>
    <sys:Double x:Key="ToolbarHeight">44</sys:Double>
    <sys:Double x:Key="PropertyRowHeight">34</sys:Double>
    <sys:Double x:Key="OperatorItemHeight">40</sys:Double>
    <CornerRadius x:Key="PanelCornerRadius">6</CornerRadius>
    <CornerRadius x:Key="ControlCornerRadius">4</CornerRadius>
    <sys:Double x:Key="TitleFontSize">18</sys:Double>
    <sys:Double x:Key="SectionFontSize">14</sys:Double>
    <sys:Double x:Key="BodyFontSize">12</sys:Double>
    <sys:Double x:Key="StatusFontSize">11</sys:Double>
</ResourceDictionary>
```

- [ ] **Step 3: Create light theme dictionary**

Create `Resources\Themes\IndustrialFluent.Light.xaml`:

```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <!-- 浅色主题颜色令牌。 -->
    <Color x:Key="AppBackgroundColor">#F4F7FA</Color>
    <Color x:Key="PanelBackgroundColor">#FFFFFF</Color>
    <Color x:Key="PanelAltBackgroundColor">#EEF2F6</Color>
    <Color x:Key="BorderColor">#D8E0E8</Color>
    <Color x:Key="PrimaryColor">#2563EB</Color>
    <Color x:Key="AccentColor">#0F9F8C</Color>
    <Color x:Key="SuccessColor">#16A34A</Color>
    <Color x:Key="WarningColor">#D97706</Color>
    <Color x:Key="ErrorColor">#DC2626</Color>
    <Color x:Key="RunningColor">#2DD4BF</Color>
    <Color x:Key="DisabledColor">#94A3B8</Color>
    <Color x:Key="RoiColor">#00B7FF</Color>
    <Color x:Key="DetectionLineColor">#22C55E</Color>
    <SolidColorBrush x:Key="AppBackgroundBrush" Color="{StaticResource AppBackgroundColor}" />
    <SolidColorBrush x:Key="PanelBackgroundBrush" Color="{StaticResource PanelBackgroundColor}" />
    <SolidColorBrush x:Key="PanelAltBackgroundBrush" Color="{StaticResource PanelAltBackgroundColor}" />
    <SolidColorBrush x:Key="BorderBrush" Color="{StaticResource BorderColor}" />
    <SolidColorBrush x:Key="PrimaryBrush" Color="{StaticResource PrimaryColor}" />
    <SolidColorBrush x:Key="AccentBrush" Color="{StaticResource AccentColor}" />
    <SolidColorBrush x:Key="SuccessBrush" Color="{StaticResource SuccessColor}" />
    <SolidColorBrush x:Key="WarningBrush" Color="{StaticResource WarningColor}" />
    <SolidColorBrush x:Key="ErrorBrush" Color="{StaticResource ErrorColor}" />
    <SolidColorBrush x:Key="RunningBrush" Color="{StaticResource RunningColor}" />
</ResourceDictionary>
```

- [ ] **Step 4: Create dark theme dictionary**

Create `Resources\Themes\IndustrialFluent.Dark.xaml`:

```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <!-- 深色主题颜色令牌。 -->
    <Color x:Key="DarkAppBackgroundColor">#12161C</Color>
    <Color x:Key="DarkPanelBackgroundColor">#1B222B</Color>
    <Color x:Key="DarkPanelAltBackgroundColor">#242D38</Color>
    <Color x:Key="DarkBorderColor">#344253</Color>
    <Color x:Key="DarkPrimaryColor">#60A5FA</Color>
    <Color x:Key="DarkAccentColor">#2DD4BF</Color>
    <Color x:Key="DarkSuccessColor">#4ADE80</Color>
    <Color x:Key="DarkWarningColor">#FBBF24</Color>
    <Color x:Key="DarkErrorColor">#F87171</Color>
</ResourceDictionary>
```

- [ ] **Step 5: Merge iNKORE and theme resources**

Update `App.xaml`:

```xml
<Application x:Class="MyFlowChart.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:modern="clr-namespace:iNKORE.UI.WPF.Modern;assembly=iNKORE.UI.WPF.Modern"
             xmlns:modernControls="clr-namespace:iNKORE.UI.WPF.Modern.Controls;assembly=iNKORE.UI.WPF.Modern.Controls"
             StartupUri="MainWindow.xaml">
    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <modern:ThemeResources RequestedTheme="Default" />
                <modernControls:XamlControlsResources />
                <ResourceDictionary Source="Resources/Themes/IndustrialFluent.Tokens.xaml" />
                <ResourceDictionary Source="Resources/Themes/IndustrialFluent.Light.xaml" />
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Application.Resources>
</Application>
```

- [ ] **Step 6: Build**

Run the MSBuild command from Task 1 Step 5.

Expected: build succeeds; no XAML parse errors for `ThemeResources` or `XamlControlsResources`.

- [ ] **Step 7: Commit**

```powershell
git add App.xaml MyFlowChart.csproj Resources\Themes
git commit -m "feat: add industrial fluent theme resources"
```

---

### Task 3: Add Vision Execution Core

**Files:**
- Create: `D:\Aopencv\MyFlowChart\Services\Vision\VisionRunContext.cs`
- Create: `D:\Aopencv\MyFlowChart\Services\Vision\VisionOperatorResult.cs`
- Create: `D:\Aopencv\MyFlowChart\Services\Vision\IVisionOperatorExecutor.cs`
- Create: `D:\Aopencv\MyFlowChart\Services\Vision\OperatorCatalog.cs`
- Create: `D:\Aopencv\MyFlowChart\Services\Vision\VisionWorkflowRunner.cs`
- Modify: `D:\Aopencv\MyFlowChart\MyFlowChart.csproj`

- [ ] **Step 1: Add compile items**

Add these to `MyFlowChart.csproj`:

```xml
<Compile Include="Services\Vision\VisionRunContext.cs" />
<Compile Include="Services\Vision\VisionOperatorResult.cs" />
<Compile Include="Services\Vision\IVisionOperatorExecutor.cs" />
<Compile Include="Services\Vision\OperatorCatalog.cs" />
<Compile Include="Services\Vision\VisionWorkflowRunner.cs" />
```

- [ ] **Step 2: Create VisionOperatorResult**

```csharp
using System;

namespace MyFlowChart.Services.Vision
{
    /// <summary>
    /// 表示单个视觉算子的执行结果。
    /// </summary>
    public sealed class VisionOperatorResult
    {
        /// <summary>
        /// 初始化算子执行结果。
        /// </summary>
        /// <param name="success">是否执行成功。</param>
        /// <param name="message">结果消息。</param>
        /// <param name="payload">结果对象。</param>
        /// <returns>无返回值。</returns>
        private VisionOperatorResult(bool success, string message, object payload)
        {
            Success = success;
            Message = message ?? string.Empty;
            Payload = payload;
        }

        /// <summary>
        /// 获取是否执行成功。
        /// </summary>
        public bool Success { get; private set; }

        /// <summary>
        /// 获取结果消息。
        /// </summary>
        public string Message { get; private set; }

        /// <summary>
        /// 获取结果对象。
        /// </summary>
        public object Payload { get; private set; }

        /// <summary>
        /// 创建成功结果。
        /// </summary>
        /// <param name="message">结果消息。</param>
        /// <param name="payload">结果对象。</param>
        /// <returns>成功结果。</returns>
        public static VisionOperatorResult Ok(string message, object payload = null)
        {
            return new VisionOperatorResult(true, message, payload);
        }

        /// <summary>
        /// 创建失败结果。
        /// </summary>
        /// <param name="message">失败消息。</param>
        /// <returns>失败结果。</returns>
        public static VisionOperatorResult Fail(string message)
        {
            return new VisionOperatorResult(false, message, null);
        }
    }
}
```

- [ ] **Step 3: Create VisionRunContext**

```csharp
using System;
using System.Collections.Generic;
using OpenCvSharp;
using OpenCvWindowTool;

namespace MyFlowChart.Services.Vision
{
    /// <summary>
    /// 表示一次流程运行期间共享的图像和结果上下文。
    /// </summary>
    public sealed class VisionRunContext : IDisposable
    {
        private readonly Dictionary<string, object> _items = new Dictionary<string, object>();
        private bool _disposed;

        /// <summary>
        /// 初始化运行上下文。
        /// </summary>
        /// <param name="image">当前运行使用的图像引用。</param>
        /// <returns>无返回值。</returns>
        public VisionRunContext(Mat image)
        {
            Image = image;
            if (image != null && !image.Empty())
            {
                LineContext = LineDetectionImageContext.FromImage(image);
            }
        }

        /// <summary>
        /// 获取当前运行图像引用。
        /// </summary>
        public Mat Image { get; private set; }

        /// <summary>
        /// 获取直线检测灰度缓存。
        /// </summary>
        public LineDetectionImageContext LineContext { get; private set; }

        /// <summary>
        /// 获取算子共享数据。
        /// </summary>
        public IDictionary<string, object> Items
        {
            get { return _items; }
        }

        /// <summary>
        /// 释放本次运行创建的非托管缓存。
        /// </summary>
        /// <returns>无返回值。</returns>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            LineContext?.Dispose();
            LineContext = null;
            Image = null;
            _disposed = true;
        }
    }
}
```

- [ ] **Step 4: Create IVisionOperatorExecutor**

```csharp
using System.Threading;
using MyFlowChart.Models;

namespace MyFlowChart.Services.Vision
{
    /// <summary>
    /// 表示一个可执行的视觉算子。
    /// </summary>
    public interface IVisionOperatorExecutor
    {
        /// <summary>
        /// 执行视觉算子。
        /// </summary>
        /// <param name="flowOperator">流程中的算子实例。</param>
        /// <param name="context">本次运行上下文。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>算子执行结果。</returns>
        VisionOperatorResult Execute(FlowOperator flowOperator, VisionRunContext context, CancellationToken cancellationToken);
    }
}
```

- [ ] **Step 5: Create OperatorCatalog**

```csharp
using System;
using System.Collections.Generic;

namespace MyFlowChart.Services.Vision
{
    /// <summary>
    /// 保存第一阶段固定视觉算子注册表。
    /// </summary>
    public sealed class OperatorCatalog
    {
        private readonly Dictionary<string, Func<IVisionOperatorExecutor>> _executors =
            new Dictionary<string, Func<IVisionOperatorExecutor>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 初始化固定算子注册表。
        /// </summary>
        /// <returns>无返回值。</returns>
        public OperatorCatalog()
        {
            _executors["图像采集"] = () => new ImageInputOperatorExecutor();
            _executors["外侧圆检测"] = () => new LineFindOperatorExecutor();
            _executors["内侧圆检测"] = () => new LineFindOperatorExecutor();
            _executors["结果输出"] = () => new ResultOutputOperatorExecutor();
            _executors["预处理"] = () => new ImageInputOperatorExecutor();
        }

        /// <summary>
        /// 根据算子名称创建执行器。
        /// </summary>
        /// <param name="operatorName">算子名称。</param>
        /// <param name="executor">创建出的执行器。</param>
        /// <returns>找到执行器时返回 true。</returns>
        public bool TryCreate(string operatorName, out IVisionOperatorExecutor executor)
        {
            executor = null;
            if (string.IsNullOrWhiteSpace(operatorName))
            {
                return false;
            }

            Func<IVisionOperatorExecutor> factory;
            if (!_executors.TryGetValue(operatorName, out factory))
            {
                return false;
            }

            executor = factory();
            return true;
        }
    }
}
```

- [ ] **Step 6: Create VisionWorkflowRunner**

```csharp
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MyFlowChart.Models;
using OpenCvSharp;

namespace MyFlowChart.Services.Vision
{
    /// <summary>
    /// 负责在后台运行视觉流程。
    /// </summary>
    public sealed class VisionWorkflowRunner : IDisposable
    {
        private readonly OperatorCatalog _catalog;
        private CancellationTokenSource _cancellation;
        private VisionRunContext _context;

        /// <summary>
        /// 初始化视觉流程运行器。
        /// </summary>
        /// <param name="catalog">算子注册表。</param>
        /// <returns>无返回值。</returns>
        public VisionWorkflowRunner(OperatorCatalog catalog)
        {
            _catalog = catalog ?? new OperatorCatalog();
        }

        /// <summary>
        /// 异步运行单个算子块。
        /// </summary>
        /// <param name="node">流程节点。</param>
        /// <param name="image">运行图像。</param>
        /// <returns>运行任务。</returns>
        public Task RunNodeAsync(FlowNode node, Mat image)
        {
            Stop();
            _context?.Dispose();
            _context = new VisionRunContext(image);
            _cancellation = new CancellationTokenSource();
            return Task.Run(() => RunNode(node, _cancellation.Token), _cancellation.Token);
        }

        /// <summary>
        /// 请求停止当前运行。
        /// </summary>
        /// <returns>无返回值。</returns>
        public void Stop()
        {
            if (_cancellation != null && !_cancellation.IsCancellationRequested)
            {
                _cancellation.Cancel();
            }
        }

        /// <summary>
        /// 释放运行器资源。
        /// </summary>
        /// <returns>无返回值。</returns>
        public void Dispose()
        {
            Stop();
            _cancellation?.Dispose();
            _context?.Dispose();
        }

        /// <summary>
        /// 同步执行节点内算子。
        /// </summary>
        /// <param name="node">流程节点。</param>
        /// <param name="token">取消令牌。</param>
        /// <returns>无返回值。</returns>
        private void RunNode(FlowNode node, CancellationToken token)
        {
            if (node == null || !node.IsEnabled)
            {
                return;
            }

            node.Status = FlowNodeStatus.Running;
            Stopwatch stopwatch = Stopwatch.StartNew();
            try
            {
                foreach (FlowOperator flowOperator in node.Operators.ToList())
                {
                    token.ThrowIfCancellationRequested();
                    RunOperator(flowOperator, token);
                }

                node.Status = FlowNodeStatus.OK;
            }
            catch (OperationCanceledException)
            {
                node.Status = FlowNodeStatus.Stopped;
            }
            catch (Exception ex)
            {
                node.Status = FlowNodeStatus.NG;
                node.Remark = ex.Message;
            }
            finally
            {
                stopwatch.Stop();
                node.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            }
        }

        /// <summary>
        /// 执行单个算子。
        /// </summary>
        /// <param name="flowOperator">算子实例。</param>
        /// <param name="token">取消令牌。</param>
        /// <returns>无返回值。</returns>
        private void RunOperator(FlowOperator flowOperator, CancellationToken token)
        {
            IVisionOperatorExecutor executor;
            if (!_catalog.TryCreate(flowOperator.OperatorName, out executor))
            {
                flowOperator.Status = FlowNodeStatus.NG;
                return;
            }

            Stopwatch stopwatch = Stopwatch.StartNew();
            flowOperator.Status = FlowNodeStatus.Running;
            VisionOperatorResult result = executor.Execute(flowOperator, _context, token);
            stopwatch.Stop();
            flowOperator.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            flowOperator.Status = result.Success ? FlowNodeStatus.OK : FlowNodeStatus.NG;
        }
    }
}
```

- [ ] **Step 7: Build**

Run the MSBuild command from Task 1 Step 5.

Expected: build succeeds.

- [ ] **Step 8: Commit**

```powershell
git add MyFlowChart.csproj Services\Vision
git commit -m "feat: add vision workflow runner core"
```

---

### Task 4: Add First Three Vision Operator Executors

**Files:**
- Create: `D:\Aopencv\MyFlowChart\Services\Vision\ImageInputOperatorExecutor.cs`
- Create: `D:\Aopencv\MyFlowChart\Services\Vision\LineFindOperatorExecutor.cs`
- Create: `D:\Aopencv\MyFlowChart\Services\Vision\ResultOutputOperatorExecutor.cs`
- Modify: `D:\Aopencv\MyFlowChart\MyFlowChart.csproj`

- [ ] **Step 1: Add compile items**

```xml
<Compile Include="Services\Vision\ImageInputOperatorExecutor.cs" />
<Compile Include="Services\Vision\LineFindOperatorExecutor.cs" />
<Compile Include="Services\Vision\ResultOutputOperatorExecutor.cs" />
```

- [ ] **Step 2: Create ImageInputOperatorExecutor**

```csharp
using System.Threading;
using MyFlowChart.Models;

namespace MyFlowChart.Services.Vision
{
    /// <summary>
    /// 校验当前流程是否已有可用图像。
    /// </summary>
    public sealed class ImageInputOperatorExecutor : IVisionOperatorExecutor
    {
        /// <summary>
        /// 执行图像输入算子。
        /// </summary>
        /// <param name="flowOperator">流程算子。</param>
        /// <param name="context">运行上下文。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>执行结果。</returns>
        public VisionOperatorResult Execute(FlowOperator flowOperator, VisionRunContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (context == null || context.Image == null || context.Image.Empty())
            {
                return VisionOperatorResult.Fail("当前没有可运行图像");
            }

            return VisionOperatorResult.Ok("图像已就绪", context.Image);
        }
    }
}
```

- [ ] **Step 3: Create LineFindOperatorExecutor**

```csharp
using System.Linq;
using System.Threading;
using MyFlowChart.Models;
using OpenCvWindowTool;

namespace MyFlowChart.Services.Vision
{
    /// <summary>
    /// 使用原 OpenCvWindowTool 直线检测逻辑执行直线查找。
    /// </summary>
    public sealed class LineFindOperatorExecutor : IVisionOperatorExecutor
    {
        private readonly OptLineDetectionOperator _operator = new OptLineDetectionOperator();

        /// <summary>
        /// 执行直线查找。
        /// </summary>
        /// <param name="flowOperator">流程算子。</param>
        /// <param name="context">运行上下文。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>执行结果。</returns>
        public VisionOperatorResult Execute(FlowOperator flowOperator, VisionRunContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (context == null || context.LineContext == null)
            {
                return VisionOperatorResult.Fail("直线检测图像上下文为空");
            }

            RoiItem roi = context.Items.Values.OfType<RoiItem>().FirstOrDefault(x => x.CanDetectLine());
            if (roi == null)
            {
                return VisionOperatorResult.Fail("请先创建矩形或带角度矩形 ROI");
            }

            LineDetectionParams parameters = new LineDetectionParams();
            LineDetectionResult result = _operator.Detect(context.LineContext, roi, parameters);
            context.Items["LineDetectionResult"] = result;
            return result.Success
                ? VisionOperatorResult.Ok(result.Message, result)
                : VisionOperatorResult.Fail(result.Message);
        }
    }
}
```

- [ ] **Step 4: Create ResultOutputOperatorExecutor**

```csharp
using System.Threading;
using MyFlowChart.Models;

namespace MyFlowChart.Services.Vision
{
    /// <summary>
    /// 输出当前流程结果。
    /// </summary>
    public sealed class ResultOutputOperatorExecutor : IVisionOperatorExecutor
    {
        /// <summary>
        /// 执行结果输出算子。
        /// </summary>
        /// <param name="flowOperator">流程算子。</param>
        /// <param name="context">运行上下文。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>执行结果。</returns>
        public VisionOperatorResult Execute(FlowOperator flowOperator, VisionRunContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (context == null || !context.Items.ContainsKey("LineDetectionResult"))
            {
                return VisionOperatorResult.Fail("没有可输出的检测结果");
            }

            return VisionOperatorResult.Ok("结果已输出", context.Items["LineDetectionResult"]);
        }
    }
}
```

- [ ] **Step 5: Build**

Run the MSBuild command from Task 1 Step 5.

Expected: build succeeds.

- [ ] **Step 6: Commit**

```powershell
git add MyFlowChart.csproj Services\Vision
git commit -m "feat: add first vision operator executors"
```

---

### Task 5: Integrate Viewer, Commands, And Fixed Layout UI

**Files:**
- Modify: `D:\Aopencv\MyFlowChart\MainWindow.xaml`
- Modify: `D:\Aopencv\MyFlowChart\MainWindow.xaml.cs`
- Modify: `D:\Aopencv\MyFlowChart\ViewModels\MainWindowViewModel.cs`
- Modify: `D:\Aopencv\MyFlowChart\MyFlowChart.csproj`

- [ ] **Step 1: Add WindowsFormsHost namespace and viewer host**

In `MainWindow.xaml`, add:

```xml
xmlns:wfi="clr-namespace:System.Windows.Forms.Integration;assembly=WindowsFormsIntegration"
```

Replace the center workspace with a fixed grid that keeps `WindowsFormsHost` inside its own cell:

```xml
<Grid Grid.Column="2">
    <Grid.RowDefinitions>
        <RowDefinition Height="2*" />
        <RowDefinition Height="8" />
        <RowDefinition Height="*" />
    </Grid.RowDefinitions>

    <Border Grid.Row="0"
            Background="{StaticResource PanelBackgroundBrush}"
            BorderBrush="{StaticResource BorderBrush}"
            BorderThickness="1"
            CornerRadius="{StaticResource PanelCornerRadius}">
        <controls:FlowChartControl x:Name="flowChart"
                                   Nodes="{Binding Nodes}"
                                   SelectedNode="{Binding SelectedNode, Mode=TwoWay}" />
    </Border>

    <Border Grid.Row="2"
            Background="Black"
            BorderBrush="{StaticResource BorderBrush}"
            BorderThickness="1"
            CornerRadius="{StaticResource PanelCornerRadius}">
        <wfi:WindowsFormsHost x:Name="ImageViewerHost" />
    </Border>
</Grid>
```

Do not place WPF `Popup`, floating `Border`, or overlay panels over `ImageViewerHost`.

- [ ] **Step 2: Add viewer bridge fields**

In `MainWindow.xaml.cs`, add fields:

```csharp
private readonly OpenCvWindowTool.OpenCvImageViewer _imageViewer;
private readonly MyFlowChart.Services.Vision.VisionWorkflowRunner _visionRunner;
```

Initialize after `InitializeComponent()`:

```csharp
_imageViewer = new OpenCvWindowTool.OpenCvImageViewer
{
    DisplayToolBar = true,
    DisplayStatusBar = true
};
ImageViewerHost.Child = _imageViewer;
_visionRunner = new MyFlowChart.Services.Vision.VisionWorkflowRunner(
    new MyFlowChart.Services.Vision.OperatorCatalog());
```

- [ ] **Step 3: Add import image bridge**

Add this method to `MainWindow.xaml.cs`:

```csharp
/// <summary>
/// 打开图像文件并加载到显示控件。
/// </summary>
/// <returns>无返回值。</returns>
private void OpenImageForVision()
{
    Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog
    {
        Filter = "图像文件|*.bmp;*.jpg;*.jpeg;*.png;*.tif;*.tiff|所有文件|*.*"
    };

    if (dialog.ShowDialog(this) != true)
    {
        return;
    }

    _imageViewer.LoadImage(dialog.FileName);
}
```

- [ ] **Step 4: Run selected operator block through runner**

Add this method to `MainWindow.xaml.cs`:

```csharp
/// <summary>
/// 在后台运行当前选中的算子块。
/// </summary>
/// <returns>无返回值。</returns>
private async void RunSelectedVisionBlock()
{
    MyFlowChart.ViewModels.MainWindowViewModel viewModel = DataContext as MyFlowChart.ViewModels.MainWindowViewModel;
    if (viewModel == null || viewModel.SelectedNode == null || _imageViewer.ImageMat == null)
    {
        return;
    }

    btnStart.IsEnabled = false;
    btnStop.IsEnabled = true;
    try
    {
        await _visionRunner.RunNodeAsync(viewModel.SelectedNode, _imageViewer.ImageMat);
    }
    finally
    {
        btnStart.IsEnabled = true;
        btnStop.IsEnabled = false;
    }
}
```

Change `btnStart_Click` to call `RunSelectedVisionBlock()` after preserving existing flowchart run behavior if no vision image exists.

- [ ] **Step 5: Dispose runner and viewer**

Add to `MainWindow.xaml.cs`:

```csharp
/// <summary>
/// 释放视觉控件和流程运行器资源。
/// </summary>
/// <param name="e">关闭事件参数。</param>
/// <returns>无返回值。</returns>
protected override void OnClosed(System.EventArgs e)
{
    _visionRunner?.Dispose();
    _imageViewer?.Dispose();
    base.OnClosed(e);
}
```

- [ ] **Step 6: Add minimal ViewModel commands**

In `MainWindowViewModel.cs`, add `OpenImageCommand` and `RunVisionCommand` only if the view bridge will bind to them. If the current button click handlers remain, skip new commands to avoid duplicate pathways.

- [ ] **Step 7: Build**

Run the MSBuild command from Task 1 Step 5.

Expected: build succeeds and `MainWindow.xaml` has no XAML parse errors.

- [ ] **Step 8: Manual smoke check**

Run:

```powershell
Start-Process D:\Aopencv\MyFlowChart\obj\CodexVerify\MyFlowChart.exe
```

Expected:

- App opens.
- Left palette and right property panel do not overlap the image viewer.
- Image viewer toolbar/status bar appears in its fixed panel.
- Existing flowchart canvas still renders.

- [ ] **Step 9: Commit**

```powershell
git add MainWindow.xaml MainWindow.xaml.cs ViewModels\MainWindowViewModel.cs MyFlowChart.csproj
git commit -m "feat: integrate vision viewer workspace"
```

---

### Task 6: Add Verification Script And Final Checks

**Files:**
- Create: `D:\Aopencv\MyFlowChart\tools\VerifyVisionIntegration.ps1`
- Modify: `D:\Aopencv\MyFlowChart\README.md`
- Modify: `D:\Aopencv\MyFlowChart\docs\PROJECT_SUMMARY.md`

- [ ] **Step 1: Create verification script**

Create `tools\VerifyVisionIntegration.ps1`:

```powershell
$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$hashes = @{
    'OpenCvWindowTool\LineDetectionOperator.cs' = '8E5448262EA475B9F74B728F17FD51F922F0004EDDF0A258B4C5F0698B221C4F'
    'OpenCvWindowTool\OptLineDetectionOperator.cs' = 'BD513931A316B39A2EBF0F5A7808BE6603931DD9ED0721335E496264E71B4981'
    'OpenCvWindowTool\LineDetectionModels.cs' = 'C8A1C431FA533C8E981ACE37596146CF618E3221C92BFFCF99B56FEDAE95C0FD'
    'OpenCvWindowTool\LineDetectionImageContext.cs' = '8D182DBF636938B01F30C119679E98641956A4E15711ED8E6289941007EAB4DB'
    'OpenCvWindowTool\RoiItem.cs' = 'A211A0A7D79A09444F8B961B94B1BE434862FF4CCDCEFB9B32455A12189E30F0'
}

foreach ($entry in $hashes.GetEnumerator()) {
    $path = Join-Path $root $entry.Key
    if (!(Test-Path $path)) { throw "Missing core file: $path" }
    $actual = (Get-FileHash $path -Algorithm SHA256).Hash
    if ($actual -ne $entry.Value) {
        throw "Hash mismatch for $($entry.Key). Expected $($entry.Value), got $actual"
    }
}

& "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe" `
  (Join-Path $root 'MyFlowChart.sln') `
  /t:Build `
  /p:Configuration=Debug `
  /p:Platform="Any CPU" `
  /p:OutDir=(Join-Path $root 'obj\CodexVerify\') `
  /m

if ($LASTEXITCODE -ne 0) {
    throw "MSBuild failed with exit code $LASTEXITCODE"
}

Write-Host "Vision integration verification passed."
```

- [ ] **Step 2: Add README verification note**

Append to `README.md`:

```markdown
## Vision Integration Verification

Run:

```powershell
.\tools\VerifyVisionIntegration.ps1
```

This verifies that copied line-detection core files still match `D:\Aopencv\OpencvMaster` and then builds the solution.
```

- [ ] **Step 3: Update project summary**

Add to `docs\PROJECT_SUMMARY.md`:

```markdown
## Vision Workflow Integration

The first vision workflow stage directly includes `OpenCvWindowTool` as source under this repository. The line-detection core files must remain byte-for-byte equivalent to `D:\Aopencv\OpencvMaster`; use `tools\VerifyVisionIntegration.ps1` before handing off changes.

The WPF shell uses iNKORE.UI.WPF.Modern and Industrial Fluent resource dictionaries for global control styling. The WinForms `OpenCvImageViewer` is hosted in a fixed `WindowsFormsHost` cell; do not place WPF popups or floating overlays above it.
```

- [ ] **Step 4: Run verification**

Run:

```powershell
.\tools\VerifyVisionIntegration.ps1
```

Expected: hash checks pass and MSBuild succeeds.

- [ ] **Step 5: Commit**

```powershell
git add tools\VerifyVisionIntegration.ps1 README.md docs\PROJECT_SUMMARY.md
git commit -m "chore: add vision integration verification"
```

---

## Self-Review Checklist

- Spec coverage: direct `OpenCvWindowTool` inclusion, hash preservation, iNKORE Modern UI, Industrial Fluent tokens, Airspace layout rule, ROI image coordinates, `IDisposable` image context, cancellation, exception isolation, and dictionary-based operator registration are covered.
- Placeholder scan: no `TBD`, `TODO`, "implement later", or vague "add error handling" steps remain.
- Type consistency: `VisionRunContext`, `VisionOperatorResult`, `IVisionOperatorExecutor`, `OperatorCatalog`, and `VisionWorkflowRunner` names match across tasks.
- YAGNI check: first phase avoids reflection plugin loading, complex DI, full theme switching, camera SDK, project persistence, and script engines.

## Execution Choice

Plan complete and saved to `docs/superpowers/plans/2026-06-30-vision-workflow-direct-opencv.md`. Two execution options:

1. Subagent-Driven (recommended) - dispatch a fresh subagent per task, review between tasks, fast iteration.
2. Inline Execution - execute tasks in this session using executing-plans, batch execution with checkpoints.

