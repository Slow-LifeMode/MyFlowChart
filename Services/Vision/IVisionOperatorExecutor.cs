using System.Threading;

namespace MyFlowChart.Services.Vision
{
    /// <summary>
    /// 定义视觉算子执行器。
    /// </summary>
    public interface IVisionOperatorExecutor
    {
        /// <summary>
        /// 执行视觉算子。
        /// </summary>
        /// <param name="workItem">本次运行的算子快照。</param>
        /// <param name="context">本次运行上下文。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>算子执行结果。</returns>
        VisionOperatorResult Execute(VisionOperatorWorkItem workItem, VisionRunContext context, CancellationToken cancellationToken);
    }
}
