$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

$env:HOST = "0.0.0.0"

$backend = Start-Process `
    -FilePath "dotnet" `
    -ArgumentList @("run", "--project", "backend/backend.csproj", "--launch-profile", "http") `
    -NoNewWindow `
    -PassThru

try {
    npm --prefix frontend run dev
}
finally {
    if ($backend -and -not $backend.HasExited) {
        Stop-Process -Id $backend.Id -ErrorAction SilentlyContinue
    }
}
