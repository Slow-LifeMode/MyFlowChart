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

### ViewModel 层

`MainWindowViewModel` 继续保存流程节点、当前选中节点、属性面板显示状态和命令。新增视觉相关状态时也放在 ViewModel 中，例如当前图像状态、运行状态、直线检测参数和结果摘要。

代码隐藏只做 WPF 与 WinForms 控件桥接、文件对话框桥接和事件转发。

### 执行层

新增最小执行层，例如 `VisionWorkflowRunner`。它负责按流程顺序执行 `FlowOperator`，并通过算子注册表找到对应执行器。

执行层在后台线程运行，避免 UI 卡死。执行完成后只把状态、耗时、结果摘要和叠加对象切回 UI 线程显示。

### 数据层

运行上下文传递图像引用和预处理缓存，不在每个节点之间复制整幅图像。第一阶段使用 `LineDetectionImageContext` 作为灰度缓存入口，让直线检测算子复用已有灰度数据。

图像导入和必要格式转换可以创建新数据；流程节点之间默认传引用。

### 算子扩展层

新增最小算子目录，例如 `OperatorCatalog`。每个算子有一个名称、显示名、参数模型和执行器。新增算子时只注册新的执行器，不修改流程图控件主逻辑。

第一阶段只需要三个执行器：

- 图像输入：加载或接收当前图像，并生成运行上下文图像引用。
- 直线查找：调用 `OpenCvWindowTool` 中原有直线检测算子。
- 结果输出：把检测结果写入流程状态和结果面板。

## 第一阶段界面

主界面保持三栏结构，但中间区域从单一流程画布升级为视觉工作区：

- 左侧：算子库。
- 中间：流程画布和图像显示区。
- 右侧：当前节点参数、ROI 参数、运行状态和检测结果。

第一阶段不做完整主题系统，只做必要的工业视觉风格调整：信息密度更高、状态清晰、按钮和结果区更稳定。

## 数据流

```text
用户导入图像
  -> OpenCvImageViewer 显示图像
  -> 运行上下文保存图像引用和 LineDetectionImageContext
  -> 用户在图像控件中创建 ROI
  -> 流程运行器后台执行直线查找
  -> 原 OpenCvWindowTool 算法返回 LineDetectionResult
  -> UI 线程更新节点状态、耗时、结果文本和叠加显示
```

## 明确不做

第一阶段不做相机 SDK、脚本系统、完整插件市场、项目文件保存、权限管理、多图像窗口管理和复杂调度系统。

这些功能只有在第一条视觉链稳定后再加。

## 验收标准

- `OpenCvWindowTool` 已被复制进当前工作目录，并由 `MyFlowChart` 项目引用。
- 直线检测核心文件与 `D:\Aopencv\OpencvMaster` 对应文件哈希一致。
- 主界面能显示 `OpenCvImageViewer`，可创建 ROI。
- 流程能从 UI 触发后台执行，不阻塞窗口拖动和基础交互。
- 直线查找执行器直接调用原有 `LineDetectionOperator` 或 `OptLineDetectionOperator`。
- 图像在流程节点之间不做重复整图复制。
- 新增算子只需新增执行器和注册项，不需要修改流程图控件主逻辑。
- Visual Studio MSBuild Debug 构建通过。
