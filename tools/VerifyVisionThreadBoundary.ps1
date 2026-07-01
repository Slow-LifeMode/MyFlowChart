$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot

function Read-ProjectFile([string]$relativePath) {
    return Get-Content -LiteralPath (Join-Path $root $relativePath) -Raw
}

$workItemPath = Join-Path $root 'Services\Vision\VisionOperatorWorkItem.cs'
$project = Read-ProjectFile 'MyFlowChart.csproj'
$executorInterface = Read-ProjectFile 'Services\Vision\IVisionOperatorExecutor.cs'
$runner = Read-ProjectFile 'Services\Vision\VisionWorkflowRunner.cs'

if (-not (Test-Path -LiteralPath $workItemPath)) { throw 'VisionOperatorWorkItem.cs is missing.' }
if ($project -notmatch 'Services\\Vision\\VisionOperatorWorkItem\.cs') { throw 'VisionOperatorWorkItem.cs is not compiled.' }
if ($executorInterface -match 'FlowOperator') { throw 'IVisionOperatorExecutor still depends on FlowOperator.' }
if ($runner -notmatch 'CreateWorkItems') { throw 'VisionWorkflowRunner does not snapshot operators before background execution.' }
if ($runner -notmatch 'RunNode\(isEnabled,\s*workItems,\s*token\)') { throw 'VisionWorkflowRunner does not pass snapshots into background execution.' }

'Vision thread boundary verification passed.'
