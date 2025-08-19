[CmdletBinding()]
param(
  [ValidateSet("elasticsearch","kibana","postgresql","rabbitmq","redpanda","redis")]
  [string]$Service
)

$ErrorActionPreference = "Stop"

# Script klasörünün tam yolunu al
$root = $PSScriptRoot

# Ortak ağın varlığını garanti et
& "$root\create-network.ps1"

# Servise göre compose + env dosyasını çalıştır
switch ($Service) {
  "elasticsearch" {
    docker compose --env-file "$root\..\env\elastic.env" `
                   -f "$root\..\compose\elasticsearch.yml" up -d
  }
  "kibana" {
    docker compose --env-file "$root\..\env\kibana.env" `
                   -f "$root\..\compose\kibana.yml" up -d
  }
  "postgresql" {
    docker compose --env-file "$root\..\env\postgres.env" `
                   -f "$root\..\compose\postgresql.yml" up -d
  }
  "rabbitmq" {
    docker compose --env-file "$root\..\env\rabbitmq.env" `
                   -f "$root\..\compose\rabbitmq.yml" up -d
  }
  "redpanda" {
    docker compose --env-file "$root\..\env\redpanda.env" `
                   -f "$root\..\compose\redpanda.yml" up -d
  }
  "redis" {
    docker compose --env-file "$root\..\env\redis.env" `
                   -f "$root\..\compose\redis.yml" up -d
  }
  default { throw "Geçersiz servis adı: $Service" }
}
