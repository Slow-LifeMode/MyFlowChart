$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot

function Read-ProjectFile([string]$relativePath) {
    return Get-Content -LiteralPath (Join-Path $root $relativePath) -Raw
}

$logItemPath = Join-Path $root 'ViewModels\VisionRunLogItem.cs'
$project = Read-ProjectFile 'MyFlowChart.csproj'
$viewModel = Read-ProjectFile 'ViewModels\MainWindowViewModel.cs'
$mainWindow = Read-ProjectFile 'MainWindow.xaml'
$mainWindowCode = Read-ProjectFile 'MainWindow.xaml.cs'

if (-not (Test-Path -LiteralPath $logItemPath)) { throw 'VisionRunLogItem.cs is missing.' }
if ($project -notmatch 'ViewModels\\VisionRunLogItem\.cs') { throw 'VisionRunLogItem.cs is not compiled.' }
if ($viewModel -notmatch 'ObservableCollection<VisionRunLogItem>') { throw 'VisionRunLogs collection is missing from MainWindowViewModel.' }
if ($viewModel -notmatch 'RefreshVisionRunLogs') { throw 'MainWindowViewModel does not refresh run logs.' }
if ($mainWindow -notmatch 'VisionRunLogs') { throw 'XAML does not bind the run log list.' }
if ($mainWindow -match '<Binding Path="Message" />') { throw 'Run log panel must not show operator message details.' }
if ($mainWindow -notmatch '运行日志') { throw 'Run log panel title is missing.' }
if ($mainWindowCode -notmatch 'RefreshVisionRunLogs\(selectedNode\)') { throw 'MainWindow code does not refresh logs after run state changes.' }

'Vision run log panel verification passed.'
