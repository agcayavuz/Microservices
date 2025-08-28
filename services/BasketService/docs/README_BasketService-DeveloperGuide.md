# BasketService

Redis tabanlı sepet (alışveriş sepeti) mikroservisi, **.NET 8 Minimal API** ile geliştirilmiştir. 
Gözlemlenebilirlik ve operasyonel dayanıklılık için tasarlanmıştır.

- **Veri deposu:** Redis (her müşteri için JSON blob, TTL desteği)
- **Loglama:** Serilog (ECS formatı) → Elasticsearch/Kibana
- **Sağlık kontrolü:** `/health/live`, `/health/ready` (Elasticsearch + Redis)
- **Validasyon:** FluentValidation + otomatik 400 yanıtları
- **Eşzamanlılık:** Dağıtık kilit (SET NX + TTL) ile atomik güncellemeler
- **Correlation:** `X-Correlation-Id` header desteği

> Bu doküman geliştirici ve operatörler içindir. API özellikleri, yapılandırma, lokal geliştirme ipuçları ve yol haritası içerir.

---

## İçindekiler
- [Mimari Genel Bakış](#mimari-genel-bakış)
- [Domain & Veri Modeli](#domain--veri-modeli)
- [Konfigürasyon](#konfigürasyon)
- [Lokal Çalıştırma](#lokal-çalıştırma)
- [Sağlık ve Gözlemlenebilirlik](#sağlık-ve-gözlemlenebilirlik)
- [API Referansı](#api-referansı)
- [Validasyon Kuralları](#validasyon-kuralları)
- [Eşzamanlılık Garantileri](#eşzamanlılık-garantileri)
- [Sorun Giderme](#sorun-giderme)
- [Güvenlik Notları](#güvenlik-notları)
- [Yol Haritası](#yol-haritası)
- [Lisans](#lisans)

---

## Mimari Genel Bakış

```text
Client/UI
   │
HTTP (JSON, Minimal API)
   │
BasketService.Api
   ├─ Middleware
   │   ├─ CorrelationIdMiddleware
   │   ├─ CorrelationLogEnricherMiddleware
   │   ├─ ExceptionHandlingMiddleware
   │   └─ RequestResponseLoggingMiddleware
   ├─ Health (ElasticsearchHealthCheck, RedisHealthCheck)
   ├─ Validation (FluentValidation + endpoint filter)
   └─ Endpoints (Minimal API)
       └─ IBasketService  ⇢  RedisBasketService (Infrastructure)
                               └─ Redis (StackExchange.Redis)
                               └─ DistributedLock (SET NX + TTL)
```

- **Onion/Clean Architecture**
  - `BasketService.Api` – endpointler, middleware’ler, health check
  - `BasketService.Application` – arayüzler, opsiyonlar, validator’lar
  - `BasketService.Contracts` – DTO ve Request modelleri
  - `BasketService.Infrastructure` – Redis implementasyonu

---

## Domain & Veri Modeli

**Sepet (müşteri başına):**
```jsonc
{
  "customerId": "c1",
  "items": [
    { "productId": "armut", "productName": "Armut", "unitPrice": 10, "quantity": 2 }
  ],
  "totalAmount": 20
}
```

- **Redis key formatı:** `basket:{customerId}`  
- **TTL yenileme:** Her yazma işleminde TTL güncellenir.  
- **Otomatik silme:** Son ürün kaldırıldığında ve `AutoDeleteEmptyOnItemRemove=true` olduğunda key silinir.

---

## Konfigürasyon

`appsettings.json` (temel kısımlar):

```jsonc
{
  "Serilog": { "Elasticsearch": { "Uri": "http://localhost:9200" } },
  "Redis": {
    "ConnectionString": "localhost:6379,password=Redis!123,ssl=False,abortConnect=False",
    "DefaultTtlDays": 30,
    "KeyPrefix": "basket",
    "Lock": {
      "ExpirySeconds": 5,
      "RetryMs": 50,
      "MaxWaitMs": 1000
    }
  },
  "Basket": {
    "AutoDeleteEmptyOnItemRemove": true,
    "CreateOrReplaceBehavior": "Merge" // veya "Replace"
  }
}
```

Kestrel limitleri (önerilen):
```jsonc
"Kestrel": {
  "Limits": {
    "MaxRequestBodySize": 262144 // 256 KB
  }
}
```

---

## Lokal Çalıştırma

1. **Ön Gereksinimler**
   - .NET 8 SDK
   - Docker (Redis, Elasticsearch, Kibana önerilir)

2. **Redis (Docker):**
   ```bash
   docker run -d --name redis -p 6379:6379 redis:7.2-alpine \
     sh -c "redis-server --requirepass 'Redis!123'"
   ```

3. **Elasticsearch & Kibana (opsiyonel):** Elastic’in resmi imajları kullanılabilir.

4. **Servisi çalıştır:**
   ```bash
   dotnet restore
   dotnet run --project src/Microservices/services/BasketService/BasketService.Api
   ```

5. **Swagger UI:** https://localhost:7258/swagger

---

## Sağlık ve Gözlemlenebilirlik

- **Health endpointleri**
  - `GET /health/live` → her zaman 200 döner (sadece liveness için).
  - `GET /health/ready` → Elasticsearch + Redis check’lerini yapar.

- **Loglama**
  - Serilog (ECS formatında) → Console + Elasticsearch sink
  - `UseSerilogRequestLogging()` + custom request/response loglama
  - Tüm yanıtlar `"traceId"` (CorrelationId) içerir.

- **Correlation**
  - Header: `X-Correlation-Id` (yoksa otomatik üretilir)
  - Loglara ve response’lara eklenir

---

## API Referansı

Tüm yanıtlar şu formatta döner:
```jsonc
{
  "success": true|false,
  "data": { ... },            
  "error": {                  
    "code": "string",
    "message": "string",
    "details": {}
  },
  "traceId": "string"
}
```

### Endpointler

| Method & Path | Açıklama | Notlar |
| --- | --- | --- |
| `GET /api/v1/baskets/{customerId}` | Sepeti getir | 200 veya 404 |
| `POST /api/v1/baskets` | **Sepet oluştur/merge** | Varsa miktarı artırır, yoksa ekler |
| `PUT /api/v1/baskets/{customerId}/items` | Ürün listesini overwrite/set eder | İdempotent |
| `DELETE /api/v1/baskets/{customerId}` | Sepeti sil | 200 veya 404 |
| `DELETE /api/v1/baskets/{customerId}/items/{productId}` | Sepetten ürün kaldır | Son ürünse sepeti de siler |
| `POST /api/v1/baskets/{customerId}/items/{productId}:increase` | Ürün adedini artır | Body: `{ "quantity": N>0 }` |
| `POST /api/v1/baskets/{customerId}/items/{productId}:decrease` | Ürün adedini azalt | Adet ≤0 olursa kaldırır |

---

## Validasyon Kuralları

- **CreateOrReplaceBasketRequest**
  - `CustomerId` boş olamaz
  - `Items` boş olamaz
  - Her item: `ProductId`, `ProductName` boş olamaz, `Quantity>0`, `UnitPrice>=0`

- **UpsertBasketItemsRequest**
  - `Items` boş olamaz

- **ChangeQuantityRequest**
  - `Quantity` > 0

---

## Eşzamanlılık Garantileri

- **Atomik güncellemeler** dağıtık kilit ile:
  - Kilit key: `basket:{customerId}:lock`
  - Uygulama: `SET key token NX PX <expiry>` → Lua ile release
  - Kullanım: merge (POST), increase, decrease, remove
- **Overwrite (PUT)** idempotent, gerekirse kilit kullanılabilir.
- **Planlanan:** Optimistic concurrency (ETag / If-Match).

---

## Sorun Giderme

- **RedisInsight’ta key göremiyorum**
  - Doğru host/port/password ile bağlan, filter: `basket:*`
- **Redis down olduğunda API hata veriyor**
  - `/health/ready` Unhealthy olacak
  - Middleware 503 `"cache_unavailable"` döner
- **Concurrency kayıpları**
  - Kilit ayarlarını kontrol et (`ExpirySeconds`, `RetryMs`, `MaxWaitMs`).

---

## Güvenlik Notları

- Sepette hassas kullanıcı verisi taşımayın.
- Public API olarak açılacaksa rate limiting ve TLS termination gerekli.
- Gerçek ortamda müşteri doğrulaması (JWT vs.) yapılmalı.

---

## Yol Haritası

- **Optimistic Concurrency**: `ETag` + `If-Match` → 409 Conflict kontrolü
- **Checkout Flow**: `POST /baskets/{customerId}:checkout` → `BasketCheckedOutEvent` (RabbitMQ)
- **Outbox/Inbox Pattern**
- **Rate Limiting** + item sayısı limiti (örn. max 200)
- **Testler**: xUnit + Testcontainers (Redis)

---

## Lisans

MIT (veya organizasyonunuzun standart lisansı)
