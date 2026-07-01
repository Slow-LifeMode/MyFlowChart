$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot

function Read-ProjectFile([string]$relativePath) {
    return Get-Content -LiteralPath (Join-Path $root $relativePath) -Raw -Encoding UTF8
}

$runner = Read-ProjectFile 'Services\Vision\VisionWorkflowRunner.cs'
$imageInput = Read-ProjectFile 'Services\Vision\ImageInputOperatorExecutor.cs'

if ($runner -notmatch 'private int _isRunning') { throw 'VisionWorkflowRunner must keep a single-run gate.' }
if ($runner -notmatch 'Interlocked\.Exchange\(ref _isRunning,\s*1\)') { throw 'RunNodeAsync must atomically enter the single-run gate.' }
if ($runner -notmatch 'Task\.FromResult\(VisionOperatorResult\.Fail') { throw 'Concurrent RunNodeAsync calls must return a running result.' }
if ($runner -notmatch 'RunNodeExclusiveAsync') { throw 'Exclusive runner method is missing.' }
if ($runner -notmatch 'ConfigureAwait\(false\)') { throw 'Background execution should not capture the UI context.' }
if ($runner -notmatch '_context\?\.Dispose\(\);\s*_context = null;') { throw 'VisionRunContext must be released after each run.' }
if ($runner -notmatch 'Interlocked\.Exchange\(ref _isRunning,\s*0\)') { throw 'Runner must release the single-run gate in finally.' }
if ($runner -notmatch 'Task\.Run') { throw 'VisionWorkflowRunner must still execute workflow work on a background task.' }
if ($imageInput -match 'VisionOperatorResult\.Ok\([^;]+context\.Image') { throw 'Image input result must not keep the image token as payload.' }

'Vision single-run gate verification passed.'
