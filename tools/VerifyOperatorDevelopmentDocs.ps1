$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$docPath = Join-Path $root 'docs\OPERATOR_DEVELOPMENT.md'
$summaryPath = Join-Path $root 'docs\PROJECT_SUMMARY.md'

if (!(Test-Path -LiteralPath $docPath)) { throw 'Operator development document is missing.' }

$doc = Get-Content -LiteralPath $docPath -Raw
$summary = Get-Content -LiteralPath $summaryPath -Raw

foreach ($pattern in @(
    'OperatorDefinition',
    'OperatorRuntimeKind',
    'OperatorEditorKind',
    'OperatorCatalog',
    'IVisionOperatorExecutor',
    'VisionOperatorResult',
    'ImageInputOperatorEditorWindow',
    'VerifyImageInputOperatorEditor\.ps1',
    'MyFlowChart\.csproj',
    'MSBuild',
    'MEF'
)) {
    if ($doc -notmatch $pattern) {
        throw "Operator development document missing required topic: $pattern"
    }
}

if ($summary -notmatch 'OPERATOR_DEVELOPMENT\.md') {
    throw 'Project summary must link the operator development document.'
}

'Operator development document verification passed.'
