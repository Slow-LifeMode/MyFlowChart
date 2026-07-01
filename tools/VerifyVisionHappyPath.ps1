$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot

function Read-ProjectFile([string]$relativePath) {
    return Get-Content -LiteralPath (Join-Path $root $relativePath) -Raw
}

$operatorDefinition = Read-ProjectFile 'Models\OperatorDefinition.cs'
$operatorCatalog = Read-ProjectFile 'Services\Vision\OperatorCatalog.cs'
$viewModel = Read-ProjectFile 'ViewModels\MainWindowViewModel.cs'
$mainWindowCode = Read-ProjectFile 'MainWindow.xaml.cs'

if ($operatorDefinition -notmatch 'LineFindName') { throw 'LineFindName operator constant is missing.' }
if ($operatorDefinition -notmatch 'Name = LineFindName[\s\S]*IsVisible = true') { throw 'Default operator palette does not expose line find.' }
if ($operatorCatalog -notmatch 'OperatorRuntimeKind\.LineFind') { throw 'OperatorCatalog does not register line find.' }
if ($viewModel -notmatch 'EnsureDefaultVisionWorkflow') { throw 'ViewModel does not create the default vision workflow.' }
if ($viewModel -notmatch 'OperatorDefinition\.ImageInputName' -or $viewModel -notmatch 'OperatorDefinition\.LineFindName' -or $viewModel -notmatch 'OperatorDefinition\.ResultOutputName') {
    throw 'Default vision workflow does not contain image input, line find, and result output.'
}
if ($mainWindowCode -match 'EnsureDefaultLineDetectionRoi') { throw 'MainWindow must not auto-create a default line detection ROI.' }
if ($mainWindowCode -match 'flowChart\.RunAsync\(\)') { throw 'Start button still runs the old flowchart simulation path.' }
if ($mainWindowCode -notmatch 'EnsureDefaultVisionWorkflow\(\)') { throw 'Start path does not resolve the default vision workflow.' }
if ($mainWindowCode -notmatch 'ResolveLineDetectionRoi\(\)') { throw 'Start path does not pass the current ROI when available.' }

'Vision happy path verification passed.'
