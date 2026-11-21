$ErrorActionPreference = "Stop"
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$testExe = Join-Path ([System.IO.Path]::GetTempPath()) ("StorageMassOverflowTests-" + [Guid]::NewGuid().ToString("N") + ".exe")
try {
    & "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo "/out:$testExe" (Join-Path $root "src\StorageMassOverflowPatch.cs") (Join-Path $root "tests\StorageMassOverflowTests.cs")
    if ($LASTEXITCODE -ne 0) { throw "Test compilation failed: $LASTEXITCODE" }
    & $testExe
    if ($LASTEXITCODE -ne 0) { throw "Tests failed: $LASTEXITCODE" }
} finally {
    Remove-Item -LiteralPath $testExe -ErrorAction SilentlyContinue
}
