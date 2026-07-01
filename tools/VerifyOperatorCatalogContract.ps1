$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot

function Read-ProjectFile([string]$relativePath) {
    return Get-Content -LiteralPath (Join-Path $root $relativePath) -Raw
}

$operatorDefinition = Read-ProjectFile 'Models\OperatorDefinition.cs'
$catalog = Read-ProjectFile 'Services\Vision\OperatorCatalog.cs'
$mainWindowCode = Read-ProjectFile 'MainWindow.xaml.cs'
$palette = Read-ProjectFile 'Controls\OperatorPaletteControl.xaml.cs'

if ($operatorDefinition -notmatch 'OperatorRuntimeKind') { throw 'Operator runtime kind contract is missing.' }
if ($operatorDefinition -notmatch 'OperatorEditorKind') { throw 'Operator editor kind contract is missing.' }
if ($operatorDefinition -notmatch 'CreateKnownOperators') { throw 'Known operator contract is missing.' }
if ($operatorDefinition -notmatch 'IsVisible') { throw 'Operator visibility contract is missing.' }
if ($operatorDefinition -notmatch 'GetRuntimeKind') { throw 'Operator runtime lookup is missing.' }
if ($operatorDefinition -notmatch 'GetEditorKind') { throw 'Operator editor lookup is missing.' }
if ($operatorDefinition -notmatch 'CreateDefaultOperators\(\)[\s\S]*IsVisible') { throw 'Default palette operators must come from visible known operators.' }
if ($operatorDefinition -notmatch 'CreateDefaultParameters\(\s*string operatorName\s*\)[\s\S]*GetRuntimeKind') { throw 'Default parameter factory must use the runtime contract.' }

if ($catalog -notmatch 'CreateKnownOperators') { throw 'OperatorCatalog must register from the known operator contract.' }
if ($catalog -notmatch 'RuntimeKind') { throw 'OperatorCatalog must choose executors by runtime kind.' }
if ($catalog -notmatch 'OperatorRuntimeKind\.LineFind') { throw 'LineFind runtime registration is missing.' }
if ($catalog -notmatch 'OperatorRuntimeKind\.ImageInput') { throw 'ImageInput runtime registration is missing.' }
if ($catalog -notmatch 'OperatorRuntimeKind\.ResultOutput') { throw 'ResultOutput runtime registration is missing.' }

if ($mainWindowCode -notmatch 'GetEditorKind') { throw 'MainWindow must choose editor by operator editor kind.' }
if ($mainWindowCode -notmatch 'OpenOperatorEditor') { throw 'MainWindow editor dispatch method is missing.' }
if ($mainWindowCode -notmatch 'OperatorEditorKind\.LineFind') { throw 'LineFind editor dispatch is missing.' }
if ($palette -notmatch 'CreateDefaultOperators') { throw 'Operator palette must still use default visible operators.' }

'Operator catalog contract verification passed.'
