#!/bin/bash
# ?? BUILD & TEST SCRIPT FOR POPUP FIX
# Windows PowerShell equivalent at bottom

# Colors for output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${YELLOW}?? VinhKhanhstreetfoods Build & Test Script${NC}"
echo "=================================================="

# Get project directory
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

echo ""
echo -e "${YELLOW}?? Project Directory: $SCRIPT_DIR${NC}"

# ============================================================================
# FUNCTION: Clean build
# ============================================================================
clean_build() {
    echo ""
    echo -e "${YELLOW}?? Cleaning build artifacts...${NC}"
    dotnet clean -c Debug -nologo -q
    rm -rf bin obj
    echo -e "${GREEN}? Clean complete${NC}"
}

# ============================================================================
# FUNCTION: Restore NuGet packages
# ============================================================================
restore_packages() {
    echo ""
    echo -e "${YELLOW}?? Restoring NuGet packages...${NC}"
    dotnet restore --no-cache -nologo
    if [ $? -eq 0 ]; then
echo -e "${GREEN}? Restore complete${NC}"
    else
    echo -e "${RED}? Restore failed${NC}"
        return 1
    fi
}

# ============================================================================
# FUNCTION: Build Windows only
# ============================================================================
build_windows() {
    echo ""
    echo -e "${YELLOW}???  Building for Windows (net9.0-windows10.0.19041.0)...${NC}"
    dotnet build -f net9.0-windows10.0.19041.0 -c Debug --no-restore --nologo
    if [ $? -eq 0 ]; then
   echo -e "${GREEN}? Windows build successful${NC}"
        return 0
    else
      echo -e "${RED}? Windows build failed${NC}"
    return 1
    fi
}

# ============================================================================
# FUNCTION: Build all frameworks
# ============================================================================
build_all() {
    echo ""
  echo -e "${YELLOW}???  Building all frameworks...${NC}"
    dotnet build -c Debug --no-restore --nologo
    if [ $? -eq 0 ]; then
        echo -e "${GREEN}? Full build successful${NC}"
        return 0
    else
        echo -e "${RED}? Full build failed (Android SDK not installed?)${NC}"
        return 1
    fi
}

# ============================================================================
# FUNCTION: Check for compilation errors
# ============================================================================
check_errors() {
 echo ""
    echo -e "${YELLOW}?? Checking for compilation errors...${NC}"
    ERROR_COUNT=$(dotnet build -f net9.0-windows10.0.19041.0 2>&1 | grep -c "error")
    if [ "$ERROR_COUNT" -eq 0 ]; then
        echo -e "${GREEN}? No compilation errors found${NC}"
 return 0
    else
  echo -e "${RED}? Found $ERROR_COUNT compilation errors${NC}"
        return 1
    fi
}

# ============================================================================
# FUNCTION: Run static analysis
# ============================================================================
run_analysis() {
  echo ""
    echo -e "${YELLOW}?? Running code analysis...${NC}"
    echo ""
    echo "Key files to check:"
    echo "  1. AppShell.xaml.cs"
    echo "     - Must have: public partial class"
    echo "     - Must have: InitializeComponent()"
    echo ""
    echo "  2. Pages/HybridPOIPopupOverlay.xaml.cs"
    echo "     - Must have: FindByName<T>() calls"
    echo "     - Must have: null checks"
    echo ""
    echo "  3. VinhKhanhstreetfoods.csproj"
    echo "     - Must have: <MauiXaml Update=\"AppShell.xaml\">"
    echo "     - Must have: <Generator>MSBuild:Compile</Generator>"
    echo ""
    echo -e "${GREEN}? Manual review recommended${NC}"
}

# ============================================================================
# FUNCTION: List build artifacts
# ============================================================================
list_artifacts() {
    echo ""
    echo -e "${YELLOW}?? Build artifacts:${NC}"
    if [ -d "bin/Debug" ]; then
        find bin/Debug -type f -name "*.dll" | head -5
        echo "  ... and more"
    else
        echo "  No artifacts found yet"
    fi
}

# ============================================================================
# FUNCTION: Show version info
# ============================================================================
show_version_info() {
    echo ""
    echo -e "${YELLOW}??  Version Information:${NC}"
    echo "  .NET SDK: $(dotnet --version)"
    echo "  MAUI Workload:"
    dotnet workload list 2>&1 | grep -i maui || echo "  (not listed)"
}

# ============================================================================
# FUNCTION: Print help
# ============================================================================
print_help() {
    echo ""
    echo "Usage: ./build.sh [COMMAND]"
    echo ""
    echo "Commands:"
  echo "  clean   - Clean build artifacts"
    echo "  restore            - Restore NuGet packages"
    echo "  windows   - Build for Windows only"
    echo "  all- Build all frameworks"
    echo "  check       - Check for errors"
    echo "  analyze       - Run code analysis"
    echo "  artifacts     - List build artifacts"
    echo "  version     - Show version info"
    echo "  full               - Full build pipeline (clean + restore + windows + check)"
  echo "  help         - Show this help"
    echo ""
}

# ============================================================================
# MAIN EXECUTION
# ============================================================================

case "${1:-help}" in
    clean)
        clean_build
        ;;
    restore)
        restore_packages
    ;;
    windows)
        restore_packages && build_windows
        ;;
    all)
        restore_packages && build_all
        ;;
check)
  check_errors
        ;;
    analyze)
        run_analysis
        ;;
    artifacts)
        list_artifacts
        ;;
    version)
   show_version_info
        ;;
    full)
    echo -e "${YELLOW}?? Running full build pipeline...${NC}"
        clean_build && \
        restore_packages && \
      build_windows && \
        check_errors && \
        list_artifacts && \
  echo "" && \
        echo -e "${GREEN}?? Full pipeline complete!${NC}"
        ;;
    help)
        print_help
        ;;
*)
        echo -e "${RED}Unknown command: $1${NC}"
        print_help
        exit 1
        ;;
esac

echo ""
echo "=================================================="
echo -e "${GREEN}? Done${NC}"
