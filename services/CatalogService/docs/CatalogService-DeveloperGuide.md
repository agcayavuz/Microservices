
# CatalogService Developer Guide

Bu doküman, **CatalogService** mikroservisini geliştirmek, çalıştırmak ve bakımını yapmak isteyen geliştiriciler için hazırlanmıştır.

---

## 📂 Proje Mimarisi

CatalogService, **Clean Architecture / Onion Architecture** prensipleri ile tasarlanmıştır:

```
src/
 └── Microservices/
      └── services/
           └── CatalogService/
                ├── CatalogService.Api           # Minimal API, Middleware, HealthChecks
                ├── CatalogService.Application   # İş kuralları, DTO'lar, Servisler
                ├── CatalogService.Domain        # Entity modelleri, Domain kuralları
                └── CatalogService.Infrastructure # RabbitMQ, Elasticsearch, Persistence
```

### Katmanlar

- **Api** → HTTP endpoint'leri, middleware, swagger, health check.
- **Application** → DTO modelleri, servis arabirimleri (IProductService vb.).
- **Domain** → İş kurallarını temsil eden entity’ler (Product, Brand, Category).
- **Infrastructure** → Dış bağımlılıklar (RabbitMQ, Elasticsearch, DB provider).

---

## 🚀 Çalıştırma Adımları

1. **Docker Servislerini Ayağa Kaldır**  
   - `docker/compose/elasticsearch.yml`
   - `docker/compose/kibana.yml`
   - `docker/compose/rabbitmq.yml`

   Öncelikle Docker network oluştur:  
   ```bash
   docker network create ms-net
   ```

   Ardından ilgili servisleri sırayla ayağa kaldır:  
   ```bash
   docker compose --env-file env/elastic.env -f docker/compose/elasticsearch.yml up -d
   docker compose --env-file env/kibana.env -f docker/compose/kibana.yml up -d
   docker compose --env-file env/rabbitmq.env -f docker/compose/rabbitmq.yml up -d
   ```

   > Not: Compose dosyaları `docker/compose` klasörü altındadır.

2. **Servisi Çalıştır**  
   ```bash
   cd src/Microservices/services/CatalogService/CatalogService.Api
   dotnet run
   ```

3. **Swagger UI'ı Aç**  
   ```
   https://localhost:7124/swagger
   ```

---

## 🧪 Health Checks

- **Tüm servisler için endpoint:**  
  ```
  GET https://localhost:7124/health
  ```

- Bu endpoint, **Elasticsearch** ve **RabbitMQ** bağlantılarını da kontrol eder.

---

## 🐇 RabbitMQ Kullanımı

- Event publish etmek için:  
  ```http
  POST /api/products/{id}/view
  ```

  Bu çağrı sonucunda **ProductViewedEvent** mesajı RabbitMQ exchange’ine gönderilir.  
  Queue adı: `ms.catalog.events`  
  Routing key formatı: `catalog.product.{id}.viewed`

---

## 📊 Elasticsearch & Kibana

- Loglar **Serilog ECS (Elastic Common Schema)** formatında Elasticsearch'e yazılır.  
- Kibana'da **Data Views → ms-catalogservice-*** oluşturularak loglar izlenebilir.

---

## 📌 Geliştirici Notları

- **CorrelationId Middleware** → Her request için `X-Correlation-Id` başlığı eklenir.  
- **Serilog** → Hem console hem Elasticsearch sink ile loglama yapılır.  
- **HealthChecks** → Elastic ve RabbitMQ bağımlılıkları runtime'da kontrol edilir.  
- **Middleware Pipeline**:  
  1. CorrelationIdMiddleware  
  2. CorrelationLogEnricherMiddleware  
  3. ExceptionHandlingMiddleware  

---

## 🔮 Gelecek Geliştirmeler

- PostgreSQL veritabanı entegrasyonu  
- Rate Limiting & Throttling  
- Monitoring & Alerting (Grafana / Prometheus)  

---

## 👨‍💻 Katkıda Bulunma

1. Fork → Branch → Commit → PR aç.  
2. Commit mesajlarını **anlamlı** yaz.  
3. Kod standartları: C# 12, .NET 9, Clean Architecture.

