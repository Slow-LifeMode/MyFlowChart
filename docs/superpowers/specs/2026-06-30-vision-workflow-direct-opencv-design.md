# 视觉流程系统第一阶段设计

## 目标

在 `D:\Aopencv\MyFlowChart` 中建设一个架构清晰、运行流畅、可扩展的视觉流程编辑系统。第一阶段只打通一条稳定闭环：图像显示、ROI 编辑、直线查找算子执行、流程状态反馈和结果展示。

## 关键决定

直接把 `D:\Aopencv\OpencvMaster\OpenCvWindowTool` 融入当前工作目录，目标位置为 `D:\Aopencv\MyFlowChart\OpenCvWindowTool`。当前项目通过项目引用直接使用 `OpenCvWindowTool.csproj`，并把它作为当前解决方案源码的一部分维护。

`OpenCvWindowToolWpfDemo` 只作为集成参考，不整体搬入当前项目，避免引入第二套 `App.xaml`、`MainWindow.xaml` 和重复的 Demo 架构。

## 不可变约束

直线检测逻辑必须与 `D:\Aopencv\OpencvMaster` 保持一致。实现期间禁止修改以下核心文件的检测行为：

- `OpenCvWindowTool\LineDetectionOperator.cs`
- `OpenCvWindowTool\OptLineDetectionOperator.cs`
- `OpenCvWindowTool\LineDetectionModels.cs`
- `OpenCvWindowTool\LineDetectionImageContext.cs`
- `OpenCvWindowTool\RoiItem.cs`

复制前记录的 SHA256：

```text
8E5448262EA475B9F74B728F17FD51F922F0004EDDF0A258B4C5F0698B221C4F  LineDetectionOperator.cs
BD513931A316B39A2EBF0F5A7808BE6603931DD9ED0721335E496264E71B4981  OptLineDetectionOperator.cs
C8A1C431FA533C8E981ACE37596146CF618E3221C92BFFCF99B56FEDAE95C0FD  LineDetectionModels.cs
8D182DBF636938B01F30C119679E98641956A4E15711ED8E6289941007EAB4DB  LineDetectionImageContext.cs
A211A0A7D79A09444F8B961B94B1BE434862FF4CCDCEFB9B32455A12189E30F0  RoiItem.cs
```

验收时必须对复制后的同名文件重新计算 SHA256。除非用户明确要求修改算法，否则这些哈希应保持一致。

## 架构边界

### UI 层

`MainWindow.xaml` 继续作为主工作区，包含算子库、流程画布、图像显示区和属性/结果面板。图像显示区使用 `WindowsFormsHost` 承载 `OpenCvImageViewer`，直接获得现有图像显示、缩放、ROI 和叠加结果能力。

UI 层只处理显示、用户输入、命令绑定和 UI 状态刷新，不直接承载直线检测算法。

由于 `WindowsFormsHost` 存在 WPF/WinForms 空域限制，图像显示区不得被 WPF 悬浮层覆盖。左侧算子库、右侧属性面板、顶部工具栏和中间图像区必须使用固定网格分栏，弹出式参数面板必须改为侧边抽屉或内嵌面板，通过重新分配布局空间挤压图像区，不能悬浮在 `OpenCvImageViewer` 上方。

### ViewModel 层

`MainWindowViewModel` 继续保存流程节点、当前选中节点、属性面板显示状态和命令。新增视觉相关状态时也放在 ViewModel 中，例如当前图像状态、运行状态、直线检测参数和结果摘要。

代码隐藏只做 WPF 与 WinForms 控件桥接、文件对话框桥接和事件转发。

ROI 坐标统一使用底层 `Mat` 图像像素坐标。`OpenCvImageViewer` 传出的 ROI 已经由控件内部从屏幕坐标转换到图像坐标，ViewModel 只保存图像坐标，不保存 WPF DIP 坐标。若后续新增 WPF 原生叠加层，必须显式通过图像坐标和显示比例转换，不能把 WPF 控件坐标直接传给检测算子。

### 执行层

新增最小执行层，例如 `VisionWorkflowRunner`。它负责按流程顺序执行 `FlowOperator`，并通过算子注册表找到对应执行器。

执行层在后台线程运行，避免 UI 卡死。执行完成后只把状态、耗时、结果摘要和叠加对象切回 UI 线程显示。

`VisionWorkflowRunner` 必须持有 `CancellationTokenSource`，启动前取消并清理上一轮运行。停止按钮通过 `CancellationToken` 请求中断，算子执行器在进入耗时步骤前检查取消令牌。

Runner 顶层必须捕获 `OpenCvSharp` 和普通托管异常，把异常转换成节点 `NG` 状态、错误文本和红色结果提示。进程级原生崩溃不能靠 `try-catch` 可靠兜住，因此调用直线检测前必须先做空图像、空 ROI、ROI 类型和图像上下文有效性检查。

### 数据层

运行上下文传递图像引用和预处理缓存，不在每个节点之间复制整幅图像。第一阶段使用 `LineDetectionImageContext` 作为灰度缓存入口，让直线检测算子复用已有灰度数据。

图像导入和必要格式转换可以创建新数据；流程节点之间默认传引用。

