using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows.Input;
using MyFlowChart.Models;
using OpenCvWindowTool;

namespace MyFlowChart.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private readonly ObservableCollection<FlowNode> _nodes = new ObservableCollection<FlowNode>();
        private readonly ObservableCollection<FlowNode> _gotoTargetNodes = new ObservableCollection<FlowNode>();
        private readonly ObservableCollection<VisionRunLogItem> _visionRunLogs = new ObservableCollection<VisionRunLogItem>();
        private readonly System.Collections.Generic.HashSet<FlowNode> _observedNodes = new System.Collections.Generic.HashSet<FlowNode>();
        private readonly System.Collections.Generic.HashSet<FlowBranch> _observedBranches = new System.Collections.Generic.HashSet<FlowBranch>();
        private FlowNode _selectedNode;
        private bool _isVisionRunning;
        private VisionRunLogItem _selectedVisionRunLog;
        private string _visionStatusText = "视觉流程就绪";
        private string _visionRoiText = "ROI：未选择";
        private string _visionResultText = "结果：未运行";
        private ICommand _addBranchCommand;
        private ICommand _removeBranchCommand;

        public ObservableCollection<FlowNode> Nodes
        {
            get { return _nodes; }
        }

        public ObservableCollection<FlowNode> GotoTargetNodes
        {
            get { return _gotoTargetNodes; }
        }

        public ObservableCollection<VisionRunLogItem> VisionRunLogs
        {
            get { return _visionRunLogs; }
        }

        public VisionRunLogItem SelectedVisionRunLog
        {
            get { return _selectedVisionRunLog; }
            set
            {
                if (Set(ref _selectedVisionRunLog, value))
                {
                    VisionResultText = FormatVisionRunLogResult(value);
                }
            }
        }

        public FlowNode SelectedNode
        {
            get { return _selectedNode; }
            set
            {
                if (Set(ref _selectedNode, value))
                {
                    RefreshGotoTargetNodes();
                    OnPropertyChanged("ShowOperatorPanel");
                    OnPropertyChanged("ShowGotoPanel");
                    OnPropertyChanged("ShowBranchPanel");
                }
            }
        }

        public ICommand AddBranchCommand
        {
            get
            {
                if (_addBranchCommand == null)
                {
                    _addBranchCommand = new RelayCommand(AddBranch, CanEditBranches);
                }

                return _addBranchCommand;
            }
        }

        public ICommand RemoveBranchCommand
        {
            get
            {
                if (_removeBranchCommand == null)
                {
                    _removeBranchCommand = new RelayCommand(RemoveBranch, CanEditBranches);
                }

                return _removeBranchCommand;
            }
        }

        public bool IsVisionRunning
        {
            get { return _isVisionRunning; }
            private set
            {
                if (Set(ref _isVisionRunning, value))
                {
                    OnPropertyChanged("CanRunVision");
                    OnPropertyChanged("CanEditVisionWorkflow");
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public bool CanRunVision
        {
            get { return !IsVisionRunning; }
        }

        public bool CanEditVisionWorkflow
        {
            get { return !IsVisionRunning; }
        }

        public string VisionStatusText
        {
            get { return _visionStatusText; }
            private set { Set(ref _visionStatusText, value); }
        }

        public string VisionRoiText
        {
            get { return _visionRoiText; }
            private set { Set(ref _visionRoiText, value); }
        }

        public string VisionResultText
        {
            get { return _visionResultText; }
            private set { Set(ref _visionResultText, value); }
        }

        public bool ShowOperatorPanel
        {
            get { return SelectedNode != null && SelectedNode.CanConfigureOperators; }
        }

        public bool ShowGotoPanel
        {
            get { return SelectedNode != null && SelectedNode.CanConfigureGoto; }
        }

        public bool ShowBranchPanel
        {
            get { return SelectedNode != null && SelectedNode.CanConfigureBranches; }
        }

        /// <summary>
        /// 标记视觉流程进入运行状态。
        /// </summary>
        /// <param name="message">运行提示消息。</param>
        /// <returns>无返回值。</returns>
        public void BeginVisionRun(string message)
        {
            SelectedVisionRunLog = null;
            VisionRunLogs.Clear();
            VisionStatusText = string.IsNullOrWhiteSpace(message) ? "视觉流程运行中" : message;
            IsVisionRunning = true;
        }

        /// <summary>
        /// 标记视觉流程结束运行。
        /// </summary>
        /// <param name="success">是否运行成功。</param>
        /// <param name="message">运行结果消息。</param>
        /// <returns>无返回值。</returns>
        public void EndVisionRun(bool success, string message)
        {
            VisionStatusText = string.IsNullOrWhiteSpace(message)
                ? (success ? "视觉流程运行完成" : "视觉流程运行失败")
                : message;
            IsVisionRunning = false;
        }

        /// <summary>
        /// 标记视觉流程已请求停止。
        /// </summary>
        /// <returns>无返回值。</returns>
        public void StopVisionRun()
        {
            VisionStatusText = "视觉流程已请求停止";
            IsVisionRunning = false;
        }

        /// <summary>
        /// 更新当前视觉 ROI 摘要。
        /// </summary>
        /// <param name="roiText">ROI 摘要文本。</param>
        /// <returns>无返回值。</returns>
        public void UpdateVisionRoi(string roiText)
        {
            VisionRoiText = string.IsNullOrWhiteSpace(roiText) ? "ROI：未选择" : roiText;
        }

        /// <summary>
        /// 更新当前视觉运行结果摘要。
        /// </summary>
        /// <param name="resultText">结果摘要文本。</param>
        /// <returns>无返回值。</returns>
        public void UpdateVisionResult(string resultText)
        {
            VisionResultText = string.IsNullOrWhiteSpace(resultText) ? "结果：未运行" : resultText;
        }

        /// <summary>
        /// 初始化主窗口视图模型。
        /// </summary>
        /// <returns>无返回值。</returns>
        /// <summary>
        /// 根据当前节点算子状态刷新本次运行日志。
        /// </summary>
        /// <param name="node">当前运行的流程节点。</param>
        /// <returns>无返回值。</returns>
        public void RefreshVisionRunLogs(FlowNode node)
        {
            SelectedVisionRunLog = null;
            VisionRunLogs.Clear();
            if (node == null)
            {
                return;
            }

            foreach (FlowOperator flowOperator in node.Operators)
            {
                VisionRunLogs.Add(new VisionRunLogItem(
                    flowOperator.Sequence,
                    flowOperator.DisplayName,
                    flowOperator.Status,
                    flowOperator.ElapsedMilliseconds,
                    flowOperator.LastMessage,
                    flowOperator.Payload));
            }
        }

        /// <summary>
        /// 格式化选中算子的运行结果摘要。
        /// </summary>
        /// <param name="item">选中的运行日志项。</param>
        /// <returns>返回右侧结果面板显示的摘要文本。</returns>
        private static string FormatVisionRunLogResult(VisionRunLogItem item)
        {
            if (item == null)
            {
                return "结果：未运行";
            }

            LineDetectionResult lineResult = item.Payload as LineDetectionResult;
            if (lineResult != null)
            {
                return FormatLineDetectionResult(lineResult);
            }

            return string.IsNullOrWhiteSpace(item.Message)
                ? "结果：" + item.StatusText
                : "结果：" + item.StatusText + "  " + item.Message;
        }

        /// <summary>
        /// 格式化直线检测结果摘要。
        /// </summary>
        /// <param name="result">直线检测结果。</param>
        /// <returns>返回直线检测结果摘要文本。</returns>
        private static string FormatLineDetectionResult(LineDetectionResult result)
        {
            if (result == null)
            {
                return "结果：未运行";
            }

            if (!result.Success)
            {
                return "结果：NG  " + result.Message;
            }

            return string.Format(
                "结果：OK  角度={0:0.00}°  强度={1:0.0}  耗时={2:0.0}ms",
                result.Angle,
                result.AverageStrength,
                result.Elapsed.TotalMilliseconds);
        }

        /// <summary>
        /// 初始化主窗口视图模型。
        /// </summary>
        /// <returns>无返回值。</returns>
        public MainWindowViewModel()
        {
            _nodes.CollectionChanged += Nodes_CollectionChanged;
            RebuildFlowSubscriptions();
            EnsureDefaultVisionWorkflow();
        }

        /// <summary>
        /// 确保流程中存在一个可直接运行的默认视觉算子块。
        /// </summary>
        /// <returns>返回默认视觉算子块。</returns>
        public FlowNode EnsureDefaultVisionWorkflow()
        {
            FlowNode node = Nodes.FirstOrDefault(ContainsLineFindOperator);
            if (node == null)
            {
                node = FlowNode.CreateOperatorBlock("Vision1");
                FlowOperator lineFindOperator = CreateVisionOperator(OperatorDefinition.LineFindName);

                node.Operators.Add(CreateVisionOperator(OperatorDefinition.ImageInputName));
                node.Operators.Add(lineFindOperator);
                node.Operators.Add(CreateVisionOperator(OperatorDefinition.ResultOutputName));
                node.SelectedOperator = lineFindOperator;

                InsertBeforeEnd(node);
            }

            SelectedNode = node;
            return node;
        }

        /// <summary>
        /// 判断流程块是否已经包含直线查找算子。
        /// </summary>
        /// <param name="node">待检查的流程块。</param>
        /// <returns>包含直线查找算子时返回 true，否则返回 false。</returns>
        private static bool ContainsLineFindOperator(FlowNode node)
        {
            return node != null && node.Operators.Any(o => OperatorDefinition.IsLineFindOperator(o.OperatorName));
        }

        /// <summary>
        /// 创建默认视觉流程中的单个算子。
        /// </summary>
        /// <param name="operatorName">算子名称。</param>
        /// <returns>返回可绑定显示的流程算子。</returns>
        private static FlowOperator CreateVisionOperator(string operatorName)
        {
            return new FlowOperator
            {
                OperatorName = operatorName,
                DisplayName = operatorName,
                Status = FlowNodeStatus.NotRun,
                LastMessage = "未运行"
            };
        }

        /// <summary>
        /// 将默认视觉块插入到结束块之前。
        /// </summary>
        /// <param name="node">需要插入的流程块。</param>
        /// <returns>无返回值。</returns>
        private void InsertBeforeEnd(FlowNode node)
        {
            int endIndex = Nodes.ToList().FindIndex(o => o.IsEndBlock);
            if (endIndex >= 0)
            {
                Nodes.Insert(endIndex, node);
                return;
            }

            Nodes.Add(node);
        }

        /// <summary>
        /// 递归刷新所有可作为 Goto 目标的算子块。
        /// </summary>
        /// <returns>无返回值。</returns>
        private void RefreshGotoTargetNodes()
        {
            GotoTargetNodes.Clear();

            foreach (FlowNode node in Nodes)
            {
                AddGotoTargetNode(node);
            }
        }

        /// <summary>
        /// 递归添加一个流程块及其分支内的可跳转目标。
        /// </summary>
        /// <param name="node">待检查的流程块。</param>
        /// <returns>无返回值。</returns>
        private void AddGotoTargetNode(FlowNode node)
        {
            if (node == null)
            {
                return;
            }

            if (node.IsOperatorBlock && node != SelectedNode)
            {
                GotoTargetNodes.Add(node);
            }

            foreach (FlowBranch branch in node.Branches)
            {
                foreach (FlowNode branchNode in branch.Nodes)
                {
                    AddGotoTargetNode(branchNode);
                }
            }
        }

        /// <summary>
        /// 处理流程块集合变化并刷新 Goto 目标列表。
        /// </summary>
        /// <param name="sender">流程块集合。</param>
        /// <param name="e">集合变化事件参数。</param>
        /// <returns>无返回值。</returns>
        private void Nodes_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            RebuildFlowSubscriptions();
            RefreshGotoTargetNodes();
        }

        /// <summary>
        /// 重新订阅全部流程块与分支集合变化。
        /// </summary>
        /// <returns>无返回值。</returns>
        private void RebuildFlowSubscriptions()
        {
            ClearFlowSubscriptions();
            ObserveNodeCollection(Nodes);
        }

        /// <summary>
        /// 清理视图模型持有的流程模型事件订阅。
        /// </summary>
        /// <returns>无返回值。</returns>
        private void ClearFlowSubscriptions()
        {
            foreach (FlowNode node in _observedNodes.ToList())
            {
                node.Branches.CollectionChanged -= NodeBranches_CollectionChanged;
            }

            foreach (FlowBranch branch in _observedBranches.ToList())
            {
                branch.Nodes.CollectionChanged -= BranchNodes_CollectionChanged;
            }

            _observedNodes.Clear();
            _observedBranches.Clear();
        }

        /// <summary>
        /// 递归订阅指定流程块集合。
        /// </summary>
        /// <param name="nodes">需要订阅的流程块集合。</param>
        /// <returns>无返回值。</returns>
        private void ObserveNodeCollection(System.Collections.Generic.IEnumerable<FlowNode> nodes)
        {
            if (nodes == null)
            {
                return;
            }

            foreach (FlowNode node in nodes)
            {
                ObserveNode(node);
            }
        }

        /// <summary>
        /// 订阅单个流程块的分支集合变化。
        /// </summary>
        /// <param name="node">需要订阅的流程块。</param>
        /// <returns>无返回值。</returns>
        private void ObserveNode(FlowNode node)
        {
            if (node == null || !_observedNodes.Add(node))
            {
                return;
            }

            node.Branches.CollectionChanged += NodeBranches_CollectionChanged;
            foreach (FlowBranch branch in node.Branches)
            {
                ObserveBranch(branch);
            }
        }

        /// <summary>
        /// 订阅单个分支的节点集合变化。
        /// </summary>
        /// <param name="branch">需要订阅的分支。</param>
        /// <returns>无返回值。</returns>
        private void ObserveBranch(FlowBranch branch)
        {
            if (branch == null || !_observedBranches.Add(branch))
            {
                return;
            }

            branch.Nodes.CollectionChanged += BranchNodes_CollectionChanged;
            ObserveNodeCollection(branch.Nodes);
        }

        /// <summary>
        /// 处理流程块分支集合变化并刷新 Goto 目标。
        /// </summary>
        /// <param name="sender">发生变化的分支集合。</param>
        /// <param name="e">集合变化参数。</param>
        /// <returns>无返回值。</returns>
        private void NodeBranches_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            RebuildFlowSubscriptions();
            RefreshGotoTargetNodes();
        }

        /// <summary>
        /// 处理分支内流程块集合变化并刷新 Goto 目标。
        /// </summary>
        /// <param name="sender">发生变化的分支内流程块集合。</param>
        /// <param name="e">集合变化参数。</param>
        /// <returns>无返回值。</returns>
        private void BranchNodes_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            RebuildFlowSubscriptions();
            RefreshGotoTargetNodes();
        }

        /// <summary>
        /// 判断当前是否可以编辑分支。
        /// </summary>
        /// <param name="parameter">命令参数。</param>
        /// <returns>可编辑时返回 true，否则返回 false。</returns>
        private bool CanEditBranches(object parameter)
        {
            return CanEditVisionWorkflow && SelectedNode != null && SelectedNode.CanConfigureBranches;
        }

        /// <summary>
        /// 给当前分支块或线程块添加分支。
        /// </summary>
        /// <param name="parameter">命令参数。</param>
        /// <returns>无返回值。</returns>
        private void AddBranch(object parameter)
        {
            if (!CanEditBranches(parameter))
            {
                return;
            }

            string conditionValue = SelectedNode.IsSwitchBlock ? "条件" + (SelectedNode.Branches.Count + 1) : "Thread" + (SelectedNode.Branches.Count + 1);
            SelectedNode.AddBranch(conditionValue);
            RefreshGotoTargetNodes();
        }

        /// <summary>
        /// 删除当前分支块或线程块选中的分支。
        /// </summary>
        /// <param name="parameter">命令参数。</param>
        /// <returns>无返回值。</returns>
        private void RemoveBranch(object parameter)
        {
            if (!CanEditBranches(parameter))
            {
                return;
            }

            SelectedNode.RemoveSelectedBranch();
            RefreshGotoTargetNodes();
        }
    }
}
