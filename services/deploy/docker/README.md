# Microservices Stack — Docker Package

This package provides **separate compose files** for each infra dependency and **env files** to pin versions and settings.

## Tree

```
ms-stack-docker/
  compose/
    elasticsearch.yml
    kibana.yml
    postgresql.yml
    rabbitmq.yml
    redpanda.yml
  env/
    elastic.env
    kibana.env
    postgres.env
    rabbitmq.env
    redpanda.env
  scripts/
    create-network.ps1
    up-service.ps1
    up-all.ps1
    down-all.ps1
  data/              # bind-mounted volumes will be created here
```

## Quick Start (Windows PowerShell)

```powershell
cd .\ms-stack-docker\scripts
./up-all.ps1
```

Or bring up a single service:

```powershell
./up-service.ps1 -Service elasticsearch
```

## Notes

- All services join the external network `ms-net`. The helper script creates it if missing.
- **Elasticsearch/Kibana** are configured for local development (single-node, security disabled). For prod, enable xpack security and TLS.
- **PostgreSQL** uses version `17.6` by default. Adjust `POSTGRES_VERSION` in `env/postgres.env` when needed.
- **RabbitMQ** uses the `-management` image exposing the UI at http://localhost:15672 (admin/admin by default).
- **Redpanda** exposes Kafka on `localhost:9092` and its Console on `http://localhost:8080`.
- Data directories are bind-mounted under `data/` to persist between restarts.
```

