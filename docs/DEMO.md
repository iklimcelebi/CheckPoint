# CheckPoint — Sunum Senaryosu (8–10 dakika)

Bu belge, projeyi canlı olarak göstermek için adım adım bir akış ve olası sorulara kısa cevaplar içerir. Kurulum için: [README.md](../README.md)

## Sunumdan önce (5 dakika hazırlık)

1. Demo verisini açın (bir kez yeterli):
   ```powershell
   dotnet user-secrets set "Seed:DemoData" "true" --project CheckPoint.Api
   ```
2. **1. terminal** (kök klasör): `dotnet run --project CheckPoint.Api` → konsolda `Demo data: ...` ve `Now listening on: http://localhost:5118`.
3. **2. terminal** (`CheckPoint.Web`): `npm run dev` → http://localhost:5173
4. Tarayıcıda iki pencere açın:
   - **Normal pencere:** http://localhost:5173 (öğrenci olarak kullanılacak).
   - **Gizli pencere:** http://localhost:5173 (yönetici olarak kullanılacak; oturumlar karışmaz).
   - Ayrı bir sekmede http://localhost:5118/swagger
5. Masaüstünde iki dosya hazırlayın:
   - `notlar.txt` — içine birkaç satır yazın.
   - `sahte.pdf` — Not Defteri'nde bir metin yazıp **`.pdf` uzantısıyla** kaydedin (gerçek PDF değil).
