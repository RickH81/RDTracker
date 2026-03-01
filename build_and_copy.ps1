Param(
    [string]$Solution = "RDTracker.sln"
)
Write-Host "Building $Solution (Release)..."

# Try msbuild
$msbuild = Get-Command msbuild -ErrorAction SilentlyContinue
if ($msbuild) {
    Write-Host "Using msbuild from: $($msbuild.Path)"
    & $msbuild.Path $Solution /p:Configuration=Release /verbosity:minimal
    if ($LASTEXITCODE -eq 0) { goto Locate }
}

# Try common MSBuild locations via registry / VS2022 path
$possible = @(
    "$Env:ProgramFiles(x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe",
    "$Env:ProgramFiles(x86)\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe",
    "$Env:ProgramFiles(x86)\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin\MSBuild.exe",
    "$Env:ProgramFiles(x86)\MSBuild\14.0\Bin\MSBuild.exe"
)
foreach ($p in $possible) {
    if (Test-Path $p) {
        Write-Host "Using MSBuild at $p"
        & $p $Solution /p:Configuration=Release /verbosity:minimal
        if ($LASTEXITCODE -eq 0) { goto Locate }
    }
}

Write-Host "msbuild not found; trying dotnet build..."
$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if ($dotnet) {
    & $dotnet.Path build $Solution -c Release
    if ($LASTEXITCODE -eq 0) { goto Locate }
}

Write-Error "No suitable build tool found on PATH. Install Visual Studio Build Tools or the .NET SDK."
exit 1

:Locate
Write-Host "Locating built EXE..."
$exe = Get-ChildItem -Path .\RDTracker\bin -Filter *.exe -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $exe) {
    Write-Error "No EXE found under RDTracker\\bin\\Release. Build might have failed."
    exit 1
}
Write-Host "Found: $($exe.FullName)"
$dest = Join-Path $Env:USERPROFILE "Desktop"
Copy-Item -Path $exe.FullName -Destination $dest -Force
if ($?) { Write-Host "Copied to $dest" } else { Write-Error "Copy failed."; exit 1 }
