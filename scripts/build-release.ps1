$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $repoRoot

Write-Host 'Publishing self-contained win-x64 application...'
if (Test-Path 'publish') {
    Remove-Item 'publish' -Recurse -Force
}

dotnet publish 'src/MarkdownDocumentConverter.App/MarkdownDocumentConverter.App.csproj' `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:WindowsAppSDKSelfContained=true `
    -o publish

$required = @(
    'publish/MarkdownDocumentConverter.App.exe',
    'publish/MarkdownDocumentConverter.App.pri',
    'publish/resources/engines/win-x64/pandoc.exe',
    'publish/resources/engines/win-x64/typst.exe',
    'publish/resources/docx/modern.docx',
    'publish/resources/typst/modern.typ'
)

foreach ($path in $required) {
    if (-not (Test-Path $path)) {
        throw "Publish output is missing: $path"
    }
}

$iscc = Get-Command iscc -ErrorAction SilentlyContinue
if ($null -eq $iscc) {
    $candidates = @(
        (Join-Path $env:LOCALAPPDATA 'Programs\Inno Setup 6\ISCC.exe'),
        'C:\Program Files (x86)\Inno Setup 6\ISCC.exe',
        'C:\Program Files\Inno Setup 6\ISCC.exe'
    )

    $isccPath = $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1

    if ($null -eq $isccPath) {
        throw 'Inno Setup 6 is required: winget install --id JRSoftware.InnoSetup'
    }
} else {
    $isccPath = $iscc.Source
}

Write-Host 'Compiling installer...'
& $isccPath 'installer/windows/setup.iss'

$installer = Get-ChildItem 'artifacts/installer/MarkdownDocumentConverter-Setup-*.exe' |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1

if ($null -eq $installer) {
    throw 'Installer output was not produced.'
}

$hash = Get-FileHash $installer.FullName -Algorithm SHA256
Write-Host "Installer: $($installer.FullName)"
Write-Host "SHA256:    $($hash.Hash)"
