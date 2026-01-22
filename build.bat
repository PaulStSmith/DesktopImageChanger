@echo off
setlocal EnableDelayedExpansion

SET LF=^


:: The above two blank lines are required for the LF variable to work correctly

echo ===============================================
echo     WorldMapWallpaper Build and Package
echo ===============================================
echo.

REM Check for Release parameter
set "cfg=%~1"
if "%cfg%"=="" set cfg=Debug
if /i "%cfg%"=="Release" (
    echo RELEASE MODE: Will create GitHub release after build
    echo.
    
    REM Check if git is available
    where git >nul 2>nul
    if !ERRORLEVEL! neq 0 (
        echo ERROR: Git not found in PATH. Git is required for release mode.
        exit /b 1
    )
    
    REM Check for clean working tree
    echo Checking git working tree status...
    git status --porcelain > nul 2>&1
    if !ERRORLEVEL! neq 0 (
        echo ERROR: Not in a git repository or git command failed.
        exit /b 1
    )
    
    for /f %%i in ('git status --porcelain') do (
        echo ERROR: Working tree is not clean. Please commit or stash changes before release.
        echo.
        echo Uncommitted changes detected:
        git status --short
        echo.
        echo Please run 'git status' to see all changes and commit them before creating a release.
        exit /b 1
    )
    
    echo Git working tree is clean. Proceeding with release build...
    echo.
)

REM Get the current directory
set "PROJECT_DIR=%~dp0"
cd /d "%PROJECT_DIR%"

REM Find MSBuild once at the start (used by both compile and publish)
echo Searching for MSBuild...
set "MSBUILD_PATH="
set "VSDEVCMD="
:: '18' is the path for VS2026, '2022' for VS2022
:: Why Microsoft changed the naming convention is beyond me...
for %%a in (18 2022) do (
    for %%e in (Enterprise Professional Community) do (
        if exist "C:\Program Files\Microsoft Visual Studio\%%a\%%e\MSBuild\Current\Bin\MSBuild.exe" (
            set "VSDEVCMD=C:\Program Files\Microsoft Visual Studio\%%a\%%e\Common7\Tools\VsDevCmd.bat"
            set "MSBUILD_PATH=C:\Program Files\Microsoft Visual Studio\%%a\%%e\MSBuild\Current\Bin\MSBuild.exe"
            goto :found_msbuild
        )
    )
)
echo ERROR: Could not find a compatible Visual Studio installation with MSBuild.
echo Please install Visual Studio 2022 or later with .NET desktop development workload.
exit /b 1

:found_msbuild
echo Found MSBuild: !MSBUILD_PATH!

REM Set up VS Developer environment if not already set
if not defined VSINSTALLDIR (
    if exist "!VSDEVCMD!" (
        echo Setting up Visual Studio Developer environment...
        call "!VSDEVCMD!" -no_logo
    )
)
echo.

echo Building and publishing WorldMapWallpaper...
echo.

REM Clean previous build
echo [1/5] Cleaning previous build...
call :delFile "ImagePainter\Install.exe"
call :delFolder "Settings\obj\"
call :delFolder "ImagePainter\obj\"
call :delFolder "Settings\bin\%cfg%\"
call :delFolder "ImagePainter\bin\%cfg%\"
call :delFolder "ImagePainter\bin\publish-64\"
echo.

echo [2/5] Building solutions...
call :BuildSolutions
if !ERRORLEVEL! neq 0 (
    echo ERROR: Build failed!
    exit /b 1
)

REM Publish the application for both Debug and Release
echo [3/5] Publishing application...
call :PublishApplication
if !ERRORLEVEL! neq 0 (
    echo ERROR: Publish failed!
    exit /b 1
)
echo Publish completed successfully.
echo.

REM Check if NSIS is available
echo [4/5] Checking for NSIS installer...
where makensis >nul 2>nul
if !ERRORLEVEL! neq 0 (
    echo WARNING: NSIS makensis not found in PATH.
    echo Please ensure NSIS is installed and makensis.exe is in your PATH.
    echo Common NSIS installation path: "C:\Program Files (x86)\NSIS\makensis.exe"
    echo.
    echo You can manually run: makensis InstallMaker.nsi
    exit /b 1
)
echo NSIS found.
echo.

