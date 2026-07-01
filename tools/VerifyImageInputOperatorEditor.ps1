$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot

function Read-ProjectFile([string]$relativePath) {
    return Get-Content -LiteralPath (Join-Path $root $relativePath) -Raw -Encoding UTF8
}

$operatorDefinition = Read-ProjectFile 'Models\OperatorDefinition.cs'
$flowOperator = Read-ProjectFile 'Models\FlowOperator.cs'
$executor = Read-ProjectFile 'Services\Vision\ImageInputOperatorExecutor.cs'
$mainWindow = Read-ProjectFile 'MainWindow.xaml.cs'
$project = Read-ProjectFile 'MyFlowChart.csproj'

foreach ($path in @(
    'Models\ImageInputOperatorParameters.cs',
    'ViewModels\ImageInputOperatorEditorViewModel.cs',
    'Views\ImageInputOperatorEditorWindow.xaml',
    'Views\ImageInputOperatorEditorWindow.xaml.cs'
)) {
    if (!(Test-Path -LiteralPath (Join-Path $root $path))) {
        throw "Missing image input operator file: $path"
    }

    if ($project -notmatch [regex]::Escape($path)) {
        throw "Project does not include image input operator file: $path"
    }
}

$parameters = Read-ProjectFile 'Models\ImageInputOperatorParameters.cs'
$viewModel = Read-ProjectFile 'ViewModels\ImageInputOperatorEditorViewModel.cs'
$windowXaml = Read-ProjectFile 'Views\ImageInputOperatorEditorWindow.xaml'
$windowCode = Read-ProjectFile 'Views\ImageInputOperatorEditorWindow.xaml.cs'

if ($operatorDefinition -notmatch 'ImageInput\s*,') { throw 'ImageInput editor kind is missing.' }
if ($operatorDefinition -notmatch 'Name = ImageInputName[\s\S]*EditorKind = OperatorEditorKind\.ImageInput') {
    throw 'Image input operator must open an editor window.'
}
if ($operatorDefinition -notmatch 'CreateDefaultParameters\(\s*string operatorName\s*\)[\s\S]*ImageInputOperatorParameters') {
    throw 'Image input operator must create default parameters.'
}
if ($flowOperator -notmatch 'ImageInputOperatorParameters') { throw 'FlowOperator must clone image input parameters.' }

if ($parameters -notmatch 'ImagePath') { throw 'Image input parameters must store ImagePath.' }
if ($parameters -notmatch 'Clone\(') { throw 'Image input parameters must be cloneable.' }

if ($executor -notmatch 'ImageInputOperatorParameters') { throw 'Image input executor must read image input parameters.' }
if ($executor -notmatch 'Cv2\.ImRead') { throw 'Image input executor must load image by path.' }
if ($executor -notmatch 'ImageFrameToken\.FromOwnedMat') { throw 'Image input executor must publish an owned image token.' }
if ($executor -notmatch 'SetCurrentImage\(token\)') { throw 'Image input executor must publish CurrentImage through VisionRunContext.' }

if ($windowXaml -notmatch 'WindowsFormsHost') { throw 'Image input editor must host OpenCvImageViewer.' }
if ($windowXaml -notmatch 'ImagePath') { throw 'Image input editor must bind ImagePath.' }
if ($windowXaml -notmatch 'SelectImage_Click') { throw 'Image input editor must expose image selection.' }
if ($viewModel -notmatch 'ImageInputOperatorParameters') { throw 'Image input editor view model must own parameters.' }
if ($windowCode -notmatch 'OpenCvImageViewer') { throw 'Image input editor must create the viewer.' }
if ($windowCode -notmatch 'OpenFileDialog') { throw 'Image input editor must open an image file dialog.' }
if ($windowCode -notmatch 'EditedParameters') { throw 'Image input editor must return edited parameters.' }

if ($mainWindow -notmatch 'OperatorEditorKind\.ImageInput') { throw 'MainWindow must dispatch image input editor.' }
if ($mainWindow -notmatch 'OpenImageInputOperatorEditor') { throw 'MainWindow image input editor method is missing.' }

'Image input operator editor verification passed.'
