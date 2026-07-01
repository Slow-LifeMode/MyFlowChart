using System;
using MyFlowChart.Models;

namespace MyFlowChart.Services.Vision
{
    /// <summary>
    /// 表示单个视觉算子的运行快照。
    /// </summary>
    public sealed class VisionOperatorRunRecord
    {
        /// <summary>
        /// 初始化单个视觉算子的运行快照。
        /// </summary>
        /// <param name="operatorId">流程算子标识。</param>
        /// <param name="status">运行状态。</param>
        /// <param name="elapsedMilliseconds">运行耗时，单位为毫秒。</param>
        /// <param name="message">运行消息。</param>
        /// <param name="payload">运行结果数据。</param>
        /// <returns>无返回值。</returns>
        public VisionOperatorRunRecord(Guid operatorId, FlowNodeStatus status, double elapsedMilliseconds, string message, object payload)
        {
            OperatorId = operatorId;
            Status = status;
            ElapsedMilliseconds = elapsedMilliseconds;
            Message = message ?? string.Empty;
            Payload = payload;
        }

        /// <summary>
        /// 获取流程算子标识。
        /// </summary>
        public Guid OperatorId { get; private set; }

        /// <summary>
        /// 获取运行状态。
        /// </summary>
        public FlowNodeStatus Status { get; private set; }

        /// <summary>
        /// 获取运行耗时，单位为毫秒。
        /// </summary>
        public double ElapsedMilliseconds { get; private set; }

        /// <summary>
        /// 获取运行消息。
        /// </summary>
        public string Message { get; private set; }

        /// <summary>
        /// 获取运行结果数据。
        /// </summary>
        public object Payload { get; private set; }
    }
}
