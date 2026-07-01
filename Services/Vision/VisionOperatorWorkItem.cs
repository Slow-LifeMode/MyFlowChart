using System;
using MyFlowChart.Models;

namespace MyFlowChart.Services.Vision
{
    /// <summary>
    /// 表示一次后台运行使用的算子快照。
    /// </summary>
    public sealed class VisionOperatorWorkItem
    {
        /// <summary>
        /// 初始化后台运行算子快照。
        /// </summary>
        /// <param name="operatorId">源算子标识。</param>
        /// <param name="operatorName">算子名称。</param>
        /// <param name="parameters">算子参数副本。</param>
        public VisionOperatorWorkItem(Guid operatorId, string operatorName, object parameters)
        {
            OperatorId = operatorId;
            OperatorName = operatorName;
            Parameters = parameters;
        }

        public Guid OperatorId { get; private set; }

        public string OperatorName { get; private set; }

        public object Parameters { get; private set; }

        /// <summary>
        /// 从 WPF 绑定算子创建后台运行快照。
        /// </summary>
        /// <param name="flowOperator">源流程算子。</param>
        /// <returns>返回后台运行快照；源算子为空时返回 null。</returns>
        public static VisionOperatorWorkItem FromFlowOperator(FlowOperator flowOperator)
        {
            if (flowOperator == null)
            {
                return null;
            }

            return new VisionOperatorWorkItem(
                flowOperator.Id,
                flowOperator.OperatorName,
                flowOperator.CloneParameters());
        }
    }
}
