#!/usr/bin/env bash
# SPDX-License-Identifier: AGPL-3.0-or-later
# Run PVS benchmark for network baseline comparison.
set -euo pipefail
cd "$(dirname "$0")/../.."
dotnet run --project Content.Benchmarks/Content.Benchmarks.csproj -c Release -- --filter "*PvsBenchmark*"
