[CmdletBinding()]
param(
  [string]$NetworkName = "ms-net"
)

# Ağ var mı kontrol et (exit code ile)
$null = docker network inspect $NetworkName 2>$null
if ($LASTEXITCODE -ne 0) {
  Write-Host "Creating network $NetworkName ..."
  docker network create $NetworkName | Out-Null
  if ($LASTEXITCODE -eq 0) {
    Write-Host "Network $NetworkName created."
  } else {
    throw "Failed to create network $NetworkName."
  }
} else {
  Write-Host "Network $NetworkName already exists."
}
