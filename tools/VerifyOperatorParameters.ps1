$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot

function Read-ProjectFile([string]$relativePath) {
    return Get-Content -LiteralPath (Join-Path $root $relativePath) -Raw
}

$flowOperator = Read-ProjectFile 'Models\FlowOperator.cs'
$operatorDefinition = Read-ProjectFile 'Models\OperatorDefinition.cs'
$lineParamsPath = Join-Path $root 'Models\LineFindOperatorParameters.cs'
$executor = Read-ProjectFile 'Services\Vision\LineFindOperatorExecutor.cs'
$catalog = Read-ProjectFile 'Services\Vision\OperatorCatalog.cs'
$mainWindow = Read-ProjectFile 'MainWindow.xaml'
$project = Read-ProjectFile 'MyFlowChart.csproj'
$palette = Read-ProjectFile 'Controls\OperatorPaletteControl.xaml.cs'

if (-not (Test-Path -LiteralPath $lineParamsPath)) { throw 'LineFindOperatorParameters.cs is missing.' }
if ($flowOperator -notmatch 'object\s+Parameters') { throw 'FlowOperator.Parameters is missing.' }
if ($flowOperator -notmatch 'CloneParameters') { throw 'FlowOperator parameter clone support is missing.' }
if ($operatorDefinition -notmatch 'CreateDefaultOperators') { throw 'OperatorDefinition does not expose default operator definitions.' }
if ($operatorDefinition -notmatch 'CreateDefaultParameters') { throw 'OperatorDefinition does not create default operator parameters.' }
if ($flowOperator -notmatch 'OperatorDefinition\.CreateDefaultParameters') { throw 'FlowOperator does not reuse OperatorDefinition parameter factory.' }
if ($palette -notmatch 'OperatorDefinition\.CreateDefaultOperators') { throw 'OperatorPaletteControl does not reuse default operator definitions.' }
if ($catalog -notmatch 'OperatorDefinition\.CreateKnownOperators') { throw 'OperatorCatalog does not reuse known operator definitions.' }
if ($catalog -notmatch 'OperatorRuntimeKind\.LineFind') { throw 'OperatorCatalog does not register line find runtime kind.' }
if ($executor -notmatch 'ToLineDetectionParams') { throw 'LineFindOperatorExecutor does not read operator parameters.' }
if ($executor -notmatch 'ResolveDetectionMode') { throw 'LineFindOperatorExecutor does not read detection mode.' }
if ($mainWindow -notmatch 'SelectedOperator\.Parameters') { throw 'Operator parameter panel is not bound.' }
if ($mainWindow -notmatch 'LineDetectionMode\.SelfMode') { throw 'Operator parameter panel does not expose detection mode.' }
if ($mainWindow -notmatch 'Content="SelfMode"' -or $mainWindow -notmatch 'Content="OPTMode"') { throw 'Operator parameter panel detection mode text must match OpencvMaster.' }
if ($project -notmatch 'Models\\LineFindOperatorParameters\.cs') { throw 'LineFindOperatorParameters.cs is not compiled.' }

$lineParams = Get-Content -LiteralPath $lineParamsPath -Raw
if ($lineParams -notmatch 'LineDetectionMode') { throw 'LineFindOperatorParameters does not store detection mode.' }
if ($lineParams -notmatch 'DetectionMode = DetectionMode') { throw 'LineFindOperatorParameters does not clone detection mode.' }

'Operator parameter verification passed.'
