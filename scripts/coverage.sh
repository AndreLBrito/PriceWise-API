#!/usr/bin/env bash
set -euo pipefail

CONFIGURATION="${1:-Debug}"
ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
RESULTS_DIR="$ROOT_DIR/artifacts/TestResults"

mkdir -p "$RESULTS_DIR"

dotnet test "$ROOT_DIR/PriceWise.slnx" \
  --configuration "$CONFIGURATION" \
  --collect:"XPlat Code Coverage" \
  --results-directory "$RESULTS_DIR"

echo "Relatorio de cobertura gerado em: $RESULTS_DIR"
