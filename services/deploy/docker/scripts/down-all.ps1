\
# Stop all known compose files
$files = @(
  "../compose/redpanda.yml",
  "../compose/rabbitmq.yml",
  "../compose/postgresql.yml",
  "../compose/kibana.yml",
  "../compose/elasticsearch.yml"
)
foreach ($f in $files) {
  if (Test-Path $f) {
    docker compose -f $f down
  }
}
