$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot

$checks = @(
    @{ Path = 'Views\VisionOperatorEditorWindow.xaml'; Pattern = 'WindowsFormsHost'; Name = 'editor window hosts viewer' },
    @{ Path = 'Views\VisionOperatorEditorWindow.xaml'; Pattern = 'Parameters.DetectionMode'; Name = 'editor binds detection mode' },
    @{ Path = 'Views\VisionOperatorEditorWindow.xaml.cs'; Pattern = 'ViewModel.Parameters.DetectionMode'; Name = 'editor uses selected detection mode' },
    @{ Path = 'ViewModels\LineFindOperatorEditorViewModel.cs'; Pattern = 'OperatorEditorModule'; Name = 'editor has module navigation' },
    @{ Path = 'MainWindow.xaml'; Pattern = 'MouseDoubleClick="OperatorList_MouseDoubleClick"'; Name = 'operator list double click wired' },
    @{ Path = 'MainWindow.xaml.cs'; Pattern = 'OpenLineFindOperatorEditor'; Name = 'main window opens line editor' },
    @{ Path = 'Models\LineFindOperatorParameters.cs'; Pattern = 'SaveRoi'; Name = 'line parameters save roi' },
    @{ Path = 'Models\LineFindOperatorParameters.cs'; Pattern = 'CreateRoi'; Name = 'line parameters restore roi' },
    @{ Path = 'Models\LineFindOperatorParameters.cs'; Pattern = 'DetectionMode'; Name = 'line parameters store detection mode' },
    @{ Path = 'Services\Vision\LineFindOperatorExecutor.cs'; Pattern = 'parameters.CreateRoi'; Name = 'executor prefers operator roi' },
    @{ Path = 'Services\Vision\LineFindOperatorExecutor.cs'; Pattern = 'ResolveDetectionMode'; Name = 'executor resolves detection mode' },
    @{ Path = 'MyFlowChart.csproj'; Pattern = 'Views\VisionOperatorEditorWindow.xaml'; Name = 'project includes editor xaml' },
    @{ Path = 'MyFlowChart.csproj'; Pattern = 'ViewModels\LineFindOperatorEditorViewModel.cs'; Name = 'project includes editor viewmodel' }
)

foreach ($check in $checks) {
    $path = Join-Path $root $check.Path
    if (-not (Test-Path $path)) {
        throw "Missing file: $($check.Path)"
    }

    $content = Get-Content -Raw $path
    if ($content -notlike "*$($check.Pattern)*") {
        throw "Missing pattern [$($check.Pattern)] in $($check.Path)"
    }

    Write-Host "OK - $($check.Name)"
}

$editorViewModel = Get-Content -LiteralPath (Join-Path $root 'ViewModels\LineFindOperatorEditorViewModel.cs') -Raw -Encoding UTF8
if ($editorViewModel -notmatch 'new ComboOption<LineDetectionMode>\("SelfMode", LineDetectionMode\.SelfMode\)') {
    throw 'Editor detection mode text must match OpencvMaster SelfMode option.'
}

if ($editorViewModel -notmatch 'new ComboOption<LineDetectionMode>\("OPTMode", LineDetectionMode\.OPTMode\)') {
    throw 'Editor detection mode text must match OpencvMaster OPTMode option.'
}

if ($editorViewModel -match 'Ransac拟合') {
    throw 'Editor fit mode options must match OpencvMaster visible options.'
}

if ($editorViewModel -match 'StrengthTypes') {
    throw 'Editor must not expose edge strength options that OpencvMaster demo does not show.'
}

$editorCode = Get-Content -LiteralPath (Join-Path $root 'Views\VisionOperatorEditorWindow.xaml.cs') -Raw -Encoding UTF8
if ($editorCode -match 'CreateDefaultRoi' -or $editorCode -match 'CloneRoi') {
    throw 'Operator editor must not auto-create or inherit ROI.'
}

$mainWindowCode = Get-Content -LiteralPath (Join-Path $root 'MainWindow.xaml.cs') -Raw -Encoding UTF8
if ($mainWindowCode -match '请先导入图像') {
    throw 'Operator editor must open even when no image is loaded.'
}

if ($editorCode -notmatch 'ResultStatus = ".*NG.*ROI') {
    throw 'Operator editor run-once must report NG when ROI is missing.'
}

Write-Host 'Vision operator editor verification passed.'
