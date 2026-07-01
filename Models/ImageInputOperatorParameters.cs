namespace MyFlowChart.Models
{
    /// <summary>
    /// 图像采集算子的可绑定参数。
    /// </summary>
    public sealed class ImageInputOperatorParameters : BindableBase
    {
        private string _imagePath;

        /// <summary>
        /// 获取或设置图像文件路径。
        /// </summary>
        public string ImagePath
        {
            get { return _imagePath; }
            set { Set(ref _imagePath, value); }
        }

        /// <summary>
        /// 创建当前参数的独立副本。
        /// </summary>
        /// <returns>返回新的图像采集参数实例。</returns>
        public ImageInputOperatorParameters Clone()
        {
            return new ImageInputOperatorParameters
            {
                ImagePath = ImagePath
            };
        }
    }
}
