# AirlineAPI

.NET Core ile geliştirilmiş, havayolu işletmeciliği temalı bir RESTful API projesi. THY temasıyla, uçak/uçuş hattı/uçuş/yolcu/bilet yönetimini kapsayan, katmanlı mimariyle (Controller-Service-DTO) tasarlanmış bir öğrenme projesidir.

## Özellikler

- **Entity Framework Core** ile SQL Server üzerinde ilişkisel veri modeli
- **JWT tabanlı Authentication/Authorization** (Admin / Passenger rolleri)
- Katmanlı mimari: Controller → Service → DTO/Mapping → Data
- Kapsamlı iş kuralı doğrulamaları (rota-numara eşleşmesi, uçak-hat tekilliği, kapasite kontrolü, vb.)
- Async/await ile veritabanı işlemleri
- Postman ile test edilmiş, dışa aktarılmış koleksiyon

## Kullanılan Teknolojiler

- .NET Core / ASP.NET Core Web API
- Entity Framework Core (Code-First, Migrations)
- SQL Server
- JWT (JSON Web Token) Authentication
- Swagger / OpenAPI

## Veri Modeli

- **Aircraft** — Uçak bilgileri (model, kapasite, tescil no, üretici)
- **Destination** — Hedef şehir ve uçuş numarası aralığı tanımları
- **FlightSchedule** — Sabit uçuş hattı tanımları (uçuş no, saat, fiyat, uçak ataması)
- **Flight** — Belirli bir tarihte gerçekleşen somut uçuş
- **Passenger** — Yolcu bilgileri
- **Ticket** — Yolcunun aldığı bilet
- **User** — Giriş yapan kullanıcı (Admin veya Passenger rolünde)

## Kurulum

### Gereksinimler
- .NET SDK
- SQL Server (LocalDB veya SQL Express)

### Adımlar

1. Repoyu klonlayın:
   ```
   git clone https://github.com/KULLANICI_ADIN/AirlineAPI.git
   cd AirlineAPI
   ```

2. Bağlantı dizesini kontrol edin (`appsettings.json` içindeki `ConnectionStrings:DefaultConnection`).

3. JWT anahtarını User Secrets ile tanımlayın:
   ```
   dotnet user-secrets init
   dotnet user-secrets set "Jwt:Key" "en-az-32-karakterlik-kendi-anahtariniz"
   ```

4. Veritabanını oluşturun:
   ```
   dotnet ef database update
   ```

5. Projeyi çalıştırın:
   ```
   dotnet run
   ```

6. Swagger arayüzü için: `https://localhost:<port>/swagger`

## API Kullanımı

Projeye ait Postman koleksiyonu `AirlineAPI.postman_collection.json` dosyasında bulunmaktadır. Postman'e import ederek tüm endpoint'leri test edebilirsiniz.

### Kimlik Doğrulama

1. `POST /api/Auth/register` ile kayıt olun (Role: `Admin` veya `Passenger`)
2. `POST /api/Auth/login` ile giriş yapıp token alın
3. Sonraki isteklerde `Authorization: Bearer <token>` header'ını ekleyin

## Yetkilendirme Özeti

| Kaynak | Görüntüleme | Ekleme/Düzenleme/Silme |
|---|---|---|
| Aircraft | Admin | Admin |
| FlightSchedule / Destination / Flight | Herkese açık | Admin |
| Passenger | Admin (kendi profili hariç) | Admin |
| Ticket | Admin + sahibi | Admin (+ sahibi sadece iptal edebilir) |
