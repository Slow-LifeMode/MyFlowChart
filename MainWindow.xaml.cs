using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using MyFlowChart.Models;

namespace MyFlowChart
{
    public partial class MainWindow : Window
    {
        private OperatorInsertAdorner _operatorInsertAdorner;
        private AdornerLayer _operatorInsertAdornerLayer;

        /// <summary>
        /// 初始化主窗口并加载流程图界面。
        /// </summary>
        /// <returns>无返回值。</returns>
        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 启动当前流程图运行。
        /// </summary>
        /// <param name="sender">触发启动的按钮。</param>
        /// <param name="e">按钮点击事件参数。</param>
        /// <returns>无返回值。</returns>
        private async void btnStart_Click(object sender, RoutedEventArgs e)
        {
            if (flowChart.Nodes == null || flowChart.Nodes.Count == 0)
            {
                return;
            }

            btnStart.IsEnabled = false;
            btnStop.IsEnabled = true;

            await flowChart.RunAsync();

            btnStart.IsEnabled = true;
            btnStop.IsEnabled = false;
        }

        /// <summary>
        /// 停止当前流程图运行。
        /// </summary>
        /// <param name="sender">触发停止的按钮。</param>
        /// <param name="e">按钮点击事件参数。</param>
        /// <returns>无返回值。</returns>
        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            flowChart.Stop();
            btnStop.IsEnabled = false;
        }

        /// <summary>
        /// 处理右侧算子列表的拖拽经过，并显示插入位置。
        /// </summary>
        /// <param name="sender">算子列表控件。</param>
        /// <param name="e">拖拽事件参数。</param>
        /// <returns>无返回值。</returns>
        private void OperatorList_DragOver(object sender, DragEventArgs e)
        {
            ListBox listBox = sender as ListBox;
            FlowNode targetNode = listBox == null ? null : listBox.DataContext as FlowNode;
            if (!CanDropOperatorToList(targetNode, e))
            {
                ClearOperatorInsertAdorner();
                e.Effects = DragDropEffects.None;
                e.Handled = true;
                return;
            }

            double indicatorY;
            GetOperatorInsertIndex(listBox, e.GetPosition(listBox), out indicatorY);
            UpdateOperatorInsertAdorner(listBox, indicatorY);

            e.Effects = DragDropEffects.Copy;
            e.Handled = true;
        }

        /// <summary>
        /// 处理右侧算子列表的拖拽离开，并清理插入位置提示。
        /// </summary>
        /// <param name="sender">算子列表控件。</param>
        /// <param name="e">拖拽事件参数。</param>
        /// <returns>无返回值。</returns>
        private void OperatorList_DragLeave(object sender, DragEventArgs e)
        {
            ListBox listBox = sender as ListBox;
            if (listBox == null || !IsPointInsideListBox(listBox, e.GetPosition(listBox)))
            {
                ClearOperatorInsertAdorner();
            }
        }

        /// <summary>
        /// 处理右侧算子列表的拖拽放置，并按提示位置插入算子。
        /// </summary>
        /// <param name="sender">算子列表控件。</param>
        /// <param name="e">拖拽事件参数。</param>
        /// <returns>无返回值。</returns>
        private void OperatorList_Drop(object sender, DragEventArgs e)
        {
            ListBox listBox = sender as ListBox;
            FlowNode targetNode = listBox == null ? null : listBox.DataContext as FlowNode;
            DragOperatorData data = GetDragOperatorData(e);
            if (!CanDropOperatorToList(targetNode, e) || data == null)
            {
                ClearOperatorInsertAdorner();
                e.Effects = DragDropEffects.None;
                e.Handled = true;
                return;
            }

            double indicatorY;
            int insertIndex = GetOperatorInsertIndex(listBox, e.GetPosition(listBox), out indicatorY);
            insertIndex = Math.Max(0, Math.Min(insertIndex, targetNode.Operators.Count));

            FlowOperator flowOperator = new FlowOperator
            {
                OperatorName = data.Name,
                DisplayName = CreateOperatorDisplayName(targetNode, data.Name),
                Status = FlowNodeStatus.NotRun
            };

            targetNode.Operators.Insert(insertIndex, flowOperator);
            targetNode.SelectedOperator = flowOperator;
            flowChart.SelectedNode = targetNode;

            ClearOperatorInsertAdorner();
            e.Effects = DragDropEffects.Copy;
            e.Handled = true;
        }