REM Create installer
echo [5/5] Creating installer...
if /i "%cfg%"=="Release" (
    echo Creating Release installer...
    makensis /DBUILD_CONFIG=Release ImagePainter\InstallMaker.nsi
) else (
    echo Creating Debug installer...
    makensis /DBUILD_CONFIG=Debug ImagePainter\InstallMaker.nsi
)
if !ERRORLEVEL! neq 0 (
    echo ERROR: Installer creation failed!
    exit /b 1
)
echo.

echo ===============================================
echo              BUILD COMPLETED
echo ===============================================
echo.

echo Published files: %PROJECT_DIR%ImagePainter\bin\publish-64\
if not exist "%PROJECT_DIR%ImagePainter\Install.exe" (
    echo WARNING: Install.exe not found.
    echo Cannot create release without installer!
    exit /b 1
)
echo Installer created: %PROJECT_DIR%ImagePainter\Install.exe
echo [RELEASE] Triggering GitHub release...
echo.
        
REM Check if gh CLI is available
where gh >nul 2>nul
if !ERRORLEVEL! neq 0 (
    echo WARNING: GitHub CLI not found. Please install GitHub CLI to create releases.
    echo You can download it from: https://cli.github.com/
    echo.
    echo Alternatively, trigger the release manually with:
    echo gh api repos/:owner/:repo/dispatches -f event_type=create-release
    exit /b 1
) 

echo Creating GitHub release...
            
REM Get version from AssemblyInfo.cs - check both locations
set "VERSION="
if exist "ImagePainter\Properties\AssemblyInfo.cs" (
    echo Reading version from ImagePainter\Properties\AssemblyInfo.cs...
    for /f "tokens=2 delims=(" %%a in ('findstr "AssemblyVersion" ImagePainter\Properties\AssemblyInfo.cs') do (
        for /f "tokens=1 delims=)" %%b in ("%%a") do (
            set VERSION=%%b
            set VERSION=!VERSION:"=!
        )
    )
) else if exist "Properties\AssemblyInfo.cs" (
    echo Reading version from Properties\AssemblyInfo.cs...
    for /f "tokens=2 delims=(" %%a in ('findstr "AssemblyVersion" Properties\AssemblyInfo.cs') do (
        for /f "tokens=1 delims=)" %%b in ("%%a") do (
            set VERSION=%%b
            set VERSION=!VERSION:"=!
        )
    )
)
            
if "!VERSION!"=="" (
    echo ERROR: Could not extract version from AssemblyInfo.cs
    exit /b 1
)
            
echo Creating release v!VERSION! with installer...
            
REM Check if tag already exists and find available suffix
set "FINAL_VERSION=!VERSION!"
set "SUFFIX_LETTERS=abcdefghijklmnopqrstuvwxyz"
            
REM Check if base version exists
gh release view "v!VERSION!" >nul 2>&1
if !ERRORLEVEL! equ 0 (
    echo Tag v!VERSION! already exists, finding available suffix...
                
    REM Try sequential letters
    for /l %%i in (0,1,25) do (
        set /a "CHAR_INDEX=%%i"
        call set "LETTER=%%SUFFIX_LETTERS:~!CHAR_INDEX!,1%%"
        set "TEST_VERSION=!VERSION!!LETTER!"
                    
        gh release view "v!TEST_VERSION!" >nul 2>&1
        if !ERRORLEVEL! neq 0 (
            set "FINAL_VERSION=!TEST_VERSION!"
            echo Using tag: v!FINAL_VERSION!
            goto :create_release
        )
    )
                
    echo ERROR: All suffixes a-z are taken for version !VERSION!
    exit /b 1
)
            
:create_release
REM Generate dynamic release notes using Claude Code
call :generate_release_notes "!FINAL_VERSION!"
if !ERRORLEVEL! neq 0 (
    echo WARNING: Failed to generate dynamic release notes, using fallback...
    set "RELEASE_NOTES=WorldMapWallpaper Release v!FINAL_VERSION! with real-time day/night cycle visualization and ISS tracking. Download and run Install.exe to install. Requires Windows 10 version 1809 or later."
) else (
    echo Dynamic release notes generated successfully.
)

if /i "%cfg%"=="Debug" (
    echo Debug build completed. No GitHub release will be created.
    exit /b 0
)

