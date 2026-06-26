using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
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

        public static readonly DependencyProperty SelectedNodeProperty =
            DependencyProperty.Register(
                nameof(SelectedNode),
                typeof(FlowNode),
                typeof(FlowChartControl),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedNodeChanged));

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

        private const double NormalNodeWidth = 104;
        private const double NormalNodeHeight = 42;
        private const double StartNodeWidth = 76;
        private const double StartNodeHeight = 44;
        private const double EndNodeSize = 42;
        private const double GotoNodeWidth = 116;
        private const double GotoNodeHeight = 38;
        private const double MainX = 80;
        private const double StartY = 28;
        private const double VerticalGap = NormalNodeHeight;
        private const double BranchGapX = 148;
        private const double BranchDropY = VerticalGap;
        private const double ScenePadding = 16;
        private const double BranchMergeGapY = VerticalGap;
        private const double MinSceneWidth = 2600;
        private const double MinSceneHeight = 1800;
        private const double MinZoom = 0.5;
        private const double MaxZoom = 2.5;
        private const double ZoomStep = 0.1;
        private const double RunningOutlinePadding = 10;
        private const double RunningOutlineArmLength = 16;
        private const double RunningOutlineCornerRadius = 8;
        private const double RunningOutlineStrokeThickness = 4;

        private readonly Dictionary<Guid, NodeLayoutInfo> _nodeLayouts = new Dictionary<Guid, NodeLayoutInfo>();
        private readonly HashSet<FlowNode> _observedNodes = new HashSet<FlowNode>();
        private readonly HashSet<FlowBranch> _observedBranches = new HashSet<FlowBranch>();
        private FlowNode _clipboardNode;
        private CancellationTokenSource _runCancellation;
        private bool _isPanning;
        private bool _isEnsuringDefaultBlocks;
        private bool _isDraggingMiniMapRoi;
        private bool _isMiniMapVisible = true;
        private Point _panStartPoint;
        private Point _miniMapDragStartPoint;
        private double _panStartHorizontalOffset;
        private double _panStartVerticalOffset;
        private double _miniMapDragStartHorizontalOffset;
        private double _miniMapDragStartVerticalOffset;

        /// <summary>
        /// 初始化流程图控件并创建默认开始、结束算子块。
        /// </summary>
        /// <returns>无返回值。</returns>
        public FlowChartControl()
        {
            InitializeComponent();
            Nodes = new ObservableCollection<FlowNode>();
            EnsureDefaultBlocks();
            RenderFlow();
            UpdateSceneWorldSize();
            Dispatcher.BeginInvoke(new Action(UpdateMiniMap), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        public ObservableCollection<FlowNode> Nodes
        {
            get { return (ObservableCollection<FlowNode>)GetValue(NodesProperty); }
            set { SetValue(NodesProperty, value); }
        }

        public FlowNode SelectedNode
        {
            get { return (FlowNode)GetValue(SelectedNodeProperty); }
            set { SetValue(SelectedNodeProperty, value); }
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

        /// <summary>
        /// 按流程块顺序运行流程，并按块内算子顺序模拟执行。
        /// </summary>
        /// <returns>返回异步执行任务。</returns>
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
                await RunNodeCollectionAsync(Nodes, context, _runCancellation.Token);
            }
            catch (OperationCanceledException)
            {
                MarkStopped(currentNode, stopwatch);
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

        /// <summary>
        /// 停止当前正在运行的流程。
        /// </summary>
        /// <returns>无返回值。</returns>
        public void Stop()
        {
            if (_runCancellation != null && !_runCancellation.IsCancellationRequested)
            {
                _runCancellation.Cancel();
            }
        }

        /// <summary>
        /// 处理流程块集合替换并重新绑定集合事件。
        /// </summary>
        /// <param name="d">触发属性变化的流程图控件。</param>
        /// <param name="e">流程块集合变化参数。</param>
        /// <returns>无返回值。</returns>
        private static void OnNodesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            FlowChartControl control = (FlowChartControl)d;

            INotifyCollectionChanged oldCollection = e.OldValue as INotifyCollectionChanged;
            if (oldCollection != null)
            {
                oldCollection.CollectionChanged -= control.Nodes_CollectionChanged;
            }

            control.ClearFlowSubscriptions();

            INotifyCollectionChanged newCollection = e.NewValue as INotifyCollectionChanged;
            if (newCollection != null)
            {
                newCollection.CollectionChanged += control.Nodes_CollectionChanged;
            }

            control.EnsureDefaultBlocks();
            control.RebuildFlowSubscriptions();
            control.RenumberNodes();
        }

        /// <summary>
        /// 处理当前选中流程块变化并刷新选中样式。
        /// </summary>
        /// <param name="d">触发属性变化的流程图控件。</param>
        /// <param name="e">选中流程块变化参数。</param>
        /// <returns>无返回值。</returns>
        private static void OnSelectedNodeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            FlowChartControl control = (FlowChartControl)d;
            FlowNode oldNode = e.OldValue as FlowNode;
            FlowNode newNode = e.NewValue as FlowNode;

            if (oldNode != null)
            {
                oldNode.IsSelected = false;
            }

            if (newNode != null)
            {
                newNode.IsSelected = true;
            }

            control.RenderFlow();
        }

        /// <summary>
        /// 处理流程块集合变化并维护固定开始、结束块。
        /// </summary>
        /// <param name="sender">触发变化的流程块集合。</param>
        /// <param name="e">集合变化事件参数。</param>
        /// <returns>无返回值。</returns>
        private void Nodes_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            EnsureDefaultBlocks();
            RebuildFlowSubscriptions();
            RenumberNodes();
        }

        /// <summary>
        /// 重新订阅全部流程块、分支和分支节点的变化事件。
        /// </summary>
        /// <returns>无返回值。</returns>
        private void RebuildFlowSubscriptions()
        {
            ClearFlowSubscriptions();
            ObserveNodeCollection(Nodes);
        }

        /// <summary>
        /// 清理当前流程图控件持有的模型事件订阅。
        /// </summary>
        /// <returns>无返回值。</returns>
        private void ClearFlowSubscriptions()
        {
            foreach (FlowNode node in _observedNodes.ToList())
            {
                node.PropertyChanged -= FlowNode_PropertyChanged;
                node.Branches.CollectionChanged -= NodeBranches_CollectionChanged;
            }

            foreach (FlowBranch branch in _observedBranches.ToList())
            {
                branch.PropertyChanged -= FlowBranch_PropertyChanged;
                branch.Nodes.CollectionChanged -= BranchNodes_CollectionChanged;
            }

            _observedNodes.Clear();
            _observedBranches.Clear();
        }

        /// <summary>
        /// 递归订阅指定流程块集合及其分支下的流程块。
        /// </summary>
        /// <param name="nodes">需要订阅的流程块集合。</param>
        /// <returns>无返回值。</returns>
        private void ObserveNodeCollection(IEnumerable<FlowNode> nodes)
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
        /// 订阅单个流程块的属性变化和分支集合变化。
        /// </summary>
        /// <param name="node">需要订阅的流程块。</param>
        /// <returns>无返回值。</returns>
        private void ObserveNode(FlowNode node)
        {
            if (node == null || !_observedNodes.Add(node))
            {
                return;
            }

            node.PropertyChanged += FlowNode_PropertyChanged;
            node.Branches.CollectionChanged += NodeBranches_CollectionChanged;

            foreach (FlowBranch branch in node.Branches)
            {
                ObserveBranch(branch);
            }
        }

        /// <summary>
        /// 订阅单个分支的属性变化和分支内流程块集合变化。
        /// </summary>
        /// <param name="branch">需要订阅的分支。</param>
        /// <returns>无返回值。</returns>
        private void ObserveBranch(FlowBranch branch)
        {
            if (branch == null || !_observedBranches.Add(branch))
            {
                return;
            }

            branch.PropertyChanged += FlowBranch_PropertyChanged;
            branch.Nodes.CollectionChanged += BranchNodes_CollectionChanged;
            ObserveNodeCollection(branch.Nodes);
        }

        /// <summary>
        /// 处理流程块属性变化并刷新画布显示。
        /// </summary>
        /// <param name="sender">发生变化的流程块。</param>
        /// <param name="e">属性变化参数。</param>
        /// <returns>无返回值。</returns>
        private void FlowNode_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            RefreshCanvas();
        }

        /// <summary>
        /// 处理流程块分支集合变化并刷新订阅和画布。
        /// </summary>
        /// <param name="sender">发生变化的分支集合。</param>
        /// <param name="e">集合变化参数。</param>
        /// <returns>无返回值。</returns>
        private void NodeBranches_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            RebuildFlowSubscriptions();
            RefreshCanvas();
        }

        /// <summary>
        /// 处理分支属性变化并刷新画布显示。
        /// </summary>
        /// <param name="sender">发生变化的分支。</param>
        /// <param name="e">属性变化参数。</param>
        /// <returns>无返回值。</returns>
        private void FlowBranch_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            RefreshCanvas();
        }

        /// <summary>
        /// 处理分支内流程块集合变化并刷新订阅和画布。
        /// </summary>
        /// <param name="sender">发生变化的分支内流程块集合。</param>
        /// <param name="e">集合变化参数。</param>
        /// <returns>无返回值。</returns>
        private void BranchNodes_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            RebuildFlowSubscriptions();
            RefreshCanvas();
        }

        /// <summary>
        /// 重新渲染流程图并同步地图尺寸和小地图。
        /// </summary>
        /// <returns>无返回值。</returns>
        private void RefreshCanvas()
        {
            RenderFlow();
            UpdateSceneWorldSize();
            Dispatcher.BeginInvoke(new Action(UpdateMiniMap), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        /// <summary>
        /// 根据拖拽数据判断流程图空白区域是否可接收算子。
        /// </summary>
        /// <param name="sender">触发拖拽事件的对象。</param>
        /// <param name="e">拖拽事件参数。</param>
        /// <returns>无返回值。</returns>
        private void FlowChart_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = CanDropOperatorToBlock(SelectedNode, e) && !IsNodeSource(e.OriginalSource as DependencyObject)
                ? DragDropEffects.Copy
                : DragDropEffects.None;
            e.Handled = true;
        }

        /// <summary>
        /// 将拖入流程图空白区域的算子追加到当前选中算子块。
        /// </summary>
        /// <param name="sender">触发放置事件的对象。</param>
        /// <param name="e">拖拽放置事件参数。</param>
        /// <returns>无返回值。</returns>
        private void FlowChart_Drop(object sender, DragEventArgs e)
        {
            if (IsNodeSource(e.OriginalSource as DependencyObject))
            {
                return;
            }

            DragOperatorData data = GetDragOperatorData(e);
            if (!CanDropOperatorToBlock(SelectedNode, e) || data == null)
            {
                return;
            }

            AddOperatorToBlock(SelectedNode, data.Name);
            e.Handled = true;
        }

        /// <summary>
        /// 打开连线中间加号按钮的小菜单。
        /// </summary>
        /// <param name="sender">触发点击事件的加号按钮。</param>
        /// <param name="e">按钮点击事件参数。</param>
        /// <returns>无返回值。</returns>
        private void ConnectorAddButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button == null || button.ContextMenu == null)
            {
                return;
            }

            button.ContextMenu.PlacementTarget = button;
            button.ContextMenu.IsOpen = true;
            e.Handled = true;
        }

        /// <summary>
        /// 在当前连线后插入一个新的流程块。
        /// </summary>
        /// <param name="sender">触发点击事件的菜单项。</param>
        /// <param name="e">菜单点击事件参数。</param>
        /// <returns>无返回值。</returns>
        private void AddBlockMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;
            ContextMenu menu = menuItem == null ? null : menuItem.Parent as ContextMenu;
            Button button = menu == null ? null : menu.PlacementTarget as Button;
            AddTarget target = button == null ? null : button.Tag as AddTarget;
            FlowBlockKind kind = GetMenuBlockKind(menuItem);

            AddBlock(target, kind);
            e.Handled = true;
        }

        /// <summary>
        /// 在当前连线位置粘贴已复制或剪切的流程块。
        /// </summary>
        /// <param name="sender">触发点击的菜单项。</param>
        /// <param name="e">菜单点击事件参数。</param>
        /// <returns>无返回值。</returns>
        private void PasteBlockMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;
            ContextMenu menu = menuItem == null ? null : menuItem.Parent as ContextMenu;
            Button button = menu == null ? null : menu.PlacementTarget as Button;
            AddTarget target = button == null ? null : button.Tag as AddTarget;
            if (target == null || _clipboardNode == null)
            {
                return;
            }

            FlowNode node = CloneNode(_clipboardNode, _clipboardNode.DisplayName);
            InsertNode(target, node);
            SelectedNode = node;
            RenderFlow();
            e.Handled = true;
        }

        /// <summary>
        /// 判断拖拽算子是否可以添加到指定流程块。
        /// </summary>
        /// <param name="targetNode">目标流程块。</param>
        /// <param name="e">拖拽事件参数。</param>
        /// <returns>可添加时返回 true，否则返回 false。</returns>
        private bool CanDropOperatorToBlock(FlowNode targetNode, DragEventArgs e)
        {
            return !IsRunning && targetNode != null && targetNode.CanConfigureOperators && GetDragOperatorData(e) != null;
        }

        /// <summary>
        /// 从拖拽事件中读取算子数据。
        /// </summary>
        /// <param name="e">拖拽事件参数。</param>
        /// <returns>返回有效算子数据；无效时返回 null。</returns>
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

        /// <summary>
        /// 根据目标位置添加流程块。
        /// </summary>
        /// <param name="target">添加目标。</param>
        /// <param name="kind">要添加的流程块类型。</param>
        /// <returns>无返回值。</returns>
        private void AddBlock(AddTarget target, FlowBlockKind kind)
        {
            if (target == null)
            {
                return;
            }

            FlowNode node = CreateBlock(kind);
            InsertNode(target, node);
            SelectedNode = node;
            RenderFlow();
        }

        /// <summary>
        /// 按指定插入目标插入流程块。
        /// </summary>
        /// <param name="target">插入目标。</param>
        /// <param name="node">要插入的流程块。</param>
        /// <returns>无返回值。</returns>
        private void InsertNode(AddTarget target, FlowNode node)
        {
            if (target == null || node == null)
            {
                return;
            }

            if (target.Branch != null)
            {
                target.Branch.Nodes.Insert(Math.Min(target.InsertIndex, target.Branch.Nodes.Count), node);
            }
            else if (Nodes != null)
            {
                int endIndex = Nodes.ToList().FindIndex(o => o.IsEndBlock);
                int insertIndex = Math.Min(target.InsertIndex, Nodes.Count);
                if (endIndex >= 0 && insertIndex > endIndex)
                {
                    insertIndex = endIndex;
                }

                Nodes.Insert(insertIndex, node);
            }
        }

        /// <summary>
        /// 根据菜单项标记获取要创建的块类型。
        /// </summary>
        /// <param name="menuItem">触发点击的菜单项。</param>
        /// <returns>返回要创建的流程块类型。</returns>
        private FlowBlockKind GetMenuBlockKind(MenuItem menuItem)
        {
            string tag = menuItem == null ? null : menuItem.Tag as string;
            switch (tag)
            {
                case "Goto":
                    return FlowBlockKind.Goto;
                case "Switch":
                    return FlowBlockKind.Switch;
                case "Thread":
                    return FlowBlockKind.Thread;
                default:
                    return FlowBlockKind.OperatorBlock;
            }
        }

        /// <summary>
        /// 根据块类型创建新的流程块。
        /// </summary>
        /// <param name="kind">要创建的流程块类型。</param>
        /// <returns>返回新建的流程块。</returns>
        private FlowNode CreateBlock(FlowBlockKind kind)
        {
            switch (kind)
            {
                case FlowBlockKind.Goto:
                    return FlowNode.CreateGotoBlock(CreateBlockName("Goto"));
                case FlowBlockKind.Switch:
                    return FlowNode.CreateSwitchBlock(CreateBlockName("Switch"));
                case FlowBlockKind.Thread:
                    return FlowNode.CreateThreadBlock(CreateBlockName("Thread"));
                default:
                    return FlowNode.CreateOperatorBlock(CreateBlockName("Block"));
            }
        }

        /// <summary>
        /// 向指定算子块追加一个算子。
        /// </summary>
        /// <param name="targetNode">目标算子块。</param>
        /// <param name="operatorName">算子名称。</param>
        /// <returns>无返回值。</returns>
        private void AddOperatorToBlock(FlowNode targetNode, string operatorName)
        {
            if (targetNode == null || !targetNode.CanConfigureOperators || string.IsNullOrWhiteSpace(operatorName))
            {
                return;
            }

            FlowOperator flowOperator = new FlowOperator
            {
                OperatorName = operatorName,
                DisplayName = CreateOperatorDisplayName(targetNode, operatorName),
                Status = FlowNodeStatus.NotRun
            };

            targetNode.Operators.Add(flowOperator);
            targetNode.SelectedOperator = flowOperator;
            RenderFlow();
        }

        /// <summary>
        /// 创建新的流程块默认名称。
        /// </summary>
        /// <param name="prefix">流程块名称前缀。</param>
        /// <returns>返回流程块名称。</returns>
        private string CreateBlockName(string prefix)
        {
            int number = CountNodesByPrefix(Nodes, prefix) + 1;
            return prefix + number;
        }

        /// <summary>
        /// 递归统计指定名称前缀的流程块数量。
        /// </summary>
        /// <param name="nodes">待统计的流程块集合。</param>
        /// <param name="prefix">名称前缀。</param>
        /// <returns>返回匹配数量。</returns>
        private int CountNodesByPrefix(IEnumerable<FlowNode> nodes, string prefix)
        {
            int count = 0;
            if (nodes == null)
            {
                return count;
            }

            foreach (FlowNode node in nodes)
            {
                if (node.DisplayName != null && node.DisplayName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    count++;
                }

                foreach (FlowBranch branch in node.Branches)
                {
                    count += CountNodesByPrefix(branch.Nodes, prefix);
                }
            }

            return count;
        }

        /// <summary>
        /// 创建块内算子的显示名称。
        /// </summary>
        /// <param name="targetNode">目标算子块。</param>
        /// <param name="operatorName">算子名称。</param>
        /// <returns>返回块内算子显示名称。</returns>
        private string CreateOperatorDisplayName(FlowNode targetNode, string operatorName)
        {
            int number = targetNode == null ? 1 : targetNode.Operators.Count(o => o.OperatorName == operatorName) + 1;
            return operatorName + "_" + number;
        }

        /// <summary>
        /// 重置流程块和块内算子的运行状态。
        /// </summary>
        /// <returns>无返回值。</returns>
        private void ResetNodes()
        {
            ResetNodeCollection(Nodes);
        }

        /// <summary>
        /// 递归重置流程块集合状态。
        /// </summary>
        /// <param name="nodes">要重置的流程块集合。</param>
        /// <returns>无返回值。</returns>
        private void ResetNodeCollection(IEnumerable<FlowNode> nodes)
        {
            if (nodes == null)
            {
                return;
            }

            foreach (FlowNode node in nodes)
            {
                node.Status = FlowNodeStatus.NotRun;
                node.ElapsedMilliseconds = 0;

                foreach (FlowOperator flowOperator in node.Operators)
                {
                    flowOperator.Status = FlowNodeStatus.NotRun;
                    flowOperator.ElapsedMilliseconds = 0;
                }

                foreach (FlowBranch branch in node.Branches)
                {
                    ResetNodeCollection(branch.Nodes);
                }
            }
        }

        /// <summary>
        /// 重置单个流程块及其内部算子的运行状态。
        /// </summary>
        /// <param name="node">需要重置的流程块。</param>
        /// <returns>无返回值。</returns>
        private void ResetNode(FlowNode node)
        {
            if (node == null)
            {
                return;
            }

            node.Status = FlowNodeStatus.NotRun;
            node.ElapsedMilliseconds = 0;

            foreach (FlowOperator flowOperator in node.Operators)
            {
                flowOperator.Status = FlowNodeStatus.NotRun;
                flowOperator.ElapsedMilliseconds = 0;
            }

            foreach (FlowBranch branch in node.Branches)
            {
                ResetNodeCollection(branch.Nodes);
            }
        }

        /// <summary>
        /// 按当前集合顺序刷新流程块序号和末尾标记。
        /// </summary>
        /// <returns>无返回值。</returns>
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

            RenderFlow();
            UpdateSceneWorldSize();
            Dispatcher.BeginInvoke(new Action(UpdateMiniMap), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        /// <summary>
        /// 确保流程图始终包含不可删除的开始和结束算子块。
        /// </summary>
        /// <returns>无返回值。</returns>
        private void EnsureDefaultBlocks()
        {
            if (_isEnsuringDefaultBlocks || Nodes == null)
            {
                return;
            }

            _isEnsuringDefaultBlocks = true;

            try
            {
                FlowNode startNode = Nodes.FirstOrDefault(o => o.IsStartBlock);
                if (startNode == null)
                {
                    Nodes.Insert(0, FlowNode.CreateStartBlock());
                }
                else if (Nodes.IndexOf(startNode) > 0)
                {
                    Nodes.Move(Nodes.IndexOf(startNode), 0);
                }

                FlowNode endNode = Nodes.FirstOrDefault(o => o.IsEndBlock);
                if (endNode == null)
                {
                    Nodes.Add(FlowNode.CreateEndBlock());
                }
                else if (Nodes.IndexOf(endNode) != Nodes.Count - 1)
                {
                    Nodes.Move(Nodes.IndexOf(endNode), Nodes.Count - 1);
                }
            }
            finally
            {
                _isEnsuringDefaultBlocks = false;
            }
        }

        /// <summary>
        /// 渲染主画布和小地图画布。
        /// </summary>
        /// <returns>无返回值。</returns>
        private void RenderFlow()
        {
            if (flowCanvas == null || miniMapCanvas == null)
            {
                return;
            }

            RenderFlowToCanvas(flowCanvas, true);
            RenderFlowToCanvas(miniMapCanvas, false);
        }

        /// <summary>
        /// 将流程图渲染到指定画布。
        /// </summary>
        /// <param name="canvas">目标画布。</param>
        /// <param name="isInteractive">是否生成可交互元素。</param>
        /// <returns>无返回值。</returns>
        private void RenderFlowToCanvas(Canvas canvas, bool isInteractive)
        {
            canvas.Children.Clear();
            if (isInteractive)
            {
                _nodeLayouts.Clear();
            }

            if (Nodes == null || Nodes.Count == 0)
            {
                return;
            }

            double nextY = StartY;
            for (int i = 0; i < Nodes.Count; i++)
            {
                FlowNode node = Nodes[i];
                bool hasNext = i < Nodes.Count - 1;
                FlowNode nextNode = hasNext ? Nodes[i + 1] : null;
                NodeLayoutInfo layout = RenderNode(canvas, node, MainX, nextY, isInteractive);

                if (node.CanConfigureBranches && nextNode != null)
                {
                    double mergeY = GetBranchMergeY(node, layout);
                    RenderBranchSplit(canvas, node, layout, mergeY, isInteractive);
                    nextY = mergeY + BranchMergeGapY;
                    RenderVerticalConnector(canvas, new Point(layout.CenterX, mergeY), new Point(layout.CenterX, nextY), isInteractive, new AddTarget(null, i + 1));
                }
                else if (nextNode != null)
                {
                    double toY = layout.Bottom + VerticalGap;
                    RenderVerticalConnector(canvas, layout.BottomCenter, new Point(layout.CenterX, toY), isInteractive, new AddTarget(null, i + 1));
                    nextY = toY;
                }
            }

            RenderGotoLinks(canvas, isInteractive);
        }

        /// <summary>
        /// 根据分支数量和最长分支节点数计算合流线位置。
        /// </summary>
        /// <param name="node">分支源流程块。</param>
        /// <param name="sourceLayout">分支源流程块布局。</param>
        /// <returns>返回合流线的纵坐标。</returns>
        private double GetBranchMergeY(FlowNode node, NodeLayoutInfo sourceLayout)
        {
            double minimumMergeY = sourceLayout.Bottom + BranchMergeGapY;
            if (node.Branches.Count == 0)
            {
                return minimumMergeY;
            }

            double branchNodeStartY = sourceLayout.Bottom + BranchDropY;
            double deepestBranchBottom = sourceLayout.Bottom;
            for (int i = 0; i < node.Branches.Count; i++)
            {
                FlowBranch branch = node.Branches[i];
                if (branch.Nodes.Count == 0)
                {
                    continue;
                }

                deepestBranchBottom = Math.Max(deepestBranchBottom, GetNodeCollectionBottom(branch.Nodes, branchNodeStartY));
            }

            return Math.Max(deepestBranchBottom + BranchMergeGapY, minimumMergeY);
        }

        /// <summary>
        /// 递归计算一组流程块在指定起始纵坐标下的最底部位置。
        /// </summary>
        /// <param name="nodes">流程块集合。</param>
        /// <param name="startY">首个流程块顶部纵坐标。</param>
        /// <returns>返回最底部纵坐标。</returns>
        private double GetNodeCollectionBottom(IList<FlowNode> nodes, double startY)
        {
            if (nodes == null || nodes.Count == 0)
            {
                return startY;
            }

            double currentTop = startY;
            double deepestBottom = startY;
            for (int i = 0; i < nodes.Count; i++)
            {
                FlowNode node = nodes[i];
                double nodeHeight = GetNodeHeight(node);
                double nodeBottom = currentTop + nodeHeight;
                deepestBottom = Math.Max(deepestBottom, nodeBottom);

                if (node.CanConfigureBranches)
                {
                    NodeLayoutInfo layout = new NodeLayoutInfo(node, 0, currentTop, GetNodeWidth(node), nodeHeight);
                    double mergeY = GetBranchMergeY(node, layout);
                    deepestBottom = Math.Max(deepestBottom, mergeY);
                    currentTop = mergeY + VerticalGap;
                }
                else
                {
                    currentTop = nodeBottom + VerticalGap;
                }
            }

            return deepestBottom;
        }

        /// <summary>
        /// 渲染分支拆分线和分支内节点。
        /// </summary>
        /// <param name="canvas">目标画布。</param>
        /// <param name="node">分支源节点。</param>
        /// <param name="sourceLayout">源节点布局信息。</param>
        /// <param name="mergeY">合流纵坐标。</param>
        /// <param name="isInteractive">是否生成可交互元素。</param>
        /// <returns>无返回值。</returns>
        private void RenderBranchSplit(Canvas canvas, FlowNode node, NodeLayoutInfo sourceLayout, double mergeY, bool isInteractive)
        {
            if (node.Branches.Count == 0)
            {
                return;
            }

            double sourceX = sourceLayout.CenterX;
            double lastBranchX = GetLastDirectBranchX(node, sourceX);

            if (node.Branches.Count > 1)
            {
                double lastBranchLeft = lastBranchX - sourceLayout.Width / 2.0;
                RenderPolyline(
                    canvas,
                    new[] { new Point(sourceLayout.Right, sourceLayout.CenterY), new Point(lastBranchLeft, sourceLayout.CenterY) },
                    false,
                    false);
            }

            RenderPolyline(
                canvas,
                new[] { new Point(sourceX, mergeY), new Point(lastBranchX, mergeY) },
                false,
                false);

            for (int i = 0; i < node.Branches.Count; i++)
            {
                FlowBranch branch = node.Branches[i];
                double branchX = GetBranchLaneX(node, sourceX, i);
                double branchNodeY = sourceLayout.Bottom + BranchDropY;
                NodeLayoutInfo branchStartLayout = i == 0
                    ? sourceLayout
                    : RenderParallelBranchBlock(canvas, node, branch, branchX, sourceLayout.Top, isInteractive);

                if (branch.Nodes.Count == 0)
                {
                    RenderVerticalConnector(canvas, branchStartLayout.BottomCenter, new Point(branchX, mergeY), isInteractive, new AddTarget(branch, 0));
                    continue;
                }

                RenderVerticalConnector(canvas, branchStartLayout.BottomCenter, new Point(branchX, branchNodeY), isInteractive, new AddTarget(branch, 0));
                RenderBranchNodes(canvas, branch, branchX, branchNodeY, mergeY, isInteractive);
            }
        }

        /// <summary>
        /// 计算指定分支的泳道横坐标。
        /// </summary>
        /// <param name="node">分支源流程块。</param>
        /// <param name="sourceX">分支源块中心横坐标。</param>
        /// <param name="branchIndex">分支序号。</param>
        /// <returns>返回分支泳道中心横坐标。</returns>
        private double GetBranchLaneX(FlowNode node, double sourceX, int branchIndex)
        {
            if (branchIndex <= 0)
            {
                return sourceX;
            }

            int laneOffset = 0;
            for (int i = 0; i < branchIndex; i++)
            {
                laneOffset += GetBranchLaneCount(node.Branches[i]);
            }

            return sourceX + laneOffset * BranchGapX;
        }

        /// <summary>
        /// 计算最后一个直接分支的横坐标。
        /// </summary>
        /// <param name="node">分支源流程块。</param>
        /// <param name="sourceX">分支源块中心横坐标。</param>
        /// <returns>返回最后一个直接分支中心横坐标。</returns>
        private double GetLastDirectBranchX(FlowNode node, double sourceX)
        {
            return node == null || node.Branches.Count == 0
                ? sourceX
                : GetBranchLaneX(node, sourceX, node.Branches.Count - 1);
        }

        /// <summary>
        /// 计算一个直接分支占用的泳道数量。
        /// </summary>
        /// <param name="branch">直接分支。</param>
        /// <returns>返回分支子树占用的泳道数量。</returns>
        private int GetBranchLaneCount(FlowBranch branch)
        {
            return Math.Max(1, branch == null ? 1 : GetNodeCollectionLaneCount(branch.Nodes));
        }

        /// <summary>
        /// 计算一组流程块占用的最大泳道数量。
        /// </summary>
        /// <param name="nodes">流程块集合。</param>
        /// <returns>返回流程块集合占用的最大泳道数量。</returns>
        private int GetNodeCollectionLaneCount(IList<FlowNode> nodes)
        {
            if (nodes == null || nodes.Count == 0)
            {
                return 1;
            }

            int laneCount = 1;
            for (int i = 0; i < nodes.Count; i++)
            {
                laneCount = Math.Max(laneCount, GetNodeLaneCount(nodes[i]));
            }

            return laneCount;
        }

        /// <summary>
        /// 计算单个流程块及其分支子树占用的泳道数量。
        /// </summary>
        /// <param name="node">流程块。</param>
        /// <returns>返回流程块占用的泳道数量。</returns>
        private int GetNodeLaneCount(FlowNode node)
        {
            if (node == null || !node.CanConfigureBranches || node.Branches.Count == 0)
            {
                return 1;
            }

            int laneCount = 0;
            for (int i = 0; i < node.Branches.Count; i++)
            {
                laneCount += GetBranchLaneCount(node.Branches[i]);
            }

            return Math.Max(1, laneCount);
        }

        /// <summary>
        /// 递归渲染分支内的流程块，并支持分支中的 Thread 或 Switch 继续展开分支。
        /// </summary>
        /// <param name="canvas">目标画布。</param>
        /// <param name="branch">当前分支。</param>
        /// <param name="centerX">分支中心横坐标。</param>
        /// <param name="startY">首个流程块顶部纵坐标。</param>
        /// <param name="terminalY">当前分支合流纵坐标。</param>
        /// <param name="isInteractive">是否生成可交互元素。</param>
        /// <returns>无返回值。</returns>
        private void RenderBranchNodes(Canvas canvas, FlowBranch branch, double centerX, double startY, double terminalY, bool isInteractive)
        {
            double branchNodeY = startY;
            for (int i = 0; i < branch.Nodes.Count; i++)
            {
                FlowNode branchNode = branch.Nodes[i];
                bool isLast = i == branch.Nodes.Count - 1;
                NodeLayoutInfo branchLayout = RenderNode(canvas, branchNode, centerX, branchNodeY, isInteractive);

                if (branchNode.CanConfigureBranches)
                {
                    double mergeY = GetBranchMergeY(branchNode, branchLayout);
                    RenderBranchSplit(canvas, branchNode, branchLayout, mergeY, isInteractive);
                    double nextY = isLast ? terminalY : mergeY + BranchMergeGapY;
                    RenderVerticalConnector(canvas, new Point(branchLayout.CenterX, mergeY), new Point(branchLayout.CenterX, nextY), isInteractive, new AddTarget(branch, i + 1));
                    branchNodeY = nextY;
                    continue;
                }

                double toY = isLast ? terminalY : branchLayout.Bottom + VerticalGap;
                RenderVerticalConnector(canvas, branchLayout.BottomCenter, new Point(centerX, toY), isInteractive, new AddTarget(branch, i + 1));
                branchNodeY = toY;
            }
        }

        /// <summary>
        /// 渲染右侧平行流程的起始块。
        /// </summary>
        /// <param name="canvas">目标画布。</param>
        /// <param name="parentNode">Thread 或 Switch 主流程块。</param>
        /// <param name="branch">右侧平行分支。</param>
        /// <param name="centerX">中心横坐标。</param>
        /// <param name="topY">顶部纵坐标。</param>
        /// <param name="isInteractive">是否生成交互元素。</param>
        /// <returns>返回平行流程起始块布局信息。</returns>
        private NodeLayoutInfo RenderParallelBranchBlock(Canvas canvas, FlowNode parentNode, FlowBranch branch, double centerX, double topY, bool isInteractive)
        {
            double width = GetNodeWidth(parentNode);
            double height = GetNodeHeight(parentNode);
            double left = centerX - width / 2.0;
            double visualPadding = GetNodeVisualPadding(parentNode);
            FrameworkElement element = CreateParallelBranchElement(parentNode, branch, width, height, isInteractive);
            Canvas.SetLeft(element, left - visualPadding);
            Canvas.SetTop(element, topY - visualPadding);
            canvas.Children.Add(element);
            return new NodeLayoutInfo(parentNode, left, topY, width, height);
        }

        /// <summary>
        /// 渲染单个流程块。
        /// </summary>
        /// <param name="canvas">目标画布。</param>
        /// <param name="node">流程块。</param>
        /// <param name="centerX">中心横坐标。</param>
        /// <param name="topY">顶部纵坐标。</param>
        /// <param name="isInteractive">是否生成交互元素。</param>
        /// <returns>返回节点布局信息。</returns>
        private NodeLayoutInfo RenderNode(Canvas canvas, FlowNode node, double centerX, double topY, bool isInteractive)
        {
            double width = GetNodeWidth(node);
            double height = GetNodeHeight(node);
            double left = centerX - width / 2.0;
            double visualPadding = GetNodeVisualPadding(node);
            FrameworkElement element = CreateNodeElement(node, width, height, isInteractive);
            Canvas.SetLeft(element, left - visualPadding);
            Canvas.SetTop(element, topY - visualPadding);
            canvas.Children.Add(element);

            NodeLayoutInfo layout = new NodeLayoutInfo(node, left, topY, width, height);
            if (isInteractive)
            {
                _nodeLayouts[node.Id] = layout;
            }

            return layout;
        }

        /// <summary>
        /// 创建流程块显示元素。
        /// </summary>
        /// <param name="node">流程块。</param>
        /// <param name="width">元素宽度。</param>
        /// <param name="height">元素高度。</param>
        /// <param name="isInteractive">是否生成交互元素。</param>
        /// <returns>返回流程块元素。</returns>
        private FrameworkElement CreateNodeElement(FlowNode node, double width, double height, bool isInteractive)
        {
            double visualPadding = GetNodeVisualPadding(node);
            bool useInnerSelectionBorder = UsesInnerSelectionBorder(node);
            bool hideOuterBorder = node.IsThreadBlock;
            Grid host = CreateNodeHost(width, height, visualPadding);

            if (node.Status == FlowNodeStatus.Running)
            {
                host.Children.Add(CreateRunningOutline(width, height));
            }

            Border border = new Border
            {
                Width = width,
                Height = height,
                Opacity = node.IsEnabled ? 1.0 : 0.45,
                Background = hideOuterBorder ? Brushes.Transparent : Brushes.White,
                BorderBrush = hideOuterBorder ? Brushes.Transparent : new SolidColorBrush(node.IsSelected ? Color.FromRgb(30, 155, 255) : Color.FromRgb(88, 217, 192)),
                BorderThickness = hideOuterBorder ? new Thickness(0) : new Thickness(useInnerSelectionBorder ? 0 : node.IsSelected ? 2 : 1),
                CornerRadius = new CornerRadius(node.IsEndBlock ? height / 2.0 : 6),
                Cursor = Cursors.Hand,
                Tag = node,
                Margin = new Thickness(visualPadding),
                SnapsToDevicePixels = true
            };

            if (isInteractive)
            {
                border.MouseLeftButtonDown += NodeElement_MouseLeftButtonDown;
                border.MouseRightButtonDown += NodeElement_MouseRightButtonDown;
                border.ContextMenu = CreateNodeContextMenu(node);
                border.AllowDrop = true;
                border.DragOver += NodeElement_DragOver;
                border.Drop += NodeElement_Drop;
            }

            Grid grid = new Grid
            {
                ClipToBounds = false
            };
            border.Child = grid;

            if (node.IsStartBlock)
            {
                grid.Children.Add(CreateStartIcon());
            }
            else if (node.IsEndBlock)
            {
                grid.Children.Add(new Rectangle
                {
                    Width = 15,
                    Height = 15,
                    RadiusX = 2,
                    RadiusY = 2,
                    Fill = new SolidColorBrush(Color.FromRgb(233, 68, 68)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                });
            }
            else if (node.IsGotoBlock)
            {
                grid.Children.Add(CreateFlagShape(width, height, true));
                grid.Children.Add(CreateLeftBadge("▶"));
                grid.Children.Add(CreateNodeText(node.DisplayName));
                grid.Children.Add(CreateElapsedText(node));
            }
            else if (node.IsSwitchBlock)
            {
                grid.Children.Add(CreateSwitchBracket(width, height));
                grid.Children.Add(CreateNodeText(node.DisplayName));
                grid.Children.Add(CreateLeftBadge("1"));
                grid.Children.Add(CreateElapsedText(node));
            }
            else if (node.IsThreadBlock)
            {
                grid.Children.Add(CreateThreadShape(width, height));
                grid.Children.Add(CreateLeftBadge("1"));
                grid.Children.Add(CreateNodeText(node.DisplayName));
                grid.Children.Add(CreateMinusCircle());
                grid.Children.Add(CreateElapsedText(node));
            }
            else
            {
                grid.Children.Add(CreateOperatorBlockShape(width, height));
                grid.Children.Add(CreateLeftBadge("▶"));
                grid.Children.Add(CreateNodeText(node.DisplayName));
                grid.Children.Add(CreateElapsedText(node));
            }

            if (node.IsSelected && useInnerSelectionBorder)
            {
                grid.Children.Add(CreateSelectionOutline(width, height, node));
            }

            host.Children.Add(border);
            return host;
        }

        /// <summary>
        /// 创建右侧平行流程起始块的显示元素。
        /// </summary>
        /// <param name="node">所属 Thread 或 Switch 流程块。</param>
        /// <param name="branch">当前平行流程分支。</param>
        /// <param name="width">元素宽度。</param>
        /// <param name="height">元素高度。</param>
        /// <param name="isInteractive">是否生成交互事件。</param>
        /// <returns>返回平行流程起始块元素。</returns>
        private FrameworkElement CreateParallelBranchElement(FlowNode node, FlowBranch branch, double width, double height, bool isInteractive)
        {
            double visualPadding = GetNodeVisualPadding(node);
            bool useInnerSelectionBorder = UsesInnerSelectionBorder(node);
            bool hideOuterBorder = node.IsThreadBlock;
            Grid host = CreateNodeHost(width, height, visualPadding);

            if (node.Status == FlowNodeStatus.Running)
            {
                host.Children.Add(CreateRunningOutline(width, height));
            }

            Border border = new Border
            {
                Width = width,
                Height = height,
                Opacity = node.IsEnabled ? 1.0 : 0.45,
                Background = hideOuterBorder ? Brushes.Transparent : Brushes.White,
                BorderBrush = hideOuterBorder ? Brushes.Transparent : new SolidColorBrush(node.IsSelected ? Color.FromRgb(30, 155, 255) : Color.FromRgb(88, 217, 192)),
                BorderThickness = hideOuterBorder ? new Thickness(0) : new Thickness(useInnerSelectionBorder ? 0 : node.IsSelected ? 2 : 1),
                CornerRadius = new CornerRadius(6),
                Cursor = Cursors.Hand,
                Tag = node,
                Margin = new Thickness(visualPadding),
                SnapsToDevicePixels = true
            };

            if (isInteractive)
            {
                border.MouseLeftButtonDown += NodeElement_MouseLeftButtonDown;
                border.MouseRightButtonDown += NodeElement_MouseRightButtonDown;
                border.ContextMenu = CreateNodeContextMenu(node);
            }

            Grid grid = new Grid
            {
                ClipToBounds = false
            };
            border.Child = grid;

            if (node.IsSwitchBlock)
            {
                grid.Children.Add(CreateSwitchBracket(width, height));
            }
            else
            {
                grid.Children.Add(CreateThreadShape(width, height));
                grid.Children.Add(CreateMinusCircle());
            }

            grid.Children.Add(CreateLeftBadge(branch.Sequence.ToString()));
            grid.Children.Add(CreateNodeText(GetParallelBranchDisplayName(node, branch)));
            grid.Children.Add(CreateElapsedText(node));

            if (node.IsSelected && useInnerSelectionBorder)
            {
                grid.Children.Add(CreateSelectionOutline(width, height, node));
            }

            host.Children.Add(border);
            return host;
        }

        /// <summary>
        /// 获取右侧平行流程起始块的显示名称。
        /// </summary>
        /// <param name="node">所属 Thread 或 Switch 流程块。</param>
        /// <param name="branch">当前平行流程分支。</param>
        /// <returns>返回起始块显示名称。</returns>
        private string GetParallelBranchDisplayName(FlowNode node, FlowBranch branch)
        {
            string prefix = node != null && node.IsSwitchBlock ? "Switch" : "Thread";
            int sequence = branch == null ? 1 : branch.Sequence;
            return prefix + sequence;
        }

        /// <summary>
        /// 创建流程块视觉宿主，给运行外轮廓预留绘制空间。
        /// </summary>
        /// <param name="width">流程块逻辑宽度。</param>
        /// <param name="height">流程块逻辑高度。</param>
        /// <param name="visualPadding">外部视觉留白。</param>
        /// <returns>返回流程块视觉宿主。</returns>
        private Grid CreateNodeHost(double width, double height, double visualPadding)
        {
            return new Grid
            {
                Width = width + visualPadding * 2,
                Height = height + visualPadding * 2,
                ClipToBounds = false
            };
        }

        /// <summary>
        /// 获取流程块外部视觉留白。
        /// </summary>
        /// <param name="node">流程块。</param>
        /// <returns>运行态返回外轮廓留白，否则返回零。</returns>
        private double GetNodeVisualPadding(FlowNode node)
        {
            return node != null && node.Status == FlowNodeStatus.Running ? RunningOutlinePadding : 0;
        }

        /// <summary>
        /// 判断流程块是否使用内部选中轮廓。
        /// </summary>
        /// <param name="node">流程块。</param>
        /// <returns>普通算子块和线程块返回 true，否则返回 false。</returns>
        private bool UsesInnerSelectionBorder(FlowNode node)
        {
            return node != null && (node.IsOperatorBlock || node.IsThreadBlock);
        }

        /// <summary>
        /// 创建流程块选中轮廓。
        /// </summary>
        /// <param name="width">流程块宽度。</param>
        /// <param name="height">流程块高度。</param>
        /// <param name="node">流程块。</param>
        /// <returns>返回选中轮廓元素。</returns>
        private UIElement CreateSelectionOutline(double width, double height, FlowNode node)
        {
            if (node != null && node.IsThreadBlock)
            {
                return CreateThreadShape(width, height, new SolidColorBrush(Color.FromRgb(30, 155, 255)), 2, Brushes.Transparent);
            }

            return new Border
            {
                Width = width,
                Height = height,
                BorderBrush = new SolidColorBrush(Color.FromRgb(30, 155, 255)),
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(node != null && node.IsEndBlock ? height / 2.0 : 6),
                IsHitTestVisible = false,
                SnapsToDevicePixels = true
            };
        }

        /// <summary>
        /// 创建开始块图标。
        /// </summary>
        /// <returns>返回图标元素。</returns>
        private UIElement CreateStartIcon()
        {
            return new Path
            {
                Width = 24,
                Height = 26,
                Data = Geometry.Parse("M 0 0 L 24 13 L 0 26 Z"),
                Fill = new SolidColorBrush(Color.FromRgb(82, 214, 156)),
                Stretch = Stretch.Fill,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
        }

        /// <summary>
        /// 创建普通算子块形状。
        /// </summary>
        /// <param name="width">宽度。</param>
        /// <param name="height">高度。</param>
        /// <returns>返回形状元素。</returns>
        private UIElement CreateOperatorBlockShape(double width, double height)
        {
            Grid grid = new Grid();
            grid.Children.Add(new Border
            {
                Width = width,
                Height = height,
                BorderBrush = new SolidColorBrush(Color.FromRgb(88, 217, 192)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Background = Brushes.White
            });

            grid.Children.Add(new Border
            {
                Width = 20,
                Height = height,
                HorizontalAlignment = HorizontalAlignment.Left,
                Background = new SolidColorBrush(Color.FromRgb(82, 214, 156)),
                CornerRadius = new CornerRadius(6, 0, 0, 6)
            });

            return grid;
        }

        /// <summary>
        /// 创建旗形块形状。
        /// </summary>
        /// <param name="width">宽度。</param>
        /// <param name="height">高度。</param>
        /// <param name="isGoto">是否为 Goto 块。</param>
        /// <returns>返回旗形块元素。</returns>
        private UIElement CreateFlagShape(double width, double height, bool isGoto)
        {
            Grid grid = new Grid();
            Path path = new Path
            {
                Data = Geometry.Parse(string.Format("M 0 0 L {0} 0 L {1} {2} L {0} {3} L 0 {3} Z", width - 10, width - 18, height / 2.0, height)),
                Fill = Brushes.White,
                Stroke = new SolidColorBrush(Color.FromRgb(88, 217, 192)),
                StrokeThickness = 1,
                Stretch = Stretch.Fill
            };
            grid.Children.Add(path);

            grid.Children.Add(new Polygon
            {
                Points = new PointCollection(new[] { new Point(0, height / 2.0), new Point(22, 0), new Point(22, height) }),
                Fill = new SolidColorBrush(Color.FromRgb(82, 214, 156))
            });

            return grid;
        }

        /// <summary>
        /// 创建线程块形状。
        /// </summary>
        /// <param name="width">宽度。</param>
        /// <param name="height">高度。</param>
        /// <returns>返回线程块形状。</returns>
        private UIElement CreateThreadShape(double width, double height)
        {
            return CreateThreadShape(width, height, new SolidColorBrush(Color.FromRgb(88, 217, 192)), 1, Brushes.White);
        }

        /// <summary>
        /// 创建线程块形状。
        /// </summary>
        /// <param name="width">宽度。</param>
        /// <param name="height">高度。</param>
        /// <param name="stroke">描边画刷。</param>
        /// <param name="strokeThickness">描边宽度。</param>
        /// <param name="fill">填充画刷。</param>
        /// <returns>返回线程块形状。</returns>
        private UIElement CreateThreadShape(double width, double height, Brush stroke, double strokeThickness, Brush fill)
        {
            return new Path
            {
                Data = Geometry.Parse(string.Format("M 0 0 L {0} 0 L {0} {1} L {2} {3} L 0 {1} Z", width, height - 9, width * 0.72, height)),
                Fill = fill,
                Stroke = stroke,
                StrokeThickness = strokeThickness,
                Stretch = Stretch.Fill
            };
        }

        /// <summary>
        /// 创建分支块括号形状。
        /// </summary>
        /// <param name="width">宽度。</param>
        /// <param name="height">高度。</param>
        /// <returns>返回括号形状。</returns>
        private UIElement CreateSwitchBracket(double width, double height)
        {
            Grid grid = new Grid();
            grid.Children.Add(new Border
            {
                Width = width,
                Height = height,
                BorderBrush = new SolidColorBrush(Color.FromRgb(88, 217, 192)),
                BorderThickness = new Thickness(0, 2, 0, 2),
                Background = Brushes.White
            });
            return grid;
        }

        /// <summary>
        /// 创建节点文字。
        /// </summary>
        /// <param name="text">文字内容。</param>
        /// <returns>返回文字元素。</returns>
        private UIElement CreateNodeText(string text)
        {
            return new TextBlock
            {
                Text = text,
                FontSize = 13,
                Foreground = new SolidColorBrush(Color.FromRgb(34, 48, 45)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(22, 0, 8, 0)
            };
        }

        /// <summary>
        /// 创建流程块运行时间显示。
        /// </summary>
        /// <param name="node">流程块。</param>
        /// <returns>返回运行时间文本元素。</returns>
        private UIElement CreateElapsedText(FlowNode node)
        {
            return new TextBlock
            {
                Text = node.ElapsedMilliseconds.ToString("0") + " ms",
                FontSize = 9,
                Foreground = new SolidColorBrush(Color.FromRgb(95, 112, 108)),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(0, 0, 7, 3)
            };
        }

        /// <summary>
        /// 创建运行中流程块的外轮廓。
        /// </summary>
        /// <param name="width">流程块宽度。</param>
        /// <param name="height">流程块高度。</param>
        /// <returns>返回运行外轮廓元素。</returns>
        private UIElement CreateRunningOutline(double width, double height)
        {
            Grid grid = new Grid
            {
                Width = width + RunningOutlinePadding * 2,
                Height = height + RunningOutlinePadding * 2,
                IsHitTestVisible = false,
                ClipToBounds = false
            };

            Brush brush = new SolidColorBrush(Color.FromRgb(82, 214, 156));
            grid.Children.Add(CreateCornerSegment(0, 0, RunningOutlineArmLength, true, true, brush, RunningOutlineCornerRadius));
            grid.Children.Add(CreateCornerSegment(1, 0, RunningOutlineArmLength, false, true, brush, RunningOutlineCornerRadius));
            grid.Children.Add(CreateCornerSegment(0, 1, RunningOutlineArmLength, true, false, brush, RunningOutlineCornerRadius));
            grid.Children.Add(CreateCornerSegment(1, 1, RunningOutlineArmLength, false, false, brush, RunningOutlineCornerRadius));
            return grid;
        }

        /// <summary>
        /// 创建运行外轮廓的单个角标。
        /// </summary>
        /// <param name="column">角标列位置。</param>
        /// <param name="row">角标行位置。</param>
        /// <param name="length">角标臂长。</param>
        /// <param name="isLeft">是否位于左侧。</param>
        /// <param name="isTop">是否位于上侧。</param>
        /// <param name="brush">角标颜色。</param>
        /// <param name="cornerRadius">角标圆角半径。</param>
        /// <returns>返回角标元素。</returns>
        private UIElement CreateCornerSegment(int column, int row, double length, bool isLeft, bool isTop, Brush brush, double cornerRadius)
        {
            double halfThickness = RunningOutlineStrokeThickness / 2.0;
            Grid container = new Grid
            {
                HorizontalAlignment = isLeft ? HorizontalAlignment.Left : HorizontalAlignment.Right,
                VerticalAlignment = isTop ? VerticalAlignment.Top : VerticalAlignment.Bottom,
                Width = length + RunningOutlineStrokeThickness,
                Height = length + RunningOutlineStrokeThickness,
                ClipToBounds = false
            };

            Path path = new Path
            {
                Stroke = brush,
                StrokeThickness = RunningOutlineStrokeThickness,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                Data = Geometry.Parse(GetCornerGeometry(length, isLeft, isTop, cornerRadius, halfThickness))
            };
            container.Children.Add(path);
            return container;
        }

        /// <summary>
        /// 获取运行外轮廓角标的路径数据。
        /// </summary>
        /// <param name="length">角标臂长。</param>
        /// <param name="isLeft">是否位于左侧。</param>
        /// <param name="isTop">是否位于上侧。</param>
        /// <param name="cornerRadius">角标圆角半径。</param>
        /// <returns>返回路径字符串。</returns>
        private string GetCornerGeometry(double length, bool isLeft, bool isTop, double cornerRadius, double offset)
        {
            double min = offset;
            double max = length + offset;
            double startX = isLeft ? max : min;
            double endX = isLeft ? min + cornerRadius : max - cornerRadius;
            double verticalX = isLeft ? min : max;
            double startY = isTop ? min : max;
            double verticalEndY = isTop ? max : min;
            double arcY = isTop ? min + cornerRadius : max - cornerRadius;
            return string.Format(
                "M {0} {1} L {2} {1} Q {3} {1} {3} {4} L {3} {5}",
                startX,
                startY,
                endX,
                verticalX,
                arcY,
                verticalEndY);
        }

        /// <summary>
        /// 创建左侧编号或图标块。
        /// </summary>
        /// <param name="text">显示文字。</param>
        /// <returns>返回徽标元素。</returns>
        private UIElement CreateLeftBadge(string text)
        {
            Border badge = new Border
            {
                Width = 24,
                Background = new SolidColorBrush(Color.FromRgb(82, 214, 156)),
                HorizontalAlignment = HorizontalAlignment.Left
            };
            badge.Child = new TextBlock
            {
                Text = text,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            return badge;
        }

        /// <summary>
        /// 创建线程块底部减号。
        /// </summary>
        /// <returns>返回减号元素。</returns>
        private UIElement CreateMinusCircle()
        {
            Grid grid = new Grid
            {
                Width = 14,
                Height = 14,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom
            };
            grid.Children.Add(new Ellipse
            {
                Fill = Brushes.White,
                Stroke = new SolidColorBrush(Color.FromRgb(88, 217, 192)),
                StrokeThickness = 1
            });
            grid.Children.Add(new Line
            {
                X1 = 3,
                X2 = 11,
                Y1 = 7,
                Y2 = 7,
                Stroke = new SolidColorBrush(Color.FromRgb(88, 217, 192)),
                StrokeThickness = 1
            });
            return grid;
        }

        /// <summary>
        /// 渲染竖向连线和中间加号。
        /// </summary>
        /// <param name="canvas">目标画布。</param>
        /// <param name="from">起点。</param>
        /// <param name="to">终点。</param>
        /// <param name="isInteractive">是否生成可交互按钮。</param>
        /// <param name="target">添加目标。</param>
        /// <returns>无返回值。</returns>
        private void RenderVerticalConnector(Canvas canvas, Point from, Point to, bool isInteractive, AddTarget target)
        {
            RenderPolyline(canvas, new[] { from, to }, false, false);
            RenderArrow(canvas, to.X, to.Y - 4, 90);

            if (isInteractive)
            {
                RenderConnectorAddButtons(canvas, from, to, target);
            }
        }

        /// <summary>
        /// 沿竖向连线按流程块间距渲染一个或多个添加按钮。
        /// </summary>
        /// <param name="canvas">目标画布。</param>
        /// <param name="from">连线起点。</param>
        /// <param name="to">连线终点。</param>
        /// <param name="target">添加目标。</param>
        /// <returns>无返回值。</returns>
        private void RenderConnectorAddButtons(Canvas canvas, Point from, Point to, AddTarget target)
        {
            double length = to.Y - from.Y;
            double firstCenterY = from.Y + VerticalGap / 2.0;
            if (length <= 34 || firstCenterY >= to.Y)
            {
                return;
            }

            for (double centerY = firstCenterY; centerY <= to.Y - VerticalGap / 2.0 + 0.1; centerY += VerticalGap)
            {
                RenderConnectorAddButton(canvas, from.X, centerY, target);
            }
        }

        /// <summary>
        /// 在指定中心点渲染一个连线添加按钮。
        /// </summary>
        /// <param name="canvas">目标画布。</param>
        /// <param name="centerX">按钮中心横坐标。</param>
        /// <param name="centerY">按钮中心纵坐标。</param>
        /// <param name="target">添加目标。</param>
        /// <returns>无返回值。</returns>
        private void RenderConnectorAddButton(Canvas canvas, double centerX, double centerY, AddTarget target)
        {
            Button button = CreateAddButton(target);
            Canvas.SetLeft(button, centerX - 14);
            Canvas.SetTop(button, centerY - 14);
            canvas.Children.Add(button);
        }

        /// <summary>
        /// 创建连线加号按钮。
        /// </summary>
        /// <param name="target">添加目标。</param>
        /// <returns>返回按钮元素。</returns>
        private Button CreateAddButton(AddTarget target)
        {
            ContextMenu contextMenu = (ContextMenu)FindResource("ConnectorMenu");
            Button button = new Button
            {
                Width = 28,
                Height = 28,
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(30, 155, 255)),
                BorderThickness = new Thickness(2),
                ContextMenu = contextMenu,
                Tag = target,
                Cursor = Cursors.Hand,
                Content = "+",
                Opacity = 0
            };
            contextMenu.Closed += AddButtonContextMenu_Closed;
            button.Click += ConnectorAddButton_Click;
            button.MouseEnter += AddButton_MouseEnter;
            button.MouseLeave += AddButton_MouseLeave;
            return button;
        }

        /// <summary>
        /// 鼠标进入连线加号热区时显示加号按钮。
        /// </summary>
        /// <param name="sender">加号按钮。</param>
        /// <param name="e">鼠标事件参数。</param>
        /// <returns>无返回值。</returns>
        private void AddButton_MouseEnter(object sender, MouseEventArgs e)
        {
            UIElement element = sender as UIElement;
            if (element != null)
            {
                element.Opacity = 1;
            }
        }

        /// <summary>
        /// 鼠标离开连线加号热区且菜单未打开时隐藏加号按钮。
        /// </summary>
        /// <param name="sender">加号按钮。</param>
        /// <param name="e">鼠标事件参数。</param>
        /// <returns>无返回值。</returns>
        private void AddButton_MouseLeave(object sender, MouseEventArgs e)
        {
            Button button = sender as Button;
            if (button != null && (button.ContextMenu == null || !button.ContextMenu.IsOpen))
            {
                button.Opacity = 0;
            }
        }

        /// <summary>
        /// 连线菜单关闭后隐藏未悬停的加号按钮。
        /// </summary>
        /// <param name="sender">连线菜单。</param>
        /// <param name="e">事件参数。</param>
        /// <returns>无返回值。</returns>
        private void AddButtonContextMenu_Closed(object sender, RoutedEventArgs e)
        {
            ContextMenu menu = sender as ContextMenu;
            Button button = menu == null ? null : menu.PlacementTarget as Button;
            if (button != null && !button.IsMouseOver)
            {
                button.Opacity = 0;
            }
        }

        /// <summary>
        /// 渲染折线。
        /// </summary>
        /// <param name="canvas">目标画布。</param>
        /// <param name="points">折线点集合。</param>
        /// <param name="isDashed">是否虚线。</param>
        /// <param name="isGoto">是否 Goto 链接。</param>
        /// <returns>无返回值。</returns>
        private void RenderPolyline(Canvas canvas, IEnumerable<Point> points, bool isDashed, bool isGoto)
        {
            Polyline line = new Polyline
            {
                Points = new PointCollection(points),
                Stroke = new SolidColorBrush(isGoto ? Color.FromRgb(19, 205, 169) : Color.FromRgb(170, 175, 178)),
                StrokeThickness = isGoto ? 1.6 : 1.0
            };

            if (isDashed)
            {
                line.StrokeDashArray = new DoubleCollection(new[] { 4.0, 3.0 });
            }

            canvas.Children.Add(line);
        }

        /// <summary>
        /// 渲染箭头。
        /// </summary>
        /// <param name="canvas">目标画布。</param>
        /// <param name="x">横坐标。</param>
        /// <param name="y">纵坐标。</param>
        /// <param name="angle">旋转角度。</param>
        /// <returns>无返回值。</returns>
        private void RenderArrow(Canvas canvas, double x, double y, double angle)
        {
            Path arrow = new Path
            {
                Width = 7,
                Height = 7,
                Data = Geometry.Parse("M 0 0 L 7 3.5 L 0 7 Z"),
                Fill = new SolidColorBrush(Color.FromRgb(170, 175, 178)),
                Stretch = Stretch.Fill,
                RenderTransform = new RotateTransform(angle, 3.5, 3.5)
            };

            Canvas.SetLeft(arrow, x - 3.5);
            Canvas.SetTop(arrow, y - 3.5);
            canvas.Children.Add(arrow);
        }

        /// <summary>
        /// 根据 Goto 绑定渲染虚线链接。
        /// </summary>
        /// <param name="canvas">目标画布。</param>
        /// <param name="isInteractive">是否为主画布。</param>
        /// <returns>无返回值。</returns>
        private void RenderGotoLinks(Canvas canvas, bool isInteractive)
        {
            Dictionary<Guid, NodeLayoutInfo> layouts = isInteractive ? _nodeLayouts : CollectLayoutsFromCanvas();
            foreach (FlowNode node in EnumerateNodes(Nodes))
            {
                if (!node.IsGotoBlock || !node.GotoTargetNodeId.HasValue)
                {
                    continue;
                }

                NodeLayoutInfo source;
                NodeLayoutInfo target;
                if (!layouts.TryGetValue(node.Id, out source) || !layouts.TryGetValue(node.GotoTargetNodeId.Value, out target))
                {
                    continue;
                }

                double x = Math.Min(source.Left, target.Left) - 18;
                RenderPolyline(
                    canvas,
                    new[]
                    {
                        new Point(source.Left, source.CenterY),
                        new Point(x, source.CenterY),
                        new Point(x, target.CenterY),
                        new Point(target.Left, target.CenterY)
                    },
                    true,
                    true);
            }
        }

        /// <summary>
        /// 收集非交互画布的布局字典。
        /// </summary>
        /// <returns>返回布局字典。</returns>
        private Dictionary<Guid, NodeLayoutInfo> CollectLayoutsFromCanvas()
        {
            return new Dictionary<Guid, NodeLayoutInfo>(_nodeLayouts);
        }

        /// <summary>
        /// 递归枚举全部流程块。
        /// </summary>
        /// <param name="nodes">流程块集合。</param>
        /// <returns>返回流程块枚举。</returns>
        private IEnumerable<FlowNode> EnumerateNodes(IEnumerable<FlowNode> nodes)
        {
            if (nodes == null)
            {
                yield break;
            }

            foreach (FlowNode node in nodes)
            {
                yield return node;
                foreach (FlowBranch branch in node.Branches)
                {
                    foreach (FlowNode branchNode in EnumerateNodes(branch.Nodes))
                    {
                        yield return branchNode;
                    }
                }
            }
        }

        /// <summary>
        /// 处理流程块点击选中。
        /// </summary>
        /// <param name="sender">触发点击的元素。</param>
        /// <param name="e">鼠标事件参数。</param>
        /// <returns>无返回值。</returns>
        private void NodeElement_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            FrameworkElement element = sender as FrameworkElement;
            FlowNode targetNode = element == null ? null : element.Tag as FlowNode;
            if (targetNode != null)
            {
                SelectedNode = targetNode;
            }
        }

        /// <summary>
        /// 右键流程块时选中该流程块。
        /// </summary>
        /// <param name="sender">触发右键的元素。</param>
        /// <param name="e">鼠标按键事件参数。</param>
        /// <returns>无返回值。</returns>
        private void NodeElement_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            FrameworkElement element = sender as FrameworkElement;
            FlowNode targetNode = element == null ? null : element.Tag as FlowNode;
            if (targetNode != null)
            {
                SelectedNode = targetNode;
                e.Handled = true;
            }
        }

        /// <summary>
        /// 创建流程块右键菜单。
        /// </summary>
        /// <param name="node">菜单对应的流程块。</param>
        /// <returns>返回右键菜单。</returns>
        private ContextMenu CreateNodeContextMenu(FlowNode node)
        {
            ContextMenu menu = new ContextMenu();
            menu.Items.Add(CreateNodeMenuItem("运行块", node, RunNodeMenuItem_Click, !node.IsFixed));
            menu.Items.Add(CreateNodeMenuItem(node.IsEnabled ? "禁用块" : "启用块", node, ToggleNodeEnabledMenuItem_Click, !node.IsFixed));
            menu.Items.Add(new Separator());
            menu.Items.Add(CreateNodeMenuItem("复制块", node, CopyNodeMenuItem_Click, !node.IsFixed));
            menu.Items.Add(CreateNodeMenuItem("剪切块", node, CutNodeMenuItem_Click, !node.IsFixed));
            menu.Items.Add(CreateNodeMenuItem("删除块", node, DeleteNodeMenuItem_Click, !node.IsFixed));
            return menu;
        }

        /// <summary>
        /// 创建流程块右键菜单项。
        /// </summary>
        /// <param name="header">菜单显示文本。</param>
        /// <param name="node">菜单对应的流程块。</param>
        /// <param name="handler">点击事件处理函数。</param>
        /// <param name="isEnabled">菜单项是否可用。</param>
        /// <returns>返回菜单项。</returns>
        private MenuItem CreateNodeMenuItem(string header, FlowNode node, RoutedEventHandler handler, bool isEnabled)
        {
            MenuItem item = new MenuItem
            {
                Header = header,
                Tag = node,
                IsEnabled = isEnabled
            };
            item.Click += handler;
            return item;
        }

        /// <summary>
        /// 处理右键菜单运行当前块。
        /// </summary>
        /// <param name="sender">菜单项。</param>
        /// <param name="e">事件参数。</param>
        /// <returns>无返回值。</returns>
        private async void RunNodeMenuItem_Click(object sender, RoutedEventArgs e)
        {
            FlowNode node = GetNodeFromMenuItem(sender);
            if (node == null || IsRunning)
            {
                return;
            }

            IsRunning = true;
            try
            {
                ResetNode(node);
                FlowExecutionContext context = new FlowExecutionContext();
                await RunSingleNodeAsync(node, context, CancellationToken.None);
            }
            finally
            {
                IsRunning = false;
            }
        }

        /// <summary>
        /// 处理右键菜单启用或禁用流程块。
        /// </summary>
        /// <param name="sender">菜单项。</param>
        /// <param name="e">事件参数。</param>
        /// <returns>无返回值。</returns>
        private void ToggleNodeEnabledMenuItem_Click(object sender, RoutedEventArgs e)
        {
            FlowNode node = GetNodeFromMenuItem(sender);
            if (node == null || node.IsFixed)
            {
                return;
            }

            node.IsEnabled = !node.IsEnabled;
            RefreshCanvas();
        }

        /// <summary>
        /// 处理右键菜单复制流程块。
        /// </summary>
        /// <param name="sender">菜单项。</param>
        /// <param name="e">事件参数。</param>
        /// <returns>无返回值。</returns>
        private void CopyNodeMenuItem_Click(object sender, RoutedEventArgs e)
        {
            FlowNode node = GetNodeFromMenuItem(sender);
            if (node != null && !node.IsFixed)
            {
                _clipboardNode = CloneNode(node, node.DisplayName + "_复制");
            }
        }

        /// <summary>
        /// 处理右键菜单剪切流程块。
        /// </summary>
        /// <param name="sender">菜单项。</param>
        /// <param name="e">事件参数。</param>
        /// <returns>无返回值。</returns>
        private void CutNodeMenuItem_Click(object sender, RoutedEventArgs e)
        {
            FlowNode node = GetNodeFromMenuItem(sender);
            if (node == null || node.IsFixed)
            {
                return;
            }

            _clipboardNode = CloneNode(node, node.DisplayName);
            RemoveNode(node);
        }

        /// <summary>
        /// 处理右键菜单删除流程块。
        /// </summary>
        /// <param name="sender">菜单项。</param>
        /// <param name="e">事件参数。</param>
        /// <returns>无返回值。</returns>
        private void DeleteNodeMenuItem_Click(object sender, RoutedEventArgs e)
        {
            FlowNode node = GetNodeFromMenuItem(sender);
            if (node != null && !node.IsFixed)
            {
                RemoveNode(node);
            }
        }

        /// <summary>
        /// 删除指定流程块。
        /// </summary>
        /// <param name="node">要删除的流程块。</param>
        /// <returns>删除成功返回 true，否则返回 false。</returns>
        private bool RemoveNode(FlowNode node)
        {
            if (node == null || node.IsFixed)
            {
                return false;
            }

            if (Nodes != null && Nodes.Remove(node))
            {
                if (SelectedNode == node)
                {
                    SelectedNode = null;
                }

                RenumberNodes();
                return true;
            }

            foreach (FlowNode owner in EnumerateNodes(Nodes))
            {
                foreach (FlowBranch branch in owner.Branches)
                {
                    if (branch.Nodes.Remove(node))
                    {
                        if (SelectedNode == node)
                        {
                            SelectedNode = null;
                        }

                        RefreshCanvas();
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// 克隆流程块及其配置。
        /// </summary>
        /// <param name="source">源流程块。</param>
        /// <param name="displayName">克隆后的显示名称。</param>
        /// <returns>返回克隆流程块。</returns>
        private FlowNode CloneNode(FlowNode source, string displayName)
        {
            if (source == null)
            {
                return null;
            }

            FlowNode target;
            switch (source.Kind)
            {
                case FlowBlockKind.Goto:
                    target = FlowNode.CreateGotoBlock(displayName);
                    target.GotoTargetNodeId = source.GotoTargetNodeId;
                    break;
                case FlowBlockKind.Switch:
                    target = FlowNode.CreateSwitchBlock(displayName);
                    CopyBranches(source, target);
                    break;
                case FlowBlockKind.Thread:
                    target = FlowNode.CreateThreadBlock(displayName);
                    CopyBranches(source, target);
                    break;
                default:
                    target = FlowNode.CreateOperatorBlock(displayName);
                    break;
            }

            target.Remark = source.Remark;
            target.ImageWindowName = source.ImageWindowName;
            target.IsEnabled = source.IsEnabled;
            target.ConditionDataName = source.ConditionDataName;

            foreach (FlowOperator flowOperator in source.Operators)
            {
                target.Operators.Add(new FlowOperator
                {
                    OperatorName = flowOperator.OperatorName,
                    DisplayName = flowOperator.DisplayName,
                    Status = FlowNodeStatus.NotRun
                });
            }

            return target;
        }

        /// <summary>
        /// 复制分支配置和分支内流程块。
        /// </summary>
        /// <param name="source">源流程块。</param>
        /// <param name="target">目标流程块。</param>
        /// <returns>无返回值。</returns>
        private void CopyBranches(FlowNode source, FlowNode target)
        {
            target.Branches.Clear();
            foreach (FlowBranch branch in source.Branches)
            {
                FlowBranch targetBranch = target.AddBranch(branch.ConditionValue);
                foreach (FlowNode branchNode in branch.Nodes)
                {
                    targetBranch.Nodes.Add(CloneNode(branchNode, branchNode.DisplayName));
                }
            }
        }

        /// <summary>
        /// 从菜单项中读取流程块。
        /// </summary>
        /// <param name="sender">菜单项。</param>
        /// <returns>返回流程块；无效时返回 null。</returns>
        private FlowNode GetNodeFromMenuItem(object sender)
        {
            MenuItem item = sender as MenuItem;
            return item == null ? null : item.Tag as FlowNode;
        }

        /// <summary>
        /// 处理流程块拖拽经过。
        /// </summary>
        /// <param name="sender">流程块元素。</param>
        /// <param name="e">拖拽事件参数。</param>
        /// <returns>无返回值。</returns>
        private void NodeElement_DragOver(object sender, DragEventArgs e)
        {
            FrameworkElement element = sender as FrameworkElement;
            FlowNode targetNode = element == null ? null : element.Tag as FlowNode;
            e.Effects = CanDropOperatorToBlock(targetNode, e) ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
        }

        /// <summary>
        /// 处理流程块拖拽放置。
        /// </summary>
        /// <param name="sender">流程块元素。</param>
        /// <param name="e">拖拽事件参数。</param>
        /// <returns>无返回值。</returns>
        private void NodeElement_Drop(object sender, DragEventArgs e)
        {
            DragOperatorData data = GetDragOperatorData(e);
            FrameworkElement element = sender as FrameworkElement;
            FlowNode targetNode = element == null ? null : element.Tag as FlowNode;
            if (data == null || targetNode == null || !targetNode.CanConfigureOperators)
            {
                return;
            }

            SelectedNode = targetNode;
            AddOperatorToBlock(targetNode, data.Name);
            e.Handled = true;
        }

        /// <summary>
        /// 获取流程块显示宽度。
        /// </summary>
        /// <param name="node">流程块。</param>
        /// <returns>返回宽度。</returns>
        private double GetNodeWidth(FlowNode node)
        {
            if (node.IsStartBlock)
            {
                return StartNodeWidth;
            }

            if (node.IsEndBlock)
            {
                return EndNodeSize;
            }

            if (node.IsGotoBlock)
            {
                return GotoNodeWidth;
            }

            return NormalNodeWidth;
        }

        /// <summary>
        /// 获取流程块显示高度。
        /// </summary>
        /// <param name="node">流程块。</param>
        /// <returns>返回高度。</returns>
        private double GetNodeHeight(FlowNode node)
        {
            if (node.IsStartBlock)
            {
                return StartNodeHeight;
            }

            if (node.IsEndBlock)
            {
                return EndNodeSize;
            }

            if (node.IsGotoBlock)
            {
                return GotoNodeHeight;
            }

            return NormalNodeHeight;
        }

        /// <summary>
        /// 运行流程块集合。
        /// </summary>
        /// <param name="nodes">流程块集合。</param>
        /// <param name="context">运行上下文。</param>
        /// <param name="token">取消令牌。</param>
        /// <returns>返回异步任务。</returns>
        private async Task RunNodeCollectionAsync(IList<FlowNode> nodes, FlowExecutionContext context, CancellationToken token)
        {
            if (nodes == null)
            {
                return;
            }

            for (int i = 0; i < nodes.Count; i++)
            {
                FlowNode node = nodes[i];
                if (!node.IsEnabled)
                {
                    continue;
                }

                token.ThrowIfCancellationRequested();
                await RunSingleNodeAsync(node, context, token);
            }
        }

        /// <summary>
        /// 运行单个流程块。
        /// </summary>
        /// <param name="node">要运行的流程块。</param>
        /// <param name="context">运行上下文。</param>
        /// <param name="token">取消令牌。</param>
        /// <returns>返回异步任务。</returns>
        private async Task RunSingleNodeAsync(FlowNode node, FlowExecutionContext context, CancellationToken token)
        {
            if (node == null || !node.IsEnabled)
            {
                return;
            }

            token.ThrowIfCancellationRequested();
            Stopwatch stopwatch = Stopwatch.StartNew();
            node.Status = FlowNodeStatus.Running;

            if (node.IsThreadBlock || node.IsSwitchBlock)
            {
                await RunBranchesAsync(node, context, token);
            }
            else if (node.IsOperatorBlock && node.Operators.Count > 0)
            {
                await RunBlockOperatorsAsync(node, context, token);
            }
            else
            {
                await Task.Delay(160, token);
            }

            stopwatch.Stop();
            node.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            node.Status = FlowNodeStatus.OK;
            context.ExecutionLog.Add(node.DisplayName);
        }

        /// <summary>
        /// 并行运行线程或分支块的各条分支。
        /// </summary>
        /// <param name="node">流程块。</param>
        /// <param name="context">运行上下文。</param>
        /// <param name="token">取消令牌。</param>
        /// <returns>返回异步任务。</returns>
        private async Task RunBranchesAsync(FlowNode node, FlowExecutionContext context, CancellationToken token)
        {
            Task[] branchTasks = node.Branches
                .Select(branch => RunNodeCollectionAsync(branch.Nodes, context, token))
                .ToArray();
            await Task.WhenAll(branchTasks);
        }

        /// <summary>
        /// 运行指定算子块内的所有算子。
        /// </summary>
        /// <param name="node">要运行的算子块。</param>
        /// <param name="context">流程运行上下文。</param>
        /// <param name="cancellationToken">取消运行令牌。</param>
        /// <returns>返回异步执行任务。</returns>
        private async Task RunBlockOperatorsAsync(FlowNode node, FlowExecutionContext context, CancellationToken cancellationToken)
        {
            foreach (FlowOperator flowOperator in node.Operators.ToList())
            {
                cancellationToken.ThrowIfCancellationRequested();
                Stopwatch stopwatch = Stopwatch.StartNew();
                flowOperator.Status = FlowNodeStatus.Running;

                await Task.Delay(260, cancellationToken);

                context.Items[flowOperator.DisplayName] = DateTime.Now;
                context.ExecutionLog.Add(flowOperator.DisplayName);
                stopwatch.Stop();
                flowOperator.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
                flowOperator.Status = FlowNodeStatus.OK;
            }
        }

        /// <summary>
        /// 将被取消的流程块标记为停止状态。
        /// </summary>
        /// <param name="currentNode">当前正在运行的流程块。</param>
        /// <param name="stopwatch">当前流程块计时器。</param>
        /// <returns>无返回值。</returns>
        private void MarkStopped(FlowNode currentNode, Stopwatch stopwatch)
        {
            if (currentNode == null || currentNode.Status != FlowNodeStatus.Running)
            {
                return;
            }

            if (stopwatch != null)
            {
                stopwatch.Stop();
                currentNode.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            }

            currentNode.Status = FlowNodeStatus.Stopped;
        }

        /// <summary>
        /// 处理鼠标滚轮缩放流程图。
        /// </summary>
        /// <param name="sender">触发滚轮事件的对象。</param>
        /// <param name="e">鼠标滚轮事件参数。</param>
        /// <returns>无返回值。</returns>
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

        /// <summary>
        /// 在空白区域按下鼠标左键时开始拖动画布。
        /// </summary>
        /// <param name="sender">触发鼠标事件的对象。</param>
        /// <param name="e">鼠标按键事件参数。</param>
        /// <returns>无返回值。</returns>
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

        /// <summary>
        /// 拖动画布时同步滚动偏移。
        /// </summary>
        /// <param name="sender">触发鼠标移动事件的对象。</param>
        /// <param name="e">鼠标移动事件参数。</param>
        /// <returns>无返回值。</returns>
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

        /// <summary>
        /// 松开鼠标左键时停止拖动画布。
        /// </summary>
        /// <param name="sender">触发鼠标事件的对象。</param>
        /// <param name="e">鼠标按键事件参数。</param>
        /// <returns>无返回值。</returns>
        private void View_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            StopPanning();
        }

        /// <summary>
        /// 失去鼠标捕获时停止拖动画布。
        /// </summary>
        /// <param name="sender">触发鼠标事件的对象。</param>
        /// <param name="e">鼠标事件参数。</param>
        /// <returns>无返回值。</returns>
        private void ViewScrollViewer_LostMouseCapture(object sender, MouseEventArgs e)
        {
            StopPanning();
        }

        /// <summary>
        /// 滚动视图变化时刷新小地图视口。
        /// </summary>
        /// <param name="sender">触发滚动事件的对象。</param>
        /// <param name="e">滚动变化事件参数。</param>
        /// <returns>无返回值。</returns>
        private void ViewScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            UpdateMiniMap();
        }

        /// <summary>
        /// 视图大小变化时刷新流程图世界尺寸和小地图。
        /// </summary>
        /// <param name="sender">触发尺寸变化事件的对象。</param>
        /// <param name="e">尺寸变化事件参数。</param>
        /// <returns>无返回值。</returns>
        private void ViewScrollViewer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateSceneWorldSize();
            Dispatcher.BeginInvoke(new Action(UpdateMiniMap), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        /// <summary>
        /// 点击小地图时移动主视图到对应区域。
        /// </summary>
        /// <param name="sender">触发鼠标事件的对象。</param>
        /// <param name="e">鼠标按键事件参数。</param>
        /// <returns>无返回值。</returns>
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

        /// <summary>
        /// 在小地图 ROI 上按下鼠标左键时开始拖动视口框。
        /// </summary>
        /// <param name="sender">小地图覆盖层。</param>
        /// <param name="e">鼠标按键事件参数。</param>
        /// <returns>无返回值。</returns>
        private void MiniMapOverlay_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (miniMapViewport == null || !IsInsideMiniMapViewport(e.GetPosition(miniMapOverlay)))
            {
                return;
            }

            _isDraggingMiniMapRoi = true;
            _miniMapDragStartPoint = e.GetPosition(miniMapOverlay);
            _miniMapDragStartHorizontalOffset = viewScrollViewer.HorizontalOffset;
            _miniMapDragStartVerticalOffset = viewScrollViewer.VerticalOffset;
            miniMapOverlay.CaptureMouse();
            e.Handled = true;
        }

        /// <summary>
        /// 拖动小地图 ROI 时同步主视图滚动位置。
        /// </summary>
        /// <param name="sender">小地图覆盖层。</param>
        /// <param name="e">鼠标移动事件参数。</param>
        /// <returns>无返回值。</returns>
        private void MiniMapOverlay_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDraggingMiniMapRoi)
            {
                return;
            }

            MiniMapLayout layout = GetMiniMapLayout();
            if (layout.Scale <= 0)
            {
                return;
            }

            Point current = e.GetPosition(miniMapOverlay);
            Vector delta = current - _miniMapDragStartPoint;
            ScrollToClamped(
                _miniMapDragStartHorizontalOffset + delta.X / layout.Scale,
                _miniMapDragStartVerticalOffset + delta.Y / layout.Scale);
            UpdateMiniMap();
            e.Handled = true;
        }

        /// <summary>
        /// 松开鼠标左键时停止拖动小地图 ROI。
        /// </summary>
        /// <param name="sender">小地图覆盖层。</param>
        /// <param name="e">鼠标按键事件参数。</param>
        /// <returns>无返回值。</returns>
        private void MiniMapOverlay_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            StopMiniMapRoiDrag();
        }

        /// <summary>
        /// 小地图覆盖层失去鼠标捕获时停止拖动 ROI。
        /// </summary>
        /// <param name="sender">小地图覆盖层。</param>
        /// <param name="e">鼠标事件参数。</param>
        /// <returns>无返回值。</returns>
        private void MiniMapOverlay_LostMouseCapture(object sender, MouseEventArgs e)
        {
            StopMiniMapRoiDrag();
        }

        /// <summary>
        /// 点击小地图右下角按钮时切换小地图显示状态。
        /// </summary>
        /// <param name="sender">折叠按钮。</param>
        /// <param name="e">按钮事件参数。</param>
        /// <returns>无返回值。</returns>
        private void MiniMapToggleButton_Click(object sender, RoutedEventArgs e)
        {
            _isMiniMapVisible = !_isMiniMapVisible;
            miniMap.Visibility = _isMiniMapVisible ? Visibility.Visible : Visibility.Collapsed;
            miniMapToggleButton.Content = _isMiniMapVisible ? "⌄" : "⌃";
        }

        /// <summary>
        /// 停止小地图 ROI 拖动并释放鼠标捕获。
        /// </summary>
        /// <returns>无返回值。</returns>
        private void StopMiniMapRoiDrag()
        {
            if (!_isDraggingMiniMapRoi)
            {
                return;
            }

            _isDraggingMiniMapRoi = false;
            if (miniMapOverlay != null && miniMapOverlay.IsMouseCaptured)
            {
                miniMapOverlay.ReleaseMouseCapture();
            }
        }

        /// <summary>
        /// 判断鼠标位置是否落在小地图 ROI 框内。
        /// </summary>
        /// <param name="point">鼠标在小地图覆盖层上的坐标。</param>
        /// <returns>落在 ROI 框内时返回 true，否则返回 false。</returns>
        private bool IsInsideMiniMapViewport(Point point)
        {
            double left = Canvas.GetLeft(miniMapViewport);
            double top = Canvas.GetTop(miniMapViewport);
            if (double.IsNaN(left) || double.IsNaN(top))
            {
                return false;
            }

            return point.X >= left
                && point.X <= left + miniMapViewport.Width
                && point.Y >= top
                && point.Y <= top + miniMapViewport.Height;
        }

        /// <summary>
        /// 停止拖动画布并恢复鼠标光标。
        /// </summary>
        /// <returns>无返回值。</returns>
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

        /// <summary>
        /// 将滚动偏移限制在当前可滚动区域内。
        /// </summary>
        /// <param name="horizontalOffset">目标水平滚动偏移。</param>
        /// <param name="verticalOffset">目标垂直滚动偏移。</param>
        /// <returns>无返回值。</returns>
        private void ScrollToClamped(double horizontalOffset, double verticalOffset)
        {
            double maxHorizontal = Math.Max(0, viewScrollViewer.ExtentWidth - viewScrollViewer.ViewportWidth);
            double maxVertical = Math.Max(0, viewScrollViewer.ExtentHeight - viewScrollViewer.ViewportHeight);

            horizontalOffset = Math.Max(0, Math.Min(maxHorizontal, horizontalOffset));
            verticalOffset = Math.Max(0, Math.Min(maxVertical, verticalOffset));

            viewScrollViewer.ScrollToHorizontalOffset(horizontalOffset);
            viewScrollViewer.ScrollToVerticalOffset(verticalOffset);
        }

        /// <summary>
        /// 根据流程块数量刷新主视图和小地图的世界尺寸。
        /// </summary>
        /// <returns>无返回值。</returns>
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
                flowContentHost.Width = Math.Max(NormalNodeWidth, worldSize.Width - ScenePadding * 2);
                flowContentHost.Height = contentHeight;
            }

            if (miniMapScene != null)
            {
                miniMapScene.Width = worldSize.Width;
                miniMapScene.Height = worldSize.Height;
            }

            if (miniMapContentHost != null)
            {
                miniMapContentHost.Width = Math.Max(NormalNodeWidth, worldSize.Width - ScenePadding * 2);
                miniMapContentHost.Height = contentHeight;
            }
        }

        /// <summary>
        /// 刷新小地图缩放比例和当前视口位置。
        /// </summary>
        /// <returns>无返回值。</returns>
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

        /// <summary>
        /// 计算流程图世界尺寸。
        /// </summary>
        /// <returns>返回流程图世界尺寸。</returns>
        private Size GetWorldSize()
        {
            return new Size(MinSceneWidth, MinSceneHeight);
        }

        /// <summary>
        /// 计算小地图内容布局参数。
        /// </summary>
        /// <returns>返回小地图布局参数。</returns>
        private MiniMapLayout GetMiniMapLayout()
        {
            Size worldSize = GetWorldSize();
            double scaledWorldWidth = worldSize.Width * Zoom;
            double scaledWorldHeight = worldSize.Height * Zoom;
            double mapWidth = Math.Max(1, miniMapRoot.ActualWidth);
            double mapHeight = Math.Max(1, miniMapRoot.ActualHeight);
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

        /// <summary>
        /// 判断当前鼠标源是否允许开始拖动画布。
        /// </summary>
        /// <param name="source">鼠标事件原始源。</param>
        /// <returns>可开始拖动画布时返回 true，否则返回 false。</returns>
        private bool CanStartPan(DependencyObject source)
        {
            if (source == null)
            {
                return true;
            }

            if (FindAncestor<ScrollBar>(source) != null || FindAncestor<ListBoxItem>(source) != null || FindAncestor<Button>(source) != null)
            {
                return false;
            }

            if (IsInside(source, miniMap) || IsNodeSource(source))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 判断事件源是否位于流程块内部。
        /// </summary>
        /// <param name="source">事件原始源。</param>
        /// <returns>位于流程块内部时返回 true，否则返回 false。</returns>
        private bool IsNodeSource(DependencyObject source)
        {
            while (source != null)
            {
                FrameworkElement element = source as FrameworkElement;
                if (element != null && element.Tag is FlowNode)
                {
                    return true;
                }

                source = VisualTreeHelper.GetParent(source);
            }

            return false;
        }

        /// <summary>
        /// 判断事件源是否位于指定祖先元素内部。
        /// </summary>
        /// <param name="source">事件原始源。</param>
        /// <param name="ancestor">目标祖先元素。</param>
        /// <returns>位于祖先内部时返回 true，否则返回 false。</returns>
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

        /// <summary>
        /// 从当前元素向上查找指定类型的父元素。
        /// </summary>
        /// <typeparam name="T">要查找的父元素类型。</typeparam>
        /// <param name="element">查找起点元素。</param>
        /// <returns>找到时返回父元素，否则返回 null。</returns>
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

        private sealed class AddTarget
        {
            /// <summary>
            /// 初始化添加目标。
            /// </summary>
            /// <param name="branch">目标分支；主线时为 null。</param>
            /// <param name="insertIndex">插入序号。</param>
            /// <returns>无返回值。</returns>
            public AddTarget(FlowBranch branch, int insertIndex)
            {
                Branch = branch;
                InsertIndex = insertIndex;
            }

            public FlowBranch Branch { get; private set; }

            public int InsertIndex { get; private set; }
        }

        private sealed class NodeLayoutInfo
        {
            /// <summary>
            /// 初始化流程块布局信息。
            /// </summary>
            /// <param name="node">流程块。</param>
            /// <param name="left">左坐标。</param>
            /// <param name="top">上坐标。</param>
            /// <param name="width">宽度。</param>
            /// <param name="height">高度。</param>
            /// <returns>无返回值。</returns>
            public NodeLayoutInfo(FlowNode node, double left, double top, double width, double height)
            {
                Node = node;
                Left = left;
                Top = top;
                Width = width;
                Height = height;
            }

            public FlowNode Node { get; private set; }

            public double Left { get; private set; }

            public double Top { get; private set; }

            public double Width { get; private set; }

            public double Height { get; private set; }

            public double Right
            {
                get { return Left + Width; }
            }

            public double Bottom
            {
                get { return Top + Height; }
            }

            public double CenterX
            {
                get { return Left + Width / 2.0; }
            }

            public double CenterY
            {
                get { return Top + Height / 2.0; }
            }

            public Point BottomCenter
            {
                get { return new Point(CenterX, Bottom); }
            }
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
