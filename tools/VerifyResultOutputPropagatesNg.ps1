$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot

function Read-ProjectFile([string]$relativePath) {
    return Get-Content -LiteralPath (Join-Path $root $relativePath) -Raw -Encoding UTF8
}

$resultOutput = Read-ProjectFile 'Services\Vision\ResultOutputOperatorExecutor.cs'

if ($resultOutput -notmatch 'LineDetectionResult') {
    throw 'Result output must inspect line detection result payloads.'
}

if ($resultOutput -notmatch 'lineResult\.Success') {
    throw 'Result output must propagate failed line detection results as NG.'
}

if ($resultOutput -notmatch 'VisionOperatorResult\.Fail\(lineResult\.Message\)') {
    throw 'Result output must return the original failed line detection message.'
}

'Result output NG propagation verification passed.'
