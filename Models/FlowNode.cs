using System;

namespace MyFlowChart.Models
{
    public class FlowNode : BindableBase
    {
        private int _sequence;
        private string _operatorName;
        private string _displayName;
        private FlowNodeStatus _status = FlowNodeStatus.NotRun;
        private double _elapsedMilliseconds;
        private bool _isLast;
        private readonly Guid _id = Guid.NewGuid();

        public Guid Id
        {
            get { return _id; }
        }

        public int Sequence
        {
            get { return _sequence; }
            set { Set(ref _sequence, value); }
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
    }
}
