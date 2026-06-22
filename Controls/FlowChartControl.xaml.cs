using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using MyFlowChart.Models;

namespace MyFlowChart.Controls
{
    public partial class FlowChartControl : UserControl
    {
        public static readonly DependencyProperty NodesProperty =
            DependencyProperty.Register(
                nameof(Nodes),
                typeof(ObservableCollection<FlowNode>),
                typeof(FlowChartControl),
                new PropertyMetadata(null, OnNodesChanged));

        public static readonly DependencyProperty HasNodesProperty =
            DependencyProperty.Register(
                nameof(HasNodes),
                typeof(bool),
                typeof(FlowChartControl),
                new PropertyMetadata(false));

        public static readonly DependencyProperty IsRunningProperty =
            DependencyProperty.Register(
                nameof(IsRunning),
                typeof(bool),
                typeof(FlowChartControl),
                new PropertyMetadata(false));

        private const double NodeWidth = 190;
        private const double NodeHeight = 54;
        private const double NodeGapHeight = 38;
        private const double ScenePadding = 16;
        private const double EmptySceneHeight = 200;
        private const double MinZoom = 0.5;
        private const double MaxZoom = 2.5;
        private const double ZoomStep = 0.1;

        private CancellationTokenSource _runCancellation;
        private bool _isPanning;
        private Point _panStartPoint;
        private double _panStartHorizontalOffset;
        private double _panStartVerticalOffset;

        public FlowChartControl()
        {
            InitializeComponent();
            Nodes = new ObservableCollection<FlowNode>();
            UpdateSceneWorldSize();
            Dispatcher.BeginInvoke(new Action(UpdateMiniMap), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        public ObservableCollection<FlowNode> Nodes
        {
            get { return (ObservableCollection<FlowNode>)GetValue(NodesProperty); }
            set { SetValue(NodesProperty, value); }
        }

        public bool HasNodes
        {
            get { return (bool)GetValue(HasNodesProperty); }
            private set { SetValue(HasNodesProperty, value); }
        }

        public bool IsRunning
        {
            get { return (bool)GetValue(IsRunningProperty); }
            private set { SetValue(IsRunningProperty, value); }
        }

        public double Zoom
        {
            get { return flowScaleTransform.ScaleX; }
        }

        public async Task RunAsync()
        {
            if (IsRunning || Nodes == null || Nodes.Count == 0)
            {
                return;
            }

            FlowNode currentNode = null;
            Stopwatch stopwatch = null;
            _runCancellation = new CancellationTokenSource();
            IsRunning = true;

            try
            {
                ResetNodes();
                FlowExecutionContext context = new FlowExecutionContext();

                foreach (FlowNode node in Nodes.ToList())
                {
                    _runCancellation.Token.ThrowIfCancellationRequested();
                    currentNode = node;
                    stopwatch = Stopwatch.StartNew();

                    node.ElapsedMilliseconds = 0;
                    node.Status = FlowNodeStatus.Running;

                    await Task.Delay(450, _runCancellation.Token);

                    context.Items["LastNode"] = node.DisplayName;
                    context.Items[node.DisplayName] = DateTime.Now;
                    context.ExecutionLog.Add(node.DisplayName);

                    stopwatch.Stop();
                    node.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
                    node.Status = FlowNodeStatus.OK;
                }
            }
            catch (OperationCanceledException)
            {
                if (currentNode != null && currentNode.Status == FlowNodeStatus.Running)
                {
                    if (stopwatch != null)
                    {
                        stopwatch.Stop();
                        currentNode.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
                    }

                    currentNode.Status = FlowNodeStatus.Stopped;
                }
            }
            finally
            {
                if (_runCancellation != null)
                {
                    _runCancellation.Dispose();
                    _runCancellation = null;
                }

                IsRunning = false;
            }
        }

        public void Stop()
        {
            if (_runCancellation != null && !_runCancellation.IsCancellationRequested)
            {
                _runCancellation.Cancel();
            }
        }

        private static void OnNodesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            FlowChartControl control = (FlowChartControl)d;

            INotifyCollectionChanged oldCollection = e.OldValue as INotifyCollectionChanged;
            if (oldCollection != null)
            {
                oldCollection.CollectionChanged -= control.Nodes_CollectionChanged;
            }

            INotifyCollectionChanged newCollection = e.NewValue as INotifyCollectionChanged;
            if (newCollection != null)
            {
                newCollection.CollectionChanged += control.Nodes_CollectionChanged;
            }

            control.RenumberNodes();
        }

        private void Nodes_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            RenumberNodes();
        }

