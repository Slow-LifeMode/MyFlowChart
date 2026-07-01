$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot

function Read-ProjectFile([string]$relativePath) {
    return Get-Content -LiteralPath (Join-Path $root $relativePath) -Raw -Encoding UTF8
}

$runner = Read-ProjectFile 'Services\Vision\VisionWorkflowRunner.cs'
$flowOperator = Read-ProjectFile 'Models\FlowOperator.cs'
$flowNode = Read-ProjectFile 'Models\FlowNode.cs'
$mainWindow = Read-ProjectFile 'MainWindow.xaml'

if ($runner -match 'if \(!result\.Success\)\s*\{\s*return result;\s*\}') {
    throw 'VisionWorkflowRunner must continue running later operators after an operator returns NG.'
}

if ($runner -notmatch 'hasFailure' -or $runner -notmatch 'failedResult') {
    throw 'VisionWorkflowRunner must aggregate NG results after running all operators.'
}

if ($runner -notmatch 'return hasFailure \? failedResult : VisionOperatorResult\.Ok') {
    throw 'VisionWorkflowRunner must return NG only after all operators have had a chance to run.'
}

if ($flowOperator -match 'DependencyHint' -or $flowOperator -match 'HasDependencyHint') {
    throw 'FlowOperator must not expose pre-run dependency hint UI state.'
}

if ($flowNode -match 'RefreshOperatorDependencyHints') {
    throw 'FlowNode must not run pre-run dependency hint checks.'
}

if ($mainWindow -match 'DependencyHint' -or $mainWindow -match 'HasDependencyHint') {
    throw 'MainWindow must not show pre-run dependency hints.'
}

'Vision continue-on-NG verification passed.'
