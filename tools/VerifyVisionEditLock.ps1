$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot

function Read-ProjectFile([string]$relativePath) {
    return Get-Content -LiteralPath (Join-Path $root $relativePath) -Raw -Encoding UTF8
}

$viewModel = Read-ProjectFile 'ViewModels\MainWindowViewModel.cs'
$mainWindow = Read-ProjectFile 'MainWindow.xaml'
$mainWindowCode = Read-ProjectFile 'MainWindow.xaml.cs'
$flowChart = Read-ProjectFile 'Controls\FlowChartControl.xaml.cs'

if ($viewModel -notmatch 'CanEditVisionWorkflow') {
    throw 'MainWindowViewModel must expose CanEditVisionWorkflow.'
}

if ($viewModel -notmatch 'OnPropertyChanged\("CanEditVisionWorkflow"\)' -or $viewModel -notmatch 'CommandManager\.InvalidateRequerySuggested\(\)') {
    throw 'Running state changes must refresh edit bindings and commands.'
}

if ($viewModel -notmatch 'CanEditBranches\(object parameter\)' -or $viewModel -notmatch 'return CanEditVisionWorkflow && SelectedNode != null && SelectedNode\.CanConfigureBranches;') {
    throw 'Branch edit commands must be disabled while the vision workflow is running.'
}

if ($mainWindow -notmatch 'CanEdit="\{Binding CanEditVisionWorkflow\}"') {
    throw 'FlowChartControl must bind CanEdit to the vision edit lock.'
}

if ($mainWindow -notmatch 'IsEnabled="\{Binding CanEditVisionWorkflow\}"') {
    throw 'Editor surfaces must be disabled while the vision workflow is running.'
}

if ($mainWindowCode -notmatch 'CanEditVisionWorkflow' -or $mainWindowCode -notmatch 'OperatorList_MouseDoubleClick') {
    throw 'Operator editor entry must check the vision edit lock.'
}

if ($flowChart -notmatch 'CanEditProperty' -or $flowChart -notmatch '!CanEdit') {
    throw 'FlowChartControl must expose and enforce CanEdit.'
}

'Vision edit-lock verification passed.'
