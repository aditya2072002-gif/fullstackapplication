$ErrorActionPreference = 'Stop'
$fullstackRoot = Split-Path -Parent $PSScriptRoot
Set-Location $fullstackRoot
$envPath = Join-Path $fullstackRoot '.env'

function New-RandomSecret {
	param([int]$Length = 36)
	$characters = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789'
	$bytes = New-Object byte[] $Length
	$generator = [System.Security.Cryptography.RandomNumberGenerator]::Create()
	try {
		$generator.GetBytes($bytes)
	}
	finally {
		$generator.Dispose()
	}

	$result = -join ($bytes | ForEach-Object { $characters[[int]$_ % $characters.Length] })
	return "$result!aA9"
}

function Set-EnvValue {
	param([string[]]$Lines, [string]$Name, [string]$Value)
	$prefix = "$Name="
	$updated = $false
	for ($index = 0; $index -lt $Lines.Length; $index++) {
		if ($Lines[$index].StartsWith($prefix, [System.StringComparison]::Ordinal)) {
			$Lines[$index] = "$prefix$Value"
			$updated = $true
		}
	}
	if (-not $updated) {
		$Lines += "$prefix$Value"
	}
	return ,$Lines
}

if (-not (Test-Path $envPath)) {
	Copy-Item (Join-Path $fullstackRoot '.env.example') $envPath
	$lines = [System.IO.File]::ReadAllLines($envPath)
	$jwtBytes = New-Object byte[] 64
	$generator = [System.Security.Cryptography.RandomNumberGenerator]::Create()
	try {
		$generator.GetBytes($jwtBytes)
	}
	finally {
		$generator.Dispose()
	}
	$jwtSecret = [Convert]::ToBase64String($jwtBytes)
	$lines = Set-EnvValue $lines 'SQL_SA_PASSWORD' (New-RandomSecret)
	$lines = Set-EnvValue $lines 'JWT_SIGNING_KEY' $jwtSecret
	$lines = Set-EnvValue $lines 'DEMO_ADMIN_PASSWORD' (New-RandomSecret)
	$lines = Set-EnvValue $lines 'DEMO_CANDIDATE_PASSWORD' (New-RandomSecret)
	[System.IO.File]::WriteAllLines($envPath, $lines, [System.Text.Encoding]::ASCII)
}

& $env:ComSpec /c 'docker info >NUL 2>&1'
if ($LASTEXITCODE -ne 0) {
	throw 'Docker is installed but its engine is unavailable. Start Docker Desktop, then rerun this script.'
}

& docker compose --env-file .env up --detach --build sql migrate server tunnel
if ($LASTEXITCODE -ne 0) {
	throw 'Docker Compose failed while building or starting the demo. Check Docker Desktop and compose logs.'
}

$tunnelUrl = $null
for ($attempt = 0; $attempt -lt 60; $attempt++) {
	$logOutput = & docker compose --env-file .env logs --no-color --tail=100 tunnel 2>$null | Out-String
	if ($logOutput -match 'https://[a-z0-9-]+\.trycloudflare\.com') {
		$tunnelUrl = $Matches[0]
		break
	}
	Start-Sleep -Seconds 2
}

if (-not $tunnelUrl) {
	throw 'The Cloudflare Quick Tunnel URL was not reported. Check `docker compose --env-file .env logs tunnel`.'
}

$lines = [System.IO.File]::ReadAllLines($envPath)
$lines = Set-EnvValue $lines 'CORS_ALLOWED_ORIGINS' $tunnelUrl
[System.IO.File]::WriteAllLines($envPath, $lines, [System.Text.Encoding]::ASCII)

& docker compose --env-file .env up --detach --no-deps --force-recreate server
if ($LASTEXITCODE -ne 0) {
	throw 'The API could not be restarted with the Cloudflare origin. Check `docker compose --env-file .env logs server`.'
}

$serverContainer = (& docker compose --env-file .env ps --quiet server | Select-Object -First 1).Trim()
$healthy = $false
for ($attempt = 0; $attempt -lt 60; $attempt++) {
	$healthState = & docker inspect --format '{{.State.Health.Status}}' $serverContainer 2>$null
	if ($healthState -eq 'healthy') {
		$healthy = $true
		break
	}
	Start-Sleep -Seconds 2
}

if (-not $healthy) {
	throw 'The API did not become healthy. Check `docker compose --env-file .env logs server`.'
}

try {
	$response = Invoke-WebRequest -UseBasicParsing -Uri $tunnelUrl -TimeoutSec 20
	if ($response.StatusCode -lt 200 -or $response.StatusCode -ge 400) {
		throw "The public demo returned HTTP $($response.StatusCode)."
	}
}
catch {
	throw "The Cloudflare URL was created, but the public demo health check failed: $($_.Exception.Message)"
}

Write-Host "Temporary public demo is ready: $tunnelUrl"
Write-Host 'Generated secrets and demo credentials are stored in fullstack/.env; do not share or commit that file.'
Write-Host 'Stop the stack with: docker compose --env-file .env down'
