using System.Globalization;
using System.Windows.Input;
using MyFlowChart.Models;
using OpenCvWindowTool;

namespace MyFlowChart.ViewModels
{
    /// <summary>
    /// 表示下拉框显示文本和实际值的对应关系。
    /// </summary>
    /// <typeparam name="T">实际值类型。</typeparam>
    public sealed class ComboOption<T>
    {
        /// <summary>
        /// 初始化下拉框选项。
        /// </summary>
        /// <param name="text">显示文本。</param>
        /// <param name="value">实际值。</param>
        /// <returns>无返回值。</returns>
        public ComboOption(string text, T value)
        {
            Text = text;
            Value = value;
        }

        /// <summary>
        /// 获取显示文本。
        /// </summary>
        public string Text { get; private set; }

        /// <summary>
        /// 获取实际值。
        /// </summary>
        public T Value { get; private set; }
    }

    /// <summary>
    /// 直线查找算子编辑窗口的视图模型。
    /// </summary>
    public sealed class LineFindOperatorEditorViewModel : BindableBase
    {
        private OperatorEditorModule _currentModule;
        private string _imageStatus;
        private string _roiStatus;
        private string _resultStatus;
        private string _resultLine;

        /// <summary>
        /// 初始化直线查找算子编辑视图模型。
        /// </summary>
        /// <param name="operatorName">算子显示名称。</param>
        /// <param name="parameters">当前算子参数副本。</param>
        /// <returns>无返回值。</returns>
        public LineFindOperatorEditorViewModel(string operatorName, LineFindOperatorParameters parameters)
        {
            OperatorName = string.IsNullOrWhiteSpace(operatorName) ? OperatorDefinition.LineFindName : operatorName;
            Parameters = parameters ?? new LineFindOperatorParameters();
            CurrentModule = OperatorEditorModule.Input;
            ImageStatus = "图像：未加载";
            RoiStatus = "ROI：未配置";
            ResultStatus = "结果：未运行";
            ResultLine = "-";

            DetectionModes = new[]
            {
                new ComboOption<LineDetectionMode>("SelfMode", LineDetectionMode.SelfMode),
                new ComboOption<LineDetectionMode>("OPTMode", LineDetectionMode.OPTMode)
            };
            ScanDirections = new[]
            {
                new ComboOption<LineScanDirection>("从左到右", LineScanDirection.LeftToRight),
                new ComboOption<LineScanDirection>("从上到下", LineScanDirection.TopToBottom),
                new ComboOption<LineScanDirection>("从下到上", LineScanDirection.BottomToTop),
                new ComboOption<LineScanDirection>("从右到左", LineScanDirection.RightToLeft)
            };
            EdgePolarities = new[]
            {
                new ComboOption<LineEdgePolarity>("从黑到白", LineEdgePolarity.Positive),
                new ComboOption<LineEdgePolarity>("从白到黑", LineEdgePolarity.Negative),
                new ComboOption<LineEdgePolarity>("全部", LineEdgePolarity.Any)
            };
            SelectionModes = new[]
            {
                new ComboOption<LineSelectionMode>("第一条", LineSelectionMode.First),
                new ComboOption<LineSelectionMode>("最后一条", LineSelectionMode.Last),
                new ComboOption<LineSelectionMode>("最强边缘", LineSelectionMode.Strongest)
            };
            FitModes = new[]
            {
                new ComboOption<LineFitMode>("局部拟合", LineFitMode.Local),
                new ComboOption<LineFitMode>("最小二乘拟合", LineFitMode.LeastSquares),
                new ComboOption<LineFitMode>("Huber拟合", LineFitMode.Huber)
            };

            ShowInputCommand = new RelayCommand(_ => SetModule(OperatorEditorModule.Input), null);
            ShowRoiCommand = new RelayCommand(_ => SetModule(OperatorEditorModule.Roi), null);
            ShowParamsCommand = new RelayCommand(_ => SetModule(OperatorEditorModule.Params), null);
            ShowResultCommand = new RelayCommand(_ => SetModule(OperatorEditorModule.Result), null);
        }

        /// <summary>
        /// 获取算子显示名称。
        /// </summary>
        public string OperatorName { get; private set; }

        /// <summary>
        /// 获取编辑中的直线查找参数。
        /// </summary>
        public LineFindOperatorParameters Parameters { get; private set; }

        /// <summary>
        /// 获取直线检测方式选项。
        /// </summary>
        public ComboOption<LineDetectionMode>[] DetectionModes { get; private set; }

        /// <summary>
        /// 获取扫描方向选项。
        /// </summary>
        public ComboOption<LineScanDirection>[] ScanDirections { get; private set; }

        /// <summary>
        /// 获取边缘极性选项。
        /// </summary>
        public ComboOption<LineEdgePolarity>[] EdgePolarities { get; private set; }

