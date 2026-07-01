using System;
using System.Collections.Generic;
using OpenCvSharp;
using OpenCvWindowTool;

namespace MyFlowChart.Services.Vision
{
    /// <summary>
    /// 表示一次视觉流程运行期间共享的图像上下文。
    /// </summary>
    public sealed class VisionRunContext : IDisposable
    {
        private readonly Dictionary<string, object> _items = new Dictionary<string, object>();
        private readonly bool _ownsResources;
        private bool _disposed;

        /// <summary>
        /// 初始化视觉运行上下文。
        /// </summary>
        /// <param name="image">当前运行使用的图像引用。</param>
        public VisionRunContext(Mat image, long frameNumber)
        {
            _ownsResources = true;
            Image = ImageFrameToken.FromBorrowedMat(image, frameNumber);
            RebuildLineContext();
        }

        /// <summary>
        /// 初始化分支视觉运行上下文。
        /// </summary>
        /// <param name="parent">父级运行上下文。</param>
        private VisionRunContext(VisionRunContext parent)
        {
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }

            Image = parent.Image;
            LineContext = parent.LineContext;
            CopyInheritedItem(parent, VisionDataKeys.CurrentImage);
            CopyInheritedItem(parent, VisionDataKeys.LineDetectionRoi);
        }

        /// <summary>
        /// 获取当前运行图像引用。
        /// </summary>
        public ImageFrameToken Image { get; private set; }

        /// <summary>
        /// 获取直线检测灰度缓存。
        /// </summary>
        public LineDetectionImageContext LineContext { get; private set; }

        /// <summary>
        /// 获取算子共享数据。
        /// </summary>
        public IDictionary<string, object> Items
        {
            get { return _items; }
        }

        /// <summary>
        /// 创建用于并行分支的隔离运行上下文。
        /// </summary>
        /// <returns>返回共享图像缓存但隔离数据字典的分支上下文。</returns>
        public VisionRunContext CreateBranchContext()
        {
            ThrowIfDisposed();
            return new VisionRunContext(this);
        }

        /// <summary>
        /// 设置当前流程图像令牌并刷新下游检测缓存。
        /// </summary>
        /// <param name="image">当前流程使用的图像令牌。</param>
        /// <returns>无返回值。</returns>
        public void SetCurrentImage(ImageFrameToken image)
        {
            ThrowIfDisposed();
            IDisposable oldImage = Image;
            Image = image;
            Items[VisionDataKeys.CurrentImage] = image;
            LineContext?.Dispose();
            LineContext = null;
            RebuildLineContext();

            if (oldImage != null && !ReferenceEquals(oldImage, image))
            {
                oldImage.Dispose();
            }
        }

        /// <summary>
        /// 释放本次运行创建的非托管缓存。
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            if (_ownsResources)
            {
                LineContext?.Dispose();
                Image?.Dispose();
                LineContext = null;
                Image = null;
            }

            DisposeItemValues();
            _items.Clear();
            _disposed = true;
        }

        /// <summary>
        /// 释放运行数据字典中由当前上下文持有的资源。
        /// </summary>
        private void DisposeItemValues()
        {
            foreach (object value in _items.Values)
            {
                if (value == null || ReferenceEquals(value, Image) || ReferenceEquals(value, LineContext))
                {
                    continue;
                }

                IDisposable disposable = value as IDisposable;
                disposable?.Dispose();
            }
        }

        /// <summary>
        /// 按当前图像令牌重建直线检测图像缓存。
        /// </summary>
        private void RebuildLineContext()
        {
            if (Image != null)
            {
                LineContext = LineDetectionImageContext.FromImage(Image.Image);
            }
        }

        /// <summary>
        /// 复制父上下文中允许跨分支继承的数据。
        /// </summary>
        /// <param name="parent">父级运行上下文。</param>
        /// <param name="key">数据键。</param>
        private void CopyInheritedItem(VisionRunContext parent, string key)
        {
            object value;
            if (parent.Items.TryGetValue(key, out value))
            {
                _items[key] = value;
            }
        }

        /// <summary>
        /// 检查当前上下文是否已经释放。
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(VisionRunContext));
            }
        }
    }
}