        private void FlowChart_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = CanAcceptDrag(e) ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
        }

        private void FlowChart_Drop(object sender, DragEventArgs e)
        {
            if (IsNodeSource(e.OriginalSource as DependencyObject))
            {
                return;
            }

            DragOperatorData data = GetDragOperatorData(e);
            if (data == null || IsRunning)
            {
                return;
            }

            AddOperatorNode(data.Name, null);
            e.Handled = true;
        }

        private void Node_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = CanAcceptDrag(e) ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
        }

        private void Node_Drop(object sender, DragEventArgs e)
        {
            DragOperatorData data = GetDragOperatorData(e);
            if (data == null || IsRunning)
            {
                return;
            }

            FrameworkElement element = sender as FrameworkElement;
            FlowNode targetNode = element == null ? null : element.DataContext as FlowNode;
            AddOperatorNode(data.Name, targetNode);
            e.Handled = true;
        }

        private bool CanAcceptDrag(DragEventArgs e)
        {
            return !IsRunning && GetDragOperatorData(e) != null;
        }

        private DragOperatorData GetDragOperatorData(DragEventArgs e)
        {
            DragOperatorData data = null;

            if (e.Data.GetDataPresent(DragOperatorData.DataFormat))
            {
                data = e.Data.GetData(DragOperatorData.DataFormat) as DragOperatorData;
            }
            else if (e.Data.GetDataPresent(typeof(DragOperatorData)))
            {
                data = e.Data.GetData(typeof(DragOperatorData)) as DragOperatorData;
            }

            if (data == null || data.SourceName != "tool" || string.IsNullOrWhiteSpace(data.Name))
            {
                return null;
            }

            return data;
        }

        private void AddOperatorNode(string operatorName, FlowNode targetNode)
        {
            if (Nodes == null)
            {
                Nodes = new ObservableCollection<FlowNode>();
            }

            FlowNode node = new FlowNode
            {
                OperatorName = operatorName,
                DisplayName = CreateDisplayName(operatorName),
                Status = FlowNodeStatus.NotRun
            };

            if (targetNode == null)
            {
                Nodes.Add(node);
                return;
            }

            int index = Nodes.IndexOf(targetNode);
            if (index < 0 || index >= Nodes.Count - 1)
            {
                Nodes.Add(node);
            }
            else
            {
                Nodes.Insert(index + 1, node);
            }
        }

        private string CreateDisplayName(string operatorName)
        {
            int number = Nodes == null ? 1 : Nodes.Count(o => o.OperatorName == operatorName) + 1;
            return operatorName + number;
        }

        private void ResetNodes()
        {
            foreach (FlowNode node in Nodes)
            {
                node.Status = FlowNodeStatus.NotRun;
                node.ElapsedMilliseconds = 0;
            }
        }

        private void RenumberNodes()
        {
            HasNodes = Nodes != null && Nodes.Count > 0;

            if (Nodes != null)
            {
                for (int i = 0; i < Nodes.Count; i++)
                {
                    Nodes[i].Sequence = i + 1;
                    Nodes[i].IsLast = i == Nodes.Count - 1;
                }
            }

            UpdateSceneWorldSize();
            Dispatcher.BeginInvoke(new Action(UpdateMiniMap), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private void View_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            double oldZoom = Zoom;
            double newZoom = oldZoom + (e.Delta > 0 ? ZoomStep : -ZoomStep);
            newZoom = Math.Max(MinZoom, Math.Min(MaxZoom, newZoom));

            if (Math.Abs(newZoom - oldZoom) < 0.0001)
            {
                e.Handled = true;
                return;
            }

            Point mouseInViewport = e.GetPosition(viewScrollViewer);
            double worldX = (viewScrollViewer.HorizontalOffset + mouseInViewport.X) / oldZoom;
            double worldY = (viewScrollViewer.VerticalOffset + mouseInViewport.Y) / oldZoom;

            flowScaleTransform.ScaleX = newZoom;
            flowScaleTransform.ScaleY = newZoom;
            UpdateMiniMap();

            Dispatcher.BeginInvoke(
                new Action(
                    delegate
                    {
                        ScrollToClamped(worldX * newZoom - mouseInViewport.X, worldY * newZoom - mouseInViewport.Y);
                        UpdateMiniMap();
                    }),
                System.Windows.Threading.DispatcherPriority.Loaded);

            e.Handled = true;
        }

        private void View_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (IsRunning || !CanStartPan(e.OriginalSource as DependencyObject))
            {
                return;
            }

            _isPanning = true;
            _panStartPoint = e.GetPosition(this);
            _panStartHorizontalOffset = viewScrollViewer.HorizontalOffset;
            _panStartVerticalOffset = viewScrollViewer.VerticalOffset;
            viewScrollViewer.Cursor = Cursors.SizeAll;
            viewScrollViewer.CaptureMouse();
            e.Handled = true;
        }

        private void View_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isPanning)
            {
                return;
            }

            Point current = e.GetPosition(this);
            Vector delta = current - _panStartPoint;
            ScrollToClamped(_panStartHorizontalOffset - delta.X, _panStartVerticalOffset - delta.Y);
            UpdateMiniMap();
        }

        private void View_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            StopPanning();
        }

        private void ViewScrollViewer_LostMouseCapture(object sender, MouseEventArgs e)
        {
            StopPanning();
        }

        private void ViewScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            UpdateMiniMap();
        }

        private void ViewScrollViewer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateSceneWorldSize();
            Dispatcher.BeginInvoke(new Action(UpdateMiniMap), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private void MiniMap_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            MiniMapLayout layout = GetMiniMapLayout();
            if (layout.Scale <= 0)
            {
                return;
            }

            Point click = e.GetPosition(miniMapRoot);
            double targetX = (click.X - layout.ContentLeft) / layout.Scale - viewScrollViewer.ViewportWidth / 2.0;
            double targetY = (click.Y - layout.ContentTop) / layout.Scale - viewScrollViewer.ViewportHeight / 2.0;

            ScrollToClamped(targetX, targetY);
            UpdateMiniMap();
        }

        private void StopPanning()
        {
            if (!_isPanning)
            {
                return;
            }

            _isPanning = false;
            viewScrollViewer.Cursor = Cursors.Cross;
            viewScrollViewer.ReleaseMouseCapture();
        }

        private void ScrollToClamped(double horizontalOffset, double verticalOffset)
        {
            double maxHorizontal = Math.Max(0, viewScrollViewer.ExtentWidth - viewScrollViewer.ViewportWidth);
            double maxVertical = Math.Max(0, viewScrollViewer.ExtentHeight - viewScrollViewer.ViewportHeight);

            horizontalOffset = Math.Max(0, Math.Min(maxHorizontal, horizontalOffset));
            verticalOffset = Math.Max(0, Math.Min(maxVertical, verticalOffset));

            viewScrollViewer.ScrollToHorizontalOffset(horizontalOffset);
            viewScrollViewer.ScrollToVerticalOffset(verticalOffset);
        }

        private void UpdateSceneWorldSize()
        {
            Size worldSize = GetWorldSize();
            double contentHeight = Math.Max(1, worldSize.Height - ScenePadding * 2);

            if (flowScene != null)
            {
                flowScene.Width = worldSize.Width;
                flowScene.Height = worldSize.Height;
            }

            if (flowContentHost != null)
            {
                flowContentHost.Width = NodeWidth;
                flowContentHost.Height = contentHeight;
            }

            if (miniMapScene != null)
            {
                miniMapScene.Width = worldSize.Width;
                miniMapScene.Height = worldSize.Height;
            }

            if (miniMapContentHost != null)
            {
                miniMapContentHost.Width = NodeWidth;
                miniMapContentHost.Height = contentHeight;
            }
        }

        private void UpdateMiniMap()
        {
            if (miniMapRoot == null || miniMapScene == null || miniMapOverlay == null || miniMapViewport == null)
            {
                return;
            }

            zoomText.Text = (Zoom * 100.0).ToString("0") + "%";

            MiniMapLayout layout = GetMiniMapLayout();
            if (layout.Scale <= 0)
            {
                miniMapViewport.Width = 0;
                miniMapViewport.Height = 0;
                return;
            }

            Canvas.SetLeft(miniMapScene, layout.ContentLeft);
            Canvas.SetTop(miniMapScene, layout.ContentTop);
            miniMapScaleTransform.ScaleX = layout.Scale * Zoom;
            miniMapScaleTransform.ScaleY = layout.Scale * Zoom;

            miniMapOverlay.Width = miniMapRoot.ActualWidth;
            miniMapOverlay.Height = miniMapRoot.ActualHeight;

            double roiLeft = layout.ContentLeft + viewScrollViewer.HorizontalOffset * layout.Scale;
            double roiTop = layout.ContentTop + viewScrollViewer.VerticalOffset * layout.Scale;
            double roiWidth = viewScrollViewer.ViewportWidth * layout.Scale;
            double roiHeight = viewScrollViewer.ViewportHeight * layout.Scale;

            double maxLeft = layout.ContentLeft + layout.ContentWidth;
            double maxTop = layout.ContentTop + layout.ContentHeight;

            roiLeft = Math.Max(layout.ContentLeft, Math.Min(maxLeft, roiLeft));
            roiTop = Math.Max(layout.ContentTop, Math.Min(maxTop, roiTop));
            roiWidth = Math.Max(0, Math.Min(roiWidth, maxLeft - roiLeft));
            roiHeight = Math.Max(0, Math.Min(roiHeight, maxTop - roiTop));

            double visibleWidth = Math.Min(layout.ContentWidth, Math.Max(8, roiWidth));
            double visibleHeight = Math.Min(layout.ContentHeight, Math.Max(8, roiHeight));

            if (roiLeft + visibleWidth > maxLeft)
            {
                roiLeft = maxLeft - visibleWidth;
            }

            if (roiTop + visibleHeight > maxTop)
            {
                roiTop = maxTop - visibleHeight;
            }

            Canvas.SetLeft(miniMapViewport, roiLeft);
            Canvas.SetTop(miniMapViewport, roiTop);
            miniMapViewport.Width = visibleWidth;
            miniMapViewport.Height = visibleHeight;
        }

        private Size GetWorldSize()
        {
            int nodeCount = Nodes == null ? 0 : Nodes.Count;
            double width = NodeWidth + ScenePadding * 2;
            double height = EmptySceneHeight;

            if (nodeCount > 0)
            {
                height = ScenePadding + nodeCount * NodeHeight + (nodeCount - 1) * NodeGapHeight + ScenePadding;
            }

            return new Size(width, height);
        }

        private MiniMapLayout GetMiniMapLayout()
        {
            Size worldSize = GetWorldSize();
            double scaledWorldWidth = worldSize.Width * Zoom;
            double scaledWorldHeight = worldSize.Height * Zoom;
            double mapWidth = Math.Max(1, miniMapRoot.ActualWidth);
            double mapHeight = Math.Max(1, miniMapRoot.ActualHeight);

            if (scaledWorldWidth <= 0 || scaledWorldHeight <= 0)
            {
                return new MiniMapLayout();
            }

            double scale = Math.Min(mapWidth / scaledWorldWidth, mapHeight / scaledWorldHeight);
            double contentWidth = scaledWorldWidth * scale;
            double contentHeight = scaledWorldHeight * scale;

            return new MiniMapLayout
            {
                Scale = scale,
                ContentLeft = (mapWidth - contentWidth) / 2.0,
                ContentTop = (mapHeight - contentHeight) / 2.0,
                ContentWidth = contentWidth,
                ContentHeight = contentHeight
            };
        }

        private bool CanStartPan(DependencyObject source)
        {
            if (source == null)
            {
                return true;
            }

            if (FindAncestor<ScrollBar>(source) != null || FindAncestor<ListBoxItem>(source) != null)
            {
                return false;
            }

            if (IsInside(source, miniMap) || IsNodeSource(source))
            {
                return false;
            }

            return true;
        }

        private bool IsNodeSource(DependencyObject source)
        {
            while (source != null)
            {
                FrameworkElement element = source as FrameworkElement;
                if (element != null && element.DataContext is FlowNode)
                {
                    return true;
                }

                source = VisualTreeHelper.GetParent(source);
            }

            return false;
        }

        private static bool IsInside(DependencyObject source, DependencyObject ancestor)
        {
            while (source != null)
            {
                if (source == ancestor)
                {
                    return true;
                }

                source = VisualTreeHelper.GetParent(source);
            }

            return false;
        }

        private static T FindAncestor<T>(DependencyObject element) where T : DependencyObject
        {
            while (element != null)
            {
                T current = element as T;
                if (current != null)
                {
                    return current;
                }

                element = VisualTreeHelper.GetParent(element);
            }

            return null;
        }

        private struct MiniMapLayout
        {
            public double Scale;
            public double ContentLeft;
            public double ContentTop;
            public double ContentWidth;
            public double ContentHeight;
        }
    }
}
