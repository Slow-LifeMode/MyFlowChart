$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot

function Read-ProjectFile([string]$relativePath) {
    return Get-Content -LiteralPath (Join-Path $root $relativePath) -Raw
}

$operatorDefinition = Read-ProjectFile 'Models\OperatorDefinition.cs'
$catalog = Read-ProjectFile 'Services\Vision\OperatorCatalog.cs'
$project = Read-ProjectFile 'MyFlowChart.csproj'
$executorPath = Join-Path $root 'Services\Vision\PlaceholderOperatorExecutor.cs'

if ($operatorDefinition -notmatch 'PlaceholderDetectName') { throw 'Placeholder operator name constant is missing.' }
if ($operatorDefinition -notmatch 'OperatorRuntimeKind\.Placeholder') { throw 'Placeholder runtime kind is missing.' }
if ($operatorDefinition -notmatch 'Name = PlaceholderDetectName[\s\S]*IsVisible = true[\s\S]*RuntimeKind = OperatorRuntimeKind\.Placeholder') {
    throw 'Placeholder operator must be visible and mapped to placeholder runtime.'
}
$placeholderLine = ($operatorDefinition -split "`r?`n") | Where-Object { $_ -match 'Name = PlaceholderDetectName' } | Select-Object -First 1
if ($placeholderLine -match 'EditorKind = OperatorEditorKind\.LineFind') {
    throw 'Placeholder operator must not open the line find editor.'
}

if ($catalog -notmatch 'OperatorRuntimeKind\.Placeholder') { throw 'OperatorCatalog does not register placeholder runtime kind.' }
if ($catalog -notmatch 'new PlaceholderOperatorExecutor\(\)') { throw 'OperatorCatalog does not create PlaceholderOperatorExecutor.' }
if (!(Test-Path -LiteralPath $executorPath)) { throw 'PlaceholderOperatorExecutor.cs is missing.' }

$executor = Get-Content -LiteralPath $executorPath -Raw
if ($executor -notmatch 'IVisionOperatorExecutor') { throw 'Placeholder executor must implement IVisionOperatorExecutor.' }
if ($executor -notmatch 'cancellationToken\.ThrowIfCancellationRequested\(\)') { throw 'Placeholder executor must honor cancellation.' }
if ($executor -notmatch 'VisionOperatorResult\.Ok') { throw 'Placeholder executor must return OK.' }
if ($executor -match 'VisionDataKeys\.CurrentImage') { throw 'Placeholder executor must not require image input.' }

if ($project -notmatch 'Services\\Vision\\PlaceholderOperatorExecutor\.cs') {
    throw 'PlaceholderOperatorExecutor.cs is not compiled by the project.'
}

'Placeholder operator verification passed.'
