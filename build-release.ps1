param()

Clear-Host
$ErrorActionPreference = 'Stop'

# ----------- Config -----------
$projectPath    = Join-Path $PSScriptRoot 'JsonFormatter.csproj'
$config         = 'Release'
$outputDirBase  = Join-Path $PSScriptRoot 'publish'

# ----------- Helpers -----------
function Require-Gum {
    if (-not (Get-Command gum -ErrorAction SilentlyContinue)) {
        Write-Host "gum not found. Install one of the options below:" -ForegroundColor Yellow
        Write-Host "  1) winget install charmbracelet.gum" -ForegroundColor Cyan
        Write-Host "  2) go install github.com/charmbracelet/gum@latest" -ForegroundColor Cyan
        Write-Host "  3) choco install gum" -ForegroundColor Cyan
        exit 1
    }
}

function Require-Dotnet {
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
        throw 'DOTNET SDK NOT FOUND! INSTALL .NET SDK 8/10.'
    }
}

function Box {
    param([string]$Title, [string]$Body, [string]$Color = 'cyan')
    gum style --border rounded --border-foreground $Color --padding "1 2" --align left -- "$Title`n$Body"
}

function Step {
    param([string]$Label, [scriptblock]$Action)
    try {
        & $Action
        Write-Host "[OK]" -ForegroundColor Green -NoNewline
        Write-Host " $Label"
    } catch {
        Write-Host "[FAIL]" -ForegroundColor Red -NoNewline
        Write-Host " $Label"
        throw
    }
}

# ----------- Flow -----------
Require-Gum
Box -Title 'CREATE JSONFORMATTER PACKAGE' -Body 'Single-file, self-contained release build'

$runtime = & gum choose --header "Select target runtime" "win-x64" "linux-x64"
if (-not $runtime) { Write-Host 'No runtime selected. Exiting.'; exit 0 }

$defaultName = if ($runtime -like 'win-*') { 'JsonFormatter.exe' } else { 'JsonFormatter' }
$exeName = & gum input --prompt "Output file name: " --placeholder $defaultName
if (-not $exeName) { $exeName = $defaultName }
if ($runtime -like 'win-*' -and -not $exeName.ToLower().EndsWith('.exe')) { $exeName += '.exe' }

$outputDir = Join-Path $outputDirBase "$runtime-single"

$summaryBody = "Runtime    : $runtime"
$summaryBody += "`nFile name  : $exeName"
$summaryBody += "`nOutput dir : $outputDir"
Box -Title 'Build Summary' -Body $summaryBody

$proceed = (& gum confirm "Proceed to build?")
if ($proceed -eq 1) { Write-Host 'Cancelled by user.'; exit 0 }

try {
    Step -Label 'Check dotnet SDK' -Action { Require-Dotnet }

    if (-not (Test-Path $projectPath)) {
        throw "JsonFormatter.csproj not found at $projectPath"
    }
    Write-Host "[OK]" -ForegroundColor Green -NoNewline; Write-Host " Project file found!"

    Step -Label "Clean output directory" -Action {
        if (Test-Path $outputDir) { Remove-Item $outputDir -Recurse -Force | Out-Null }
    }

    $publishArgs = @(
        'publish', $projectPath,
        '-c', $config,
        '-r', $runtime,
        '/p:PublishSingleFile=true',
        '/p:SelfContained=true',
        '/p:PublishTrimmed=false',
        '/p:IncludeNativeLibrariesForSelfExtract=true',
        '--output', $outputDir
    )

    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    Step -Label "dotnet publish ($runtime)" -Action { dotnet @publishArgs }
    $sw.Stop()

    # rename default output to custom name if needed
    $defaultExe  = if ($runtime -like 'win-*') { 'JsonFormatter.exe' } else { 'JsonFormatter' }
    $defaultPath = Join-Path $outputDir $defaultExe
    $customPath  = Join-Path $outputDir $exeName

    if ($defaultPath -ne $customPath -and (Test-Path $defaultPath)) {
        Step -Label "Rename to $exeName" -Action { Rename-Item -Path $defaultPath -NewName $exeName -Force }
    }

    if (-not (Test-Path $customPath)) {
        throw "Build finished but file not found at $customPath"
    }
    $fullPath = (Resolve-Path $customPath).Path

    $elapsed = [math]::Round($sw.Elapsed.TotalSeconds, 1)
    $doneBody = "File     : $exeName"
    $doneBody += "`nLocation : $fullPath"
    $doneBody += "`nDuration : ${elapsed}s"
    Box -Title 'Build Completed!' -Body $doneBody -Color 'yellow'
}
catch {
    $errMsg = "$_"
    Box -Title 'Build Failed' -Body $errMsg -Color 'red'
    exit 1
}
