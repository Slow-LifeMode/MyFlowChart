using System;
using System.Collections.Generic;
using MyFlowChart.Models;

namespace MyFlowChart.Services.Vision
{
    /// <summary>
    /// 保存视觉算子执行器注册表。
    /// </summary>
    public sealed class OperatorCatalog
    {
        private readonly Dictionary<string, Func<IVisionOperatorExecutor>> _executors =
            new Dictionary<string, Func<IVisionOperatorExecutor>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 初始化默认视觉算子注册表。
        /// </summary>
        public OperatorCatalog()
        {
            foreach (OperatorDefinition definition in OperatorDefinition.CreateKnownOperators())
            {
                Func<IVisionOperatorExecutor> factory = CreateFactory(definition.RuntimeKind);
                if (factory != null)
                {
                    Register(definition.Name, factory);
                }
            }
        }

        /// <summary>
        /// 根据算子运行类型创建执行器工厂。
        /// </summary>
        /// <param name="runtimeKind">算子运行类型。</param>
        /// <returns>返回执行器工厂；无运行器时返回 null。</returns>
        private static Func<IVisionOperatorExecutor> CreateFactory(OperatorRuntimeKind runtimeKind)
        {
            switch (runtimeKind)
            {
                case OperatorRuntimeKind.ImageInput:
                    return () => new ImageInputOperatorExecutor();
                case OperatorRuntimeKind.LineFind:
                    return () => new LineFindOperatorExecutor();
                case OperatorRuntimeKind.Placeholder:
                    return () => new PlaceholderOperatorExecutor();
                case OperatorRuntimeKind.ResultOutput:
                    return () => new ResultOutputOperatorExecutor();
                default:
                    return null;
            }
        }

        /// <summary>
        /// 注册视觉算子执行器。
        /// </summary>
        /// <param name="operatorName">算子名称。</param>
        /// <param name="factory">执行器工厂。</param>
        public void Register(string operatorName, Func<IVisionOperatorExecutor> factory)
        {
            if (string.IsNullOrWhiteSpace(operatorName))
            {
                throw new ArgumentException("算子名称不能为空。", nameof(operatorName));
            }

            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            _executors[operatorName] = factory;
        }

        /// <summary>
        /// 根据算子名称创建执行器。
        /// </summary>
        /// <param name="operatorName">算子名称。</param>
        /// <param name="executor">创建出的执行器。</param>
        /// <returns>找到执行器时返回 true。</returns>
        public bool TryCreate(string operatorName, out IVisionOperatorExecutor executor)
        {
            executor = null;
            if (string.IsNullOrWhiteSpace(operatorName))
            {
                return false;
            }

            Func<IVisionOperatorExecutor> factory;
            if (!_executors.TryGetValue(operatorName, out factory))
            {
                return false;
            }

            executor = factory();
            return executor != null;
        }
    }
}
