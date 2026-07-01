using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MyFlowChart.Models;
using OpenCvSharp;
using OpenCvWindowTool;

namespace MyFlowChart.Services.Vision
{
    /// <summary>
    /// 负责在后台运行视觉流程。
    /// </summary>
    public sealed class VisionWorkflowRunner : IDisposable
    {
        private readonly List<VisionOperatorRunRecord> _operatorResults = new List<VisionOperatorRunRecord>();
        private readonly object _operatorResultsLock = new object();
        private readonly OperatorCatalog _catalog;
        private CancellationTokenSource _cancellation;
        private VisionRunContext _context;
        private VisionOperatorResult _lastOperatorResult;
        private long _frameNumber;
        private int _isRunning;

        /// <summary>
        /// 初始化视觉流程运行器。
        /// </summary>
        /// <param name="catalog">算子注册表。</param>
        public VisionWorkflowRunner(OperatorCatalog catalog)
        {
            _catalog = catalog ?? new OperatorCatalog();
        }

        /// <summary>
        /// 获取最近一次完成的算子结果。
        /// </summary>
        public VisionOperatorResult LastOperatorResult
        {
            get
            {
                lock (_operatorResultsLock)
                {
                    return _lastOperatorResult;
                }
            }
        }

        /// <summary>
        /// 获取最近一次运行的算子快照。
        /// </summary>
        public IReadOnlyList<VisionOperatorRunRecord> OperatorResults
        {
            get
            {
                lock (_operatorResultsLock)
                {
                    return _operatorResults.ToList();
                }
            }
        }

        /// <summary>
        /// 异步运行单个流程块。
        /// </summary>
        /// <param name="node">流程节点。</param>
        /// <param name="image">运行图像。</param>
        /// <param name="lineDetectionRoi">直线检测使用的 ROI。</param>
        /// <returns>运行任务。</returns>
        public Task<VisionOperatorResult> RunNodeAsync(FlowNode node, Mat image, RoiItem lineDetectionRoi = null)
        {
            if (Interlocked.Exchange(ref _isRunning, 1) == 1)
            {
                return Task.FromResult(VisionOperatorResult.Fail("视觉流程正在运行。"));
            }

            return RunNodeExclusiveAsync(node, image, lineDetectionRoi);
        }

        /// <summary>
        /// 异步运行完整流程图节点集合。
        /// </summary>
        /// <param name="nodes">流程节点集合。</param>
        /// <param name="image">运行图像。</param>
        /// <param name="lineDetectionRoi">直线检测使用的 ROI。</param>
        /// <returns>运行任务。</returns>
        public Task<VisionOperatorResult> RunGraphAsync(IList<FlowNode> nodes, Mat image, RoiItem lineDetectionRoi = null)
        {
            if (Interlocked.Exchange(ref _isRunning, 1) == 1)
            {
                return Task.FromResult(VisionOperatorResult.Fail("视觉流程正在运行。"));
            }

            return RunGraphExclusiveAsync(nodes, image, lineDetectionRoi);
        }

        /// <summary>
        /// 在单运行门闩内执行流程。
        /// </summary>
        /// <param name="node">流程节点。</param>
        /// <param name="image">运行图像。</param>
        /// <param name="lineDetectionRoi">直线检测使用的 ROI。</param>
        /// <returns>运行任务。</returns>
        private async Task<VisionOperatorResult> RunNodeExclusiveAsync(FlowNode node, Mat image, RoiItem lineDetectionRoi)
        {
            _cancellation?.Dispose();
            _cancellation = new CancellationTokenSource();
            _context?.Dispose();
            try
            {
                _context = new VisionRunContext(image, Interlocked.Increment(ref _frameNumber));
                ResetOperatorResults();
                if (lineDetectionRoi != null)
                {
                    _context.Items[VisionDataKeys.LineDetectionRoi] = lineDetectionRoi;
                }

                bool isEnabled = node != null && node.IsEnabled;
                List<VisionOperatorWorkItem> workItems = CreateWorkItems(node);
                CancellationToken token = _cancellation.Token;
                return await Task.Run(() => RunNode(isEnabled, workItems, token), token).ConfigureAwait(false);
            }
            finally
            {
                _context?.Dispose();
                _context = null;
                Interlocked.Exchange(ref _isRunning, 0);
            }
        }

