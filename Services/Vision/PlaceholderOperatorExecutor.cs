using System.Threading;

namespace MyFlowChart.Services.Vision
{
    /// <summary>
    /// 用于验证算子扩展链路的占位检测算子。
    /// </summary>
    public sealed class PlaceholderOperatorExecutor : IVisionOperatorExecutor
    {
        /// <summary>
        /// 执行占位检测算子。
        /// </summary>
        /// <param name="workItem">本次运行的算子快照。</param>
        /// <param name="context">运行上下文。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>执行结果。</returns>
        public VisionOperatorResult Execute(VisionOperatorWorkItem workItem, VisionRunContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return VisionOperatorResult.Ok("占位检测完成。");
        }
    }
}
