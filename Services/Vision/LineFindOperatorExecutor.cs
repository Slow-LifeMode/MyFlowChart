using System.Threading;
using MyFlowChart.Models;
using OpenCvWindowTool;

namespace MyFlowChart.Services.Vision
{
    /// <summary>
    /// 使用 OpenCvWindowTool 原始直线检测算子执行直线查找。
    /// </summary>
    public sealed class LineFindOperatorExecutor : IVisionOperatorExecutor
    {
        private readonly LineDetectionOperator _lineDetectionOperator = new LineDetectionOperator();
        private readonly OptLineDetectionOperator _optLineDetectionOperator = new OptLineDetectionOperator();

        /// <summary>
        /// 执行直线查找算子。
        /// </summary>
        /// <param name="workItem">本次运行的算子快照。</param>
        /// <param name="context">运行上下文。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>执行结果。</returns>
        public VisionOperatorResult Execute(VisionOperatorWorkItem workItem, VisionRunContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (context == null || context.LineContext == null)
            {
                return VisionOperatorResult.Fail("直线检测图像上下文为空。");
            }

            object imageValue;
            ImageFrameToken inputImage = context.Items.TryGetValue(VisionDataKeys.CurrentImage, out imageValue) ? imageValue as ImageFrameToken : null;
            if (inputImage == null || inputImage.Image.Empty())
            {
                return VisionOperatorResult.Fail("直线查找未接收到图像输入。");
            }

            RoiItem roi = ResolveRoi(context, workItem);
            if (roi == null)
            {
                return VisionOperatorResult.Fail("请先创建矩形或带角度矩形 ROI。");
            }

            LineDetectionParams parameters = ResolveParameters(workItem);
            LineDetectionResult result = ResolveDetectionMode(workItem) == LineDetectionMode.OPTMode
                ? _optLineDetectionOperator.Detect(context.LineContext, roi, parameters)
                : _lineDetectionOperator.Detect(context.LineContext, roi, parameters);
            context.Items[VisionDataKeys.LineDetectionResult] = result;
            return result.Success
                ? VisionOperatorResult.Ok(result.Message, result)
                : VisionOperatorResult.Fail(result.Message);
        }

        /// <summary>
        /// 从运行快照读取直线检测参数。
        /// </summary>
        /// <param name="workItem">本次运行的算子快照。</param>
        /// <returns>返回直线检测参数；未配置时返回默认参数。</returns>
        private static LineDetectionParams ResolveParameters(VisionOperatorWorkItem workItem)
        {
            LineFindOperatorParameters parameters = workItem == null ? null : workItem.Parameters as LineFindOperatorParameters;
            return parameters == null ? new LineDetectionParams() : parameters.ToLineDetectionParams();
        }

        /// <summary>
        /// 从运行快照读取直线检测运行模式。
        /// </summary>
        /// <param name="workItem">本次运行的算子快照。</param>
        /// <returns>返回直线检测运行模式；未配置时使用示例默认模式。</returns>
        private static LineDetectionMode ResolveDetectionMode(VisionOperatorWorkItem workItem)
        {
            LineFindOperatorParameters parameters = workItem == null ? null : workItem.Parameters as LineFindOperatorParameters;
            return parameters == null ? LineDetectionMode.SelfMode : parameters.DetectionMode;
        }

        /// <summary>
        /// 从当前算子参数中获取可用于直线检测的 ROI。
        /// </summary>
        /// <param name="context">运行上下文。</param>
        /// <param name="workItem">本次运行的算子快照。</param>
        /// <returns>可检测直线的 ROI；未找到时返回 null。</returns>
        private static RoiItem ResolveRoi(VisionRunContext context, VisionOperatorWorkItem workItem)
        {
            LineFindOperatorParameters parameters = workItem == null ? null : workItem.Parameters as LineFindOperatorParameters;
            RoiItem parameterRoi = parameters == null ? null : parameters.CreateRoi();
            return parameterRoi != null && parameterRoi.CanDetectLine() ? parameterRoi : null;
        }
    }
}
