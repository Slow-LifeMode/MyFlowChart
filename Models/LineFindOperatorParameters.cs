using OpenCvWindowTool;
using System.Drawing;

namespace MyFlowChart.Models
{
    /// <summary>
    /// 直线查找算子的可绑定参数。
    /// </summary>
    public sealed class LineFindOperatorParameters : BindableBase
    {
        private float _edgeThreshold;
        private int _sampleCount;
        private float _sampleStep;
        private int _smoothSize;
        private int _edgeWidth;
        private int _projectionWidth;
        private int _rejectRatio;
        private int _rejectDistance;
        private int _profileLineIndex;
        private bool _showSearchLines;
        private LineEdgePolarity _edgePolarity;
        private LineEdgeStrengthType _strengthType;
        private LineSelectionMode _selectionMode;
        private LineScanDirection _scanDirection;
        private LineFitMode _fitMode;
        private LineDetectionMode _detectionMode;
        private bool _hasRoi;
        private RoiShape _roiShape;
        private float _roiCenterX;
        private float _roiCenterY;
        private float _roiWidth;
        private float _roiHeight;
        private float _roiAngle;

        /// <summary>
        /// 使用 OpenCvWindowTool 的原始默认值初始化直线查找参数。
        /// </summary>
        public LineFindOperatorParameters()
        {
            LineDetectionParams defaults = new LineDetectionParams();
            _edgeThreshold = defaults.EdgeThreshold;
            _sampleCount = defaults.SampleCount;
            _sampleStep = defaults.SampleStep;
            _smoothSize = defaults.SmoothSize;
            _edgeWidth = defaults.EdgeWidth;
            _projectionWidth = defaults.ProjectionWidth;
            _rejectRatio = defaults.RejectRatio;
            _rejectDistance = defaults.RejectDistance;
            _profileLineIndex = defaults.ProfileLineIndex;
            _showSearchLines = defaults.ShowSearchLines;
            _edgePolarity = defaults.EdgePolarity;
            _strengthType = defaults.StrengthType;
            _selectionMode = defaults.SelectionMode;
            _scanDirection = defaults.ScanDirection;
            _fitMode = defaults.FitMode;
            _detectionMode = LineDetectionMode.SelfMode;
            _roiShape = RoiShape.Rectangle;
        }

        public float EdgeThreshold
        {
            get { return _edgeThreshold; }
            set { Set(ref _edgeThreshold, value); }
        }

        public int SampleCount
        {
            get { return _sampleCount; }
            set { Set(ref _sampleCount, value); }
        }

        public float SampleStep
        {
            get { return _sampleStep; }
            set { Set(ref _sampleStep, value); }
        }

        public int SmoothSize
        {
            get { return _smoothSize; }
            set { Set(ref _smoothSize, value); }
        }

        public int EdgeWidth
        {
            get { return _edgeWidth; }
            set { Set(ref _edgeWidth, value); }
        }

        public int ProjectionWidth
        {
            get { return _projectionWidth; }
            set { Set(ref _projectionWidth, value); }
        }

        public int RejectRatio
        {
            get { return _rejectRatio; }
            set { Set(ref _rejectRatio, value); }
        }

        public int RejectDistance
        {
            get { return _rejectDistance; }
            set { Set(ref _rejectDistance, value); }
        }

        public int ProfileLineIndex
        {
            get { return _profileLineIndex; }
            set { Set(ref _profileLineIndex, value); }
        }

        public bool ShowSearchLines
        {
            get { return _showSearchLines; }
            set { Set(ref _showSearchLines, value); }
        }

        public LineEdgePolarity EdgePolarity
        {
            get { return _edgePolarity; }
            set { Set(ref _edgePolarity, value); }
        }

        public LineEdgeStrengthType StrengthType
        {
            get { return _strengthType; }
            set { Set(ref _strengthType, value); }
        }

        public LineSelectionMode SelectionMode
        {
            get { return _selectionMode; }
            set { Set(ref _selectionMode, value); }
        }

        public LineScanDirection ScanDirection
        {
            get { return _scanDirection; }
            set { Set(ref _scanDirection, value); }
        }

        public LineFitMode FitMode
        {
            get { return _fitMode; }
            set { Set(ref _fitMode, value); }
        }

        public LineDetectionMode DetectionMode
        {
            get { return _detectionMode; }
            set { Set(ref _detectionMode, value); }
        }

        public bool HasRoi
        {
            get { return _hasRoi; }
            set { Set(ref _hasRoi, value); }
        }

