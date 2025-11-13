#!/bin/bash
set -e

# --- Tool Paths ---
CYCLONEDX_PATH="/home/jules/.dotnet/tools/dotnet-CycloneDX"

# Generate .NET SBOM
$CYCLONEDX_PATH Smartstore.sln --output-format json > bom.json
