using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using MyFlowChart.Models;
using MyFlowChart.Services.Vision;
using MyFlowChart.ViewModels;
using MyFlowChart.Views;
using OpenCvWindowTool;

namespace MyFlowChart
{
    public partial class MainWindow : Window
    {
        private OperatorInsertAdorner _operatorInsertAdorner;
        private AdornerLayer _operatorInsertAdornerLayer;
        private readonly OpenCvImageViewer _imageViewer;
        private readonly VisionWorkflowRunner _visionRunner;

        /// <summary>
        /// 初始化主窗口并加载流程图界面。
        /// </summary>
        /// <returns>无返回值。</returns>
        public MainWindow()
        {
            InitializeComponent();
            _imageViewer = new OpenCvImageViewer
            {
                DisplayToolBar = true,
                DisplayStatusBar = true,
                EnableRoiInteraction = true
            };
            ImageViewerHost.Child = _imageViewer;
            _visionRunner = new VisionWorkflowRunner(new OperatorCatalog());
            _imageViewer.SelectedRoiChanged += ImageViewer_RoiChanged;
            _imageViewer.RoiChanged += ImageViewer_RoiChanged;
            _imageViewer.RoiEditCompleted += ImageViewer_RoiChanged;
            UpdateVisionRoiSummary();
        }

        /// <summary>
        /// 打开图像文件并加载到视觉显示控件。
        /// </summary>
        /// <param name="sender">触发打开图像的按钮。</param>
        /// <param name="e">按钮点击事件参数。</param>
        /// <returns>无返回值。</returns>
        private void btnOpenImage_Click(object sender, RoutedEventArgs e)
        {
            OpenImageForVision();
        }

        /// <summary>
        /// 启动当前流程图运行。
        /// </summary>
        /// <param name="sender">触发启动的按钮。</param>
        /// <param name="e">按钮点击事件参数。</param>
        /// <returns>无返回值。</returns>
        private async void btnStart_Click(object sender, RoutedEventArgs e)
        {
            await RunVisionGraphAsync();
        }

        /// <summary>
        /// 停止当前流程图运行。
        /// </summary>
        /// <param name="sender">触发停止的按钮。</param>
        /// <param name="e">按钮点击事件参数。</param>
        /// <returns>无返回值。</returns>
        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            flowChart.Stop();
            _visionRunner.Stop();
            GetViewModel()?.StopVisionRun();
        }

