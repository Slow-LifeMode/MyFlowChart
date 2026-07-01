using System;
using System.Linq;

namespace MyFlowChart.Models
{
    public enum OperatorRuntimeKind
    {
        None,
        ImageInput,
        LineFind,
        Placeholder,
        ResultOutput
    }

    public enum OperatorEditorKind
    {
        None,
        ImageInput,
        LineFind
    }

    public class OperatorDefinition
    {
        public const string ImageInputName = "图像采集";
        public const string PreprocessName = "预处理";
        public const string LineFindName = "直线查找";
        public const string PlaceholderDetectName = "占位检测";
        public const string OuterCircleDetectName = "外侧圆检测";
        public const string InnerCircleDetectName = "内侧圆检测";
        public const string ResultOutputName = "结果输出";

        public string Name { get; set; }

        public bool IsVisible { get; set; }

        public OperatorRuntimeKind RuntimeKind { get; set; }

        public OperatorEditorKind EditorKind { get; set; }

        /// <summary>
        /// 创建全部内置算子定义。
        /// </summary>
        /// <returns>返回内置算子定义数组。</returns>
        public static OperatorDefinition[] CreateKnownOperators()
        {
            return new[]
            {
                new OperatorDefinition { Name = ImageInputName, IsVisible = true, RuntimeKind = OperatorRuntimeKind.ImageInput, EditorKind = OperatorEditorKind.ImageInput },
                new OperatorDefinition { Name = PreprocessName, RuntimeKind = OperatorRuntimeKind.ImageInput },
                new OperatorDefinition { Name = LineFindName, IsVisible = true, RuntimeKind = OperatorRuntimeKind.LineFind, EditorKind = OperatorEditorKind.LineFind },
                new OperatorDefinition { Name = PlaceholderDetectName, IsVisible = true, RuntimeKind = OperatorRuntimeKind.Placeholder },
                new OperatorDefinition { Name = OuterCircleDetectName, RuntimeKind = OperatorRuntimeKind.LineFind, EditorKind = OperatorEditorKind.LineFind },
                new OperatorDefinition { Name = InnerCircleDetectName, RuntimeKind = OperatorRuntimeKind.LineFind, EditorKind = OperatorEditorKind.LineFind },
                new OperatorDefinition { Name = ResultOutputName, IsVisible = true, RuntimeKind = OperatorRuntimeKind.ResultOutput }
            };
        }

        /// <summary>
        /// 创建默认显示在算子库中的算子列表。
        /// </summary>
        /// <returns>返回默认可见的视觉算子定义。</returns>
        public static OperatorDefinition[] CreateDefaultOperators()
        {
            return CreateKnownOperators().Where(x => x.IsVisible).ToArray();
        }

        /// <summary>
        /// 根据算子名称创建默认参数。
        /// </summary>
        /// <param name="operatorName">算子名称。</param>
        /// <returns>返回默认参数；不需要参数时返回 null。</returns>
        public static object CreateDefaultParameters(string operatorName)
        {
            switch (GetRuntimeKind(operatorName))
            {
                case OperatorRuntimeKind.ImageInput:
                    return new ImageInputOperatorParameters();
                case OperatorRuntimeKind.LineFind:
                    return new LineFindOperatorParameters();
                default:
                    return null;
            }
        }

        /// <summary>
        /// 读取指定算子的运行类型。
        /// </summary>
        /// <param name="operatorName">算子名称。</param>
        /// <returns>返回运行类型；未知算子返回 None。</returns>
        public static OperatorRuntimeKind GetRuntimeKind(string operatorName)
        {
            OperatorDefinition definition = Find(operatorName);
            return definition == null ? OperatorRuntimeKind.None : definition.RuntimeKind;
        }

        /// <summary>
        /// 读取指定算子的编辑器类型。
        /// </summary>
        /// <param name="operatorName">算子名称。</param>
        /// <returns>返回编辑器类型；无编辑器时返回 None。</returns>
        public static OperatorEditorKind GetEditorKind(string operatorName)
        {
            OperatorDefinition definition = Find(operatorName);
            return definition == null ? OperatorEditorKind.None : definition.EditorKind;
        }

        /// <summary>
        /// 判断算子是否使用直线查找参数和执行器。
        /// </summary>
        /// <param name="operatorName">算子名称。</param>
        /// <returns>直线查找类算子返回 true，否则返回 false。</returns>
        public static bool IsLineFindOperator(string operatorName)
        {
            return GetRuntimeKind(operatorName) == OperatorRuntimeKind.LineFind;
        }

        /// <summary>
        /// 查找指定名称的内置算子定义。
        /// </summary>
        /// <param name="operatorName">算子名称。</param>
        /// <returns>返回匹配的算子定义；未找到时返回 null。</returns>
        private static OperatorDefinition Find(string operatorName)
        {
            if (string.IsNullOrWhiteSpace(operatorName))
            {
                return null;
            }

            return CreateKnownOperators().FirstOrDefault(x => string.Equals(x.Name, operatorName, StringComparison.OrdinalIgnoreCase));
        }
    }
}
