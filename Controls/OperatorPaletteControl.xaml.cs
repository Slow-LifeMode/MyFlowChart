using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MyFlowChart.Models;

namespace MyFlowChart.Controls
{
    public partial class OperatorPaletteControl : UserControl
    {
        private Point _dragStartPoint;
        private readonly ObservableCollection<OperatorDefinition> _operators =
            new ObservableCollection<OperatorDefinition>();

        /// <summary>
        /// 初始化算子库控件。
        /// </summary>
        public OperatorPaletteControl()
        {
            InitializeComponent();
            foreach (OperatorDefinition definition in OperatorDefinition.CreateDefaultOperators())
            {
                Operators.Add(definition);
            }
        }

        public ObservableCollection<OperatorDefinition> Operators
        {
            get { return _operators; }
        }

        /// <summary>
        /// 记录鼠标按下位置，用于判断是否开始拖拽。
        /// </summary>
        /// <param name="sender">事件源。</param>
        /// <param name="e">鼠标按键事件参数。</param>
        private void OperatorList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(null);
        }

        /// <summary>
        /// 鼠标移动超过拖拽阈值后启动算子拖拽。
        /// </summary>
        /// <param name="sender">算子列表控件。</param>
        /// <param name="e">鼠标事件参数。</param>
        private void OperatorList_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }

            Point currentPoint = e.GetPosition(null);
            if (Math.Abs(currentPoint.X - _dragStartPoint.X) < SystemParameters.MinimumHorizontalDragDistance
                && Math.Abs(currentPoint.Y - _dragStartPoint.Y) < SystemParameters.MinimumVerticalDragDistance)
            {
                return;
            }

            ListBox listBox = sender as ListBox;
            OperatorDefinition definition = GetOperatorDefinition(listBox, e);
            if (definition == null)
            {
                return;
            }

            DragOperatorData data = new DragOperatorData
            {
                Name = definition.Name,
                SourceName = "tool"
            };

            DataObject dataObject = new DataObject();
            dataObject.SetData(DragOperatorData.DataFormat, data);
            dataObject.SetData(typeof(DragOperatorData), data);
            DragDrop.DoDragDrop(listBox, dataObject, DragDropEffects.Copy);
        }

        /// <summary>
        /// 从鼠标命中的列表项读取算子定义。
        /// </summary>
        /// <param name="listBox">算子列表控件。</param>
        /// <param name="e">鼠标事件参数。</param>
        /// <returns>返回算子定义；未命中时返回 null。</returns>
        private OperatorDefinition GetOperatorDefinition(ListBox listBox, MouseEventArgs e)
        {
            if (listBox == null)
            {
                return null;
            }

            DependencyObject hitElement = listBox.InputHitTest(e.GetPosition(listBox)) as DependencyObject;
            ListBoxItem item = FindVisualParent<ListBoxItem>(hitElement);
            return item == null ? null : item.DataContext as OperatorDefinition;
        }

        /// <summary>
        /// 向上查找指定类型的可视父级。
        /// </summary>
        /// <typeparam name="T">父级类型。</typeparam>
        /// <param name="element">起始元素。</param>
        /// <returns>返回匹配的父级；未找到时返回 null。</returns>
        private static T FindVisualParent<T>(DependencyObject element) where T : DependencyObject
        {
            while (element != null)
            {
                T parent = element as T;
                if (parent != null)
                {
                    return parent;
                }

                element = VisualTreeHelper.GetParent(element);
            }

            return null;
        }
    }
}
