using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows.Input;
using MyFlowChart.Models;

namespace MyFlowChart.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private readonly ObservableCollection<FlowNode> _nodes = new ObservableCollection<FlowNode>();
        private readonly ObservableCollection<FlowNode> _gotoTargetNodes = new ObservableCollection<FlowNode>();
        private readonly System.Collections.Generic.HashSet<FlowNode> _observedNodes = new System.Collections.Generic.HashSet<FlowNode>();
        private readonly System.Collections.Generic.HashSet<FlowBranch> _observedBranches = new System.Collections.Generic.HashSet<FlowBranch>();
        private FlowNode _selectedNode;
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
        /// 初始化主窗口视图模型。
        /// </summary>
        /// <returns>无返回值。</returns>
        public MainWindowViewModel()
        {
            _nodes.CollectionChanged += Nodes_CollectionChanged;
            RebuildFlowSubscriptions();
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
            return SelectedNode != null && SelectedNode.CanConfigureBranches;
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
