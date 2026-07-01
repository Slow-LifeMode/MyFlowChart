$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot

function Read-ProjectFile([string]$relativePath) {
    return Get-Content -LiteralPath (Join-Path $root $relativePath) -Raw
}

$flowOperator = Read-ProjectFile 'Models\FlowOperator.cs'
$mainWindow = Read-ProjectFile 'MainWindow.xaml'
$mainWindowCode = Read-ProjectFile 'MainWindow.xaml.cs'

if ($flowOperator -notmatch 'LastMessage') { throw 'FlowOperator.LastMessage is missing.' }
if ($mainWindowCode -notmatch 'record\.Message') { throw 'Operator run message is not applied to FlowOperator.' }
if ($mainWindow -match 'Text="OK"') { throw 'Operator status pill is still static OK text.' }
if ($mainWindow -notmatch 'ElapsedMilliseconds') { throw 'Operator elapsed time is not shown in runtime panel.' }
if ($mainWindow -notmatch 'LastMessage') { throw 'Operator last message is not shown in runtime panel.' }
if ($mainWindow -notmatch 'FlowNodeStatus\.Running') { throw 'Operator status visual does not bind Running state.' }

'Operator runtime panel verification passed.'