gh release create "v!FINAL_VERSION!" "%PROJECT_DIR%ImagePainter\Install.exe" --title "WorldMapWallpaper v!FINAL_VERSION!" --notes "!RELEASE_NOTES!"
            
if !ERRORLEVEL! neq 0 (
    echo ERROR: Failed to create GitHub release.
    echo Make sure you're authenticated with: gh auth login
    exit /b 1
) 

echo GitHub release created successfully!
echo View at: https://github.com/PaulStSmith/DesktopImageChanger/releases/tag/v!FINAL_VERSION!
echo The installer is ready for distribution!
exit /b 0

:BuildSolutions
echo "  ___      _ _    _ _                  "
echo " | _ )_  _(_) |__| (_)_ _  __ _        "
echo " | _ \ || | | / _` | | ' \/ _` |_ _ _  "
echo " |___/\_,_|_|_\__,_|_|_||_\__, (_)_)_) "
echo "                          |___/        "
echo.
echo Configuration: !cfg!
echo.

:: Find all solution files in the current directory
set SOLUTION_COUNT=0
set SOLUTIONS=
for %%f in ("%~dp0*.sln") do (
    set /a SOLUTION_COUNT+=1
    set SOLUTIONS=!SOLUTIONS! "%%f"
    echo Found solution: %%~nxf
)

if !SOLUTION_COUNT! equ 0 (
    echo No solution files found in the current directory.
    echo Please ensure there are .sln files in the same folder as this script.
    exit /b 1
)

echo.
echo Found !SOLUTION_COUNT! solution(s) to build.
echo.

echo Cleaning `obj` and `bin` folders...
for /r /d %%i in (obj) do if exist "%%i"       rmdir /s /q "%%i"       2> nul
for /r /d %%i in (bin) do if exist "%%i\!cfg!" rmdir /s /q "%%i\!cfg!" 2> nul

:: Create Logs directory if it doesn't exist
if not exist "%~dp0Logs\Build" mkdir "%~dp0Logs\Build"

echo Building solutions...
set BUILD_FAILED=0
for %%s in (!SOLUTIONS!) do (
    echo.
    echo ========================================
    echo Building: %%~nxs
    echo ========================================
    call :CompileSolution %%s > "%~dp0Logs\Build\%%~ns.log" 2>&1
    if !ERRORLEVEL! neq 0 (
        echo Build failed for: %%~nxs
        set BUILD_FAILED=1
    ) else (
        echo Build succeeded for: %%~nxs
    )
)

if !BUILD_FAILED! equ 1 (
    echo.
    echo "  ___      _ _    _     ___     _ _        _ _  "
    echo " | _ )_  _(_) |__| |   | __|_ _(_) |___ __| | | "
    echo " | _ \ || | | / _` |   | _/ _` | | / -_) _` |_| "
    echo " |___/\_,_|_|_\__,_|   |_|\__,_|_|_\___\__,_(_) "
    echo.
    echo One or more builds failed. Check the log files in "%~dp0Logs\" for details.
    exit /b 1
)

echo.
echo All solutions built successfully!
exit /b 0

:CompileSolution
set PROJECT_FILE=%1
if "%PROJECT_FILE%"=="" (
    echo No project file specified.
    echo Usage: %0 ProjectFile.sln
    exit /b 1
)

echo Running NuGet restore...
"!MSBUILD_PATH!" "%PROJECT_FILE%" /t:Restore /p:Configuration=!cfg! /p:Platform="Any CPU" /p:RuntimeIdentifiers=win-x64
if %ERRORLEVEL% neq 0 (
    echo Build failed for AnyCPU.
    exit /b %ERRORLEVEL%
)

echo.
echo Building AnyCPU version...
"!MSBUILD_PATH!" "%PROJECT_FILE%" /p:Configuration=!cfg! /p:Platform="Any CPU" /p:RuntimeIdentifiers=win-x64 /v:m
if %ERRORLEVEL% neq 0 (
    echo Build failed for AnyCPU.
    exit /b %ERRORLEVEL%
)

echo.
echo Build complete successfully.
exit /b 0

:PublishApplication
echo Publishing with MSBuild...

