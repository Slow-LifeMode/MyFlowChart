using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MyFlowChart.Models
{
    /// <summary>
    /// 引脚方向。
    /// </summary>
    public enum PinDirection
    {
        /// <summary>
        /// 输入引脚。
        /// </summary>
        Input,

        /// <summary>
        /// 输出引脚。
        /// </summary>
        Output
    }

    /// <summary>
    /// 视觉节点引脚接口。
    /// </summary>
    public interface IPin
    {
        /// <summary>
        /// 引脚方向。
        /// </summary>
        PinDirection Direction { get; set; }

        /// <summary>
        /// 引脚当前值。
        /// </summary>
        object Value { get; set; }
    }

    /// <summary>
    /// 视觉节点接口。
    /// </summary>
    public interface INode
    {
        /// <summary>
        /// 输入引脚集合。
        /// </summary>
        ObservableCollection<IPin> Inputs { get; }

        /// <summary>
        /// 输出引脚集合。
        /// </summary>
        ObservableCollection<IPin> Outputs { get; }
    }

    /// <summary>
    /// 视觉节点引脚。
    /// </summary>
    public partial class Pin : ObservableObject, IPin
    {
        /// <summary>
        /// 引脚当前值。
        /// </summary>
        [ObservableProperty]
        private object _value;

        /// <summary>
        /// 引脚方向。
        /// </summary>
        public PinDirection Direction { get; set; }
    }

    /// <summary>
    /// 视觉节点抽象基类。
    /// </summary>
    public abstract class NodeBase : ObservableObject, INode
    {
        /// <summary>
        /// 输入引脚集合。
        /// </summary>
        public ObservableCollection<IPin> Inputs { get; } = new ObservableCollection<IPin>();

        /// <summary>
        /// 输出引脚集合。
        /// </summary>
        public ObservableCollection<IPin> Outputs { get; } = new ObservableCollection<IPin>();
    }
}
