$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot

function Read-ProjectFile([string]$relativePath) {
    return Get-Content -LiteralPath (Join-Path $root $relativePath) -Raw -Encoding UTF8
}

$template = Read-ProjectFile 'Controls\FlowChartControl.xaml'
$renderer = Read-ProjectFile 'Controls\FlowChartControl.xaml.cs'

if ($template -match 'Text="\{Binding StatusText\}"') { throw 'Flow block template must not render status text.' }
if ($template -notmatch 'Value="\{x:Static models:FlowNodeStatus.OK\}"[\s\S]*Value="#38D99A"') { throw 'Flow block OK state must render through green visual state.' }
if ($template -notmatch 'Value="\{x:Static models:FlowNodeStatus.NG\}"[\s\S]*Value="#E65757"') { throw 'Flow block NG state must render through red visual state.' }
if ($template -match 'Value="\{x:Static models:FlowNodeStatus.Running\}"') { throw 'Flow block template must not render a running color state.' }
if ($template -match 'Value="\{x:Static models:FlowNodeStatus.Stopped\}"') { throw 'Flow block template must not render a stopped color state.' }
if ($renderer -notmatch 'GetNodeStatusAccentBrush\(node\)') { throw 'Dynamic flow block renderer must use node status accent color.' }
if ($renderer -match 'StatusText') { throw 'Dynamic flow block renderer must not render status text.' }
if ($renderer -match 'case FlowNodeStatus\.Running:[\s\S]*Color\.FromRgb\(61,\s*125,\s*255\)') { throw 'Dynamic flow block renderer must not render running blue.' }
if ($renderer -match 'case FlowNodeStatus\.Stopped:[\s\S]*Color\.FromRgb\(232,\s*164,\s*56\)') { throw 'Dynamic flow block renderer must not render stopped yellow.' }
if ($renderer -match 'node\.Status == FlowNodeStatus\.Running[\s\S]*CreateRunningOutline') { throw 'Flow block renderer must not draw a running outline.' }

$mainWindow = Read-ProjectFile 'MainWindow.xaml.cs'
if ($mainWindow -match 'SetVisionNodeStatus\(selectedNode,\s*FlowNodeStatus\.Running') { throw 'MainWindow must not paint flow blocks as running.' }
if ($mainWindow -notmatch 'ApplyOperatorBlockStatus\(node\)') { throw 'MainWindow must aggregate operator block status after operator results.' }

'Flow block status color-only verification passed.'
