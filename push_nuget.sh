#!/usr/bin/env bash
set -euo pipefail

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
BLUE='\033[1;34m'
YELLOW='\033[1;33m'
NC='\033[0m' # "no color"

# Line prefix
PREFIX="║ "

# Print header
echo -e "${BLUE}"
echo -e "╔══════════════════════════════════════╗"
echo -e "║      NuGet Release Publisher         ║"
echo -e "╠══════════════════════════════════════╣"

# Function to pipe output through a framed formatter
frame() {
  while IFS= read -r line; do
    echo -e "${BLUE}${PREFIX}${NC}${line}"
  done
}

# Run steps with framed output
{
  echo -e "${RED}Cleaning old NuGet packages..."
  rm -f bin/Release/*.nupkg

  echo -e "${YELLOW}Building project (Release)..."
  dotnet build *.csproj -c Release

  echo -e "${YELLOW}"
  echo -e "Attempting NuGet push..."
  echo -e "${NC}"
  dotnet nuget push bin/Release/*.nupkg \
    --api-key "$NugetApiKey" \
    --source https://api.nuget.org/v3/index.json \
    --skip-duplicate

  echo -e "${GREEN}Done."
} 2>&1 | frame

echo -e "${BLUE}╚══════════════════════════════════════╝\n"
