$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

$tunnelOut = Join-Path $root "tunnel.out"
$tunnelErr = Join-Path $root "tunnel.err"

Remove-Item $tunnelOut, $tunnelErr -ErrorAction SilentlyContinue

Get-Process ssh -ErrorAction SilentlyContinue |
    Stop-Process -ErrorAction SilentlyContinue

$existingBackendListeners = Get-NetTCPConnection `
    -LocalPort 5050 `
    -State Listen `
    -ErrorAction SilentlyContinue

foreach ($listener in $existingBackendListeners) {
    Stop-Process -Id $listener.OwningProcess -ErrorAction SilentlyContinue
}

Write-Host "Building frontend..."
npm run build

Write-Host ""
Write-Host "Starting backend on http://localhost:5050..."
$backend = Start-Process `
    -FilePath "dotnet" `
    -ArgumentList @("run", "--project", "backend/backend.csproj", "--launch-profile", "http") `
    -NoNewWindow `
    -PassThru

try {
    Write-Host "Waiting for backend..."
    $backendReady = $false

    for ($attempt = 1; $attempt -le 30; $attempt++) {
        try {
            Invoke-WebRequest -UseBasicParsing "http://127.0.0.1:5050/products" | Out-Null
            $backendReady = $true
            break
        }
        catch {
            Start-Sleep -Seconds 1
        }
    }

    if (-not $backendReady) {
        throw "Backend did not respond on http://127.0.0.1:5050."
    }

    Write-Host ""
    Write-Host "Starting public tunnel..."
    $tunnel = Start-Process `
        -FilePath "ssh" `
        -ArgumentList @("-o", "StrictHostKeyChecking=no", "-R", "80:127.0.0.1:5050", "nokey@localhost.run") `
        -RedirectStandardOutput $tunnelOut `
        -RedirectStandardError $tunnelErr `
        -NoNewWindow `
        -PassThru

    $publicUrl = $null

    for ($attempt = 1; $attempt -le 30; $attempt++) {
        if (Test-Path $tunnelOut) {
            $output = Get-Content $tunnelOut -Raw
            if ($null -eq $output) {
                $output = ""
            }

            $match = [regex]::Match($output, "https://[^\s]+\.lhr\.life")

            if ($match.Success) {
                $publicUrl = $match.Value
                break
            }
        }

        Start-Sleep -Seconds 1
    }

    Write-Host ""

    if ($publicUrl) {
        Write-Host "Share this link with your team:" -ForegroundColor Green
        Write-Host $publicUrl -ForegroundColor Cyan
    }
    else {
        Write-Host "Tunnel started, but I could not find the link automatically." -ForegroundColor Yellow
        Write-Host "Check tunnel.out for the https://...lhr.life link."
    }

    Write-Host ""
    Write-Host "Keep this window open. Press Ctrl+C to stop sharing."

    while (-not $backend.HasExited -and -not $tunnel.HasExited) {
        Start-Sleep -Seconds 2
    }
}
finally {
    if ($backend -and -not $backend.HasExited) {
        Stop-Process -Id $backend.Id -ErrorAction SilentlyContinue
    }

    if ($tunnel -and -not $tunnel.HasExited) {
        Stop-Process -Id $tunnel.Id -ErrorAction SilentlyContinue
    }
}
