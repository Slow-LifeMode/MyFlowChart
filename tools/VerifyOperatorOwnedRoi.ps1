$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot

function Read-ProjectFile([string]$relativePath) {
    return Get-Content -LiteralPath (Join-Path $root $relativePath) -Raw -Encoding UTF8
}

$lineParams = Read-ProjectFile 'Models\LineFindOperatorParameters.cs'
$workItem = Read-ProjectFile 'Services\Vision\VisionOperatorWorkItem.cs'
$executor = Read-ProjectFile 'Services\Vision\LineFindOperatorExecutor.cs'
$editor = Read-ProjectFile 'Views\VisionOperatorEditorWindow.xaml.cs'

if ($lineParams -notmatch 'HasRoi = false') { throw 'LineFindOperatorParameters must support empty ROI state.' }
if ($lineParams -notmatch 'HasRoi = HasRoi') { throw 'LineFindOperatorParameters.Clone must copy ROI state.' }
if ($lineParams -notmatch 'RoiCenterX = RoiCenterX' -or $lineParams -notmatch 'RoiAngle = RoiAngle') {
    throw 'LineFindOperatorParameters.Clone must copy ROI geometry.'
}

if ($workItem -notmatch 'flowOperator\.CloneParameters\(\)') {
    throw 'VisionOperatorWorkItem must snapshot each operator parameters independently.'
}

if ($editor -notmatch 'ViewModel\.Parameters\.SaveRoi\(GetCurrentLineRoi\(\)\)' -or $editor -notmatch 'EditedParameters = ViewModel\.Parameters\.Clone\(\)') {
    throw 'Operator editor must save ROI into the edited operator parameters only.'
}

if ($executor -match 'LineDetectionRoi' -or $executor -match 'FirstOrDefault') {
    throw 'LineFindOperatorExecutor must not fall back to main viewer ROI; missing operator ROI should run NG.'
}

if ($executor -match 'context\.Items.*Roi' -or $executor -match 'TryGetValue\(.*Roi') {
    throw 'LineFindOperatorExecutor must not fall back to main viewer ROI; missing operator ROI should run NG.'
}

if ($executor -notmatch 'parameters\.CreateRoi\(\)') {
    throw 'LineFindOperatorExecutor must use the current operator saved ROI.'
}

'Operator-owned ROI verification passed.'
