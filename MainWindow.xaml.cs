using System.Windows;

namespace MyFlowChart
{
    public partial class MainWindow : Window
    {
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
    }
}
