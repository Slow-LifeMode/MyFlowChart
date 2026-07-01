using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using MyFlowChart.Models;
using MyFlowChart.ViewModels;
using OpenCvSharp;
using OpenCvWindowTool;

namespace MyFlowChart.Views
{
    /// <summary>
    /// 视觉算子的标准编辑窗口。
    /// </summary>
    public partial class VisionOperatorEditorWindow : System.Windows.Window
    {
        private readonly OpenCvImageViewer _viewer;
        private bool _refreshing;

        /// <summary>
        /// 初始化直线查找算子编辑窗口。
        /// </summary>
        /// <param name="flowOperator">待编辑的流程算子。</param>
        /// <param name="image">当前输入图像。</param>
        /// <returns>无返回值。</returns>
        public VisionOperatorEditorWindow(FlowOperator flowOperator, Mat image)
        {
            InitializeComponent();

            FlowOperator = flowOperator;
            ViewModel = new LineFindOperatorEditorViewModel(
                flowOperator == null ? OperatorDefinition.LineFindName : flowOperator.DisplayName,
                CloneParameters(flowOperator));
            DataContext = ViewModel;

            _viewer = new OpenCvImageViewer
            {
                DisplayToolBar = true,
                DisplayStatusBar = true,
                EnableRoiInteraction = true
            };
            _viewer.SelectedRoiChanged += Viewer_SelectedRoiChanged;
            _viewer.RoiChanged += Viewer_RoiChanged;
            _viewer.RoiEditCompleted += Viewer_RoiEditCompleted;
            ViewModel.Parameters.PropertyChanged += Parameters_PropertyChanged;
            FormsHost.Child = _viewer;

            LoadImage(image);
            LoadInitialRoi();
            RefreshLineDisplay(false);
        }

        /// <summary>
        /// 获取编辑后的直线查找参数。
        /// </summary>
        public LineFindOperatorParameters EditedParameters { get; private set; }

        /// <summary>
        /// 获取编辑窗口最近一次运行状态。
        /// </summary>
        public FlowNodeStatus? EditedStatus { get; private set; }

        /// <summary>
        /// 获取编辑窗口最近一次运行耗时。
        /// </summary>
        public double EditedElapsedMilliseconds { get; private set; }

        /// <summary>
        /// 获取编辑窗口最近一次运行消息。
        /// </summary>
        public string EditedMessage { get; private set; }

        /// <summary>
        /// 获取当前编辑的流程算子。
        /// </summary>
        private FlowOperator FlowOperator { get; }

        /// <summary>
        /// 获取窗口视图模型。
        /// </summary>
        private LineFindOperatorEditorViewModel ViewModel { get; }

        /// <summary>
        /// 克隆算子参数，避免取消编辑时污染原算子。
        /// </summary>
        /// <param name="flowOperator">待编辑的流程算子。</param>
        /// <returns>返回参数副本。</returns>
        private static LineFindOperatorParameters CloneParameters(FlowOperator flowOperator)
        {
            LineFindOperatorParameters parameters = flowOperator == null ? null : flowOperator.Parameters as LineFindOperatorParameters;
            return parameters == null ? new LineFindOperatorParameters() : parameters.Clone();
        }

        /// <summary>
        /// 加载编辑窗口使用的图像。
        /// </summary>
        /// <param name="image">当前输入图像。</param>
        /// <returns>无返回值。</returns>
        private void LoadImage(Mat image)
        {
            if (image == null || image.Empty())
            {
                ViewModel.ImageStatus = "图像：未加载";
                return;
            }

            _viewer.SetImage(image);
            ViewModel.ImageStatus = string.Format("图像：{0} x {1}", image.Width, image.Height);
        }

        /// <summary>
        /// 加载算子保存的 ROI。
        /// </summary>
        /// <returns>无返回值。</returns>
        private void LoadInitialRoi()
        {
            RoiItem roi = ViewModel.Parameters.CreateRoi();
            if (roi != null)
            {
                _viewer.AddRoi(roi);
            }

            ViewModel.SetRoi(GetCurrentLineRoi());
        }

