using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MyFlowChart
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

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

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            flowChart.Stop();
            btnStop.IsEnabled = false;
        }
    }
}