        /// <summary>
        /// 获取边缘选择选项。
        /// </summary>
        public ComboOption<LineSelectionMode>[] SelectionModes { get; private set; }

        /// <summary>
        /// 获取直线拟合选项。
        /// </summary>
        public ComboOption<LineFitMode>[] FitModes { get; private set; }

        /// <summary>
        /// 获取切换到输入页的命令。
        /// </summary>
        public ICommand ShowInputCommand { get; private set; }

        /// <summary>
        /// 获取切换到 ROI 页的命令。
        /// </summary>
        public ICommand ShowRoiCommand { get; private set; }

        /// <summary>
        /// 获取切换到参数页的命令。
        /// </summary>
        public ICommand ShowParamsCommand { get; private set; }

        /// <summary>
        /// 获取切换到结果页的命令。
        /// </summary>
        public ICommand ShowResultCommand { get; private set; }

        /// <summary>
        /// 获取或设置当前编辑模块。
        /// </summary>
        public OperatorEditorModule CurrentModule
        {
            get { return _currentModule; }
            private set
            {
                if (!Set(ref _currentModule, value))
                {
                    return;
                }

                OnPropertyChanged(nameof(IsInputModule));
                OnPropertyChanged(nameof(IsRoiModule));
                OnPropertyChanged(nameof(IsParamsModule));
                OnPropertyChanged(nameof(IsResultModule));
            }
        }

        /// <summary>
        /// 获取当前是否显示输入页。
        /// </summary>
        public bool IsInputModule => CurrentModule == OperatorEditorModule.Input;

        /// <summary>
        /// 获取当前是否显示 ROI 页。
        /// </summary>
        public bool IsRoiModule => CurrentModule == OperatorEditorModule.Roi;

        /// <summary>
        /// 获取当前是否显示参数页。
        /// </summary>
        public bool IsParamsModule => CurrentModule == OperatorEditorModule.Params;

        /// <summary>
        /// 获取当前是否显示结果页。
        /// </summary>
        public bool IsResultModule => CurrentModule == OperatorEditorModule.Result;

        /// <summary>
        /// 获取或设置图像状态文本。
        /// </summary>
        public string ImageStatus
        {
            get { return _imageStatus; }
            set { Set(ref _imageStatus, value); }
        }

        /// <summary>
        /// 获取或设置 ROI 状态文本。
        /// </summary>
        public string RoiStatus
        {
            get { return _roiStatus; }
            set { Set(ref _roiStatus, value); }
        }

        /// <summary>
        /// 获取或设置检测状态文本。
        /// </summary>
        public string ResultStatus
        {
            get { return _resultStatus; }
            set { Set(ref _resultStatus, value); }
        }

        /// <summary>
        /// 获取或设置检测直线文本。
        /// </summary>
        public string ResultLine
        {
            get { return _resultLine; }
            set { Set(ref _resultLine, value); }
        }

        /// <summary>
        /// 设置当前显示模块。
        /// </summary>
        /// <param name="module">目标模块。</param>
        /// <returns>无返回值。</returns>
        private void SetModule(OperatorEditorModule module)
        {
            CurrentModule = module;
        }

        /// <summary>
        /// 按 ROI 更新界面摘要。
        /// </summary>
        /// <param name="roi">当前 ROI。</param>
        /// <returns>无返回值。</returns>
        public void SetRoi(RoiItem roi)
        {
            if (roi == null)
            {
                RoiStatus = "ROI：未配置";
                return;
            }

            RoiStatus = string.Format(
                CultureInfo.InvariantCulture,
                "ROI：{0}  X={1:0.0}, Y={2:0.0}, W={3:0.0}, H={4:0.0}, A={5:0.0}",
                roi.Shape,
                roi.Center.X,
                roi.Center.Y,
                roi.Width,
                roi.Height,
                roi.Angle);
        }

        /// <summary>
        /// 按检测结果更新界面摘要。
        /// </summary>
        /// <param name="result">直线检测结果。</param>
        /// <returns>无返回值。</returns>
        public void SetResult(LineDetectionResult result)
        {
            if (result == null)
            {
                ResultStatus = "结果：未运行";
                ResultLine = "-";
                return;
            }

            ResultStatus = result.Success ? "结果：OK  " + result.Message : "结果：NG  " + result.Message;
            ResultLine = result.Success
                ? string.Format(CultureInfo.InvariantCulture, "角度={0:0.00}  强度={1:0.0}  耗时={2:0.000}ms", result.Angle, result.AverageStrength, result.Elapsed.TotalMilliseconds)
                : string.Format(CultureInfo.InvariantCulture, "边缘点={0}  耗时={1:0.000}ms", result.EdgePoints.Count, result.Elapsed.TotalMilliseconds);
        }
    }

    /// <summary>
    /// 算子编辑窗口的标准模块。
    /// </summary>
    public enum OperatorEditorModule
    {
        Input,
        Roi,
        Params,
        Result
    }
}
