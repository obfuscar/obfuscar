#!/usr/bin/env bash
# Equivalent of dist.pack.bat for Unix-like shells
set -o pipefail

# Run PowerShell scripts with ExecutionPolicy Bypass and exit
# with the same code if any step fails.
run_ps() {
  pwsh -ExecutionPolicy Bypass -File "$1"
  rc=$?
  if [ $rc -ne 0 ]; then
    exit $rc
  fi
}

run_ps pre.build.ps1
run_ps release.ps1
run_ps sign.ps1

exit 0
