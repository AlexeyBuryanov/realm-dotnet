# Run with: pwsh .github/tests/wrappers-build.Tests.ps1
# Exercise native CMake exit codes without requiring Visual Studio or building Core.
$ErrorActionPreference = 'Stop'
$testRoot = Join-Path ([System.IO.Path]::GetTempPath()) "realm-wrappers-test-$([guid]::NewGuid())"
$originalPath = $env:PATH

try {
    New-Item $testRoot -ItemType Directory | Out-Null
    Copy-Item (Join-Path $PSScriptRoot '../../wrappers/build.ps1') $testRoot
    @'
@echo off
echo %*>>"%REALM_TEST_CMAKE_LOG%"
if "%1"=="--build" exit /b %REALM_TEST_BUILD_EXIT%
exit /b %REALM_TEST_CONFIGURE_EXIT%
'@ | Set-Content (Join-Path $testRoot 'cmake.cmd')
    $env:PATH = "$testRoot;$originalPath"
    $env:REALM_TEST_CMAKE_LOG = Join-Path $testRoot 'cmake.log'

    foreach ($target in @('Windows', 'WindowsStore')) {
        foreach ($case in @(
            @{ Configure = 7; Build = 0; Expected = 7; Calls = 1; Incremental = $false },
            @{ Configure = 0; Build = 9; Expected = 9; Calls = 2; Incremental = $false },
            @{ Configure = 0; Build = 0; Expected = 0; Calls = 2; Incremental = $false },
            @{ Configure = 7; Build = 0; Expected = 0; Calls = 1; Incremental = $true },
            @{ Configure = 7; Build = 9; Expected = 9; Calls = 1; Incremental = $true }
        )) {
            $env:REALM_TEST_CONFIGURE_EXIT = [string]$case.Configure
            $env:REALM_TEST_BUILD_EXIT = [string]$case.Build
            if (Test-Path $env:REALM_TEST_CMAKE_LOG) {
                Remove-Item -LiteralPath $env:REALM_TEST_CMAKE_LOG
            }
            $extraArgs = @()
            if ($case.Incremental) {
                # An incremental build must reuse an existing directory and skip configuration.
                New-Item (Join-Path $testRoot "cmake/$target/Release-x64") -ItemType Directory -Force | Out-Null
                $extraArgs += '-Incremental'
            }
            & pwsh -NoProfile -File (Join-Path $testRoot 'build.ps1') $target -Platforms x64 -Configuration Release -ExtraCMakeArgs '-T v143,version=14.35' @extraArgs | Out-Null
            if ($LASTEXITCODE -ne $case.Expected) {
                throw "$target returned $LASTEXITCODE; expected $($case.Expected)"
            }
            $calls = @(Get-Content $env:REALM_TEST_CMAKE_LOG)
            if ($calls.Count -ne $case.Calls) {
                throw "$target made $($calls.Count) CMake calls; expected $($case.Calls)"
            }
            if ($case.Incremental) {
                if ($calls[0] -notmatch '^--build ') {
                    throw 'Incremental builds must skip CMake configuration'
                }
            } elseif ($calls[0] -match '(?:^|\s)-G') {
                throw 'The shared script must allow CMake to select the generator'
            }
            Write-Host "$target incremental=$($case.Incremental) configure=$($case.Configure) build=$($case.Build): passed"
        }
    }
} finally {
    $env:PATH = $originalPath
    Remove-Item Env:REALM_TEST_CMAKE_LOG, Env:REALM_TEST_CONFIGURE_EXIT, Env:REALM_TEST_BUILD_EXIT -ErrorAction SilentlyContinue
    $resolvedTestRoot = [System.IO.Path]::GetFullPath($testRoot)
    $tempPrefix = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath()).TrimEnd('\') + '\'
    if ($resolvedTestRoot.StartsWith($tempPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        Remove-Item -LiteralPath $resolvedTestRoot -Recurse -Force
    }
}
