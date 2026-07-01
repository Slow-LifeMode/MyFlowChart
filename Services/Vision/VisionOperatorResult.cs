using System;

namespace MyFlowChart.Services.Vision
{
    /// <summary>
    /// 表示视觉算子的执行结果。
    /// </summary>
    public sealed class VisionOperatorResult
    {
        /// <summary>
        /// 初始化视觉算子执行结果。
        /// </summary>
        /// <param name="success">是否执行成功。</param>
        /// <param name="message">执行消息。</param>
        /// <param name="payload">结果数据。</param>
        private VisionOperatorResult(bool success, string message, object payload)
        {
            Success = success;
            Message = message ?? string.Empty;
            Payload = payload;
        }

        /// <summary>
        /// 获取是否执行成功。
        /// </summary>
        public bool Success { get; private set; }

        /// <summary>
        /// 获取执行消息。
        /// </summary>
        public string Message { get; private set; }

        /// <summary>
        /// 获取结果数据。
        /// </summary>
        public object Payload { get; private set; }

        /// <summary>
        /// 创建成功结果。
        /// </summary>
        /// <param name="message">执行消息。</param>
        /// <param name="payload">结果数据。</param>
        /// <returns>成功结果。</returns>
        public static VisionOperatorResult Ok(string message, object payload = null)
        {
            return new VisionOperatorResult(true, message, payload);
        }

        /// <summary>
        /// 创建失败结果。
        /// </summary>
        /// <param name="message">失败消息。</param>
        /// <returns>失败结果。</returns>
        public static VisionOperatorResult Fail(string message)
        {
            return new VisionOperatorResult(false, message, null);
        }

        /// <summary>
        /// 创建异常失败结果。
        /// </summary>
        /// <param name="exception">执行异常。</param>
        /// <returns>失败结果。</returns>
        public static VisionOperatorResult FromException(Exception exception)
        {
            return Fail(exception == null ? "视觉流程执行失败。" : exception.Message);
        }
    }
}
