namespace MyFlowChart.Models
{
    public class FlowBranch : BindableBase
    {
        private int _sequence;
        private string _conditionValue;
        private readonly System.Collections.ObjectModel.ObservableCollection<FlowNode> _nodes =
            new System.Collections.ObjectModel.ObservableCollection<FlowNode>();

        public int Sequence
        {
            get { return _sequence; }
            set { Set(ref _sequence, value); }
        }

        public string ConditionValue
        {
            get { return _conditionValue; }
            set { Set(ref _conditionValue, value); }
        }

        public System.Collections.ObjectModel.ObservableCollection<FlowNode> Nodes
        {
            get { return _nodes; }
        }
    }
}
