using System;

namespace MyFlowChart.Models
{
    public class FlowOperator : BindableBase
    {
        private int _sequence;
        private string _operatorName;
        private string _displayName;
        private object _parameters;
        private object _payload;
        private FlowNodeStatus _status = FlowNodeStatus.NotRun;
        private double _elapsedMilliseconds;
        private string _lastMessage;
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
            set
            {
                if (Set(ref _operatorName, value))
                {
                    Parameters = OperatorDefinition.CreateDefaultParameters(value);
                }
            }
        }

        public string DisplayName
        {
            get { return _displayName; }
            set { Set(ref _displayName, value); }
        }

        public object Parameters
        {
            get { return _parameters; }
            set { Set(ref _parameters, value); }
        }

        public object Payload
        {
            get { return _payload; }
            set { Set(ref _payload, value); }
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

        public string LastMessage
        {
            get { return _lastMessage; }
            set { Set(ref _lastMessage, value); }
        }

        public string StatusText
        {
            get
            {
                switch (Status)
                {
                    case FlowNodeStatus.Running:
                        return "running";
                    case FlowNodeStatus.OK:
                        return "OK";
                    case FlowNodeStatus.NG:
                        return "NG";
                    case FlowNodeStatus.Stopped:
                        return "stopped";
                    default:
                        return "not run";
                }
            }
        }

        /// <summary>
        /// 创建当前算子参数的独立副本。
        /// </summary>
        /// <returns>返回参数副本；没有参数时返回 null。</returns>
        public object CloneParameters()
        {
            ImageInputOperatorParameters imageInputParameters = Parameters as ImageInputOperatorParameters;
            if (imageInputParameters != null)
            {
                return imageInputParameters.Clone();
            }

            LineFindOperatorParameters lineFindParameters = Parameters as LineFindOperatorParameters;
            return lineFindParameters == null ? null : lineFindParameters.Clone();
        }
    }
}
