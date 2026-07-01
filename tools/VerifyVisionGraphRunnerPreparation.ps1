$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot

function Read-ProjectFile([string]$relativePath) {
    return Get-Content -LiteralPath (Join-Path $root $relativePath) -Raw -Encoding UTF8
}

$runner = Read-ProjectFile 'Services\Vision\VisionWorkflowRunner.cs'
$context = Read-ProjectFile 'Services\Vision\VisionRunContext.cs'

if ($runner -notmatch 'RunGraphAsync\(IList<FlowNode>') { throw 'VisionWorkflowRunner must expose a graph execution entry.' }
if ($runner -notmatch 'RunGraphExclusiveAsync') { throw 'VisionWorkflowRunner must isolate graph execution behind the single-run gate.' }
if ($runner -notmatch 'RunNodeCollection\(VisionRunContext context') { throw 'Graph execution must traverse node collections with an explicit context.' }
if ($runner -notmatch 'RunFlowNode\(VisionRunContext context') { throw 'Graph execution must route by FlowNode kind.' }
if ($runner -notmatch 'node\.IsThreadBlock') { throw 'Graph execution must recognize Thread blocks.' }
if ($runner -notmatch 'RunThreadBranches') { throw 'Thread block execution helper is missing.' }
if ($runner -notmatch 'CreateBranchContext') { throw 'Thread branches must use isolated branch contexts.' }
if ($runner -notmatch 'Task\.Run' -or $runner -notmatch 'Task\.WaitAll') { throw 'Thread branches must run through parallel tasks.' }
if ($runner -notmatch 'node\.IsSwitchBlock' -or $runner -notmatch 'node\.IsGotoBlock') { throw 'Graph execution must explicitly handle unsupported Switch and Goto blocks.' }
if ($runner -notmatch 'RunOperator\(VisionRunContext context') { throw 'Operator execution must accept an explicit context.' }
if ($runner -notmatch 'RunOperatorCore\(VisionRunContext context') { throw 'Operator core must accept an explicit context.' }
if ($runner -notmatch '_operatorResultsLock') { throw 'Parallel graph execution must protect shared operator result records.' }
if ($context -notmatch 'CreateBranchContext') { throw 'Branch context support is required before graph execution.' }

'Vision graph runner preparation verification passed.'
