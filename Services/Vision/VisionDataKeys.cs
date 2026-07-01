namespace MyFlowChart.Services.Vision
{
    /// <summary>
    /// 定义视觉流程运行上下文中的共享数据键。
    /// </summary>
    public static class VisionDataKeys
    {
        /// <summary>
        /// 当前流程中可供下游算子使用的图像令牌。
        /// </summary>
        public const string CurrentImage = "CurrentImage";

        /// <summary>
        /// 当前流程中可供直线检测使用的 ROI。
        /// </summary>
        public const string LineDetectionRoi = "LineDetectionRoi";

        /// <summary>
        /// 当前流程中最近一次直线检测结果。
        /// </summary>
        public const string LineDetectionResult = "LineDetectionResult";
    }
}
