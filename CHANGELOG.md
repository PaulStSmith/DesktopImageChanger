# Changelog

All notable changes to this project will be documented in this file.

## [Unreleased]

### Added
- **Resolution Accommodation feature for different screen resolutions**
  - Added new `ResolutionMode` setting with three options:
    - `None` - Keep original 1920x1080 resolution (default, current behavior)
    - `Fit` - Scale to fit screen while preserving 16:9 aspect ratio (adds letterboxing/pillarboxing)
    - `Stretch` - Scale to fill entire screen (may distort aspect ratio)
  - Auto-detects primary monitor resolution using Windows API
  - Optional custom resolution override (set width and height in settings)
  - High-quality scaling using bicubic interpolation
  - Resolution scaling applied as final step before saving wallpaper
  - New "Resolution Settings" section in Settings UI with:
    - Resolution mode dropdown
    - Detected screen resolution display
    - Custom width/height input fields

### Changed
- **Simplify and consolidate MSBuild detection in build script**
  - Moved MSBuild detection to script initialization (runs once at startup)
  - Eliminated duplicate detection code from `:CompileSolution` and `:PublishApplication`
  - VS Developer environment now set up once at the start
  - Now supports Visual Studio 2026 and 2022 only (removed VS2019, VS2017, and .NET Framework fallbacks)
  - Updated error message to clearly indicate VS2022 or later is required
  - Removed fallback to MSBuild in PATH for more predictable builds

### Fixed
- **Fix publish failure due to missing ReadyToRun runtime packages**
  - Added dedicated restore step in `:PublishApplication` with `PublishReadyToRun=true`
  - Resolves NETSDK1094 error when publishing with ReadyToRun optimization enabled

## [1.5.25.1005] - 2025-10-05

### Added
- **Implement professional SGP4-based ISS tracking with TLE data**
  - Replaced Open Notify API with Two-Line Element (TLE) orbital mechanics
  - Added TLE data fetching from CelesTrak with automatic caching
  - Implemented SGP4 orbital propagator for accurate satellite position calculations
  - Added comprehensive orbital mathematics utilities (GMST, coordinate transforms)
  - Enhanced ISS tracking accuracy with professional satellite tracking algorithms
  - Added offline operation capability when TLE data is cached
  - Improved orbital velocity calculations (now ~27,571 km/h, matching real ISS speed)

- **Enhanced orbit visualization with directional indicators**
  - Limited orbit display to 100 points (50 before/after current position)
  - Added small directional arrows along orbital path every 10 segments
  - Improved orbit path clarity with focused time span (20 minutes total)
  - Enhanced visual feedback showing ISS movement direction

### Changed
- **Remove Windows Event Log functionality**
  - Eliminated unused Windows Event Log integration from application
  - Removed System.Diagnostics.EventLog package dependency
  - Simplified Logger.cs to use file-only logging with graceful error handling
  - Updated installer to remove Event Log source creation and cleanup
  - Reduced application complexity and removed unnecessary Windows dependencies

- **Improve ISS position calculation accuracy**
  - Fixed Greenwich Mean Sidereal Time (GMST) calculation using correct IAU-82 formula
  - Corrected coordinate transformation from ECI to geodetic coordinates
  - Resolved 105° longitude error that placed ISS over wrong hemisphere
  - Enhanced time synchronization for real-time position accuracy

### Technical Improvements
- **Simplify GitHub release notes in build script**
  - Replaced detailed release notes with a concise summary
  - Highlighted key features: day/night cycle and ISS tracking
  - Provided basic installation instructions in the notes
  - Streamlined the release process for easier maintenance

- **Replace GitHub Actions release workflow with build.bat logic**
  - Removed `release.yml` and the GitHub Actions workflow for releases
  - Updated `AssemblyVersion` and `AssemblyFileVersion` to 1.5.25.0930
  - Added version extraction and release creation logic to `build.bat`
  - Integrated `gh release create` into `build.bat` for manual releases
  - Enhanced release notes with new features, installation, and requirements
  - Simplified release process by consolidating it into the build script

- **Switch to MSBuild for build and publish process**
  - Added MSBuild setup step using microsoft/setup-msbuild@v2
  - Replaced `dotnet publish` with MSBuild for detailed control
  - Cleaned previous build artifacts before starting the build
  - Built solution explicitly with MSBuild before publishing
  - Published `WorldMapWallpaper.csproj` using MSBuild to avoid assembly info conflicts
  - Retained NSIS installer creation process with `makensis`

## [1.5.25.0929] - 2025-09-29

### Changed
- **Update build script to adjust build failure handling**
  - Modified `build.bat` to change behavior when `BUILD_FAILED` is set
  - Added a success message (`echo All solutions built successfully!`)
  - Introduced `set cfg=` before exiting with a status code of `0`
  - Adjusted handling to potentially treat certain builds as successful

- **Improve build script messaging and error handling**
  - Displayed current configuration (`%cfg%`) in the build process
  - Added handling for "Debug" builds with a specific message
  - Enhanced "Release" build messaging for GitHub release triggers
  - Improved error handling for GitHub release workflow failures
  - Added a warning if `Install.exe` is not found during the build

