@echo off
REM SPDX-License-Identifier: AGPL-3.0-or-later
cd /d "%~dp0..\.."
dotnet run --project Content.Benchmarks/Content.Benchmarks.csproj -c Release -- --filter "*PvsBenchmark*"
