using System;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using MyFlowChart.Models;
using MyFlowChart.ViewModels;
using OpenCvSharp;
using OpenCvWindowTool;

namespace MyFlowChart.Views
{
    /// <summary>
    /// 图像采集算子的标准编辑窗口。
    /// </summary>
    public partial class ImageInputOperatorEditorWindow : System.Windows.Window
    {
        private readonly OpenCvImageViewer _viewer;

        /// <summary>
        /// 初始化图像采集算子编辑窗口。
        /// </summary>
        /// <param name="flowOperator">待编辑的流程算子。</param>
        /// <returns>无返回值。</returns>
        public ImageInputOperatorEditorWindow(FlowOperator flowOperator)
        {
            InitializeComponent();

            ViewModel = new ImageInputOperatorEditorViewModel(
                flowOperator == null ? OperatorDefinition.ImageInputName : flowOperator.DisplayName,
                CloneParameters(flowOperator));
            DataContext = ViewModel;

            _viewer = new OpenCvImageViewer
            {
                DisplayToolBar = true,
                DisplayStatusBar = true,
                EnableRoiInteraction = false
            };
            FormsHost.Child = _viewer;
            LoadPreview();
        }

        /// <summary>
        /// 获取编辑后的图像采集参数。
        /// </summary>
        public ImageInputOperatorParameters EditedParameters { get; private set; }

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
        /// 获取窗口视图模型。
        /// </summary>
        private ImageInputOperatorEditorViewModel ViewModel { get; }

        /// <summary>
        /// 克隆算子参数，避免取消编辑时污染原算子。
        /// </summary>
        /// <param name="flowOperator">待编辑的流程算子。</param>
        /// <returns>返回参数副本。</returns>
        private static ImageInputOperatorParameters CloneParameters(FlowOperator flowOperator)
        {
            ImageInputOperatorParameters parameters = flowOperator == null ? null : flowOperator.Parameters as ImageInputOperatorParameters;
            return parameters == null ? new ImageInputOperatorParameters() : parameters.Clone();
        }

        /// <summary>
        /// 处理选择图像按钮。
        /// </summary>
        /// <param name="sender">事件源。</param>
        /// <param name="e">按钮事件参数。</param>
        /// <returns>无返回值。</returns>
        private void SelectImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Filter = "图像文件|*.bmp;*.jpg;*.jpeg;*.png;*.tif;*.tiff|所有文件|*.*"
            };

            if (dialog.ShowDialog(this) != true)
            {
                return;
            }

            ViewModel.Parameters.ImagePath = dialog.FileName;
            LoadPreview();
        }

        /// <summary>
        /// 加载当前路径的图像预览。
        /// </summary>
        /// <returns>加载成功返回 true，否则返回 false。</returns>
        private bool LoadPreview()
        {
            string imagePath = ViewModel.Parameters.ImagePath;
            if (string.IsNullOrWhiteSpace(imagePath))
            {
                _viewer.ClearImage();
                ViewModel.ImageStatus = "图像：未选择";
                return false;
            }

            if (!File.Exists(imagePath))
            {
                _viewer.ClearImage();
                ViewModel.ImageStatus = "图像：文件不存在";
                return false;
            }

            try
            {
                _viewer.LoadImage(imagePath);
                Mat image = _viewer.ImageMat;
                ViewModel.ImageStatus = image == null || image.Empty()
                    ? "图像：加载失败"
                    : string.Format("图像：{0} x {1}", image.Width, image.Height);
                return image != null && !image.Empty();
            }
            catch (Exception ex)
            {
                _viewer.ClearImage();
                ViewModel.ImageStatus = "图像：加载失败  " + ex.Message;
                return false;
            }
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
        /// 处理预览按钮。
        /// </summary>
        /// <param name="sender">事件源。</param>
        /// <param name="e">按钮事件参数。</param>
        /// <returns>无返回值。</returns>
        private void Preview_Click(object sender, RoutedEventArgs e)
        {
            LoadPreview();
        }

        /// <summary>
        /// 处理运行一次按钮。
        /// </summary>
        /// <param name="sender">事件源。</param>
        /// <param name="e">按钮事件参数。</param>
        /// <returns>无返回值。</returns>
        private void RunOnce_Click(object sender, RoutedEventArgs e)
        {
            bool loaded = LoadPreview();
            ViewModel.ResultStatus = loaded ? "结果：OK  图像采集完成。" : "结果：NG  图像采集失败。";
            EditedStatus = loaded ? FlowNodeStatus.OK : FlowNodeStatus.NG;
            EditedElapsedMilliseconds = 0;
            EditedMessage = loaded ? "图像采集完成。" : "图像采集失败。";
        }

        /// <summary>
        /// 处理应用按钮。
        /// </summary>
        /// <param name="sender">事件源。</param>
        /// <param name="e">按钮事件参数。</param>
        /// <returns>无返回值。</returns>
        private void Apply_Click(object sender, RoutedEventArgs e)
        {
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
            _viewer.Dispose();
            base.OnClosed(e);
        }
    }
}
