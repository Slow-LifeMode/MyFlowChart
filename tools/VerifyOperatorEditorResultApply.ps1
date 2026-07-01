$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot

function Read-ProjectFile([string]$relativePath) {
    return Get-Content -LiteralPath (Join-Path $root $relativePath) -Raw -Encoding UTF8
}

$editor = Read-ProjectFile 'Views\VisionOperatorEditorWindow.xaml.cs'
$mainWindow = Read-ProjectFile 'MainWindow.xaml.cs'

if ($editor -notmatch 'EditedStatus') { throw 'Operator editor must expose the last run status.' }
if ($editor -notmatch 'EditedElapsedMilliseconds') { throw 'Operator editor must expose the last run elapsed time.' }
if ($editor -notmatch 'EditedMessage') { throw 'Operator editor must expose the last run message.' }
if ($editor -notmatch 'UpdateEditedRunResult') { throw 'Operator editor must update the result snapshot after run-once.' }

if ($mainWindow -notmatch 'editor\.EditedStatus' -or $mainWindow -notmatch 'editor\.EditedElapsedMilliseconds' -or $mainWindow -notmatch 'editor\.EditedMessage') {
    throw 'MainWindow must apply editor run-once status back to the FlowOperator.'
}

if ($mainWindow -notmatch 'RefreshVisionRunLogs\(viewModel\.SelectedNode\)') {
    throw 'MainWindow must refresh the run log after applying editor result.'
}

'Operator editor result apply verification passed.'
