using System.Windows.Input;
using MyFlowChart.Models;

namespace MyFlowChart.ViewModels
{
    /// <summary>
    /// 图像采集算子编辑窗口的视图模型。
    /// </summary>
    public sealed class ImageInputOperatorEditorViewModel : BindableBase
    {
        private ImageInputEditorModule _currentModule;
        private string _imageStatus;
        private string _resultStatus;

        /// <summary>
        /// 初始化图像采集算子编辑视图模型。
        /// </summary>
        /// <param name="operatorName">算子显示名称。</param>
        /// <param name="parameters">当前算子参数副本。</param>
        /// <returns>无返回值。</returns>
        public ImageInputOperatorEditorViewModel(string operatorName, ImageInputOperatorParameters parameters)
        {
            OperatorName = string.IsNullOrWhiteSpace(operatorName) ? OperatorDefinition.ImageInputName : operatorName;
            Parameters = parameters ?? new ImageInputOperatorParameters();
            CurrentModule = ImageInputEditorModule.Input;
            ImageStatus = "图像：未加载";
            ResultStatus = "结果：未运行";

            ShowInputCommand = new RelayCommand(_ => CurrentModule = ImageInputEditorModule.Input, null);
            ShowParamsCommand = new RelayCommand(_ => CurrentModule = ImageInputEditorModule.Params, null);
            ShowResultCommand = new RelayCommand(_ => CurrentModule = ImageInputEditorModule.Result, null);
        }

        /// <summary>
        /// 获取算子显示名称。
        /// </summary>
        public string OperatorName { get; private set; }

        /// <summary>
        /// 获取编辑中的图像采集参数。
        /// </summary>
        public ImageInputOperatorParameters Parameters { get; private set; }

        /// <summary>
        /// 获取切换到输入页的命令。
        /// </summary>
        public ICommand ShowInputCommand { get; private set; }

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
        public ImageInputEditorModule CurrentModule
        {
            get { return _currentModule; }
            set
            {
                if (!Set(ref _currentModule, value))
                {
                    return;
                }

                OnPropertyChanged(nameof(IsInputModule));
                OnPropertyChanged(nameof(IsParamsModule));
                OnPropertyChanged(nameof(IsResultModule));
            }
        }

        /// <summary>
        /// 获取当前是否显示输入页。
        /// </summary>
        public bool IsInputModule => CurrentModule == ImageInputEditorModule.Input;

        /// <summary>
        /// 获取当前是否显示参数页。
        /// </summary>
        public bool IsParamsModule => CurrentModule == ImageInputEditorModule.Params;

        /// <summary>
        /// 获取当前是否显示结果页。
        /// </summary>
        public bool IsResultModule => CurrentModule == ImageInputEditorModule.Result;

        /// <summary>
        /// 获取或设置图像状态文本。
        /// </summary>
        public string ImageStatus
        {
            get { return _imageStatus; }
            set { Set(ref _imageStatus, value); }
        }

        /// <summary>
        /// 获取或设置运行结果文本。
        /// </summary>
        public string ResultStatus
        {
            get { return _resultStatus; }
            set { Set(ref _resultStatus, value); }
        }
    }

    /// <summary>
    /// 图像采集编辑窗口的标准模块。
    /// </summary>
    public enum ImageInputEditorModule
    {
        Input,
        Params,
        Result
    }
}
