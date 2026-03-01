@echo off
setlocal
set SOLUTION=RDTracker.sln
echo Building %SOLUTION% (Release)...

REM Try msbuild on PATH



:: Check common VS install locations (x86 Program Files)
set MSBUILD_PATH=
if exist "%ProgramFiles(x86)%\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe" set MSBUILD_PATH="%ProgramFiles(x86)%\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe"
if exist "%ProgramFiles(x86)%\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" set MSBUILD_PATH="%ProgramFiles(x86)%\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
if exist "%ProgramFiles(x86)%\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin\MSBuild.exe" set MSBUILD_PATH="%ProgramFiles(x86)%\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin\MSBuild.exe"
if exist "%ProgramFiles(x86)%\MSBuild\14.0\Bin\MSBuild.exe" set MSBUILD_PATH="%ProgramFiles(x86)%\MSBuild\14.0\Bin\MSBuild.exe"

if defined MSBUILD_PATH (
    echo Using %MSBUILD_PATH%
    %MSBUILD_PATH% %SOLUTION% /p:Configuration=Release /verbosity:minimal
    if %errorlevel%==0 goto LocateExe
) else (
    echo MSBuild not found in common locations.
)

echo Trying dotnet build as fallback...
where dotnet >nul 2>&1
if %errorlevel%==0 (
    dotnet build %SOLUTION% -c Release
    if %errorlevel%==0 goto LocateExe
) else (
    echo dotnet not found. Cannot build here.
    goto End
)

:LocateExe
echo Locating built EXE...
set EXE_PATH=
for /f "delims=" %%f in ('dir /b /s RDTracker\bin\Release\*.exe 2^>nul') do (
    set EXE_PATH=%%f
    goto CopyExe
)
for /f "delims=" %%f in ('dir /b /s RDTracker\bin\Release\net*\*.exe 2^>nul') do (
    set EXE_PATH=%%f
    goto CopyExe
)

echo No EXE found in RDTracker\bin\Release.
goto End

:CopyExe
if not defined EXE_PATH (
    echo No EXE to copy.
    goto End
)
echo Found EXE: %EXE_PATH%
set DEST=%USERPROFILE%\Desktop
copy /Y "%EXE_PATH%" "%DEST%\"
if %errorlevel%==0 (
    echo Copied to %DEST%
) else (
    echo Failed to copy to %DEST%
)
goto End

:End
endlocal
pausewhere msbuild >nul 2>&1
if %errorlevel%==0 (
    echo Found msbuild on PATH
    msbuild %SOLUTION% /p:Configuration=Release /verbosity:minimal
    if %errorlevel%==0 goto LocateExe
) else (
    echo msbuild not on PATH, checking common locations...
)