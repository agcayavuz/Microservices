\
# Ensure the shared network exists
./create-network.ps1

# Bring up services in dependency-friendly order
docker compose --env-file ../env/elastic.env -f ../compose/elasticsearch.yml up -d
docker compose --env-file ../env/kibana.env -f ../compose/kibana.yml up -d
docker compose --env-file ../env/postgres.env -f ../compose/postgresql.yml up -d
docker compose --env-file ../env/rabbitmq.env -f ../compose/rabbitmq.yml up -d
docker compose --env-file ../env/redpanda.env -f ../compose/redpanda.yml up -d

Write-Host "All services requested are starting..."
