using System.Threading;
using MyFlowChart.Models;
using OpenCvSharp;

namespace MyFlowChart.Services.Vision
{
    /// <summary>
    /// 校验当前流程是否已经具备可运行图像。
    /// </summary>
    public sealed class ImageInputOperatorExecutor : IVisionOperatorExecutor
    {
        /// <summary>
        /// 执行图像输入算子。
        /// </summary>
        /// <param name="workItem">本次运行的算子快照。</param>
        /// <param name="context">运行上下文。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>执行结果。</returns>
        public VisionOperatorResult Execute(VisionOperatorWorkItem workItem, VisionRunContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ImageInputOperatorParameters parameters = workItem == null ? null : workItem.Parameters as ImageInputOperatorParameters;
            if (context == null)
            {
                return VisionOperatorResult.Fail("图像采集运行上下文为空。");
            }

            if (parameters == null || string.IsNullOrWhiteSpace(parameters.ImagePath))
            {
                return VisionOperatorResult.Fail("图像采集未配置图像路径。");
            }

            Mat image = Cv2.ImRead(parameters.ImagePath, ImreadModes.Unchanged);
            if (image == null || image.Empty())
            {
                image?.Dispose();
                return VisionOperatorResult.Fail("图像采集加载图像失败。");
            }

            ImageFrameToken token = ImageFrameToken.FromOwnedMat(image, context.Image == null ? 0 : context.Image.FrameNumber);
            if (token == null)
            {
                image.Dispose();
                return VisionOperatorResult.Fail("图像采集加载图像失败。");
            }

            context.SetCurrentImage(token);
            return VisionOperatorResult.Ok(string.Format("图像已加载：{0} x {1}", token.Width, token.Height));
        }
    }
}
