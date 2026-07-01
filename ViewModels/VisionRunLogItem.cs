using MyFlowChart.Models;

namespace MyFlowChart.ViewModels
{
    /// <summary>
    /// 表示本次视觉运行中的一条算子日志。
    /// </summary>
    public sealed class VisionRunLogItem
    {
        /// <summary>
        /// 初始化视觉运行日志项。
        /// </summary>
        /// <param name="sequence">算子序号。</param>
        /// <param name="operatorName">算子显示名称。</param>
        /// <param name="status">算子运行状态。</param>
        /// <param name="elapsedMilliseconds">算子耗时，单位毫秒。</param>
        /// <param name="message">算子运行消息。</param>
        /// <param name="payload">算子运行结果数据。</param>
        public VisionRunLogItem(int sequence, string operatorName, FlowNodeStatus status, double elapsedMilliseconds, string message, object payload)
        {
            Sequence = sequence;
            OperatorName = operatorName ?? string.Empty;
            Status = status;
            ElapsedMilliseconds = elapsedMilliseconds;
            Message = message ?? string.Empty;
            Payload = payload;
        }

        public int Sequence { get; private set; }

        public string OperatorName { get; private set; }

        public FlowNodeStatus Status { get; private set; }

        public double ElapsedMilliseconds { get; private set; }

        public string Message { get; private set; }

        public object Payload { get; private set; }

        public string StatusText
        {
            get
            {
                switch (Status)
                {
                    case FlowNodeStatus.Running:
                        return "running";
                    case FlowNodeStatus.OK:
                        return "OK";
                    case FlowNodeStatus.NG:
                        return "NG";
                    case FlowNodeStatus.Stopped:
                        return "stopped";
                    default:
                        return "not run";
                }
            }
        }
    }
}
