# ?? BUILD & TEST SCRIPT FOR POPUP FIX (Windows PowerShell)
# Author: AI Assistant
# Platform: Windows
# .NET Version: 9.0

param(
    [string]$Command = "help"
)

$ErrorActionPreference = "Continue"

# Colors
$Green = 'Green'
$Red = 'Red'
$Yellow = 'Yellow'

function Write-Success {
    Write-Host "? $args" -ForegroundColor $Green
}

function Write-Error-Custom {
    Write-Host "? $args" -ForegroundColor $Red
}

function Write-Info {
    Write-Host "??  $args"
}

function Write-Step {
    Write-Host "? $args" -ForegroundColor $Yellow
}

# ============================================================================
# FUNCTION: Clean build
# ============================================================================
function Clean-Build {
    Write-Step "Cleaning build artifacts..."
    dotnet clean -c Debug -nologo -q 2>&1
    if (Test-Path "bin") {
    Remove-Item -Recurse -Force "bin" -ErrorAction SilentlyContinue
    }
    if (Test-Path "obj") {
Remove-Item -Recurse -Force "obj" -ErrorAction SilentlyContinue
  }
    Write-Success "Clean complete"
}

# ============================================================================
# FUNCTION: Restore NuGet packages
# ============================================================================
function Restore-Packages {
    Write-Step "Restoring NuGet packages..."
    dotnet restore --no-cache -nologo 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Success "Restore complete"
      return $true
    }
    else {
        Write-Error-Custom "Restore failed"
      return $false
    }
}

# ============================================================================
# FUNCTION: Build Windows only
# ============================================================================
function Build-Windows {
    Write-Step "Building for Windows (net9.0-windows10.0.19041.0)..."
    dotnet build -f net9.0-windows10.0.19041.0 -c Debug --no-restore --nologo 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Success "Windows build successful"
        return $true
    }
    else {
        Write-Error-Custom "Windows build failed"
        return $false
    }
}

# ============================================================================
# FUNCTION: Build all frameworks
# ============================================================================
function Build-All {
    Write-Step "Building all frameworks..."
    dotnet build -c Debug --no-restore --nologo 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Success "Full build successful"
        return $true
    }
    else {
 Write-Error-Custom "Full build failed (Android SDK not installed?)"
        return $false
    }
}

# ============================================================================
# FUNCTION: Check for compilation errors
# ============================================================================
function Check-Errors {
    Write-Step "Checking for compilation errors..."
 $output = dotnet build -f net9.0-windows10.0.19041.0 2>&1
    $errorCount = ($output | Select-String "error" | Measure-Object).Count
    
    if ($errorCount -eq 0) {
        Write-Success "No compilation errors found"
        return $true
    }
    else {
     Write-Error-Custom "Found $errorCount compilation errors"
        return $false
    }
}

# ============================================================================
# FUNCTION: Run static analysis
# ============================================================================
function Run-Analysis {
  Write-Info ""
  Write-Step "Code analysis checklist:"
    Write-Info ""
    Write-Info "1. AppShell.xaml.cs"
    Write-Info "   - Must have: public partial class"
    Write-Info "   - Must have: InitializeComponent()"
    Write-Info ""
Write-Info "2. Pages/HybridPOIPopupOverlay.xaml.cs"
    Write-Info "   - Must have: FindByName<T>() calls"
    Write-Info "   - Must have: null checks"
    Write-Info ""
 Write-Info "3. VinhKhanhstreetfoods.csproj"
    Write-Info "   - Must have: <MauiXaml Update=\"AppShell.xaml\">"
    Write-Info "   - Must have: <Generator>MSBuild:Compile</Generator>"
    Write-Info ""
    Write-Success "Manual review recommended"
}

# ============================================================================
# FUNCTION: List build artifacts
# ============================================================================
function List-Artifacts {
    Write-Step "Build artifacts:"
    if (Test-Path "bin/Debug") {
        Get-ChildItem -Recurse -Path "bin/Debug" -Filter "*.dll" | Select-Object -First 5
     Write-Info "  ... and more"
    }
    else {
        Write-Info "  No artifacts found yet"
    }
}

# ============================================================================
# FUNCTION: Show version info
# ============================================================================
function Show-Version-Info {
    Write-Step "Version Information:"
    Write-Info ".NET SDK: $(dotnet --version)"
    Write-Info "MAUI Workload:"
    $workloads = dotnet workload list 2>&1
$mauiWorkload = $workloads | Select-String "maui"
    if ($mauiWorkload) {
        Write-Info $mauiWorkload
    }
    else {
        Write-Info "(not listed)"
    }
}

# ============================================================================
# FUNCTION: Print help
# ============================================================================
function Print-Help {
    Write-Host ""
    Write-Host "Usage: .\build.ps1 -Command <COMMAND>" -ForegroundColor $Yellow
    Write-Host ""
    Write-Host "Commands:"
    Write-Host "  clean    - Clean build artifacts"
    Write-Host "  restore - Restore NuGet packages"
    Write-Host "  windows        - Build for Windows only (RECOMMENDED)"
    Write-Host "  all           - Build all frameworks"
    Write-Host "  check            - Check for errors"
    Write-Host "  analyze        - Run code analysis"
    Write-Host "  artifacts        - List build artifacts"
    Write-Host "  version          - Show version info"
    Write-Host "  full     - Full build pipeline (clean + restore + windows + check)"
  Write-Host "  help         - Show this help"
    Write-Host ""
    Write-Host "Examples:"
    Write-Host "  .\build.ps1 -Command windows   # Build for Windows"
    Write-Host "  .\build.ps1 -Command full      # Full pipeline"
    Write-Host "  .\build.ps1   # Shows help"
    Write-Host ""
}

# ============================================================================
# MAIN EXECUTION
# ============================================================================

Write-Host "=================================================="
Write-Host "?? VinhKhanhstreetfoods Build & Test Script" -ForegroundColor $Yellow
Write-Host "=================================================="

$projectDir = Get-Location
Write-Info "Project Directory: $projectDir"

switch ($Command) {
    "clean" {
        Clean-Build
    }
    "restore" {
        Restore-Packages
    }
    "windows" {
        if (Restore-Packages) {
    Build-Windows
        }
    }
    "all" {
        if (Restore-Packages) {
   Build-All
    }
    }
    "check" {
        Check-Errors
    }
    "analyze" {
        Run-Analysis
    }
    "artifacts" {
        List-Artifacts
    }
    "version" {
        Show-Version-Info
    }
    "full" {
        Write-Step "Running full build pipeline..."
        Write-Host ""
        
        Clean-Build
    if (Restore-Packages) {
            if (Build-Windows) {
  if (Check-Errors) {
   List-Artifacts
  Write-Host ""
       Write-Host "?? Full pipeline complete!" -ForegroundColor $Green
          }
  }
        }
    }
    "help" {
        Print-Help
    }
 default {
        Write-Error-Custom "Unknown command: $Command"
 Print-Help
   exit 1
 }
}

Write-Host ""
Write-Host "=================================================="
Write-Host "? Done" -ForegroundColor $Green