图像所有权必须明确。显示控件持有自己的显示图像；单次流程运行使用 `VisionRunContext` 持有运行图像引用和 `LineDetectionImageContext` 缓存。`VisionRunContext` 实现 `IDisposable`，每次新运行开始前释放上一轮上下文，窗口关闭时释放当前上下文，避免 `Mat` 和灰度缓存占用的非托管内存累积。

### 算子扩展层

新增最小算子目录，例如 `OperatorCatalog`。每个算子有一个名称、显示名、参数模型和执行器。新增算子时只注册新的执行器，不修改流程图控件主逻辑。

第一阶段不做反射扫描、插件加载或 DI 自动发现。`OperatorCatalog` 使用手写 `Dictionary<string, Func<IVisionOperatorExecutor>>` 注册固定算子，保持实现可读、可调试。动态 DLL 扫描等机制等算子数量和部署需求真实出现后再加。

第一阶段只需要三个执行器：

- 图像输入：加载或接收当前图像，并生成运行上下文图像引用。
- 直线查找：调用 `OpenCvWindowTool` 中原有直线检测算子。
- 结果输出：把检测结果写入流程状态和结果面板。

## 第一阶段界面

主界面保持三栏结构，但中间区域从单一流程画布升级为视觉工作区：

- 左侧：算子库。
- 中间：流程画布和图像显示区。
- 右侧：当前节点参数、ROI 参数、运行状态和检测结果。

第一阶段引入 `iNKORE.UI.WPF.Modern` 作为全局现代 WPF 控件样式基础，版本优先固定为 `0.10.2.1`。它兼容当前 `.NET Framework 4.8` 项目，并提供 Fluent 2 风格控件、浅色/深色主题、Mica、Acrylic 和常用现代控件能力。

视觉方向选定为 **Industrial Fluent**：以 Fluent 2 的控件一致性、Mica 窗口质感和清晰状态反馈为基础，叠加工业视觉软件需要的高信息密度、低装饰噪音和稳定扫描结构。第一阶段不做 Swiss 与 Industrial 双风格切换，避免多套主题增加维护成本。

项目需要新增全局设计 Token 资源字典，例如 `Resources\Themes\IndustrialFluent.Light.xaml`、`Resources\Themes\IndustrialFluent.Dark.xaml` 和 `Resources\Themes\IndustrialFluent.Tokens.xaml`。Token 覆盖以下内容：

- 颜色：背景、面板、边框、主色、强调色、成功、警告、错误、运行中、禁用、ROI、检测线。
- 密度：紧凑型 Padding、Margin、工具栏高度、属性行高度、列表项高度。
- 字体：窗口标题、区域标题、字段标签、正文、辅助说明、状态数字。
- 圆角：窗口面板、按钮、输入框、状态标签和节点块的统一半径。

Mica 只用于主窗口背景和不覆盖 `WindowsFormsHost` 的外层区域。Acrylic 只能用于左侧/右侧固定面板内部，不能作为覆盖图像显示区的半透明悬浮层。所有 iNKORE 控件样式必须继续遵守空域限制。

## 数据流

```text
用户导入图像
  -> OpenCvImageViewer 显示图像
  -> 用户在图像控件中创建 ROI，ROI 坐标为 Mat 图像像素坐标
  -> 运行前释放旧 VisionRunContext
  -> 新 VisionRunContext 保存图像引用和 LineDetectionImageContext
  -> 流程运行器后台执行直线查找
  -> 原 OpenCvWindowTool 算法返回 LineDetectionResult
  -> UI 线程更新节点状态、耗时、结果文本和叠加显示
```

## 明确不做

第一阶段不做相机 SDK、脚本系统、完整插件市场、项目文件保存、权限管理、多图像窗口管理和复杂调度系统。

这些功能只有在第一条视觉链稳定后再加。

## 验收标准

- `OpenCvWindowTool` 已被复制进当前工作目录，并由 `MyFlowChart` 项目引用。
- `MyFlowChart` 引用 `iNKORE.UI.WPF.Modern`，并在 `App.xaml` 合并全局现代控件样式和 Industrial Fluent Token 字典。
- 项目提供浅色和深色两套基础 Token，按钮、输入框、列表、属性面板和状态标签使用统一颜色、密度、字体层级和圆角。
- 直线检测核心文件与 `D:\Aopencv\OpencvMaster` 对应文件哈希一致。
- 主界面能显示 `OpenCvImageViewer`，可创建 ROI。
- 左侧算子库、右侧属性面板和图像显示区互不重叠；所有参数面板不悬浮覆盖 `WindowsFormsHost`。
- ROI 传入检测算子前保持为底层 `Mat` 图像像素坐标，在 100%、125%、150% 系统缩放下不使用 WPF DIP 坐标参与检测。
- 流程能从 UI 触发后台执行，不阻塞窗口拖动和基础交互。
- 停止按钮能请求取消当前后台运行；取消或异常会把节点标记为 `Stopped` 或 `NG`，不会让托管异常直接冒泡到 UI 线程。
- 直线查找执行器直接调用原有 `LineDetectionOperator` 或 `OptLineDetectionOperator`。
- 图像在流程节点之间不做重复整图复制。
- 新一轮流程开始前会释放上一轮 `VisionRunContext`，窗口关闭时释放当前运行上下文。
- 新增算子只需新增执行器和注册项，不需要修改流程图控件主逻辑。
- 第一阶段算子注册使用手写字典，不引入反射扫描、插件加载或复杂 DI。
- Visual Studio MSBuild Debug 构建通过。