        /// <summary>
        /// 在单运行门闩内执行完整流程图。
        /// </summary>
        /// <param name="nodes">流程节点集合。</param>
        /// <param name="image">运行图像。</param>
        /// <param name="lineDetectionRoi">直线检测使用的 ROI。</param>
        /// <returns>运行任务。</returns>
        private async Task<VisionOperatorResult> RunGraphExclusiveAsync(IList<FlowNode> nodes, Mat image, RoiItem lineDetectionRoi)
        {
            _cancellation?.Dispose();
            _cancellation = new CancellationTokenSource();
            _context?.Dispose();
            try
            {
                _context = new VisionRunContext(image, Interlocked.Increment(ref _frameNumber));
                ResetOperatorResults();
                if (lineDetectionRoi != null)
                {
                    _context.Items[VisionDataKeys.LineDetectionRoi] = lineDetectionRoi;
                }

                CancellationToken token = _cancellation.Token;
                return await Task.Run(() => RunNodeCollection(_context, nodes, token), token).ConfigureAwait(false);
            }
            finally
            {
                _context?.Dispose();
                _context = null;
                Interlocked.Exchange(ref _isRunning, 0);
            }
        }

        /// <summary>
        /// 请求停止当前运行。
        /// </summary>
        public void Stop()
        {
            if (_cancellation != null && !_cancellation.IsCancellationRequested)
            {
                _cancellation.Cancel();
            }
        }

        /// <summary>
        /// 释放运行器资源。
        /// </summary>
        public void Dispose()
        {
            Stop();
            _cancellation?.Dispose();
            _context?.Dispose();
        }

        /// <summary>
        /// 在 UI 线程创建后台运行需要的算子快照。
        /// </summary>
        /// <param name="node">流程节点。</param>
        /// <returns>后台运行算子快照集合。</returns>
        private static List<VisionOperatorWorkItem> CreateWorkItems(FlowNode node)
        {
            if (node == null)
            {
                return null;
            }

            return node.Operators
                .Select(VisionOperatorWorkItem.FromFlowOperator)
                .Where(x => x != null)
                .ToList();
        }

        /// <summary>
        /// 清空上一次运行的算子结果。
        /// </summary>
        private void ResetOperatorResults()
        {
            lock (_operatorResultsLock)
            {
                _operatorResults.Clear();
                _lastOperatorResult = null;
            }
        }

        /// <summary>
        /// 运行流程节点集合。
        /// </summary>
        /// <param name="context">运行上下文。</param>
        /// <param name="nodes">流程节点集合。</param>
        /// <param name="token">取消令牌。</param>
        /// <returns>运行结果。</returns>
        private VisionOperatorResult RunNodeCollection(VisionRunContext context, IList<FlowNode> nodes, CancellationToken token)
        {
            if (nodes == null)
            {
                return VisionOperatorResult.Fail("流程节点集合为空。");
            }

            try
            {
                bool hasFailure = false;
                VisionOperatorResult failedResult = null;
                foreach (FlowNode node in nodes)
                {
                    if (node == null || !node.IsEnabled)
                    {
                        continue;
                    }

                    token.ThrowIfCancellationRequested();
                    VisionOperatorResult result = RunFlowNode(context, node, token);
                    if (!result.Success && !hasFailure)
                    {
                        hasFailure = true;
                        failedResult = result;
                    }
                }

                return hasFailure ? failedResult : VisionOperatorResult.Ok("视觉流程运行完成。");
            }
            catch (OperationCanceledException)
            {
                return VisionOperatorResult.Fail("视觉流程已停止。");
            }
            catch (Exception ex)
            {
                return VisionOperatorResult.FromException(ex);
            }
        }

