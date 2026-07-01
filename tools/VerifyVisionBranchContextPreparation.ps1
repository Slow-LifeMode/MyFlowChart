$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot

function Read-ProjectFile([string]$relativePath) {
    return Get-Content -LiteralPath (Join-Path $root $relativePath) -Raw -Encoding UTF8
}

$context = Read-ProjectFile 'Services\Vision\VisionRunContext.cs'
$keys = Read-ProjectFile 'Services\Vision\VisionDataKeys.cs'
$runner = Read-ProjectFile 'Services\Vision\VisionWorkflowRunner.cs'

if ($context -notmatch 'CreateBranchContext') { throw 'VisionRunContext must expose CreateBranchContext for future branch execution.' }
if ($context -notmatch 'private readonly bool _ownsResources') { throw 'VisionRunContext must distinguish root and branch ownership.' }
if ($context -notmatch 'new Dictionary<string, object>\(\)') { throw 'Branch contexts must have isolated Items dictionaries.' }
if ($context -notmatch 'Image = parent\.Image') { throw 'Branch contexts must share the image token.' }
if ($context -notmatch 'LineContext = parent\.LineContext') { throw 'Branch contexts must share the preprocessed image context.' }
if ($context -notmatch 'CopyInheritedItem\(parent,\s*VisionDataKeys\.CurrentImage\)') { throw 'Branch contexts must inherit the current image token.' }
if ($context -notmatch 'CopyInheritedItem\(parent,\s*VisionDataKeys\.LineDetectionRoi\)') { throw 'Branch contexts must inherit ROI context.' }
if ($context -notmatch 'if \(_ownsResources\)[\s\S]*LineContext\?\.Dispose\(\);[\s\S]*Image\?\.Dispose\(\);') { throw 'Only root contexts may release shared image resources.' }
if ($keys -notmatch 'LineDetectionRoi') { throw 'VisionDataKeys must define LineDetectionRoi.' }
if ($runner -notmatch 'VisionDataKeys\.LineDetectionRoi') { throw 'VisionWorkflowRunner must use the shared ROI key.' }

'Vision branch context preparation verification passed.'
