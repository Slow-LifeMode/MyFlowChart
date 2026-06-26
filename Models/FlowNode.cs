using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace MyFlowChart.Models
{
    public class FlowNode : BindableBase
    {
        private int _sequence;
        private string _operatorName;
        private string _displayName;
        private string _remark;
        private string _imageWindowName = "图像窗口1";
        private FlowBlockKind _kind = FlowBlockKind.OperatorBlock;
        private FlowNodeStatus _status = FlowNodeStatus.NotRun;
        private double _elapsedMilliseconds;
        private bool _isLast;
        private bool _isEnabled = true;
        private bool _isSelected;
        private bool _forceBranch;
        private bool _forceInvalidInRun = true;
        private Guid? _gotoTargetNodeId;
        private string _conditionDataName = "未设置";
        private FlowOperator _selectedOperator;
        private FlowBranch _selectedBranch;
        private readonly Guid _id = Guid.NewGuid();
        private readonly ObservableCollection<FlowOperator> _operators = new ObservableCollection<FlowOperator>();
        private readonly ObservableCollection<FlowBranch> _branches = new ObservableCollection<FlowBranch>();

        /// <summary>
        /// 初始化流程块，并监听块内算子列表变化。
        /// </summary>
        /// <returns>无返回值。</returns>
        public FlowNode()
        {
            _operators.CollectionChanged += Operators_CollectionChanged;
            _branches.CollectionChanged += Branches_CollectionChanged;
        }

        public Guid Id
        {
            get { return _id; }
        }

        public int Sequence
        {
            get { return _sequence; }
            set { Set(ref _sequence, value); }
        }

        public FlowBlockKind Kind
        {
            get { return _kind; }
            set
            {
                if (Set(ref _kind, value))
                {
                    OnPropertyChanged("IsStartBlock");
                    OnPropertyChanged("IsEndBlock");
                    OnPropertyChanged("IsOperatorBlock");
                    OnPropertyChanged("IsGotoBlock");
                    OnPropertyChanged("IsSwitchBlock");
                    OnPropertyChanged("IsThreadBlock");
                    OnPropertyChanged("IsFixed");
                    OnPropertyChanged("IsConfigurable");
                    OnPropertyChanged("CanConfigureOperators");
                    OnPropertyChanged("CanConfigureGoto");
                    OnPropertyChanged("CanConfigureBranches");
                    OnPropertyChanged("BlockKindText");
                    OnPropertyChanged("NodeDisplayWidth");
                    OnPropertyChanged("TemplateWidth");
                }
            }
        }

        public string OperatorName
        {
            get { return _operatorName; }
            set { Set(ref _operatorName, value); }
        }

        public string DisplayName
        {
            get { return _displayName; }
            set { Set(ref _displayName, value); }
        }

        public string Remark
        {
            get { return _remark; }
            set { Set(ref _remark, value); }
        }

        public string ImageWindowName
        {
            get { return _imageWindowName; }
            set { Set(ref _imageWindowName, value); }
        }

        public FlowNodeStatus Status
        {
            get { return _status; }
            set
            {
                if (Set(ref _status, value))
                {
                    OnPropertyChanged("StatusText");
                }
            }
        }

        public double ElapsedMilliseconds
        {
            get { return _elapsedMilliseconds; }
            set { Set(ref _elapsedMilliseconds, value); }
        }

        public bool IsLast
        {
            get { return _isLast; }
            set { Set(ref _isLast, value); }
        }

        public bool IsEnabled
        {
            get { return _isEnabled; }
            set
            {
                if (Set(ref _isEnabled, value))
                {
                    SyncBranchChildrenEnabled(value);
                }
            }
        }

        public bool IsSelected
        {
            get { return _isSelected; }
            set { Set(ref _isSelected, value); }
        }

        public bool ForceBranch
        {
            get { return _forceBranch; }
            set { Set(ref _forceBranch, value); }
        }

        public bool ForceInvalidInRun
        {
            get { return _forceInvalidInRun; }
            set { Set(ref _forceInvalidInRun, value); }
        }

        public Guid? GotoTargetNodeId
        {
            get { return _gotoTargetNodeId; }
            set { Set(ref _gotoTargetNodeId, value); }
        }

        public string ConditionDataName
        {
            get { return _conditionDataName; }
            set { Set(ref _conditionDataName, value); }
        }

        public FlowOperator SelectedOperator
        {
            get { return _selectedOperator; }
            set { Set(ref _selectedOperator, value); }
        }

        public FlowBranch SelectedBranch
        {
            get { return _selectedBranch; }
            set { Set(ref _selectedBranch, value); }
        }

        public ObservableCollection<FlowOperator> Operators
        {
            get { return _operators; }
        }

        public ObservableCollection<FlowBranch> Branches
        {
            get { return _branches; }
        }

        public bool IsStartBlock
        {
            get { return Kind == FlowBlockKind.Start; }
        }

        public bool IsEndBlock
        {
            get { return Kind == FlowBlockKind.End; }
        }

        public bool IsOperatorBlock
        {
            get { return Kind == FlowBlockKind.OperatorBlock; }
        }

        public bool IsGotoBlock
        {
            get { return Kind == FlowBlockKind.Goto; }
        }

        public bool IsSwitchBlock
        {
            get { return Kind == FlowBlockKind.Switch; }
        }

        public bool IsThreadBlock
        {
            get { return Kind == FlowBlockKind.Thread; }
        }

        public bool IsFixed
        {
            get { return Kind == FlowBlockKind.Start || Kind == FlowBlockKind.End; }
        }

        public bool IsConfigurable
        {
            get { return Kind != FlowBlockKind.Start && Kind != FlowBlockKind.End; }
        }

        public bool CanConfigureOperators
        {
            get { return Kind == FlowBlockKind.OperatorBlock; }
        }

        public bool CanConfigureGoto
        {
            get { return Kind == FlowBlockKind.Goto; }
        }

        public bool CanConfigureBranches
        {
            get { return Kind == FlowBlockKind.Switch || Kind == FlowBlockKind.Thread; }
        }

        public string OperatorCountText
        {
            get
            {
                if (IsGotoBlock)
                {
                    return "Goto";
                }

                if (IsSwitchBlock)
                {
                    return Branches.Count + " 个分支";
                }

                if (IsThreadBlock)
                {
                    return Branches.Count + " 条线程";
                }

                return Operators.Count + " 个算子";
            }
        }

        public string BlockKindText
        {
            get
            {
                switch (Kind)
                {
                    case FlowBlockKind.Goto:
                        return "Goto块";
                    case FlowBlockKind.Switch:
                        return "分支块";
                    case FlowBlockKind.Thread:
                        return "线程块";
                    case FlowBlockKind.Start:
                        return "开始";
                    case FlowBlockKind.End:
                        return "结束";
                    default:
                        return "算子块";
                }
            }
        }

        public double NodeDisplayWidth
        {
            get
            {
                if (IsThreadBlock)
                {
                    return Math.Max(136, Branches.Count * 110);
                }

                return 190;
            }
        }

        public double TemplateWidth
        {
            get { return Math.Max(190, NodeDisplayWidth); }
        }

        public string StatusText
        {
            get
            {
                switch (Status)
                {
                    case FlowNodeStatus.Running:
                        return "运行中";
                    case FlowNodeStatus.OK:
                        return "完成";
                    case FlowNodeStatus.NG:
                        return "失败";
                    case FlowNodeStatus.Stopped:
                        return "已停止";
                    default:
                        return "未运行";
                }
            }
        }

        /// <summary>
        /// 创建不可删除的开始块。
        /// </summary>
        /// <returns>返回开始流程块。</returns>
        public static FlowNode CreateStartBlock()
        {
            return new FlowNode
            {
                Kind = FlowBlockKind.Start,
                DisplayName = "开始",
                OperatorName = "开始",
                IsEnabled = true
            };
        }

        /// <summary>
        /// 创建不可删除的结束块。
        /// </summary>
        /// <returns>返回结束流程块。</returns>
        public static FlowNode CreateEndBlock()
        {
            return new FlowNode
            {
                Kind = FlowBlockKind.End,
                DisplayName = "结束",
                OperatorName = "结束",
                IsEnabled = true
            };
        }

        /// <summary>
        /// 创建普通算子块。
        /// </summary>
        /// <param name="blockName">算子块名称。</param>
        /// <returns>返回普通算子块。</returns>
        public static FlowNode CreateOperatorBlock(string blockName)
        {
            return new FlowNode
            {
                Kind = FlowBlockKind.OperatorBlock,
                DisplayName = blockName,
                OperatorName = "算子块",
                IsEnabled = true
            };
        }

        /// <summary>
        /// 创建普通 Goto 块。
        /// </summary>
        /// <param name="blockName">Goto 块名称。</param>
        /// <returns>返回 Goto 流程块。</returns>
        public static FlowNode CreateGotoBlock(string blockName)
        {
            return new FlowNode
            {
                Kind = FlowBlockKind.Goto,
                DisplayName = blockName,
                OperatorName = "Goto块",
                IsEnabled = true
            };
        }

        /// <summary>
        /// 创建普通分支块。
        /// </summary>
        /// <param name="blockName">分支块名称。</param>
        /// <returns>返回分支流程块。</returns>
        public static FlowNode CreateSwitchBlock(string blockName)
        {
            FlowNode node = new FlowNode
            {
                Kind = FlowBlockKind.Switch,
                DisplayName = blockName,
                OperatorName = "分支块",
                IsEnabled = true
            };

            node.AddBranch("条件1");
            node.AddBranch("条件2");
            return node;
        }

        /// <summary>
        /// 创建普通线程块。
        /// </summary>
        /// <param name="blockName">线程块名称。</param>
        /// <returns>返回线程流程块。</returns>
        public static FlowNode CreateThreadBlock(string blockName)
        {
            FlowNode node = new FlowNode
            {
                Kind = FlowBlockKind.Thread,
                DisplayName = blockName,
                OperatorName = "线程块",
                IsEnabled = true
            };

            node.AddBranch("Thread1");
            node.AddBranch("Thread2");
            return node;
        }

        /// <summary>
        /// 向当前块追加一个分支配置。
        /// </summary>
        /// <param name="conditionValue">分支匹配值。</param>
        /// <returns>返回新增的分支配置。</returns>
        public FlowBranch AddBranch(string conditionValue)
        {
            FlowBranch branch = new FlowBranch
            {
                ConditionValue = conditionValue
            };

            Branches.Add(branch);
            SelectedBranch = branch;
            return branch;
        }

        /// <summary>
        /// 删除当前选中的分支配置。
        /// </summary>
        /// <returns>删除成功返回 true，否则返回 false。</returns>
        public bool RemoveSelectedBranch()
        {
            if (SelectedBranch == null || Branches.Count <= 1)
            {
                return false;
            }

            int index = Branches.IndexOf(SelectedBranch);
            Branches.Remove(SelectedBranch);
            SelectedBranch = Branches.Count == 0 ? null : Branches[Math.Max(0, Math.Min(index, Branches.Count - 1))];
            return true;
        }

        /// <summary>
        /// 处理块内算子集合变化并刷新序号。
        /// </summary>
        /// <param name="sender">触发变化的算子集合。</param>
        /// <param name="e">集合变化事件参数。</param>
        /// <returns>无返回值。</returns>
        private void Operators_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            for (int i = 0; i < Operators.Count; i++)
            {
                Operators[i].Sequence = i + 1;
            }

            if (SelectedOperator != null && !Operators.Contains(SelectedOperator))
            {
                SelectedOperator = null;
            }

            OnPropertyChanged("OperatorCountText");
            OnPropertyChanged("NodeDisplayWidth");
            OnPropertyChanged("TemplateWidth");
        }

        /// <summary>
        /// 同步当前分支块或线程块内部所有流程块的启用状态。
        /// </summary>
        /// <param name="isEnabled">需要同步到子流程块的启用状态。</param>
        /// <returns>无返回值。</returns>
        private void SyncBranchChildrenEnabled(bool isEnabled)
        {
            if (!CanConfigureBranches)
            {
                return;
            }

            foreach (FlowBranch branch in Branches)
            {
                foreach (FlowNode node in branch.Nodes)
                {
                    node.ApplyParentEnabled(isEnabled);
                }
            }
        }

        /// <summary>
        /// 应用父分支块传下来的启用状态，并继续同步到更深层子流程块。
        /// </summary>
        /// <param name="isEnabled">父分支块传下来的启用状态。</param>
        /// <returns>无返回值。</returns>
        private void ApplyParentEnabled(bool isEnabled)
        {
            Set(ref _isEnabled, isEnabled);
            SyncBranchChildrenEnabled(isEnabled);
        }

        /// <summary>
        /// 处理分支集合变化并刷新分支序号。
        /// </summary>
        /// <param name="sender">触发变化的分支集合。</param>
        /// <param name="e">集合变化事件参数。</param>
        /// <returns>无返回值。</returns>
        private void Branches_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            for (int i = 0; i < Branches.Count; i++)
            {
                Branches[i].Sequence = i + 1;
            }

            if (SelectedBranch != null && !Branches.Contains(SelectedBranch))
            {
                SelectedBranch = null;
            }

            OnPropertyChanged("OperatorCountText");
        }
    }
}
