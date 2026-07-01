$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot

function Read-ProjectFile([string]$relativePath) {
    return Get-Content -LiteralPath (Join-Path $root $relativePath) -Raw -Encoding UTF8
}

$project = Read-ProjectFile 'MyFlowChart.csproj'
$keys = Read-ProjectFile 'Services\Vision\VisionDataKeys.cs'
$imageInput = Read-ProjectFile 'Services\Vision\ImageInputOperatorExecutor.cs'
$lineFind = Read-ProjectFile 'Services\Vision\LineFindOperatorExecutor.cs'
$resultOutput = Read-ProjectFile 'Services\Vision\ResultOutputOperatorExecutor.cs'

if ($project -notmatch 'Services\\Vision\\VisionDataKeys\.cs') {
    throw 'VisionDataKeys.cs is not compiled.'
}

if ($keys -notmatch 'CurrentImage' -or $keys -notmatch 'LineDetectionResult') {
    throw 'Vision data keys must define CurrentImage and LineDetectionResult.'
}

if ($imageInput -notmatch 'SetCurrentImage\(token\)') {
    throw 'Image input must publish the current image into the run context.'
}

if ($lineFind -notmatch 'VisionDataKeys\.CurrentImage' -or $lineFind -notmatch 'TryGetValue') {
    throw 'Line find must read the current image from explicit upstream context data.'
}

if ($lineFind -notmatch 'VisionDataKeys\.LineDetectionResult' -or $resultOutput -notmatch 'VisionDataKeys\.LineDetectionResult') {
    throw 'Line detection result must use the shared data-flow key.'
}

foreach ($source in @($imageInput, $lineFind, $resultOutput)) {
    if ($source -match 'Clone\(' -or $source -match 'CopyTo\(') {
        throw 'Vision data flow must not copy the full image between operators.'
    }
}

'Vision data-flow verification passed.'