        /// <summary>
        /// 判断拖拽算子是否可以放入右侧算子列表。
        /// </summary>
        /// <param name="targetNode">目标流程块。</param>
        /// <param name="e">拖拽事件参数。</param>
        /// <returns>可放入时返回 true，否则返回 false。</returns>
        private bool CanDropOperatorToList(FlowNode targetNode, DragEventArgs e)
        {
            return flowChart != null && !flowChart.IsRunning && targetNode != null && targetNode.CanConfigureOperators && GetDragOperatorData(e) != null;
        }

        /// <summary>
        /// 从拖拽事件中读取算子数据。
        /// </summary>
        /// <param name="e">拖拽事件参数。</param>
        /// <returns>返回有效算子数据；无效时返回 null。</returns>
        private static DragOperatorData GetDragOperatorData(DragEventArgs e)
        {
            DragOperatorData data = null;
            if (e != null && e.Data.GetDataPresent(DragOperatorData.DataFormat))
            {
                data = e.Data.GetData(DragOperatorData.DataFormat) as DragOperatorData;
            }
            else if (e != null && e.Data.GetDataPresent(typeof(DragOperatorData)))
            {
                data = e.Data.GetData(typeof(DragOperatorData)) as DragOperatorData;
            }

            if (data == null || data.SourceName != "tool" || string.IsNullOrWhiteSpace(data.Name))
            {
                return null;
            }

            return data;
        }

        /// <summary>
        /// 计算拖拽位置对应的算子插入序号和提示线纵坐标。
        /// </summary>
        /// <param name="listBox">算子列表控件。</param>
        /// <param name="point">拖拽点在列表内的位置。</param>
        /// <param name="indicatorY">输出提示线纵坐标。</param>
        /// <returns>返回插入序号。</returns>
        private static int GetOperatorInsertIndex(ListBox listBox, Point point, out double indicatorY)
        {
            indicatorY = 2.0;
            if (listBox == null)
            {
                return 0;
            }

            ListBoxItem item = FindListBoxItem(listBox, point);
            if (item == null)
            {
                indicatorY = GetOperatorListTailY(listBox, point);
                return listBox.Items.Count;
            }

            int itemIndex = listBox.ItemContainerGenerator.IndexFromContainer(item);
            Point itemPoint = item.TranslatePoint(new Point(0, 0), listBox);
            bool insertAfter = point.Y > itemPoint.Y + item.ActualHeight / 2.0;
            indicatorY = itemPoint.Y + (insertAfter ? item.ActualHeight : 0);
            return itemIndex + (insertAfter ? 1 : 0);
        }

        /// <summary>
        /// 获取拖拽点下方的算子列表项。
        /// </summary>
        /// <param name="listBox">算子列表控件。</param>
        /// <param name="point">拖拽点在列表内的位置。</param>
        /// <returns>返回命中的列表项；未命中时返回 null。</returns>
        private static ListBoxItem FindListBoxItem(ListBox listBox, Point point)
        {
            HitTestResult result = VisualTreeHelper.HitTest(listBox, point);
            DependencyObject source = result == null ? null : result.VisualHit;
            return FindAncestor<ListBoxItem>(source);
        }

        /// <summary>
        /// 获取列表末尾提示线的纵坐标。
        /// </summary>
        /// <param name="listBox">算子列表控件。</param>
        /// <param name="point">拖拽点在列表内的位置。</param>
        /// <returns>返回提示线纵坐标。</returns>
        private static double GetOperatorListTailY(ListBox listBox, Point point)
        {
            for (int i = listBox.Items.Count - 1; i >= 0; i--)
            {
                FrameworkElement item = listBox.ItemContainerGenerator.ContainerFromIndex(i) as FrameworkElement;
                if (item != null)
                {
                    Point itemPoint = item.TranslatePoint(new Point(0, 0), listBox);
                    return itemPoint.Y + item.ActualHeight;
                }
            }

            double maxY = Math.Max(2.0, listBox.ActualHeight - 2.0);
            return Math.Max(2.0, Math.Min(maxY, point.Y));
        }

        /// <summary>
        /// 判断拖拽点是否仍在算子列表内。
        /// </summary>
        /// <param name="listBox">算子列表控件。</param>
        /// <param name="point">拖拽点在列表内的位置。</param>
        /// <returns>在列表范围内返回 true，否则返回 false。</returns>
        private static bool IsPointInsideListBox(ListBox listBox, Point point)
        {
            return listBox != null && point.X >= 0 && point.Y >= 0 && point.X <= listBox.ActualWidth && point.Y <= listBox.ActualHeight;
        }

