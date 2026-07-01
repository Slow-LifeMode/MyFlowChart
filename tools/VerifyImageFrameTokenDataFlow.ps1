$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot

function Read-ProjectFile([string]$relativePath) {
    return Get-Content -LiteralPath (Join-Path $root $relativePath) -Raw -Encoding UTF8
}

$project = Read-ProjectFile 'MyFlowChart.csproj'
$tokenPath = Join-Path $root 'Services\Vision\ImageFrameToken.cs'
$context = Read-ProjectFile 'Services\Vision\VisionRunContext.cs'
$imageInput = Read-ProjectFile 'Services\Vision\ImageInputOperatorExecutor.cs'
$lineFind = Read-ProjectFile 'Services\Vision\LineFindOperatorExecutor.cs'
$mainWindowCode = Read-ProjectFile 'MainWindow.xaml.cs'

if (-not (Test-Path -LiteralPath $tokenPath)) { throw 'ImageFrameToken.cs is missing.' }
if ($project -notmatch 'Services\\Vision\\ImageFrameToken\.cs') { throw 'ImageFrameToken.cs is not compiled.' }

$token = Get-Content -LiteralPath $tokenPath -Raw -Encoding UTF8
foreach ($member in @('FrameId', 'FrameNumber', 'NativePtr', 'Width', 'Height', 'Channels', 'IsReadOnly')) {
    if ($token -notmatch $member) { throw "ImageFrameToken must expose $member." }
}

if ($token -notmatch 'IntPtr') { throw 'ImageFrameToken must expose a native pointer.' }
if ($token -notmatch 'FromBorrowedMat') { throw 'ImageFrameToken must support borrowed Mat references.' }
if ($token -notmatch 'FromOwnedMat') { throw 'ImageFrameToken must support owned Mat references.' }
if ($token -notmatch 'Dispose\(') { throw 'ImageFrameToken must define lifecycle cleanup.' }
if ($token -match 'Clone\(' -or $token -match 'CopyTo\(') { throw 'ImageFrameToken must not copy the full image.' }

if ($context -notmatch 'ImageFrameToken') { throw 'VisionRunContext must store an ImageFrameToken.' }
if ($context -notmatch 'SetCurrentImage') { throw 'VisionRunContext must centralize current image updates.' }
if ($imageInput -notmatch 'SetCurrentImage\(token\)') { throw 'Image input must publish the image token through VisionRunContext.' }
if ($imageInput -match 'VisionOperatorResult\.Ok\([^,]+,\s*context\.Image\.Image') { throw 'Image input must not publish a raw Mat payload.' }
if ($lineFind -notmatch 'ImageFrameToken') { throw 'Line find must consume ImageFrameToken.' }
if ($lineFind -match 'as Mat') { throw 'Line find must not read CurrentImage as a raw Mat.' }
if ($mainWindowCode -notmatch 'record\.Payload is ImageFrameToken') { throw 'UI result cache must not keep image tokens.' }

'Image frame token data-flow verification passed.'