6. Yönetici e-postası ve parolası elinizin altında olsun (user-secrets'ta belirlediğiniz `SeedAdmin` bilgileri).

## Akış

| Süre | Ekran | Eylem | Beklenen sonuç / anlatılacak nokta |
|---|---|---|---|
| 0:00 | — | Projeyi tanıtın | Öğrencilerin üniversite/ders bazında not, sunum, çalışma sorusu ve geçmiş sınav sorusu paylaştığı platform. Arka uç ASP.NET Core 10 + EF Core + SQL Server LocalDB; ön yüz React + TypeScript. |
| 0:45 | Normal pencere → Giriş | `ayse.demo@example.com` ile **yanlış parola** | **401:** "E-posta veya parola hatalı." (Hangisinin yanlış olduğu bilerek söylenmez.) |
| 1:00 | "Kayıt olun" | Parolaya `sadeceharf` yazıp gönderin | İstemci kuralı, sunucuya gitmeden: "Parola en az bir harf ve bir rakam içermelidir." |
| 1:15 | Kayıt | E-posta olarak `ayse.demo@example.com`, geçerli parola `Parola123` | **409:** "Bu kullanıcı adı veya e-posta adresi zaten kayıtlı." |
| 1:30 | Kayıt | Yeni bir e-postayla kayıt olun (ör. `ogrenci1@example.com` / `Parola123`) | Otomatik giriş ve **Ana Sayfa** açılır: hızlı filtre, popüler dersler, son eklenen kaynaklar, üniversitelere göre kaynak sayıları. Sağ üstte kullanıcı menüsü (Profili görüntüle, Çıkış yap). Herkes `User` rolüyle kaydolur. |
| 2:00 | Üniversiteler → Pamukkale Üniversitesi | Sol menü **Üniversiteler** → **Pamukkale Üniversitesi** adına tıklayın → Ders: **BLG201** | Ders listesi seçilen üniversiteye göre yüklenir; adres çubuğu `?universite=..&ders=..` olur (bağlantı paylaşılabilir). |
| 2:30 | Kaynaklar | "Filtreleri temizle" → Ara kutusuna `calisma` yazın | Türkçe karakterler olmadan yazılsa da "Kalkülüs I limit **çalışma** soruları" bulunur. Tür rozetleri, dosya boyutu ve Türkiye saatiyle tarih. |
| 3:00 | Kaynak ayrıntısı | Üstteki arama çubuğuna `veri` yazıp Enter → "Veri Yapıları vize soruları 2025" → **Favorilere ekle** → **İndir** | Arama sonuçları üniversite, ders ve kaynak olarak gruplanır. Görüntülenme sayısı artar; kaynak sol menüdeki **Favoriler** sayfasında görünür. PDF iner. Bu kaynak `mehmet_demo`'ya ait olduğu için **Düzenle/Sil düğmeleri yok.** |
| 3:30 | Adres çubuğu | Aynı kaynağın adresinin sonuna `/duzenle` ekleyin | "Erişim reddedildi": arayüz de sahiplik kontrolü yapar. |
| 3:45 | Kaynak Yükle | Formu doldurun, dosya olarak **`sahte.pdf`** seçin → Yükle | **415:** "Dosya içeriği, belirtilen dosya türüyle eşleşmiyor." Sunucu yalnızca uzantıya değil, dosyanın ilk baytlarına da bakar. |
| 4:15 | Kaynak Yükle | Dosyayı **`notlar.txt`** yapın → Yükle | İlerleme çubuğu, ardından kaynak sayfası ve "Kaynak yüklendi." Dosya diske rastgele (GUID) bir adla kaydedilir. |
| 4:45 | Kaynak ayrıntısı | **Düzenle** → başlığı ve türü değiştirin → Kaydet | "Değişiklikler kaydedildi." Dosya ve ders değiştirilemez (formda belirtilir). |
| 5:15 | Kaynaklarım | Listeyi gösterin → yeni kaynakta **Sil** → Vazgeç → tekrar Sil → Sil | Onay penceresi; silince liste yenilenir. Dosya diskten de silinir. |
| 5:45 | Adres çubuğu | http://localhost:5173/yonetim | Öğrenci için "Erişim reddedildi"; sol menüde "Yönetim" bağlantısı da görünmez. |
| 6:00 | **Gizli pencere** | Yönetici hesabıyla giriş → **Yönetim** | Üniversite, ders, kaynak ve kullanıcı sayıları. **Kullanıcılar** sekmesinde `ogrenci1` hesabını silin (kaynakları da silinir; yönetici hesabında Sil yok). Sonra sol menü **Dersler** → `BLG201` satırında **Sil** (Düzenle/Sil yalnızca yöneticide görünür) → **409** "Bu derse bağlı kaynaklar olduğu için silinemez." |
| 6:20 | Üniversiteler | "Üniversite ekle" → `Test Üniversitesi`, İl listesinden **Denizli** | Eklenir. Aynı adla tekrar eklemek → **409** "Bu isimde bir üniversite zaten var." |
| 6:50 | Üniversiteler | **Pamukkale Üniversitesi → Sil → Sil** | **409:** "Bu üniversiteye bağlı dersler olduğu için silinemez." (Veri bütünlüğü korunur.) |
| 7:10 | Üniversiteler | `Test Üniversitesi` → Sil | Dersi olmadığı için silinir. |
| 7:30 | Kaynaklar (yönetici) | Bir öğrencinin kaynağını açın | Yönetici başkasının kaynağında da Düzenle/Sil görür (moderasyon). |
| 7:50 | Swagger | **Authorize yapmadan** `GET /api/universities` → Execute | **401** ve Türkçe ProblemDetails gövdesi. |
| 8:10 | Swagger | `POST /api/auth/login` ile `ayse.demo@example.com` / `DemoParola1` → token'ı **Authorize**'a yapıştırın → `DELETE /api/resources/{id}`; id olarak mehmet_demo'nun kaynağının numarasını yazın (3:00'teki kaynak sayfasının adresindeki sayı, ör. `/kaynaklar/6`) | **403:** "Bu kaynağı silme yetkiniz yok." Yetki kontrolü yalnızca arayüzde değil, sunucuda da var. |
| 8:40 | Terminal | (İsteğe bağlı) `CheckPoint.Web` içinde `npm test` | 81 frontend testi ~10 saniyede geçer. Backend'de 249, uçtan uca 7 test var. |
| 9:15 | — | Kapanış | Bilinen sınırlamalar ve sonraki adımlar (README'nin son bölümü). |

**İpucu:** Bir adım beklenmedik şekilde giderse (ör. 429 "Çok fazla deneme"), bu da bir özelliktir: giriş ve kayıt IP başına dakikada 10 istekle sınırlıdır. Bir dakika bekleyin.

## Olası sorular ve kısa cevaplar

**Mimari nasıl?**
İki ayrı uygulama. **ASP.NET Core Web API** iş kurallarını, yetkiyi ve dosyaları yönetir; **React** ön yüz yalnızca bu API'yi çağırır. API katmanları:
- Controller: uç noktalar.
- DTO + doğrulama: dış dünyaya entity döndürülmez.
- EF Core: veritabanı.
- Servis: dosya depolama.

Hatalar her yerde aynı biçimde (ProblemDetails) döner. Ön yüzde tek bir fetch sarmalayıcısı token'ı ekler, hataları tipli bir nesneye çevirir, 401'de oturumu kapatır.

**Neden MSSQL?**
Ödev gereksinimi. Ayrıca LocalDB, kurulum gerektirmeden her Windows geliştirme makinesinde çalışıyor. EF Core Code First ve migration'lar sayesinde şema kodla birlikte sürümleniyor. Türkçe aramayı uygun bir karşılaştırma kuralıyla (`Latin1_General_100_CI_AI`) çözdük; böylece "izmir" araması "İzmir"i buluyor.

**Güvenlik için neler yapıldı?**
- Parolalar BCrypt ile saklanıyor ve parola kuralı var.
- JWT 60 dakika geçerli. Kullanıcı silinir ya da rolü değişirse eski token anında geçersiz oluyor.
- Giriş ve kayıtta hız sınırı (429) var.
- Dosyada uzantı, içerik türü ve ilk bayt (magic bytes) denetimi yapılıyor. Boyut sınırı hem uygulamada hem web sunucusu düzeyinde uygulanıyor. Dosyalar GUID adıyla, web'e açık olmayan bir klasörde tutuluyor.
- Yetki kontrolleri sunucuda yapılıyor; arayüzdeki gizleme sadece kolaylık.
- Güvenlik başlıkları ekleniyor ve CORS yalnızca izinli adreslere açık.
- Günlüklerde parola, token ve e-posta yok.
- Repoda sır yok; sırlar user-secrets'ta.

**Token neden localStorage'da?**
Sayfa yenilenince oturumun korunması istendi; token yalnızca bellekte tutulsaydı bu mümkün olmazdı. XSS açısından localStorage ile sessionStorage aynı risk düzeyinde. Risk, kısa token süresi ve sıkı Content-Security-Policy ile azaltıldı. Daha güvenli bir sonraki adım, HttpOnly çerez + refresh token olurdu.

**Nasıl test edildi?**
Üç katman var:
- **249 backend entegrasyon testi:** Her test kendi geçici veritabanını oluşturup siliyor.
- **81 frontend bileşen testi:** Vitest, React Testing Library ve sahte API (MSW).
- **7 uçtan uca test:** Playwright gerçek tarayıcıda gerçek API'ye karşı tüm akışı ve 403 senaryosunu çalıştırıyor.

Ayrıca her aşamadan sonra test adımlarını elle uygulayıp doğruladım.

**Yapay zekâ ile geliştirme süreci nasıldı?**
Proje bir yapay zekâ asistanıyla (Claude) küçük aşamalar hâlinde geliştirildi. Her aşamanın düzeni şuydu:
- **Plan:** Asistan aşamanın başında yapacaklarını ve dosyaları yazdı.
- **Kod ve testler:** Kodu ve otomatik testleri birlikte üretip çalıştırdı. Önemli akışları gerçek tarayıcıda denedi.
- **Doğrulama:** Ben her aşamanın elle test adımlarını kendim uyguladım ve doğruladım. Ancak bundan sonra commit'i kendim attım.

Testler gerçek hatalar yakaladı ve bunlar düzeltildi. Örnekler:
- Türkçe büyük-küçük harf araması ("izmir"→"İzmir") çalışmıyordu.
- Web sunucusu istek boyutu sınırı uygulanmıyordu.
- Testlerin geçici veritabanını silmediği bir durum vardı.

Asistanın varsayım yaptığı kararlar açıkça işaretlendi ve benim onayıma sunuldu; örneğin token'ın nerede saklanacağı ve kayıt sonrası otomatik giriş.

**Neden kaynak düzenlemede dosya değiştirilemiyor?**
Basitlik ve tutarlılık için. Dosya değişince türü ve içerik denetimi yeniden gerekir, eski dosyanın da güvenle silinmesi gerekir. Bu sürümde kaynağı silip yeniden yüklemek yeterli görüldü.

**Neler eksik?**
Refresh token, e-posta doğrulama, parola sıfırlama, moderasyon/şikâyet, beğeni/yorum, kullanıcı yönetimi ekranı, bulut depolama ve canlı ortama dağıtım. Hepsi bilinçli olarak ilk sürümün dışında bırakıldı.
