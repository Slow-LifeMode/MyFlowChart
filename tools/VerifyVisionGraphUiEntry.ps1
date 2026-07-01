$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot

function Read-ProjectFile([string]$relativePath) {
    return Get-Content -LiteralPath (Join-Path $root $relativePath) -Raw -Encoding UTF8
}

$mainWindow = Read-ProjectFile 'MainWindow.xaml.cs'

if ($mainWindow -notmatch 'RunVisionGraphAsync') { throw 'MainWindow must expose a graph run UI entry.' }
if ($mainWindow -notmatch 'btnStart_Click[\s\S]*RunVisionGraphAsync\(\)') { throw 'Start button must run the full vision graph.' }
if ($mainWindow -notmatch 'RunGraphAsync\(viewModel\.Nodes,\s*_imageViewer\.ImageMat,\s*roi\)') { throw 'Graph UI entry must pass the current node collection into VisionWorkflowRunner.' }
if ($mainWindow -match 'RunVisionGraphAsync[\s\S]*RunNodeAsync') { throw 'Graph UI entry must not fall back to single-node execution.' }
if ($mainWindow -notmatch 'ApplyVisionOperatorResults\(viewModel\.Nodes,\s*_visionRunner\.OperatorResults\)') { throw 'Graph UI entry must apply operator results across the node collection.' }

'Vision graph UI entry verification passed.'
