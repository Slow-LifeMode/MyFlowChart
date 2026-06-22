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

        public OperatorPaletteControl()
        {
            InitializeComponent();

            Operators.Add(new OperatorDefinition { Name = "图像采集" });
            Operators.Add(new OperatorDefinition { Name = "预处理" });
            Operators.Add(new OperatorDefinition { Name = "外侧圆检测" });
            Operators.Add(new OperatorDefinition { Name = "内侧圆检测" });
            Operators.Add(new OperatorDefinition { Name = "结果输出" });
        }

        public ObservableCollection<OperatorDefinition> Operators
        {
            get { return _operators; }
        }

        private void OperatorList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(null);
        }

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