REM Use MSBuild to publish with the profile - find the main project
cd /d "%PROJECT_DIR%"
if exist "ImagePainter\WorldMapWallpaper.csproj" (
    set "PUBLISH_PROJECT=ImagePainter\WorldMapWallpaper.csproj"
) else if exist "WorldMapWallpaper.csproj" (
    set "PUBLISH_PROJECT=WorldMapWallpaper.csproj"
) else (
    echo ERROR: Could not find WorldMapWallpaper.csproj
    exit /b 1
)

echo Restoring packages for publish (with ReadyToRun support)...
"!MSBUILD_PATH!" "!PUBLISH_PROJECT!" /t:Restore /p:Configuration=!cfg! /p:RuntimeIdentifier=win-x64 /p:PublishReadyToRun=true /v:m
if !ERRORLEVEL! neq 0 (
    echo ERROR: Restore for publish failed.
    exit /b 1
)

echo Publishing !PUBLISH_PROJECT!...
"!MSBUILD_PATH!" "!PUBLISH_PROJECT!" /p:PublishProfile=Publish_x64 /p:Configuration=!cfg! /t:Publish /v:m
exit /b %ERRORLEVEL%

:generate_release_notes
set "version=%~1"
echo Generating dynamic release notes for version %version%...

REM Check if Claude Code CLI is available
where claude >nul 2>nul
if !ERRORLEVEL! neq 0 (
    echo Claude Code CLI not found. Please install Claude Code CLI first.
    echo Visit: https://claude.ai/code for installation instructions
    echo Using fallback release notes...
    exit /b 1
)

REM Get recent git commits since last release
echo Collecting recent changes...
git log --oneline --since="2 weeks ago" --pretty=format:"- %%s" > "%PROJECT_DIR%temp_changes.txt" 2>nul
if !ERRORLEVEL! neq 0 (
    echo Warning: Could not get git log, using basic changes...
    echo - Latest improvements and bug fixes > "%PROJECT_DIR%temp_changes.txt"
)

REM Read changes into variable (handle multi-line)
set "CHANGES="
for /f "usebackq delims=" %%i in ("%PROJECT_DIR%temp_changes.txt") do (
    set "CHANGES=!CHANGES!%%i "
)

REM Call Claude Code to generate release notes
echo Calling Claude Code to generate professional release notes...
claude -p exit"Create a professional GitHub release description for WorldMapWallpaper v%version%. DO NOT use emojis, only ASCII chars are ok. Markdown OK. Recent commits: %CHANGES% Make it engaging, user-focused, and under 300 words. Include: version highlights, installation instructions (Download and run Install.exe), and system requirements (Windows 10 1809+). Focus on benefits like real-time day/night visualization, ISS tracking with SGP4, timezone clocks, and dynamic wallpaper updates." > "%PROJECT_DIR%temp_release.txt" 2>nul

if !ERRORLEVEL! neq 0 (
    echo Failed to generate release notes with Claude Code
    del "%PROJECT_DIR%temp_changes.txt" 2>nul
    del "%PROJECT_DIR%temp_release.txt" 2>nul
    exit /b 1
)

REM Read the generated release notes (handle multi-line)
set "RELEASE_NOTES="
for /f "usebackq delims=" %%i in (`findstr /n "^" "%PROJECT_DIR%temp_release.txt"`) do (
    set "line=%%i"
    set "line=!line:*:=!"
    if defined RELEASE_NOTES (
        set "RELEASE_NOTES=!RELEASE_NOTES!!LF!!line!"
    ) else (
        set "RELEASE_NOTES=!line!"
    )
)

REM Clean up temporary files
del "%PROJECT_DIR%temp_changes.txt" 2>nul
del "%PROJECT_DIR%temp_release.txt" 2>nul

if "!RELEASE_NOTES!"=="" (
    echo Generated release notes are empty
    exit /b 1
)

echo Generated release notes preview:
echo =====================================
echo !RELEASE_NOTES!
echo =====================================

exit /b 0

:delFile
if exist "%~1" (
    del /q "%~1"
    echo "%~1" deleted.
) else (
    echo "%~1" not found.
)
exit /b 0

:delFolder
if exist "%~1" (
    rmdir /s /q "%~1"
    echo "%~1" folder deleted.
) else (
    echo "%~1" folder not found.
)
exit /b 0