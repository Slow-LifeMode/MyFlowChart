using System.Threading;
using OpenCvWindowTool;

namespace MyFlowChart.Services.Vision
{
    /// <summary>
    /// 输出当前视觉流程结果。
    /// </summary>
    public sealed class ResultOutputOperatorExecutor : IVisionOperatorExecutor
    {
        /// <summary>
        /// 执行结果输出算子。
        /// </summary>
        /// <param name="workItem">本次运行的算子快照。</param>
        /// <param name="context">运行上下文。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>执行结果。</returns>
        public VisionOperatorResult Execute(VisionOperatorWorkItem workItem, VisionRunContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (context == null || !context.Items.ContainsKey(VisionDataKeys.LineDetectionResult))
            {
                return VisionOperatorResult.Fail("没有可输出的检测结果。");
            }

            object result = context.Items[VisionDataKeys.LineDetectionResult];
            LineDetectionResult lineResult = result as LineDetectionResult;
            if (lineResult != null && !lineResult.Success)
            {
                return VisionOperatorResult.Fail(lineResult.Message);
            }

            return VisionOperatorResult.Ok("结果已输出。", result);
        }
    }
}