        /// <summary>
        /// 按流程块类型运行单个节点。
        /// </summary>
        /// <param name="context">运行上下文。</param>
        /// <param name="node">流程节点。</param>
        /// <param name="token">取消令牌。</param>
        /// <returns>运行结果。</returns>
        private VisionOperatorResult RunFlowNode(VisionRunContext context, FlowNode node, CancellationToken token)
        {
            if (node == null || !node.IsEnabled)
            {
                return VisionOperatorResult.Ok("流程节点已跳过。");
            }

            if (node.IsOperatorBlock)
            {
                return RunOperatorBlock(context, node.IsEnabled, CreateWorkItems(node), token);
            }

            if (node.IsThreadBlock)
            {
                return RunThreadBranches(context, node, token);
            }

            if (node.IsSwitchBlock)
            {
                return VisionOperatorResult.Fail("Switch 块真实视觉执行尚未启用。");
            }

            if (node.IsGotoBlock)
            {
                return VisionOperatorResult.Fail("Goto 块真实视觉执行尚未启用。");
            }

            return VisionOperatorResult.Ok("流程节点已跳过。");
        }

        /// <summary>
        /// 并行运行线程块的各条分支。
        /// </summary>
        /// <param name="context">父级运行上下文。</param>
        /// <param name="node">线程块。</param>
        /// <param name="token">取消令牌。</param>
        /// <returns>运行结果。</returns>
        private VisionOperatorResult RunThreadBranches(VisionRunContext context, FlowNode node, CancellationToken token)
        {
            if (node == null || node.Branches.Count == 0)
            {
                return VisionOperatorResult.Ok("线程块没有分支。");
            }

            Task<VisionOperatorResult>[] branchTasks = node.Branches
                .Select(branch => Task.Run(() =>
                {
                    using (VisionRunContext branchContext = context.CreateBranchContext())
                    {
                        return RunNodeCollection(branchContext, branch.Nodes.ToList(), token);
                    }
                }, token))
                .ToArray();

            try
            {
                Task.WaitAll(branchTasks);
            }
            catch (AggregateException ex)
            {
                if (ex.InnerExceptions.Any(e => e is OperationCanceledException))
                {
                    return VisionOperatorResult.Fail("视觉流程已停止。");
                }

                return VisionOperatorResult.FromException(ex.Flatten().InnerExceptions.FirstOrDefault());
            }

            bool hasFailure = false;
            VisionOperatorResult failedResult = null;
            foreach (Task<VisionOperatorResult> task in branchTasks)
            {
                VisionOperatorResult result = task.Result;
                if (!result.Success && !hasFailure)
                {
                    hasFailure = true;
                    failedResult = result;
                }
            }

            return hasFailure ? failedResult : VisionOperatorResult.Ok("线程块运行完成。");
        }

        /// <summary>
        /// 同步执行节点内算子。
        /// </summary>
        /// <param name="isEnabled">流程节点是否启用。</param>
        /// <param name="workItems">后台运行算子快照集合。</param>
        /// <param name="token">取消令牌。</param>
        /// <returns>运行结果。</returns>
        private VisionOperatorResult RunNode(bool isEnabled, List<VisionOperatorWorkItem> workItems, CancellationToken token)
        {
            return RunOperatorBlock(_context, isEnabled, workItems, token);
        }

