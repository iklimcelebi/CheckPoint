# CheckPoint

CheckPoint, öğrencilerin üniversitelerine ve derslerine göre **ders notu, sunum, çalışma sorusu ve geçmiş sınav sorusu** paylaştığı bir eğitim kaynakları platformudur. Kullanıcılar kaynakları üniversite, ders, tür ve metinle arar; dosyaları indirir ve yeni kaynak yükler. Arayüz Türkçedir.

Proje bir ders ödevi olarak geliştirilmiştir ve **yerel bilgisayarda (Windows + SQL Server LocalDB)** çalışacak şekilde yapılandırılmıştır.

## İçindekiler

- [Özellikler](#özellikler)
- [Mimari](#mimari)
- [Gereksinimler](#gereksinimler)
- [Sıfırdan kurulum](#sıfırdan-kurulum)
- [Demo hesapları](#demo-hesapları)
- [Yapılandırma](#yapılandırma)
- [API uç noktaları](#api-uç-noktaları)
- [Dosya yükleme kuralları](#dosya-yükleme-kuralları)
- [Testler](#testler)
- [Güvenlik](#güvenlik)
- [Sık karşılaşılan sorunlar](#sık-karşılaşılan-sorunlar)
- [Bilinen sınırlamalar ve sonraki adımlar](#bilinen-sınırlamalar-ve-sonraki-adımlar)

Sunum için adım adım senaryo: [docs/DEMO.md](docs/DEMO.md)

## Özellikler

- Kayıt, giriş, çıkış, profil ve parola değiştirme (JWT ile kimlik doğrulama, 60 dakikalık oturum).
- Kaynak listesi: üniversite, ders, tür ve metin filtresi; sayfalama; filtreler adres çubuğunda saklanır.
- Kaynak ayrıntısı ve indirme (Türkçe dosya adları korunur).
- Kaynak yükleme (ilerleme çubuğuyla), düzenleme (başlık, açıklama, tür) ve silme.
- "Benim Kaynaklarım" sayfası.
- Üniversiteler (listeleme herkese; ekleme, düzenleme, silme yalnızca yöneticiye) ve dersler (herkes ekleyip düzenleyebilir).
- Rol tabanlı yetki: `User` ve `Admin`. Bir kaynağı yalnızca yükleyen kişi veya yönetici düzenleyip silebilir.
- Tüm hata mesajları Türkçe; arama Türkçe karakterlere duyarsızdır ("izmir" → "İzmir", "universite" → "Üniversite").

## Mimari

```
┌──────────────────────────┐   HTTP + JSON (JWT)   ┌───────────────────────────┐       ┌──────────────────┐
│ CheckPoint.Web           │ ────────────────────▶ │ CheckPoint.Api            │ ────▶ │ SQL Server       │
│ React + TypeScript (Vite)│ ◀──────────────────── │ ASP.NET Core Web API      │       │ LocalDB          │
│ http://localhost:5173    │   ProblemDetails      │ http://localhost:5118     │       │ CheckPointDb     │
└──────────────────────────┘                       └─────────────┬─────────────┘       └──────────────────┘
                                                                 │ dosyalar (GUID adıyla)
                                                                 ▼
                                                   CheckPoint.Api/uploads/
```

| Klasör | İçerik |
|---|---|
| `CheckPoint.Api/` | .NET 10 Web API: `Controllers` (uç noktalar), `Dtos` (istek/yanıt modelleri ve doğrulama), `Models` + `Data` (EF Core, migration'lar, admin ve demo verisi), `Services` (dosya depolama), `Middleware`, `Filters`, `OpenApi` (Swagger) |
| `CheckPoint.Api.Tests/` | xUnit entegrasyon testleri (her test kendi LocalDB veritabanını kullanır) |
| `CheckPoint.Web/` | React 19 + TypeScript (strict) + Vite: `src/api` (tek fetch sarmalayıcısı, OpenAPI'den üretilmiş tipler), `src/features/*` (sayfalar), `src/components` (ortak bileşenler), `src/routes` (yönlendirme ve yetki korumaları), `src/lib` |
| `CheckPoint.Web/e2e/` | Playwright uçtan uca testleri |
| `docs/` | Sunum senaryosu |

**Teknolojiler:**
- **Backend:** ASP.NET Core 10 (controller tabanlı), Entity Framework Core 10 (Code First, migration'lar), SQL Server LocalDB, JWT (HS256), BCrypt, Swagger UI.
- **Frontend:** React 19, TypeScript, Vite 8, React Router, TanStack Query, react-hook-form, zod, Tailwind CSS.
- **Testler:** xUnit, Vitest, React Testing Library, MSW, Playwright.

## Gereksinimler

| Araç | Sürüm | Kontrol komutu |
|---|---|---|
| Git | herhangi | `git --version` |
| .NET SDK | **10.0** (10.0.404 veya daha yeni bir 10.0 sürümü) | `dotnet --list-sdks` → listede `10.0.` ile başlayan bir satır olmalı |
| Entity Framework aracı | 10.x | `dotnet tool list -g` → `dotnet-ef` |
| SQL Server LocalDB | 2019 veya daha yeni | `sqllocaldb info` → `MSSQLLocalDB` |
| Node.js | 22 LTS veya 24 LTS (en az 20.19) | `node -v` |

Eksik olanları kurmak için:

- .NET 10 SDK: `winget install Microsoft.DotNet.SDK.10` (veya https://dotnet.microsoft.com/download)
- EF aracı: `dotnet tool install --global dotnet-ef --version 10.0.*` (kuruluysa `update` ile)
- LocalDB: Visual Studio ile gelir; ayrıca "SQL Server Express LocalDB" olarak kurulabilir.
- Node.js: `winget install OpenJS.NodeJS.LTS`

## Sıfırdan kurulum

Komutlar **PowerShell** içindir. Git Bash'te farklı olan tek komut ayrıca belirtilmiştir. Aksi yazmadıkça komutlar **depo kök klasöründe** (`CheckPoint`) çalıştırılır.

### 1. Depoyu klonlayın

```powershell
git clone https://github.com/iklimcelebi/CheckPoint.git
cd CheckPoint
```

### 2. Sırları ayarlayın (user-secrets)

Sırlar depoda **yoktur**; her geliştirici kendi bilgisayarında `dotnet user-secrets` ile ayarlar.

JWT imzalama anahtarı (48 rastgele bayt):

```powershell
$bytes = New-Object byte[] 48
[System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
dotnet user-secrets set "Jwt:Key" ([Convert]::ToBase64String($bytes)) --project CheckPoint.Api
```

Git Bash kullanıyorsanız:

```bash
dotnet user-secrets set "Jwt:Key" "$(openssl rand -base64 48)" --project CheckPoint.Api
```

İlk yönetici (Admin) hesabı. E-postayı ve parolayı kendiniz belirleyin; parola **8–64 karakter olmalı, en az bir harf ve bir rakam içermelidir**:

```powershell
dotnet user-secrets set "SeedAdmin:Email" "admin@example.com" --project CheckPoint.Api
dotnet user-secrets set "SeedAdmin:Password" "<kendi-parolanız>" --project CheckPoint.Api
```

Yönetici hesabı, uygulama ilk açıldığında veritabanında hiç Admin yoksa oluşturulur. Parolayı daha sonra değiştirmek için uygulamada **Profil → Parolayı değiştir** bölümünü kullanın; `SeedAdmin:Password` değerini sonradan değiştirmek mevcut hesabı etkilemez.

### 3. Derleyin ve veritabanını oluşturun

```powershell
dotnet build
dotnet ef database update --project CheckPoint.Api
```

İlk komut NuGet paketlerini indirir ve projeyi derler; yeni klonlanmış bir depoda bu yapılmadan `dotnet ef` "project.assets.json not found" hatası verir. İkinci komut `CheckPointDb` veritabanını LocalDB'de oluşturur ve tabloları kurar. (Alternatif: `dotnet user-secrets set "Database:AutoMigrate" "true" --project CheckPoint.Api` ayarıyla migration'lar uygulama açılırken otomatik uygulanır; yalnızca Development ortamında çalışır.)

### 4. (İsteğe bağlı) Demo verisini açın

```powershell
dotnet user-secrets set "Seed:DemoData" "true" --project CheckPoint.Api
```

Uygulama açılışta 3 üniversite, 7 ders, 2 demo kullanıcı ve 7 örnek kaynak ekler (örnek dosyalar kod içinde üretilir, telifsizdir). Tekrar açıldığında kayıtlar çoğalmaz. Kapatmak için: `dotnet user-secrets remove "Seed:DemoData" --project CheckPoint.Api`.

### 5. Backend'i çalıştırın (1. terminal)

```powershell
dotnet run --project CheckPoint.Api
```

Konsolda `Now listening on: http://localhost:5118` görünmelidir. Bu terminali açık bırakın.

- Swagger (API belgesi ve deneme arayüzü): http://localhost:5118/swagger
- Sağlık kontrolü: http://localhost:5118/health (uygulama), http://localhost:5118/health/ready (veritabanı dahil)

### 6. Frontend'i çalıştırın (2. terminal)

```powershell
cd CheckPoint.Web
npm install
npm run dev
```

Tarayıcıda **http://localhost:5173** adresini açın. API farklı bir adreste çalışıyorsa `CheckPoint.Web/.env.example` dosyasını `.env` adıyla kopyalayıp `VITE_API_BASE_URL` değerini değiştirin.

## Demo hesapları

Yalnızca 4. adımdaki demo verisi açıksa vardır. **Bu parolalar yalnızca demo içindir.**

| Rol | E-posta | Parola |
|---|---|---|
| User | `ayse.demo@example.com` | `DemoParola1` |
| User | `mehmet.demo@example.com` | `DemoParola1` |
| Admin | 2. adımda `SeedAdmin:Email` olarak belirlediğiniz e-posta | user-secrets'ta belirlediğiniz parola |

## Yapılandırma

Ayarlar şu sırayla birleştirilir (sonraki öncekini ezer): `appsettings.json` → `appsettings.Development.json` → user-secrets → ortam değişkenleri (`:` yerine `__`, ör. `Jwt__Key`).

| Anahtar | Sır mı? | Varsayılan / kaynak | Açıklama |
|---|---|---|---|
| `Jwt:Key` | **Evet** | user-secrets | JWT imzalama anahtarı, en az 32 karakter. Eksikse uygulama açılmaz. |
| `SeedAdmin:Email` | Kişisel veri | user-secrets | İlk yöneticinin e-postası (isteğe bağlı) |
| `SeedAdmin:Password` | **Evet** | user-secrets | İlk yöneticinin parolası (isteğe bağlı) |
| `ConnectionStrings:DefaultConnection` | Hayır (LocalDB, parolasız) | `appsettings.Development.json` | `(localdb)\MSSQLLocalDB`, veritabanı `CheckPointDb` |
| `Jwt:Issuer` / `Jwt:Audience` / `Jwt:ExpiryMinutes` | Hayır | `CheckPoint.Api` / `CheckPoint.Client` / `60` | Token ayarları |
| `FileStorage:RootPath` | Hayır | `uploads` | Dosyaların saklandığı klasör (`CheckPoint.Api/uploads`, Git'e girmez) |
| `FileStorage:MaxFileSizeMB` | Hayır | `20` | En büyük dosya boyutu |
| `Kestrel:Limits:MaxRequestBodySize` | Hayır | `1048576` (1 MB) | Yükleme dışındaki isteklerin gövde sınırı |
| `RateLimiting:Auth:PermitLimit` / `WindowSeconds` | Hayır | `10` / `60` | Giriş ve kayıt için IP başına dakikada en fazla 10 istek |
| `Cors:AllowedOrigins` | Hayır | Development: `http://localhost:5173` | Tarayıcıdan API'ye erişebilecek adresler |
| `Database:AutoMigrate` | Hayır | `false` | `true` ise migration'lar açılışta uygulanır (yalnızca Development) |
| `Seed:DemoData` | Hayır | `false` | `true` ise demo verisi eklenir (yalnızca Development) |
| `VITE_API_BASE_URL` (frontend) | Hayır | `http://localhost:5118` | Frontend'in bağlandığı API adresi |

Zorunlu ayarlardan biri eksik veya hatalıysa uygulama açılmaz ve eksiklerin hepsini tek bir Türkçe mesajda listeler.

## API uç noktaları

`register`, `login` ve sağlık kontrolleri dışındaki tüm uç noktalar `Authorization: Bearer <token>` başlığı ister. Token yoksa veya süresi dolmuşsa **401** döner. Ayrıntılı belge ve deneme için: http://localhost:5118/swagger

| Yöntem | Adres | Yetki | Açıklama | Başlıca hata kodları |
|---|---|---|---|---|
| POST | `/api/auth/register` | Herkes | Kayıt (herkes `User` olur) | 400, 409, 429 |
| POST | `/api/auth/login` | Herkes | Giriş → `{ token, expiresAt, user }` | 400, 401, 429 |
| GET | `/api/users/me` | Giriş yapmış | Kendi bilgileri | 401 |
| GET | `/api/users/me/resources` | Giriş yapmış | Kendi yüklediği kaynaklar (sayfalı) | 400 |
| PUT | `/api/users/me/password` | Giriş yapmış | Kendi parolasını değiştir (`{ currentPassword, newPassword }`) | 400 (mevcut parola hatalı / kural), 429 |
| GET | `/api/universities?q=&page=&pageSize=` | Giriş yapmış | Üniversite listesi (sayfalı, arama) | 400 |
| GET | `/api/universities/{id}` | Giriş yapmış | Üniversite ayrıntısı | 404 |
| POST | `/api/universities` | **Admin** | Üniversite ekle | 400, 403, 409 |
| PUT | `/api/universities/{id}` | **Admin** | Üniversite güncelle | 400, 403, 404, 409 |
| DELETE | `/api/universities/{id}` | **Admin** | Üniversite sil (dersi varsa silinemez) | 403, 404, **409** |
| GET | `/api/courses?universityId=&q=&page=&pageSize=` | Giriş yapmış | Ders listesi | 400 |
| GET | `/api/courses/{id}` | Giriş yapmış | Ders ayrıntısı | 404 |
| POST | `/api/courses` | Giriş yapmış | Ders ekle (kod büyük harfe çevrilir) | 400, 409 |
| PUT | `/api/courses/{id}` | Giriş yapmış | Ders adı/kodu güncelle (üniversite değişmez) | 400, 404, 409 |
| GET | `/api/resources?universityId=&courseId=&type=&q=&page=&pageSize=` | Giriş yapmış | Kaynak listesi (en yeni önce) | 400 |
| GET | `/api/resources/{id}` | Giriş yapmış | Kaynak ayrıntısı | 404 |
| GET | `/api/resources/{id}/download` | Giriş yapmış | Dosyayı indir | 404, 503 |
| POST | `/api/resources` | Giriş yapmış | Kaynak yükle (`multipart/form-data`) | 400, **413**, **415**, 503 |
| PUT | `/api/resources/{id}` | **Yükleyen veya Admin** | Başlık, açıklama, tür güncelle | 400, **403**, 404 |
| DELETE | `/api/resources/{id}` | **Yükleyen veya Admin** | Kaynağı ve dosyasını sil | **403**, 404, 503 |
| GET | `/health`, `/health/ready` | Herkes | Sağlık kontrolü (`Healthy` / `Unhealthy`) | 503 |

- **Sayfalı listeler:** `{ items, page, pageSize, totalCount }` döner. Varsayılan `page=1`, `pageSize=20`, en fazla 100.
- **Kaynak türleri:** `LectureNote` (Ders Notu), `Presentation` (Sunum), `StudyQuestion` (Çalışma Sorusu), `PastExamQuestion` (Geçmiş Sınav Sorusu).
- **Hata yanıtları** [ProblemDetails](https://www.rfc-editor.org/rfc/rfc9457) biçimindedir ve Türkçedir:

```json
{
  "title": "Çakışma",
  "status": 409,
  "detail": "Bu üniversiteye bağlı dersler olduğu için silinemez."
}
```

## Dosya yükleme kuralları

- İzin verilen uzantılar: `.pdf .doc .docx .ppt .pptx .txt .png .jpg .jpeg`
- En büyük boyut: **20 MB** → aşılırsa **413**.
- Boş dosya → **400**.
- Uzantı, içerik türü (Content-Type) ve dosyanın ilk baytları (PDF, PNG, JPG, DOCX, PPTX imzası) birbiriyle uyuşmalıdır; aksi hâlde **415** (ör. `.pdf` uzantılı bir metin dosyası reddedilir).
- Seçilen ders, seçilen üniversiteye ait olmalıdır → aksi hâlde **400**.
- Dosyalar diske rastgele (GUID) bir adla kaydedilir; kullanıcının verdiği ad yalnızca indirmede kullanılır ve temizlenir. `uploads` klasörü doğrudan web'e açık değildir; indirme yalnızca yetkili API isteğiyle yapılır.
- Arayüz aynı kuralları göndermeden önce de denetler; asıl denetim sunucudadır.

## Testler

| Test | Komut (klasör) | Ne test eder | Sayı |
|---|---|---|---|
| Backend | `dotnet test CheckPoint.sln` (kök) | Tüm uç noktalar, yetkiler, doğrulama, dosya kuralları, hız sınırı, Türkçe arama, parola değiştirme, demo verisi | 225 |
| Frontend birim/bileşen | `npm test` (`CheckPoint.Web`) | Formlar, korumalı rotalar, filtreler, yükleme doğrulamaları, rol bazlı düğmeler, parola değiştirme | 68 |
| Uçtan uca | `npm run test:e2e` (`CheckPoint.Web`) | Gerçek API + tarayıcı: kayıt → giriş → üniversite → ders → yükleme → arama → indirme → düzenleme → silme; başkasının kaynağına 403; hata ekranları; parola değiştirme | 5 |

- Backend testleri ve uçtan uca testler kendi geçici LocalDB veritabanlarını oluşturur ve sonunda siler; `CheckPointDb`'ye dokunmaz.
- Uçtan uca testler user-secrets gerektirmez (anahtar ve admin parolası her çalıştırmada rastgele üretilir). Çalıştığınız API (5118) açık olsa bile 5199 ve 5174 portlarını kullanır.
- Uçtan uca testlerden önce bir kez tarayıcıyı indirin: `npx playwright install chromium`.
- Kod kalitesi: `npm run lint` (0 uyarı), `npm run build`.

## Güvenlik

- **Parolalar:** BCrypt ile saklanır. Parola kuralı: 8–64 karakter, en az bir harf ve bir rakam, en fazla 72 bayt (BCrypt sınırı).
- **Token'lar:** JWT 60 dakika geçerlidir ve tarayıcıda `localStorage`'da tutulur; süre dolunca kullanıcı giriş sayfasına yönlendirilir. Kullanıcı silinirse veya rolü değişirse eski token hemen geçersizleşir.
- **Kaba kuvvet koruması:** Giriş ve kayıt IP başına dakikada 10 istekle sınırlıdır; aşılırsa **429** ve `Retry-After` döner.
- **Güvenlik başlıkları:** `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, `Content-Security-Policy`.
- **CORS:** Yalnızca `Cors:AllowedOrigins` listesindeki adresler.
- **Günlükler:** Her istek tek satır olarak (yöntem, yol, durum, süre, kullanıcı kimliği) kaydedilir; parola, token, e-posta ve sorgu metni günlüğe yazılmaz.
- **Sırlar:** Repoda gerçek sır yoktur. `Jwt:Key` değiştirilirse eski token'ların hepsi geçersiz olur ve herkes yeniden giriş yapmalıdır.

## Sık karşılaşılan sorunlar

| Belirti | Çözüm |
|---|---|
| Uygulama açılmıyor: "Uygulama yapılandırması eksik veya geçersiz: Jwt:Key ..." | 2. adımdaki `Jwt:Key` komutunu çalıştırın. Komutu kök klasörde ve `--project CheckPoint.Api` ile verdiğinizden emin olun. |
| "A network-related or instance-specific error ... LocalDB" | `sqllocaldb start MSSQLLocalDB` çalıştırın; `sqllocaldb info MSSQLLocalDB` ile durumu kontrol edin. |
| Açılışta `Cannot open database "CheckPointDb" requested by the login`, veya kayıt/girişte "Sunucu hatası" | Veritabanı oluşturulmamış: 3. adımı (`dotnet ef database update --project CheckPoint.Api`) çalıştırın. |
| `dotnet ef database update`: "Assets file ... project.assets.json not found" | Önce kök klasörde `dotnet build` çalıştırın (3. adım). |
| `dotnet ef` bulunamadı | `dotnet tool install --global dotnet-ef --version 10.0.*`, ardından terminali yeniden açın. |
| "The Entity Framework tools version ... is older" uyarısı | Zararsızdır; `dotnet tool update --global dotnet-ef --version 10.0.*` ile giderilir. |
| Build hatası: "file is locked by: CheckPoint.Api" | API zaten çalışıyor. Çalıştığı terminalde `Ctrl+C` ile durdurup tekrar deneyin. |
| "Failed to bind to address ... address already in use" | 5118 veya 5173 portunu başka bir uygulama kullanıyor; o uygulamayı kapatın. |
| Arayüzde "Sunucuya ulaşılamadı" | Backend çalışmıyor veya farklı adreste. 1. terminali kontrol edin; gerekirse `.env` içinde `VITE_API_BASE_URL` ayarlayın. |
| Tarayıcı konsolunda CORS hatası | Frontend'i 5173 dışında bir portta açtıysanız o adresi `Cors:AllowedOrigins` listesine ekleyin. |
| `npm install` "ENOENT package.json" | Komutu `CheckPoint.Web` klasöründe çalıştırın. Kök klasörde yanlışlıkla oluşan `package-lock.json` dosyasını silin. |
| Giriş: "Çok fazla deneme yaptınız" (429) | Bir dakika bekleyin (IP başına dakikada 10 deneme). |
| Demo hesaplarıyla giriş yapılamıyor | 4. adımda demo verisini açıp API'yi yeniden başlatın; konsolda `Demo data: ...` satırı görünmelidir. |
| Admin hesabıyla giriş yapılamıyor | Admin yalnızca veritabanında hiç Admin yokken oluşturulur. user-secrets'taki parolayı sonradan değiştirdiyseniz veritabanındaki hesap eski parolayla kalır; eski parolayla girip **Profil → Parolayı değiştir** bölümünden değiştirin. |
| `npm run test:e2e`: "Executable doesn't exist" | `npx playwright install chromium` çalıştırın. |

## Bilinen sınırlamalar ve sonraki adımlar

**Bu sürümde bilinçli olarak yer almayanlar:**

- Yenileme token'ı (refresh token) yok; 60 dakika sonra yeniden giriş gerekir.
- E-posta doğrulama ve "parolamı unuttum" (e-postayla sıfırlama) yok; giriş yapmış kullanıcı parolasını profilinden değiştirebilir. Parola değişince açık oturumlar süreleri dolana kadar geçerli kalır.
- Kullanıcıları listeleme, devre dışı bırakma veya silme için arayüz ve uç nokta yok.
- Ders silme yok; dersi olan üniversite silinemez.
- Dosya ve dersi değiştirmek için kaynağın silinip yeniden yüklenmesi gerekir.
- Dosyalar yerel diskte tutulur (bulut depolama yok). Virüs taraması için bir genişleme noktası (`IFileScanner`) vardır, ancak tarayıcı bağlı değildir.
- Hız sınırı ve oturum durumu tek sunucu içindir; uygulama canlı ortama dağıtılmak üzere yapılandırılmamıştır (Production ayarları, HTTPS sertifikası, Docker yoktur).
- Açılır listeler en fazla 100 üniversite/ders gösterir.
- Yalnızca Windows + LocalDB üzerinde test edilmiştir.

**Olası sonraki sürüm özellikleri:** refresh token, e-posta doğrulama ve parola sıfırlama, içerik moderasyonu ve kaynak şikâyeti, beğeni/yorum ve puanlama, yönetici için kullanıcı yönetimi, ders silme, bulut depolama ve canlı ortam dağıtımı.
