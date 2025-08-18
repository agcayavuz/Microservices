
# 📦 CatalogService

**CatalogService**, Mikroservis Eğitimi kapsamında geliştirilen bir **ürün, marka ve kategori yönetim servisi**dir.  
Servis, **.NET 8**, **RabbitMQ** (mesajlaşma), **Elasticsearch + Kibana** (gözlemlenebilirlik/loglama) altyapılarını kullanır.  

---

## 🏗 Mimari Yapı

Proje **Clean Architecture** ve **Katmanlı Mimari** yaklaşımıyla tasarlanmıştır:

```
src/
 └── Microservices/
      └── services/
           └── CatalogService/
                ├── CatalogService.Api           → Minimal API + Middleware + Swagger
                ├── CatalogService.Application   → Business Logic (DTOs, Services, Validators)
                ├── CatalogService.Domain        → Entity & Domain Modelleri
                └── CatalogService.Infrastructure→ Persistence, RabbitMQ, Elasticsearch
```

### Katmanlar
- **Api** → Endpoint tanımları, Middleware, Exception Handling, HealthChecks  
- **Application** → Servisler, DTO modelleri, Validasyon  
- **Domain** → Temel entity tanımları (Product, Brand, Category)  
- **Infrastructure** → Veritabanı bağlantısı, RabbitMQ publisher/consumer, Elasticsearch loglama  

---

## ⚙️ Teknolojiler

- **.NET 8 (Minimal API)**
- **RabbitMQ** – Event publish/consume
- **Elasticsearch** – Log saklama
- **Kibana** – Log görselleştirme
- **Serilog (ECS Formatter)** – Structured logging
- **Scrutor** – Dependency Injection scanning
- **Docker Compose** – Servis orkestrasyonu

---

## 🚀 Çalıştırma Adımları

### 1. Docker Network oluştur
```bash
docker network create ms-net
```

### 2. Docker Compose ile altyapıyı ayağa kaldır
`docker/compose/` klasöründeki dosyaları sırayla çalıştırın:  
- PostgreSQL  
- RabbitMQ  
- Elasticsearch  
- Kibana  

Örnek:
```bash
cd docker/compose
docker compose --env-file ../env/postgres.env -f postgresql.yml up -d
```

⚠️ **Not**: Compose dosyaları bu repository içindedir.  
Sadece doğru sırayla çalıştırmanız gerekir.

### 3. CatalogService’i çalıştır
```bash
cd src/Microservices/services/CatalogService/CatalogService.Api
dotnet run
```

---

## 🔍 Kullanım

Swagger arayüzüne gidin:
```
https://localhost:7124/swagger
```

### Örnek Endpoint’ler
- `GET /api/products` → Tüm ürünler  
- `GET /api/brands` → Tüm markalar  
- `GET /api/categories` → Tüm kategoriler  
- `POST /api/products/{id}/view` → RabbitMQ’ya **ProductViewedEvent** publish eder  

---

## 🩺 Health Checks

Servis; **Elasticsearch** ve **RabbitMQ** bağlantılarını health check olarak expose eder:  

```bash
GET /health
```

---

## 📊 Observability

- Loglar **Elasticsearch**’e gönderilir.  
- Kibana UI → `http://localhost:5601`  
- Index pattern:  
  ```
  ms-catalogservice-*
  ```

---

## 👤 Geliştirici Rehberi

Daha fazla teknik detay için:  
📄 **CatalogService-DeveloperGuide.md**

---

## 📌 Katkıda Bulunma

1. Fork’la 🍴  
2. Yeni branch aç: `feature/xyz`  
3. Commit → Push → PR  

---

## 📄 Lisans

MIT License  