        /// <summary>
        /// 更新右侧算子列表的插入位置提示线。
        /// </summary>
        /// <param name="listBox">算子列表控件。</param>
        /// <param name="indicatorY">提示线纵坐标。</param>
        /// <returns>无返回值。</returns>
        private void UpdateOperatorInsertAdorner(ListBox listBox, double indicatorY)
        {
            AdornerLayer layer = AdornerLayer.GetAdornerLayer(listBox);
            if (layer == null)
            {
                return;
            }

            if (_operatorInsertAdorner == null || _operatorInsertAdorner.AdornedElement != listBox || _operatorInsertAdornerLayer != layer)
            {
                ClearOperatorInsertAdorner();
                _operatorInsertAdorner = new OperatorInsertAdorner(listBox, indicatorY);
                _operatorInsertAdornerLayer = layer;
                layer.Add(_operatorInsertAdorner);
                return;
            }

            _operatorInsertAdorner.UpdateY(indicatorY);
        }

        /// <summary>
        /// 清理右侧算子列表的插入位置提示线。
        /// </summary>
        /// <returns>无返回值。</returns>
        private void ClearOperatorInsertAdorner()
        {
            if (_operatorInsertAdorner != null && _operatorInsertAdornerLayer != null)
            {
                _operatorInsertAdornerLayer.Remove(_operatorInsertAdorner);
            }

            _operatorInsertAdorner = null;
            _operatorInsertAdornerLayer = null;
        }

        /// <summary>
        /// 创建块内算子的显示名称。
        /// </summary>
        /// <param name="targetNode">目标算子块。</param>
        /// <param name="operatorName">算子名称。</param>
        /// <returns>返回块内算子显示名称。</returns>
        private static string CreateOperatorDisplayName(FlowNode targetNode, string operatorName)
        {
            int number = targetNode == null ? 1 : targetNode.Operators.Count(o => o.OperatorName == operatorName) + 1;
            return operatorName + "_" + number;
        }

        /// <summary>
        /// 查找指定类型的可视化父级。
        /// </summary>
        /// <typeparam name="T">要查找的父级类型。</typeparam>
        /// <param name="source">起始元素。</param>
        /// <returns>找到时返回父级元素，否则返回 null。</returns>
        private static T FindAncestor<T>(DependencyObject source) where T : DependencyObject
        {
            while (source != null)
            {
                T target = source as T;
                if (target != null)
                {
                    return target;
                }

                source = VisualTreeHelper.GetParent(source);
            }

            return null;
        }

        private sealed class OperatorInsertAdorner : Adorner
        {
            private readonly Pen _pen;
            private double _y;

            /// <summary>
            /// 初始化算子插入提示线。
            /// </summary>
            /// <param name="adornedElement">需要覆盖提示线的列表控件。</param>
            /// <param name="y">提示线纵坐标。</param>
            /// <returns>无返回值。</returns>
            public OperatorInsertAdorner(UIElement adornedElement, double y)
                : base(adornedElement)
            {
                IsHitTestVisible = false;
                _y = y;
                _pen = new Pen(new SolidColorBrush(Color.FromRgb(36, 120, 224)), 2.0);
                _pen.Freeze();
            }

            /// <summary>
            /// 更新提示线纵坐标并重新绘制。
            /// </summary>
            /// <param name="y">提示线纵坐标。</param>
            /// <returns>无返回值。</returns>
            public void UpdateY(double y)
            {
                _y = y;
                InvalidateVisual();
            }

            /// <summary>
            /// 绘制算子插入位置提示线。
            /// </summary>
            /// <param name="drawingContext">绘图上下文。</param>
            /// <returns>无返回值。</returns>
            protected override void OnRender(DrawingContext drawingContext)
            {
                double y = Math.Max(1.0, Math.Min(RenderSize.Height - 1.0, _y));
                double left = 6.0;
                double right = Math.Max(left, RenderSize.Width - 6.0);
                drawingContext.DrawLine(_pen, new Point(left, y), new Point(right, y));
                drawingContext.DrawEllipse(_pen.Brush, null, new Point(left, y), 3.0, 3.0);
                drawingContext.DrawEllipse(_pen.Brush, null, new Point(right, y), 3.0, 3.0);
            }
        }
    }
}
