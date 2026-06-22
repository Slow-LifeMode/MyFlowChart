using System.Collections.Generic;

namespace MyFlowChart.Models
{
    public class FlowExecutionContext
    {
        private readonly Dictionary<string, object> _items = new Dictionary<string, object>();
        private readonly List<string> _executionLog = new List<string>();

        public Dictionary<string, object> Items
        {
            get { return _items; }
        }

        public List<string> ExecutionLog
        {
            get { return _executionLog; }
        }
    }
}