        /// <summary>
        /// 同步执行算子块内算子。
        /// </summary>
        /// <param name="context">运行上下文。</param>
        /// <param name="isEnabled">流程节点是否启用。</param>
        /// <param name="workItems">后台运行算子快照集合。</param>
        /// <param name="token">取消令牌。</param>
        /// <returns>运行结果。</returns>
        private VisionOperatorResult RunOperatorBlock(VisionRunContext context, bool isEnabled, List<VisionOperatorWorkItem> workItems, CancellationToken token)
        {
            if (workItems == null)
            {
                return VisionOperatorResult.Fail("未选择流程节点。");
            }

            if (!isEnabled)
            {
                return VisionOperatorResult.Fail("流程节点未启用。");
            }

            Stopwatch stopwatch = Stopwatch.StartNew();
            try
            {
                bool hasFailure = false;
                VisionOperatorResult failedResult = null;
                foreach (VisionOperatorWorkItem workItem in workItems)
                {
                    token.ThrowIfCancellationRequested();
                    VisionOperatorResult result = RunOperator(context, workItem, token);
                    if (!result.Success && !hasFailure)
                    {
                        hasFailure = true;
                        failedResult = result;
                    }
                }

                return hasFailure ? failedResult : VisionOperatorResult.Ok("视觉流程运行完成。");
            }
            catch (OperationCanceledException)
            {
                return VisionOperatorResult.Fail("视觉流程已停止。");
            }
            catch (Exception ex)
            {
                return VisionOperatorResult.FromException(ex);
            }
            finally
            {
                stopwatch.Stop();
            }
        }

        /// <summary>
        /// 执行单个算子并记录运行快照。
        /// </summary>
        /// <param name="workItem">本次运行的算子快照。</param>
        /// <param name="token">取消令牌。</param>
        /// <returns>算子执行结果。</returns>
        private VisionOperatorResult RunOperator(VisionOperatorWorkItem workItem, CancellationToken token)
        {
            return RunOperator(_context, workItem, token);
        }

        /// <summary>
        /// 使用指定上下文执行单个算子并记录运行快照。
        /// </summary>
        /// <param name="context">运行上下文。</param>
        /// <param name="workItem">本次运行的算子快照。</param>
        /// <param name="token">取消令牌。</param>
        /// <returns>算子执行结果。</returns>
        private VisionOperatorResult RunOperator(VisionRunContext context, VisionOperatorWorkItem workItem, CancellationToken token)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            VisionOperatorResult result;

            try
            {
                result = RunOperatorCore(context, workItem, token);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                result = VisionOperatorResult.FromException(ex);
            }
            finally
            {
                stopwatch.Stop();
            }

            if (result == null)
            {
                result = VisionOperatorResult.Fail("算子返回空结果。");
            }

            lock (_operatorResultsLock)
            {
                _lastOperatorResult = result;
                if (workItem != null)
                {
                    _operatorResults.Add(new VisionOperatorRunRecord(
                        workItem.OperatorId,
                        result.Success ? FlowNodeStatus.OK : FlowNodeStatus.NG,
                        stopwatch.Elapsed.TotalMilliseconds,
                        result.Message,
                        result.Payload));
                }
            }

            return result;
        }

        /// <summary>
        /// 执行单个算子。
        /// </summary>
        /// <param name="workItem">本次运行的算子快照。</param>
        /// <param name="token">取消令牌。</param>
        /// <returns>算子执行结果。</returns>
        private VisionOperatorResult RunOperatorCore(VisionOperatorWorkItem workItem, CancellationToken token)
        {
            return RunOperatorCore(_context, workItem, token);
        }

        /// <summary>
        /// 使用指定上下文执行单个算子。
        /// </summary>
        /// <param name="context">运行上下文。</param>
        /// <param name="workItem">本次运行的算子快照。</param>
        /// <param name="token">取消令牌。</param>
        /// <returns>算子执行结果。</returns>
        private VisionOperatorResult RunOperatorCore(VisionRunContext context, VisionOperatorWorkItem workItem, CancellationToken token)
        {
            if (workItem == null)
            {
                return VisionOperatorResult.Fail("流程算子为空。");
            }

            IVisionOperatorExecutor executor;
            if (!_catalog.TryCreate(workItem.OperatorName, out executor))
            {
                return VisionOperatorResult.Fail("未注册算子：" + workItem.OperatorName);
            }

            VisionOperatorResult result = executor.Execute(workItem, context, token);
            lock (_operatorResultsLock)
            {
                _lastOperatorResult = result;
            }

            return result;
        }
    }
}