        /// <summary>
        /// 获取当前可用于直线检测的 ROI。
        /// </summary>
        /// <returns>返回可检测 ROI；不存在时返回 null。</returns>
        private RoiItem GetCurrentLineRoi()
        {
            if (_viewer.SelectedRoi != null && _viewer.SelectedRoi.CanDetectLine())
            {
                return _viewer.SelectedRoi;
            }

            return _viewer.Rois.FirstOrDefault(x => x.CanDetectLine());
        }

        /// <summary>
        /// 处理当前 ROI 选中变化。
        /// </summary>
        /// <param name="sender">事件源。</param>
        /// <param name="e">事件参数。</param>
        /// <returns>无返回值。</returns>
        private void Viewer_SelectedRoiChanged(object sender, EventArgs e)
        {
            ViewModel.SetRoi(GetCurrentLineRoi());
            RefreshLineDisplay(false);
        }

        /// <summary>
        /// 处理 ROI 拖动过程中的变化。
        /// </summary>
        /// <param name="sender">事件源。</param>
        /// <param name="e">ROI 事件参数。</param>
        /// <returns>无返回值。</returns>
        private void Viewer_RoiChanged(object sender, RoiEventArgs e)
        {
            ViewModel.SetRoi(GetCurrentLineRoi());
            RefreshLineDisplay(false);
        }

        /// <summary>
        /// 处理 ROI 编辑完成事件。
        /// </summary>
        /// <param name="sender">事件源。</param>
        /// <param name="e">ROI 事件参数。</param>
        /// <returns>无返回值。</returns>
        private void Viewer_RoiEditCompleted(object sender, RoiEventArgs e)
        {
            ViewModel.SetRoi(GetCurrentLineRoi());
            RefreshLineDisplay(true);
        }

        /// <summary>
        /// 处理参数变化并刷新预览。
        /// </summary>
        /// <param name="sender">事件源。</param>
        /// <param name="e">属性变化参数。</param>
        /// <returns>无返回值。</returns>
        private void Parameters_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            RefreshLineDisplay(false);
        }

        /// <summary>
        /// 刷新直线检测预览或执行检测。
        /// </summary>
        /// <param name="runDetection">是否立即执行检测。</param>
        /// <returns>无返回值。</returns>
        private void RefreshLineDisplay(bool runDetection)
        {
            if (_refreshing)
            {
                return;
            }

            _refreshing = true;
            try
            {
                RoiItem roi = GetCurrentLineRoi();
                LineDetectionParams parameters = ViewModel.Parameters.ToLineDetectionParams();
                if (roi == null)
                {
                    _viewer.ClearLineDetectionPreview();
                    _viewer.ClearLineDetectionResult();
                    if (runDetection)
                    {
                        ViewModel.ResultStatus = "结果：NG  请先创建矩形或带角度矩形 ROI。";
                        ViewModel.ResultLine = "-";
                        UpdateEditedRunResult(FlowNodeStatus.NG, 0, "请先创建矩形或带角度矩形 ROI。");
                    }
                    else
                    {
                        ViewModel.SetResult(null);
                    }

                    return;
                }

                _viewer.ShowLineDetectionPreview(roi.ToLineDetectionFrame(), parameters);
                if (!runDetection)
                {
                    return;
                }

                LineDetectionResult result = _viewer.DetectLine(roi, parameters, ViewModel.Parameters.DetectionMode);
                _viewer.ShowLineDetectionResult(result, parameters);
                ViewModel.SetResult(result);
                UpdateEditedRunResult(
                    result.Success ? FlowNodeStatus.OK : FlowNodeStatus.NG,
                    result.Elapsed.TotalMilliseconds,
                    result.Message);
            }
            catch (Exception ex)
            {
                ViewModel.ResultStatus = "结果：NG  " + ex.Message;
                ViewModel.ResultLine = "-";
                UpdateEditedRunResult(FlowNodeStatus.NG, 0, ex.Message);
            }
            finally
            {
                _refreshing = false;
            }
        }