### Added
- **Refactor GaussianBlur and enhance build script**
  - Refactored `GaussianBlur.cs` for readability and maintainability:
    - Renamed methods to PascalCase and added XML documentation
    - Improved exception handling and modernized syntax
    - Enhanced comments to explain Gaussian blur algorithm
  - Updated `AssemblyInfo.cs` to change version to `1.5.25.0929`
  - Modified `WorldMapWallpaper.csproj`:
    - Disabled auto-generation of assembly info
    - Suppressed warning `CA1416`
  - Enhanced `build.bat` for better automation:
    - Added modular functions for building and publishing
    - Improved logging and error handling for dependencies
    - Added support for multiple `.sln` files and MSBuild detection
  - Improved release handling in `build.bat`:
    - Added Git working tree checks for clean releases
    - Enhanced error messages for missing GitHub CLI
  - General improvements in `build.bat`:
    - Added ASCII art banners for better feedback
    - Improved cleanup of `obj` and `bin` folders
    - Enhanced maintainability with modular functions
    - Updated progress messages and step numbering
    - Improved handling of edge cases in build and release processes

- **Automate release process and improve versioning**
  - Added GitHub Actions workflow to automate release creation
  - Introduced `DateVersioning.targets` for date-based versioning
  - Updated `.gitignore` to exclude `.claude/` and `Install.exe`
  - Added `AssemblyInfo.cs` for assembly metadata and versioning
  - Updated `WorldMapWallpaper.csproj` to integrate versioning logic
  - Enhanced `build.bat` to support "Release" mode with Git checks
  - Added GitHub CLI integration for triggering release workflows
  - Improved error handling for missing installer in release mode
  - Updated NuGet dependencies to version `9.0.9`
  - Included detailed release notes in GitHub Actions workflow

- **Refactor ISSTracker and add build automation script**
  - Removed unused `System.Text.RegularExpressions` import from `ISSTracker.cs`
  - Updated `using` statements in `ISSTracker.cs` for consistency and added `WorldMapWallpaper.Properties`
  - Added `build.bat` script to automate build and packaging:
    - Cleans previous build artifacts
    - Publishes the application using `dotnet publish`
    - Checks for NSIS `makensis` tool availability
    - Creates an installer using `InstallMaker.nsi`
    - Provides detailed feedback for each step

- **Update target framework to .NET 9.0**
  - Updated `<TargetFramework>` in `Publish_x64.pubxml` from `net7.0-windows10.0.17763.0` to `net9.0-windows10.0.17763.0`
  - Updated `<TargetFramework>` in `WorldMapWallpaper.csproj` from `net7.0-windows10.0.17763.0` to `net9.0-windows10.0.17763.0`

- **Add ISS tracking and enhance wallpaper generation**
  - Introduced `ISSTracker` to fetch and render ISS position
  - Updated `.gitignore` to exclude new files from version control
  - Refactored `Program.cs` for solar calculations and terminator curves
  - Added high-res day/night maps and ISS icon resources
  - Enhanced `Logger` to ensure log directory creation
  - Updated `README.md` with new features and installation details
  - Added `Newtonsoft.Json` for API handling in `WorldMapWallpaper.csproj`
  - Improved image processing, error handling, and logging
  - Streamlined wallpaper generation and cleanup logic

## [Previous Versions] - 2024

### Changed
- **2024-07-26**: Removed log4net. Write our own log. Write to the event log on exception
- **2024-07-25**: Added log4net
- **2024-07-25**: Fixed automation to create installer
- **2024-07-25**: Fixed Arctic/Antarctic day/night cycle. Added automation to create installer

### Fixed
- **2024-06-10**: Correct scheduler task XML file creation
- **2024-06-09**: Updated installer
- **2024-06-09**: Renamed project to WorldMapWallpaper. Created Publish settings. Created Installer. Added some more comments in code
- **2024-03-20**: Fixed the alpha mask creation
- **2024-03-19**: Fixed the usage of wrong datatype for the declination calculation. It was int when it was supposed to be float
- **2024-02-27**: Fixed the calculation of the vernal equinox that messed up the drawing of the day/night terminator

## [Initial Versions] - 2023

### Added
- **2023-09-25**: Political map over the satellite imagery. World clocks
- **2023-08-26**: Update README.md - Fixed grammar
- **2023-08-24**: Changed app type from Console to Windows in order to hide the display of the console window when running as a task
- **2023-08-24**: Create README.md

### Fixed
- **2023-08-24**: Fixed a bug that it deleted the previous wallpaper
- **2023-08-23**: Fixed another bug that sneaked under the radar. The program didn't check if the current wallpaper was generated or a user defined one
- **2023-08-23**: Fixed a bug that prevented the updating of the wallpaper on a subsequent run

### Initial Release
- **2023-08-23**: Add project files
- **2023-08-23**: Add .gitattributes and .gitignore