        /// <summary>
        /// 打开图像文件并加载到视觉显示控件。
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
            GetViewModel()?.UpdateVisionResult("结果：已加载图像，等待运行");
            UpdateVisionRoiSummary();
        }

        /// <summary>
        /// 在后台运行当前视觉流程图。
        /// </summary>
        /// <returns>异步运行任务。</returns>
        private async System.Threading.Tasks.Task RunVisionGraphAsync()
        {
            MainWindowViewModel viewModel = GetViewModel();
            if (viewModel == null || !viewModel.CanRunVision)
            {
                return;
            }

            FlowNode selectedNode = ResolveVisionRunNode(viewModel);
            if (selectedNode == null)
            {
                return;
            }

            RoiItem roi = ResolveLineDetectionRoi();
            viewModel.BeginVisionRun("视觉流程运行中");
            viewModel.RefreshVisionRunLogs(selectedNode);

            try
            {
                VisionOperatorResult result = await _visionRunner.RunGraphAsync(viewModel.Nodes, _imageViewer.ImageMat, roi);
                viewModel.EndVisionRun(result.Success, result.Message);
                ApplyVisionOperatorResults(viewModel.Nodes, _visionRunner.OperatorResults);
                viewModel.RefreshVisionRunLogs(selectedNode);
                LineDetectionResult lineResult = result.Payload as LineDetectionResult;
                if (lineResult == null && _visionRunner.LastOperatorResult != null)
                {
                    lineResult = _visionRunner.LastOperatorResult.Payload as LineDetectionResult;
                }

                if (lineResult != null)
                {
                    _imageViewer.ShowLineDetectionResult(lineResult, ResolveLineDetectionParams(selectedNode));
                    viewModel.UpdateVisionResult(FormatLineDetectionResult(lineResult));
                }
                else
                {
                    viewModel.UpdateVisionResult("结果：" + result.Message);
                }
            }
            finally
            {
                if (viewModel.IsVisionRunning)
                {
                    viewModel.EndVisionRun(false, "视觉流程已结束");
                }
            }
        }

        /// <summary>
        /// 处理图像控件 ROI 变化并刷新右侧结果面板。
        /// </summary>
        /// <param name="sender">触发事件的图像控件。</param>
        /// <param name="e">事件参数。</param>
        /// <returns>无返回值。</returns>
        private void ImageViewer_RoiChanged(object sender, EventArgs e)
        {
            UpdateVisionRoiSummary();
        }

        /// <summary>
        /// 获取主窗口视图模型。
        /// </summary>
        /// <returns>主窗口视图模型；未绑定时返回 null。</returns>
        private MainWindowViewModel GetViewModel()
        {
            return DataContext as MainWindowViewModel;
        }

        /// <summary>
        /// 获取本次启动需要运行的视觉算子块。
        /// </summary>
        /// <param name="viewModel">主窗口视图模型。</param>
        /// <returns>返回可运行的视觉算子块。</returns>
        private static FlowNode ResolveVisionRunNode(MainWindowViewModel viewModel)
        {
            FlowNode selectedNode = viewModel == null ? null : viewModel.SelectedNode;
            if (selectedNode != null
                && selectedNode.CanConfigureOperators
                && selectedNode.Operators.Any(o => OperatorDefinition.IsLineFindOperator(o.OperatorName)))
            {
                return selectedNode;
            }

            return viewModel == null ? null : viewModel.EnsureDefaultVisionWorkflow();
        }

        /// <summary>
        /// 获取当前可用于直线检测的 ROI。
        /// </summary>
        /// <returns>可检测直线的 ROI；未找到时返回 null。</returns>
        private RoiItem ResolveLineDetectionRoi()
        {
            RoiItem selectedRoi = _imageViewer.SelectedRoi;
            if (selectedRoi != null && selectedRoi.CanDetectLine())
            {
                return selectedRoi;
            }

            return _imageViewer.Rois.FirstOrDefault(x => x.CanDetectLine());
        }

        /// <summary>
        /// 刷新当前 ROI 摘要。
        /// </summary>
        /// <returns>无返回值。</returns>
        private void UpdateVisionRoiSummary()
        {
            RoiItem roi = ResolveLineDetectionRoi();
            if (roi == null)
            {
                GetViewModel()?.UpdateVisionRoi("ROI：未选择可检测直线的矩形 ROI");
                return;
            }

            GetViewModel()?.UpdateVisionRoi(string.Format(
                "ROI：{0}  X={1:0.0}, Y={2:0.0}, W={3:0.0}, H={4:0.0}",
                roi.Shape,
                roi.Center.X,
                roi.Center.Y,
                roi.Width,
                roi.Height));
        }

        /// <summary>
        /// 格式化直线检测结果摘要。
        /// </summary>
        /// <param name="result">直线检测结果。</param>
        /// <returns>结果摘要文本。</returns>
        private static string FormatLineDetectionResult(LineDetectionResult result)
        {
            if (result == null)
            {
                return "结果：未运行";
            }

            if (!result.Success)
            {
                return "结果：NG  " + result.Message;
            }

            return string.Format(
                "结果：OK  角度={0:0.00}°  强度={1:0.0}  耗时={2:0.0}ms",
                result.Angle,
                result.AverageStrength,
                result.Elapsed.TotalMilliseconds);
        }

        /// <summary>
        /// 从节点中读取当前直线查找参数。
        /// </summary>
        /// <param name="node">流程节点。</param>
        /// <returns>返回直线检测参数；未配置时返回默认参数。</returns>
        private static LineDetectionParams ResolveLineDetectionParams(FlowNode node)
        {
            LineFindOperatorParameters parameters = null;
            if (node != null && node.SelectedOperator != null)
            {
                parameters = node.SelectedOperator.Parameters as LineFindOperatorParameters;
            }

            if (parameters == null && node != null)
            {
                FlowOperator flowOperator = node.Operators.FirstOrDefault(x => x.Parameters is LineFindOperatorParameters);
                parameters = flowOperator == null ? null : flowOperator.Parameters as LineFindOperatorParameters;
            }

            return parameters == null ? new LineDetectionParams() : parameters.ToLineDetectionParams();
        }

        /// <summary>
        /// 按算子运行快照回写流程图中所有 WPF 绑定对象。
        /// </summary>
        /// <param name="nodes">需要更新的流程节点集合。</param>
        /// <param name="records">算子运行快照集合。</param>
        /// <returns>无返回值。</returns>
        private static void ApplyVisionOperatorResults(System.Collections.Generic.IEnumerable<FlowNode> nodes, System.Collections.Generic.IEnumerable<VisionOperatorRunRecord> records)
        {
            if (nodes == null || records == null)
            {
                return;
            }

            System.Collections.Generic.List<VisionOperatorRunRecord> recordList = records.ToList();
            foreach (FlowNode node in nodes)
            {
                ApplyVisionOperatorResults(node, recordList);
                ApplyOperatorBlockStatus(node);
                foreach (FlowBranch branch in node.Branches)
                {
                    ApplyVisionOperatorResults(branch.Nodes, recordList);
                }
            }
        }

        /// <summary>
        /// 根据块内算子结果聚合算子块颜色状态。
        /// </summary>
        /// <param name="node">需要聚合的流程块。</param>
        /// <returns>无返回值。</returns>
        private static void ApplyOperatorBlockStatus(FlowNode node)
        {
            if (node == null || !node.IsOperatorBlock || node.Operators.Count == 0)
            {
                return;
            }

            if (node.Operators.Any(x => x.Status == FlowNodeStatus.NG))
            {
                node.Status = FlowNodeStatus.NG;
                return;
            }

            if (node.Operators.All(x => x.Status == FlowNodeStatus.OK))
            {
                node.Status = FlowNodeStatus.OK;
            }
        }

        /// <summary>
        /// 按算子运行快照回写 WPF 绑定对象。
        /// </summary>
        /// <param name="node">需要更新的流程节点。</param>
        /// <param name="records">算子运行快照集合。</param>
        /// <returns>无返回值。</returns>
        private static void ApplyVisionOperatorResults(FlowNode node, System.Collections.Generic.IEnumerable<VisionOperatorRunRecord> records)
        {
            if (node == null || records == null)
            {
                return;
            }

            System.Collections.Generic.HashSet<Guid> appliedOperatorIds = new System.Collections.Generic.HashSet<Guid>();
            foreach (VisionOperatorRunRecord record in records)
            {
                FlowOperator flowOperator = node.Operators.FirstOrDefault(x => x.Id == record.OperatorId);
                if (flowOperator == null)
                {
                    continue;
                }

                flowOperator.Status = record.Status;
                flowOperator.ElapsedMilliseconds = record.ElapsedMilliseconds;
                flowOperator.LastMessage = record.Message;
                flowOperator.Payload = record.Payload is OpenCvSharp.Mat || record.Payload is ImageFrameToken ? null : record.Payload;
                appliedOperatorIds.Add(record.OperatorId);
            }

            foreach (FlowOperator flowOperator in node.Operators)
            {
                if (flowOperator.Status == FlowNodeStatus.Running && !appliedOperatorIds.Contains(flowOperator.Id))
                {
                    flowOperator.Status = FlowNodeStatus.Stopped;
                    flowOperator.ElapsedMilliseconds = 0;
                    flowOperator.LastMessage = "未执行";
                    flowOperator.Payload = null;
                }
            }
        }

        /// <summary>
        /// 处理右侧算子列表的拖拽经过，并显示插入位置。
        /// </summary>
        /// <param name="sender">算子列表控件。</param>
        /// <param name="e">拖拽事件参数。</param>
        /// <returns>无返回值。</returns>
        private void OperatorList_DragOver(object sender, DragEventArgs e)
        {
            ListBox listBox = sender as ListBox;
            FlowNode targetNode = listBox == null ? null : listBox.DataContext as FlowNode;
            if (!CanDropOperatorToList(targetNode, e))
            {
                ClearOperatorInsertAdorner();
                e.Effects = DragDropEffects.None;
                e.Handled = true;
                return;
            }

            double indicatorY;
            GetOperatorInsertIndex(listBox, e.GetPosition(listBox), out indicatorY);
            UpdateOperatorInsertAdorner(listBox, indicatorY);

            e.Effects = DragDropEffects.Copy;
            e.Handled = true;
        }

        /// <summary>
        /// 处理右侧算子列表双击，打开标准算子编辑窗口。
        /// </summary>
        /// <param name="sender">算子列表控件。</param>
        /// <param name="e">鼠标事件参数。</param>
        /// <returns>无返回值。</returns>
        private void OperatorList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            MainWindowViewModel viewModel = GetViewModel();
            if (viewModel == null || !viewModel.CanEditVisionWorkflow)
            {
                return;
            }

            ListBox listBox = sender as ListBox;
            if (listBox == null)
            {
                return;
            }

            ListBoxItem item = FindListBoxItem(listBox, e.GetPosition(listBox));
            FlowOperator flowOperator = item == null ? listBox.SelectedItem as FlowOperator : item.DataContext as FlowOperator;
            OperatorEditorKind editorKind = flowOperator == null
                ? OperatorEditorKind.None
                : OperatorDefinition.GetEditorKind(flowOperator.OperatorName);
            if (editorKind == OperatorEditorKind.None)
            {
                return;
            }

            e.Handled = true;
            OpenOperatorEditor(flowOperator, editorKind);
        }

        /// <summary>
        /// 按算子编辑器类型打开标准配置窗口。
        /// </summary>
        /// <param name="flowOperator">待编辑的流程算子。</param>
        /// <param name="editorKind">算子编辑器类型。</param>
        /// <returns>无返回值。</returns>
        private void OpenOperatorEditor(FlowOperator flowOperator, OperatorEditorKind editorKind)
        {
            switch (editorKind)
            {
                case OperatorEditorKind.ImageInput:
                    OpenImageInputOperatorEditor(flowOperator);
                    break;
                case OperatorEditorKind.LineFind:
                    OpenLineFindOperatorEditor(flowOperator);
                    break;
            }
        }

        /// <summary>
        /// 打开图像采集算子的标准编辑窗口。
        /// </summary>
        /// <param name="flowOperator">待编辑的图像采集算子。</param>
        /// <returns>无返回值。</returns>
        private void OpenImageInputOperatorEditor(FlowOperator flowOperator)
        {
            ImageInputOperatorEditorWindow editor = new ImageInputOperatorEditorWindow(flowOperator)
            {
                Owner = this
            };

            if (editor.ShowDialog() != true || editor.EditedParameters == null)
            {
                return;
            }

            flowOperator.Parameters = editor.EditedParameters;
            if (!string.IsNullOrWhiteSpace(editor.EditedParameters.ImagePath))
            {
                _imageViewer.LoadImage(editor.EditedParameters.ImagePath);
                UpdateVisionRoiSummary();
            }

            if (editor.EditedStatus.HasValue)
            {
                flowOperator.Status = editor.EditedStatus.Value;
                flowOperator.ElapsedMilliseconds = editor.EditedElapsedMilliseconds;
                flowOperator.LastMessage = editor.EditedMessage;
            }
            else
            {
                flowOperator.Status = FlowNodeStatus.NotRun;
                flowOperator.ElapsedMilliseconds = 0;
                flowOperator.LastMessage = "参数已更新";
            }

            MainWindowViewModel viewModel = GetViewModel();
            if (viewModel != null && viewModel.SelectedNode != null)
            {
                viewModel.RefreshVisionRunLogs(viewModel.SelectedNode);
            }
        }

        /// <summary>
        /// 打开直线查找算子的标准编辑窗口。
        /// </summary>
        /// <param name="flowOperator">待编辑的直线查找算子。</param>
        /// <returns>无返回值。</returns>
        private void OpenLineFindOperatorEditor(FlowOperator flowOperator)
        {
            VisionOperatorEditorWindow editor = new VisionOperatorEditorWindow(flowOperator, _imageViewer.ImageMat)
            {
                Owner = this
            };

            if (editor.ShowDialog() != true || editor.EditedParameters == null)
            {
                return;
            }

            flowOperator.Parameters = editor.EditedParameters;
            if (editor.EditedStatus.HasValue)
            {
                flowOperator.Status = editor.EditedStatus.Value;
                flowOperator.ElapsedMilliseconds = editor.EditedElapsedMilliseconds;
                flowOperator.LastMessage = editor.EditedMessage;
            }
            else
            {
                flowOperator.Status = FlowNodeStatus.NotRun;
                flowOperator.ElapsedMilliseconds = 0;
                flowOperator.LastMessage = "参数已更新";
            }

            MainWindowViewModel viewModel = GetViewModel();
            if (viewModel != null && viewModel.SelectedNode != null)
            {
                viewModel.RefreshVisionRunLogs(viewModel.SelectedNode);
            }
        }

        /// <summary>
        /// 处理右侧算子列表的拖拽离开，并清理插入位置提示。
        /// </summary>
        /// <param name="sender">算子列表控件。</param>
        /// <param name="e">拖拽事件参数。</param>
        /// <returns>无返回值。</returns>
        private void OperatorList_DragLeave(object sender, DragEventArgs e)
        {
            ListBox listBox = sender as ListBox;
            if (listBox == null || !IsPointInsideListBox(listBox, e.GetPosition(listBox)))
            {
                ClearOperatorInsertAdorner();
            }
        }

        /// <summary>
        /// 处理右侧算子列表的拖拽放置，并按提示位置插入算子。
        /// </summary>
        /// <param name="sender">算子列表控件。</param>
        /// <param name="e">拖拽事件参数。</param>
        /// <returns>无返回值。</returns>
        private void OperatorList_Drop(object sender, DragEventArgs e)
        {
            ListBox listBox = sender as ListBox;
            FlowNode targetNode = listBox == null ? null : listBox.DataContext as FlowNode;
            DragOperatorData data = GetDragOperatorData(e);
            if (!CanDropOperatorToList(targetNode, e) || data == null)
            {
                ClearOperatorInsertAdorner();
                e.Effects = DragDropEffects.None;
                e.Handled = true;
                return;
            }

            double indicatorY;
            int insertIndex = GetOperatorInsertIndex(listBox, e.GetPosition(listBox), out indicatorY);
            insertIndex = Math.Max(0, Math.Min(insertIndex, targetNode.Operators.Count));

            FlowOperator flowOperator = new FlowOperator
            {
                OperatorName = data.Name,
                DisplayName = CreateOperatorDisplayName(targetNode, data.Name),
                Status = FlowNodeStatus.NotRun
            };

            targetNode.Operators.Insert(insertIndex, flowOperator);
            targetNode.SelectedOperator = flowOperator;
            flowChart.SelectedNode = targetNode;

            ClearOperatorInsertAdorner();
            e.Effects = DragDropEffects.Copy;
            e.Handled = true;
        }

        /// <summary>
        /// 判断拖拽算子是否可以放入右侧算子列表。
        /// </summary>
        /// <param name="targetNode">目标流程块。</param>
        /// <param name="e">拖拽事件参数。</param>
        /// <returns>可放入时返回 true，否则返回 false。</returns>
        private bool CanDropOperatorToList(FlowNode targetNode, DragEventArgs e)
        {
            MainWindowViewModel viewModel = GetViewModel();
            return viewModel != null
                && viewModel.CanEditVisionWorkflow
                && flowChart != null
                && !flowChart.IsRunning
                && targetNode != null
                && targetNode.CanConfigureOperators
                && GetDragOperatorData(e) != null;
        }

        /// <summary>
        /// 从拖拽事件中读取算子数据。
        /// </summary>
        /// <param name="e">拖拽事件参数。</param>
        /// <returns>返回有效算子数据；无效时返回 null。</returns>
        private static DragOperatorData GetDragOperatorData(DragEventArgs e)
        {
            DragOperatorData data = null;
            if (e != null && e.Data.GetDataPresent(DragOperatorData.DataFormat))
            {
                data = e.Data.GetData(DragOperatorData.DataFormat) as DragOperatorData;
            }
            else if (e != null && e.Data.GetDataPresent(typeof(DragOperatorData)))
            {
                data = e.Data.GetData(typeof(DragOperatorData)) as DragOperatorData;
            }

            if (data == null || data.SourceName != "tool" || string.IsNullOrWhiteSpace(data.Name))
            {
                return null;
            }

            return data;
        }

        /// <summary>
        /// 计算拖拽位置对应的算子插入序号和提示线纵坐标。
        /// </summary>
        /// <param name="listBox">算子列表控件。</param>
        /// <param name="point">拖拽点在列表内的位置。</param>
        /// <param name="indicatorY">输出提示线纵坐标。</param>
        /// <returns>返回插入序号。</returns>
        private static int GetOperatorInsertIndex(ListBox listBox, Point point, out double indicatorY)
        {
            indicatorY = 2.0;
            if (listBox == null)
            {
                return 0;
            }

            ListBoxItem item = FindListBoxItem(listBox, point);
            if (item == null)
            {
                indicatorY = GetOperatorListTailY(listBox, point);
                return listBox.Items.Count;
            }

            int itemIndex = listBox.ItemContainerGenerator.IndexFromContainer(item);
            Point itemPoint = item.TranslatePoint(new Point(0, 0), listBox);
            bool insertAfter = point.Y > itemPoint.Y + item.ActualHeight / 2.0;
            indicatorY = itemPoint.Y + (insertAfter ? item.ActualHeight : 0);
            return itemIndex + (insertAfter ? 1 : 0);
        }

        /// <summary>
        /// 获取拖拽点下方的算子列表项。
        /// </summary>
        /// <param name="listBox">算子列表控件。</param>
        /// <param name="point">拖拽点在列表内的位置。</param>
        /// <returns>返回命中的列表项；未命中时返回 null。</returns>
        private static ListBoxItem FindListBoxItem(ListBox listBox, Point point)
        {
            HitTestResult result = VisualTreeHelper.HitTest(listBox, point);
            DependencyObject source = result == null ? null : result.VisualHit;
            return FindAncestor<ListBoxItem>(source);
        }

        /// <summary>
        /// 获取列表末尾提示线的纵坐标。
        /// </summary>
        /// <param name="listBox">算子列表控件。</param>
        /// <param name="point">拖拽点在列表内的位置。</param>
        /// <returns>返回提示线纵坐标。</returns>
        private static double GetOperatorListTailY(ListBox listBox, Point point)
        {
            for (int i = listBox.Items.Count - 1; i >= 0; i--)
            {
                FrameworkElement item = listBox.ItemContainerGenerator.ContainerFromIndex(i) as FrameworkElement;
                if (item != null)
                {
                    Point itemPoint = item.TranslatePoint(new Point(0, 0), listBox);
                    return itemPoint.Y + item.ActualHeight;
                }
            }

            double maxY = Math.Max(2.0, listBox.ActualHeight - 2.0);
            return Math.Max(2.0, Math.Min(maxY, point.Y));
        }

        /// <summary>
        /// 判断拖拽点是否仍在算子列表内。
        /// </summary>
        /// <param name="listBox">算子列表控件。</param>
        /// <param name="point">拖拽点在列表内的位置。</param>
        /// <returns>在列表范围内返回 true，否则返回 false。</returns>
        private static bool IsPointInsideListBox(ListBox listBox, Point point)
        {
            return listBox != null && point.X >= 0 && point.Y >= 0 && point.X <= listBox.ActualWidth && point.Y <= listBox.ActualHeight;
        }

        /// <summary>
        /// 更新右侧算子列表的插入位置提示线。
        /// </summary>
        /// <param name="listBox">算子列表控件。</param>
        /// <param name="indicatorY">提示线纵坐标。</param>
        /// <returns>无返回值。</returns>
        private void UpdateOperatorInsertAdorner(ListBox listBox, double indicatorY)
        {
            AdornerLayer layer = AdornerLayer.GetAdornerLayer(listBox);
            if (layer == null)
            {
                return;
            }

            if (_operatorInsertAdorner == null || _operatorInsertAdorner.AdornedElement != listBox || _operatorInsertAdornerLayer != layer)
            {
                ClearOperatorInsertAdorner();
                _operatorInsertAdorner = new OperatorInsertAdorner(listBox, indicatorY);
                _operatorInsertAdornerLayer = layer;
                layer.Add(_operatorInsertAdorner);
                return;
            }

            _operatorInsertAdorner.UpdateY(indicatorY);
        }

        /// <summary>
        /// 清理右侧算子列表的插入位置提示线。
        /// </summary>
        /// <returns>无返回值。</returns>
        private void ClearOperatorInsertAdorner()
        {
            if (_operatorInsertAdorner != null && _operatorInsertAdornerLayer != null)
            {
                _operatorInsertAdornerLayer.Remove(_operatorInsertAdorner);
            }

            _operatorInsertAdorner = null;
            _operatorInsertAdornerLayer = null;
        }

        /// <summary>
        /// 创建块内算子的显示名称。
        /// </summary>
        /// <param name="targetNode">目标算子块。</param>
        /// <param name="operatorName">算子名称。</param>
        /// <returns>返回块内算子显示名称。</returns>
        private static string CreateOperatorDisplayName(FlowNode targetNode, string operatorName)
        {
            int number = targetNode == null ? 1 : targetNode.Operators.Count(o => o.OperatorName == operatorName) + 1;
            return operatorName + "_" + number;
        }

        /// <summary>
        /// 查找指定类型的可视化父级。
        /// </summary>
        /// <typeparam name="T">要查找的父级类型。</typeparam>
        /// <param name="source">起始元素。</param>
        /// <returns>找到时返回父级元素，否则返回 null。</returns>
        private static T FindAncestor<T>(DependencyObject source) where T : DependencyObject
        {
            while (source != null)
            {
                T target = source as T;
                if (target != null)
                {
                    return target;
                }

                source = VisualTreeHelper.GetParent(source);
            }

            return null;
        }

        /// <summary>
        /// 释放视觉控件和流程运行器资源。
        /// </summary>
        /// <param name="e">窗口关闭事件参数。</param>
        /// <returns>无返回值。</returns>
        protected override void OnClosed(EventArgs e)
        {
            _imageViewer.SelectedRoiChanged -= ImageViewer_RoiChanged;
            _imageViewer.RoiChanged -= ImageViewer_RoiChanged;
            _imageViewer.RoiEditCompleted -= ImageViewer_RoiChanged;
            _visionRunner?.Dispose();
            _imageViewer?.Dispose();
            base.OnClosed(e);
        }

        private sealed class OperatorInsertAdorner : Adorner
        {
            private readonly Pen _pen;
            private double _y;

            /// <summary>
            /// 初始化算子插入提示线。
            /// </summary>
            /// <param name="adornedElement">需要覆盖提示线的列表控件。</param>
            /// <param name="y">提示线纵坐标。</param>
            /// <returns>无返回值。</returns>
            public OperatorInsertAdorner(UIElement adornedElement, double y)
                : base(adornedElement)
            {
                IsHitTestVisible = false;
                _y = y;
                _pen = new Pen(new SolidColorBrush(Color.FromRgb(36, 120, 224)), 2.0);
                _pen.Freeze();
            }

            /// <summary>
            /// 更新提示线纵坐标并重新绘制。
            /// </summary>
            /// <param name="y">提示线纵坐标。</param>
            /// <returns>无返回值。</returns>
            public void UpdateY(double y)
            {
                _y = y;
                InvalidateVisual();
            }

            /// <summary>
            /// 绘制算子插入位置提示线。
            /// </summary>
            /// <param name="drawingContext">绘图上下文。</param>
            /// <returns>无返回值。</returns>
            protected override void OnRender(DrawingContext drawingContext)
            {
                double y = Math.Max(1.0, Math.Min(RenderSize.Height - 1.0, _y));
                double left = 6.0;
                double right = Math.Max(left, RenderSize.Width - 6.0);
                drawingContext.DrawLine(_pen, new Point(left, y), new Point(right, y));
                drawingContext.DrawEllipse(_pen.Brush, null, new Point(left, y), 3.0, 3.0);
                drawingContext.DrawEllipse(_pen.Brush, null, new Point(right, y), 3.0, 3.0);
            }
        }
    }
}