        public RoiShape RoiKind
        {
            get { return _roiShape; }
            set { Set(ref _roiShape, value); }
        }

        public float RoiCenterX
        {
            get { return _roiCenterX; }
            set { Set(ref _roiCenterX, value); }
        }

        public float RoiCenterY
        {
            get { return _roiCenterY; }
            set { Set(ref _roiCenterY, value); }
        }

        public float RoiWidth
        {
            get { return _roiWidth; }
            set { Set(ref _roiWidth, value); }
        }

        public float RoiHeight
        {
            get { return _roiHeight; }
            set { Set(ref _roiHeight, value); }
        }

        public float RoiAngle
        {
            get { return _roiAngle; }
            set { Set(ref _roiAngle, value); }
        }

        /// <summary>
        /// 转换为 OpenCvWindowTool 原始直线检测参数。
        /// </summary>
        /// <returns>返回可直接传入直线检测算子的参数。</returns>
        public LineDetectionParams ToLineDetectionParams()
        {
            return new LineDetectionParams
            {
                EdgeThreshold = EdgeThreshold,
                SampleCount = SampleCount,
                SampleStep = SampleStep,
                SmoothSize = SmoothSize,
                EdgeWidth = EdgeWidth,
                ProjectionWidth = ProjectionWidth,
                RejectRatio = RejectRatio,
                RejectDistance = RejectDistance,
                ProfileLineIndex = ProfileLineIndex,
                ShowSearchLines = ShowSearchLines,
                EdgePolarity = EdgePolarity,
                StrengthType = StrengthType,
                SelectionMode = SelectionMode,
                ScanDirection = ScanDirection,
                FitMode = FitMode
            };
        }

        /// <summary>
        /// 从参数中创建已保存的直线检测 ROI。
        /// </summary>
        /// <returns>返回 ROI；未保存 ROI 时返回 null。</returns>
        public RoiItem CreateRoi()
        {
            if (!HasRoi)
            {
                return null;
            }

            float width = RoiWidth < 4f ? 4f : RoiWidth;
            float height = RoiHeight < 4f ? 4f : RoiHeight;
            if (RoiKind == RoiShape.RotatedRectangle)
            {
                return RoiItem.RotatedRectangle("LineROI", new PointF(RoiCenterX, RoiCenterY), width, height, RoiAngle);
            }

            return RoiItem.Rectangle(
                "LineROI",
                new RectangleF(RoiCenterX - width / 2f, RoiCenterY - height / 2f, width, height));
        }

        /// <summary>
        /// 保存当前直线检测 ROI 到参数。
        /// </summary>
        /// <param name="roi">需要保存的 ROI。</param>
        /// <returns>无返回值。</returns>
        public void SaveRoi(RoiItem roi)
        {
            if (roi == null || !roi.CanDetectLine())
            {
                HasRoi = false;
                return;
            }

            HasRoi = true;
            RoiKind = roi.Shape == RoiShape.RotatedRectangle ? RoiShape.RotatedRectangle : RoiShape.Rectangle;
            RoiCenterX = roi.Center.X;
            RoiCenterY = roi.Center.Y;
            RoiWidth = roi.Width;
            RoiHeight = roi.Height;
            RoiAngle = RoiKind == RoiShape.RotatedRectangle ? roi.Angle : 0f;
        }

        /// <summary>
        /// 创建当前参数的独立副本。
        /// </summary>
        /// <returns>返回新的直线查找参数实例。</returns>
        public LineFindOperatorParameters Clone()
        {
            return new LineFindOperatorParameters
            {
                EdgeThreshold = EdgeThreshold,
                SampleCount = SampleCount,
                SampleStep = SampleStep,
                SmoothSize = SmoothSize,
                EdgeWidth = EdgeWidth,
                ProjectionWidth = ProjectionWidth,
                RejectRatio = RejectRatio,
                RejectDistance = RejectDistance,
                ProfileLineIndex = ProfileLineIndex,
                ShowSearchLines = ShowSearchLines,
                EdgePolarity = EdgePolarity,
                StrengthType = StrengthType,
                SelectionMode = SelectionMode,
                ScanDirection = ScanDirection,
                FitMode = FitMode,
                DetectionMode = DetectionMode,
                HasRoi = HasRoi,
                RoiKind = RoiKind,
                RoiCenterX = RoiCenterX,
                RoiCenterY = RoiCenterY,
                RoiWidth = RoiWidth,
                RoiHeight = RoiHeight,
                RoiAngle = RoiAngle
            };
        }
    }
}
