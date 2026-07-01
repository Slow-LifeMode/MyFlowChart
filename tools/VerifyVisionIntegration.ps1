$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$hashes = @{
    'OpenCvWindowTool\LineDetectionOperator.cs' = '8E5448262EA475B9F74B728F17FD51F922F0004EDDF0A258B4C5F0698B221C4F'
    'OpenCvWindowTool\OptLineDetectionOperator.cs' = 'BD513931A316B39A2EBF0F5A7808BE6603931DD9ED0721335E496264E71B4981'
    'OpenCvWindowTool\LineDetectionModels.cs' = 'C8A1C431FA533C8E981ACE37596146CF618E3221C92BFFCF99B56FEDAE95C0FD'
    'OpenCvWindowTool\LineDetectionImageContext.cs' = '8D182DBF636938B01F30C119679E98641956A4E15711ED8E6289941007EAB4DB'
    'OpenCvWindowTool\RoiItem.cs' = 'A211A0A7D79A09444F8B961B94B1BE434862FF4CCDCEFB9B32455A12189E30F0'
}

foreach ($entry in $hashes.GetEnumerator()) {
    $path = Join-Path $root $entry.Key
    if (!(Test-Path -LiteralPath $path)) {
        throw "Missing core file: $path"
    }

    $actual = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash
    if ($actual -ne $entry.Value) {
        throw "Hash mismatch for $($entry.Key). Expected $($entry.Value), got $actual"
    }
}

$msbuild = 'C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe'
$solution = Join-Path $root 'MyFlowChart.sln'
$outDir = Join-Path $root 'obj\CodexVerify\'
$buildArgs = @(
    $solution,
    '/restore',
    '/t:Build',
    '/p:Configuration=Debug',
    '/p:Platform=Any CPU',
    "/p:OutDir=$outDir",
    '/m',
    '/v:minimal'
)

& $msbuild @buildArgs
if ($LASTEXITCODE -ne 0) {
    throw "MSBuild failed with exit code $LASTEXITCODE"
}

$exe = Join-Path $outDir 'MyFlowChart.exe'
$process = Start-Process -FilePath $exe -PassThru -WindowStyle Hidden
try {
    Start-Sleep -Seconds 4
    if ($process.HasExited) {
        throw "MyFlowChart exited during startup. ExitCode=$($process.ExitCode)"
    }
}
finally {
    if (!$process.HasExited) {
        Stop-Process -Id $process.Id -Force
    }
}

Write-Host 'Vision integration verification passed.'
