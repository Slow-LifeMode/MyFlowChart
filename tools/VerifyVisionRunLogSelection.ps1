$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot

function Read-ProjectFile([string]$relativePath) {
    return Get-Content -LiteralPath (Join-Path $root $relativePath) -Raw
}

$logItem = Read-ProjectFile 'ViewModels\VisionRunLogItem.cs'
$viewModel = Read-ProjectFile 'ViewModels\MainWindowViewModel.cs'
$flowOperator = Read-ProjectFile 'Models\FlowOperator.cs'
$mainWindow = Read-ProjectFile 'MainWindow.xaml'
$mainWindowCode = Read-ProjectFile 'MainWindow.xaml.cs'

if ($logItem -notmatch 'public object Payload') { throw 'VisionRunLogItem must expose the operator result payload.' }
if ($logItem -notmatch 'VisionRunLogItem\([^)]*object payload') { throw 'VisionRunLogItem constructor must receive the payload.' }
if ($viewModel -notmatch 'SelectedVisionRunLog') { throw 'MainWindowViewModel must expose SelectedVisionRunLog.' }
if ($viewModel -notmatch 'FormatVisionRunLogResult') { throw 'MainWindowViewModel must format the selected log result.' }
if ($viewModel -notmatch 'LineDetectionResult') { throw 'Selected log formatter must support LineDetectionResult.' }
if ($viewModel -notmatch 'flowOperator\.Payload') { throw 'Run log refresh must read FlowOperator.Payload.' }
if ($flowOperator -notmatch 'public object Payload') { throw 'FlowOperator must keep the last operator payload.' }
if ($mainWindow -notmatch 'SelectedItem="\{Binding SelectedVisionRunLog, Mode=TwoWay\}"') { throw 'Run log list must bind SelectedItem to SelectedVisionRunLog.' }
if ($mainWindowCode -notmatch 'flowOperator\.Payload = record\.Payload') { throw 'MainWindow must write runner payload back to FlowOperator.' }
if ($mainWindow -match '<Binding Path="Message" />') { throw 'Run log list itself must still avoid showing message details.' }

'Vision run log selection verification passed.'