        /// <summary>
        /// 保存编辑窗口最近一次运行结果，供主窗口应用时回写。
        /// </summary>
        /// <param name="status">运行状态。</param>
        /// <param name="elapsedMilliseconds">运行耗时。</param>
        /// <param name="message">运行消息。</param>
        /// <returns>无返回值。</returns>
        private void UpdateEditedRunResult(FlowNodeStatus status, double elapsedMilliseconds, string message)
        {
            EditedStatus = status;
            EditedElapsedMilliseconds = elapsedMilliseconds;
            EditedMessage = string.IsNullOrWhiteSpace(message) ? status.ToString() : message;
        }

        /// <summary>
        /// 处理适应窗口按钮。
        /// </summary>
        /// <param name="sender">事件源。</param>
        /// <param name="e">按钮事件参数。</param>
        /// <returns>无返回值。</returns>
        private void FitImage_Click(object sender, RoutedEventArgs e)
        {
            _viewer.FitImage();
        }

        /// <summary>
        /// 处理创建矩形 ROI 按钮。
        /// </summary>
        /// <param name="sender">事件源。</param>
        /// <param name="e">按钮事件参数。</param>
        /// <returns>无返回值。</returns>
        private void CreateRectangleRoi_Click(object sender, RoutedEventArgs e)
        {
            _viewer.StartCreateRoi(RoiShape.Rectangle);
        }

        /// <summary>
        /// 处理创建旋转矩形 ROI 按钮。
        /// </summary>
        /// <param name="sender">事件源。</param>
        /// <param name="e">按钮事件参数。</param>
        /// <returns>无返回值。</returns>
        private void CreateRotatedRectangleRoi_Click(object sender, RoutedEventArgs e)
        {
            _viewer.StartCreateRoi(RoiShape.RotatedRectangle);
        }

        /// <summary>
        /// 处理清除 ROI 按钮。
        /// </summary>
        /// <param name="sender">事件源。</param>
        /// <param name="e">按钮事件参数。</param>
        /// <returns>无返回值。</returns>
        private void ClearRoi_Click(object sender, RoutedEventArgs e)
        {
            _viewer.ClearRois();
            ViewModel.SetRoi(null);
            RefreshLineDisplay(false);
        }

        /// <summary>
        /// 处理预览按钮。
        /// </summary>
        /// <param name="sender">事件源。</param>
        /// <param name="e">按钮事件参数。</param>
        /// <returns>无返回值。</returns>
        private void Preview_Click(object sender, RoutedEventArgs e)
        {
            RefreshLineDisplay(false);
        }

        /// <summary>
        /// 处理运行一次按钮。
        /// </summary>
        /// <param name="sender">事件源。</param>
        /// <param name="e">按钮事件参数。</param>
        /// <returns>无返回值。</returns>
        private void RunOnce_Click(object sender, RoutedEventArgs e)
        {
            RefreshLineDisplay(true);
        }

        /// <summary>
        /// 处理应用按钮。
        /// </summary>
        /// <param name="sender">事件源。</param>
        /// <param name="e">按钮事件参数。</param>
        /// <returns>无返回值。</returns>
        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.Parameters.SaveRoi(GetCurrentLineRoi());
            EditedParameters = ViewModel.Parameters.Clone();
            DialogResult = true;
        }

        /// <summary>
        /// 处理取消按钮。
        /// </summary>
        /// <param name="sender">事件源。</param>
        /// <param name="e">按钮事件参数。</param>
        /// <returns>无返回值。</returns>
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        /// <summary>
        /// 释放窗口中的图像控件资源。
        /// </summary>
        /// <param name="e">关闭事件参数。</param>
        /// <returns>无返回值。</returns>
        protected override void OnClosed(EventArgs e)
        {
            ViewModel.Parameters.PropertyChanged -= Parameters_PropertyChanged;
            _viewer.SelectedRoiChanged -= Viewer_SelectedRoiChanged;
            _viewer.RoiChanged -= Viewer_RoiChanged;
            _viewer.RoiEditCompleted -= Viewer_RoiEditCompleted;
            _viewer.Dispose();
            base.OnClosed(e);
        }
    }
}
