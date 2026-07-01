using System;
using OpenCvSharp;

namespace MyFlowChart.Services.Vision
{
    /// <summary>
    /// 表示流程中传递的一帧图像令牌。
    /// </summary>
    public sealed class ImageFrameToken : IDisposable
    {
        private readonly bool _ownsImage;
        private Mat _image;
        private bool _disposed;

        /// <summary>
        /// 初始化图像帧令牌。
        /// </summary>
        /// <param name="image">底层 OpenCV 图像。</param>
        /// <param name="frameNumber">帧序号。</param>
        /// <param name="ownsImage">是否由令牌释放图像。</param>
        private ImageFrameToken(Mat image, long frameNumber, bool ownsImage)
        {
            _image = image ?? throw new ArgumentNullException(nameof(image));
            _ownsImage = ownsImage;
            FrameId = Guid.NewGuid();
            FrameNumber = frameNumber;
            NativePtr = image.CvPtr;
            Width = image.Width;
            Height = image.Height;
            Channels = image.Channels();
            IsReadOnly = true;
        }

        /// <summary>
        /// 获取帧标识。
        /// </summary>
        public Guid FrameId { get; private set; }

        /// <summary>
        /// 获取帧序号。
        /// </summary>
        public long FrameNumber { get; private set; }

        /// <summary>
        /// 获取底层 OpenCV Mat 指针。
        /// </summary>
        public IntPtr NativePtr { get; private set; }

        /// <summary>
        /// 获取图像宽度。
        /// </summary>
        public int Width { get; private set; }

        /// <summary>
        /// 获取图像高度。
        /// </summary>
        public int Height { get; private set; }

        /// <summary>
        /// 获取图像通道数。
        /// </summary>
        public int Channels { get; private set; }

        /// <summary>
        /// 获取令牌是否按只读图像传递。
        /// </summary>
        public bool IsReadOnly { get; private set; }

        /// <summary>
        /// 获取底层 OpenCV 图像引用。
        /// </summary>
        public Mat Image
        {
            get
            {
                ThrowIfDisposed();
                return _image;
            }
        }

        /// <summary>
        /// 从外部持有的 Mat 创建借用令牌。
        /// </summary>
        /// <param name="image">外部持有的 OpenCV 图像。</param>
        /// <param name="frameNumber">帧序号。</param>
        /// <returns>图像有效时返回帧令牌，否则返回 null。</returns>
        public static ImageFrameToken FromBorrowedMat(Mat image, long frameNumber)
        {
            return image == null || image.Empty()
                ? null
                : new ImageFrameToken(image, frameNumber, false);
        }

        /// <summary>
        /// 从当前流程持有的 Mat 创建自有令牌。
        /// </summary>
        /// <param name="image">需要由令牌释放的 OpenCV 图像。</param>
        /// <param name="frameNumber">帧序号。</param>
        /// <returns>图像有效时返回帧令牌，否则返回 null。</returns>
        public static ImageFrameToken FromOwnedMat(Mat image, long frameNumber)
        {
            return image == null || image.Empty()
                ? null
                : new ImageFrameToken(image, frameNumber, true);
        }

        /// <summary>
        /// 释放令牌持有的图像引用。
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            if (_ownsImage)
            {
                _image?.Dispose();
            }

            _image = null;
            NativePtr = IntPtr.Zero;
            _disposed = true;
        }

        /// <summary>
        /// 检查令牌是否已经释放。
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(ImageFrameToken));
            }
        }
    }
}
