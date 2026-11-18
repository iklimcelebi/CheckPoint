using System.Text;
using CheckPoint.Api.Models;
using CheckPoint.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace CheckPoint.Api.Data;

public static class DemoDataSeeder
{
    public const string DemoPassword = "DemoParola1";

    private static readonly DemoUniversity[] Universities =
    [
        new("Pamukkale Üniversitesi", "Denizli",
        [
            new("BLG201", "Veri Yapıları"),
            new("BLG301", "Veritabanı Yönetim Sistemleri"),
            new("MAT101", "Matematik I"),
            new("BLG311", "İşletim Sistemleri")
        ]),
        new("Ege Üniversitesi", "İzmir",
        [
            new("BIL105", "Programlamaya Giriş"),
            new("BIL204", "Algoritma Analizi"),
            new("BIL302", "Bilgisayar Ağları")
        ]),
        new("Orta Doğu Teknik Üniversitesi", "Ankara",
        [
            new("CENG242", "Programlama Dilleri"),
            new("MATH119", "Kalkülüs I"),
            new("PHYS105", "Fizik I")
        ]),
        new("İstanbul Teknik Üniversitesi", "İstanbul",
        [
            new("BLG252E", "Nesneye Dayalı Programlama"),
            new("MAT281E", "Lineer Cebir"),
            new("EHB211E", "Elektrik Devreleri I")
        ]),
        new("Hacettepe Üniversitesi", "Ankara",
        [
            new("BBM101", "Bilgisayar Mühendisliğine Giriş"),
            new("BBM371", "Veri Yönetimi"),
            new("IST201", "Olasılık ve İstatistik")
        ]),
        new("Dokuz Eylül Üniversitesi", "İzmir",
        [
            new("CME2201", "Ayrık Matematik"),
            new("CME3201", "Yazılım Mühendisliği")
        ]),
        new("Boğaziçi Üniversitesi", "İstanbul",
        [
            new("CMPE150", "Programlamaya Giriş"),
            new("EC101", "İktisada Giriş")
        ]),
        new("Karadeniz Teknik Üniversitesi", "Trabzon",
        [
            new("BIL1001", "Bilgisayar Bilimlerine Giriş"),
            new("MAT1011", "Analiz I")
        ])
    ];

    private static readonly DemoUser[] Users =
    [
        new("ayse_demo", "ayse.demo@example.com"),
        new("mehmet_demo", "mehmet.demo@example.com"),
        new("zeynep_demo", "zeynep.demo@example.com"),
        new("ali_demo", "ali.demo@example.com"),
        new("elif_demo", "elif.demo@example.com"),
        new("burak_demo", "burak.demo@example.com")
    ];

    private static readonly DemoResource[] Resources =
    [
        new("Veri Yapıları 1-4. hafta ders notları", "Diziler, bağlı listeler, yığın ve kuyruk konularının özeti.",
            "Pamukkale Üniversitesi", "BLG201", ResourceType.LectureNote, "ayse.demo@example.com",
            "veri-yapilari-hafta-1-4.txt", 20,
            () => Text("Veri Yapıları — Demo Ders Notu\n\n1. Diziler\n2. Bağlı listeler\n3. Yığın (stack)\n4. Kuyruk (queue)\n\nBu dosya yalnızca demo amaçlı üretilmiştir.")),
        new("Veri Yapıları vize soruları 2025", "Geçen yılın vize sınavından derlenmiş örnek sorular.",
            "Pamukkale Üniversitesi", "BLG201", ResourceType.PastExamQuestion, "mehmet.demo@example.com",
            "veri-yapilari-vize-2025.pdf", 12,
            () => Pdf("Veri Yapilari - Vize Sorulari (Demo)", "Soru 1: Bagli listede eleman ekleme karmasikligi nedir?", "Soru 2: Yigin ile kuyruk arasindaki fark nedir?")),
        new("SQL sorgu alıştırmaları", "JOIN, GROUP BY ve alt sorgu örnekleri; cevaplar dosyanın sonunda.",
            "Pamukkale Üniversitesi", "BLG301", ResourceType.StudyQuestion, "ayse.demo@example.com",
            "sql-alistirmalari.txt", 9,
            () => Text("SQL Alıştırmaları (Demo)\n\n1) Her bölümdeki öğrenci sayısını listeleyin.\n2) Hiç ders almamış öğrencileri bulun.\n\nCevaplar:\n1) SELECT Bolum, COUNT(*) FROM Ogrenci GROUP BY Bolum;\n2) NOT EXISTS ile alt sorgu kullanın.")),
        new("Normalizasyon sunumu", "1NF, 2NF, 3NF ve BCNF örnekleriyle sunum.",
            "Pamukkale Üniversitesi", "BLG301", ResourceType.Presentation, "mehmet.demo@example.com",
            "normalizasyon-sunum.pdf", 7,
            () => Pdf("Normalizasyon (Demo Sunum)", "1NF: Atomik degerler", "2NF ve 3NF: Kismi ve gecisli bagimliliklar")),
        new("Programlamaya giriş akış şeması", "Döngü ve koşul yapılarını gösteren basit akış şeması görseli.",
            "Ege Üniversitesi", "BIL105", ResourceType.LectureNote, "ayse.demo@example.com",
            "akis-semasi.png", 5,
            () => Png()),
        new("Algoritma analizi final soruları", "Büyük-O gösterimi ve özyineleme bağıntıları.",
            "Ege Üniversitesi", "BIL204", ResourceType.PastExamQuestion, "mehmet.demo@example.com",
            "algoritma-final.pdf", 3,
            () => Pdf("Algoritma Analizi - Final (Demo)", "Soru 1: T(n) = 2T(n/2) + n bagintisini cozun.", "Soru 2: Ikili aramanin karmasikligi nedir?")),
        new("Kalkülüs I limit çalışma soruları", "Limit ve süreklilik üzerine 10 alıştırma.",
            "Orta Doğu Teknik Üniversitesi", "MATH119", ResourceType.StudyQuestion, "ayse.demo@example.com",
            "limit-sorulari.txt", 1,
            () => Text("Kalkülüs I — Limit Soruları (Demo)\n\n1) lim x→0 sin(x)/x = ?\n2) lim x→∞ (1 + 1/x)^x = ?\n\nBu dosya yalnızca demo amaçlı üretilmiştir.")),
        new("İşletim sistemleri süreç yönetimi notları", "Süreç durumları, zamanlama algoritmaları ve kilitlenme.",
            "Pamukkale Üniversitesi", "BLG311", ResourceType.LectureNote, "zeynep.demo@example.com",
            "surec-yonetimi.txt", 4,
            () => Text("İşletim Sistemleri — Süreç Yönetimi (Demo)\n\n1. Süreç durumları\n2. FCFS, SJF, Round Robin\n3. Kilitlenme koşulları\n\nBu dosya yalnızca demo amaçlı üretilmiştir."), 18),
        new("Matematik I türev formülleri", "Temel türev kuralları ve zincir kuralı örnekleri.",
            "Pamukkale Üniversitesi", "MAT101", ResourceType.StudyQuestion, "ali.demo@example.com",
            "turev-formulleri.txt", 15,
            () => Text("Matematik I — Türev Formülleri (Demo)\n\n(x^n)' = n·x^(n-1)\n(sin x)' = cos x\n(e^x)' = e^x\n\nBu dosya yalnızca demo amaçlı üretilmiştir."), 9),
        new("Bilgisayar ağları OSI katmanları sunumu", "Yedi katmanın görevleri ve örnek protokoller.",
            "Ege Üniversitesi", "BIL302", ResourceType.Presentation, "elif.demo@example.com",
            "osi-katmanlari.pdf", 6,
            () => Pdf("OSI Katmanlari (Demo Sunum)", "Fiziksel, Veri Baglantisi, Ag, Tasima", "Oturum, Sunum, Uygulama"), 25),
        new("Bilgisayar ağları vize soruları", "TCP/UDP farkları ve alt ağ hesaplama soruları.",
            "Ege Üniversitesi", "BIL302", ResourceType.PastExamQuestion, "burak.demo@example.com",
            "aglar-vize.pdf", 10,
            () => Pdf("Bilgisayar Aglari - Vize (Demo)", "Soru 1: TCP ile UDP arasindaki farklar nelerdir?", "Soru 2: Bir /26 agda kac adres vardir?"), 14),
        new("Programlama dilleri Haskell giriş notları", "Fonksiyonel programlamaya giriş: liste işlemleri ve özyineleme.",
            "Orta Doğu Teknik Üniversitesi", "CENG242", ResourceType.LectureNote, "zeynep.demo@example.com",
            "haskell-giris.txt", 8,
            () => Text("Programlama Dilleri — Haskell Girişi (Demo)\n\nsum [] = 0\nsum (x:xs) = x + sum xs\n\nBu dosya yalnızca demo amaçlı üretilmiştir."), 11),
        new("Fizik I hareket problemleri", "Sabit ivmeli hareket ve eğik atış problemleri.",
            "Orta Doğu Teknik Üniversitesi", "PHYS105", ResourceType.StudyQuestion, "ali.demo@example.com",
            "fizik-hareket.txt", 2,
            () => Text("Fizik I — Hareket Problemleri (Demo)\n\n1) 20 m/s hızla yukarı atılan cisim kaç saniyede geri döner?\n2) Eğik atışta menzil formülünü çıkarın.\n\nBu dosya yalnızca demo amaçlı üretilmiştir."), 6),
        new("Nesneye dayalı programlama kalıtım sunumu", "Kalıtım, çok biçimlilik ve soyut sınıflar.",
            "İstanbul Teknik Üniversitesi", "BLG252E", ResourceType.Presentation, "elif.demo@example.com",
            "kalitim-sunum.pdf", 11,
            () => Pdf("Nesneye Dayali Programlama (Demo Sunum)", "Kalitim ve cok bicimlilik", "Soyut siniflar ve arayuzler"), 21),
        new("Lineer cebir matris işlemleri", "Determinant, ters matris ve özdeğer örnekleri.",
            "İstanbul Teknik Üniversitesi", "MAT281E", ResourceType.LectureNote, "burak.demo@example.com",
            "matris-islemleri.txt", 13,
            () => Text("Lineer Cebir — Matris İşlemleri (Demo)\n\n1. Determinant\n2. Ters matris\n3. Özdeğer ve özvektör\n\nBu dosya yalnızca demo amaçlı üretilmiştir."), 16),
        new("Elektrik devreleri final soruları", "Kirchhoff yasaları ve Thevenin eşdeğeri soruları.",
            "İstanbul Teknik Üniversitesi", "EHB211E", ResourceType.PastExamQuestion, "mehmet.demo@example.com",
            "devreler-final.pdf", 30,
            () => Pdf("Elektrik Devreleri I - Final (Demo)", "Soru 1: Devrenin Thevenin esdegerini bulun.", "Soru 2: Dugum gerilimlerini hesaplayin."), 7),
        new("Bilgisayar mühendisliğine giriş ders notları", "Algoritma kavramı, sayı sistemleri ve sözde kod.",
            "Hacettepe Üniversitesi", "BBM101", ResourceType.LectureNote, "zeynep.demo@example.com",
            "bbm101-notlar.txt", 16,
            () => Text("Bilgisayar Mühendisliğine Giriş (Demo)\n\n1. Algoritma nedir?\n2. İkilik ve onaltılık sayı sistemleri\n3. Sözde kod\n\nBu dosya yalnızca demo amaçlı üretilmiştir."), 10),
        new("Veri yönetimi ER diyagramı örneği", "Kütüphane sistemi için örnek varlık-ilişki diyagramı.",
            "Hacettepe Üniversitesi", "BBM371", ResourceType.Presentation, "ayse.demo@example.com",
            "er-diyagrami.png", 19,
            () => Png(), 5),
        new("Olasılık ve istatistik çalışma soruları", "Koşullu olasılık, Bayes teoremi ve normal dağılım.",
            "Hacettepe Üniversitesi", "IST201", ResourceType.StudyQuestion, "elif.demo@example.com",
            "olasilik-sorulari.txt", 5,
            () => Text("Olasılık ve İstatistik (Demo)\n\n1) P(A|B) nasıl hesaplanır?\n2) Bayes teoremini bir örnekle açıklayın.\n\nBu dosya yalnızca demo amaçlı üretilmiştir."), 19),
        new("Ayrık matematik mantık ve kümeler", "Önermeler mantığı, küme işlemleri ve ispat yöntemleri.",
            "Dokuz Eylül Üniversitesi", "CME2201", ResourceType.LectureNote, "ali.demo@example.com",
            "ayrik-matematik.txt", 22,
            () => Text("Ayrık Matematik — Mantık ve Kümeler (Demo)\n\n1. Önermeler mantığı\n2. Küme işlemleri\n3. Tümevarım ile ispat\n\nBu dosya yalnızca demo amaçlı üretilmiştir."), 8),
        new("Yazılım mühendisliği vize soruları", "Yazılım yaşam döngüsü modelleri ve gereksinim analizi.",
            "Dokuz Eylül Üniversitesi", "CME3201", ResourceType.PastExamQuestion, "burak.demo@example.com",
            "yazilim-vize.pdf", 9,
            () => Pdf("Yazilim Muhendisligi - Vize (Demo)", "Soru 1: Selale ve cevik modelleri karsilastirin.", "Soru 2: Islevsel olmayan gereksinimlere ornek verin."), 12),
        new("Programlamaya giriş Python alıştırmaları", "Döngüler, listeler ve fonksiyonlar üzerine alıştırmalar.",
            "Boğaziçi Üniversitesi", "CMPE150", ResourceType.StudyQuestion, "zeynep.demo@example.com",
            "python-alistirmalari.txt", 3,
            () => Text("Python Alıştırmaları (Demo)\n\n1) 1 ile 100 arasındaki çift sayıları yazdırın.\n2) Bir listedeki en büyük elemanı bulan fonksiyon yazın.\n\nBu dosya yalnızca demo amaçlı üretilmiştir."), 23),
        new("İktisada giriş arz ve talep sunumu", "Piyasa dengesi, esneklik ve örnek grafikler.",
            "Boğaziçi Üniversitesi", "EC101", ResourceType.Presentation, "mehmet.demo@example.com",
            "arz-talep.pdf", 17,
            () => Pdf("Iktisada Giris (Demo Sunum)", "Arz ve talep egrileri", "Piyasa dengesi ve esneklik"), 4),
        new("Bilgisayar bilimlerine giriş ders notları", "Bilgisayarın tarihçesi, donanım ve yazılım kavramları.",
            "Karadeniz Teknik Üniversitesi", "BIL1001", ResourceType.LectureNote, "elif.demo@example.com",
            "bil1001-notlar.txt", 25,
            () => Text("Bilgisayar Bilimlerine Giriş (Demo)\n\n1. Tarihçe\n2. Donanım bileşenleri\n3. İşletim sistemi ve uygulamalar\n\nBu dosya yalnızca demo amaçlı üretilmiştir."), 3),
        new("Analiz I geçmiş sınav soruları", "Limit, süreklilik ve türev konularından final soruları.",
            "Karadeniz Teknik Üniversitesi", "MAT1011", ResourceType.PastExamQuestion, "ali.demo@example.com",
            "analiz-final.pdf", 40,
            () => Pdf("Analiz I - Final (Demo)", "Soru 1: x^2 sin(1/x) fonksiyonunun surekliligini inceleyin.", "Soru 2: Ortalama deger teoremini ispatlayin."), 9)
    ];

    public static async Task<DemoSeedResult> SeedAsync(
        AppDbContext db,
        IFileStorageService storage,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var result = new DemoSeedResult();
        var universities = new Dictionary<string, University>();

        foreach (var demoUniversity in Universities)
        {
            var university = await db.Universities
                .FirstOrDefaultAsync(candidate => candidate.Name == demoUniversity.Name, cancellationToken);
            if (university is null)
            {
                university = new University { Name = demoUniversity.Name, City = demoUniversity.City };
                db.Universities.Add(university);
                await db.SaveChangesAsync(cancellationToken);
                result.Universities++;
            }

            universities[university.Name] = university;

            foreach (var demoCourse in demoUniversity.Courses)
            {
                var exists = await db.Courses.AnyAsync(
                    course => course.UniversityId == university.Id && course.Code == demoCourse.Code,
                    cancellationToken);
                if (exists)
                    continue;

                db.Courses.Add(new Course { Name = demoCourse.Name, Code = demoCourse.Code, UniversityId = university.Id });
                await db.SaveChangesAsync(cancellationToken);
                result.Courses++;
            }
        }

        var users = new Dictionary<string, User>();
        foreach (var demoUser in Users)
        {
            var user = await db.Users.FirstOrDefaultAsync(candidate => candidate.Email == demoUser.Email, cancellationToken);
            if (user is null)
            {
                if (await db.Users.AnyAsync(candidate => candidate.Username == demoUser.Username, cancellationToken))
                {
                    logger.LogWarning("Demo user name {Username} is taken by another account; skipping it.", demoUser.Username);
                    continue;
                }

                user = new User
                {
                    Username = demoUser.Username,
                    Email = demoUser.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(DemoPassword),
                    Role = "User",
                    CreatedAt = DateTimeOffset.UtcNow
                };
                db.Users.Add(user);
                await db.SaveChangesAsync(cancellationToken);
                result.Users++;
            }

            users[user.Email] = user;
        }

        foreach (var demoResource in Resources)
        {
            if (!users.TryGetValue(demoResource.UploaderEmail, out var uploader))
                continue;

            var university = universities[demoResource.UniversityName];
            var course = await db.Courses.SingleAsync(
                candidate => candidate.UniversityId == university.Id && candidate.Code == demoResource.CourseCode,
                cancellationToken);
            var content = demoResource.CreateContent();

            var resource = await db.EducationalResources.FirstOrDefaultAsync(
                candidate => candidate.Title == demoResource.Title && candidate.UploadedByUserId == uploader.Id,
                cancellationToken);

            if (resource is not null)
            {
                await using var existing = await storage.OpenReadAsync(resource.StoredFileName, cancellationToken);
                if (existing is not null)
                    continue;

                resource.StoredFileName = await SaveAsync(storage, content, resource.OriginalFileName, cancellationToken);
                resource.FileSize = content.Length;
                await db.SaveChangesAsync(cancellationToken);
                result.RestoredFiles++;
                continue;
            }

            var storedFileName = await SaveAsync(storage, content, demoResource.FileName, cancellationToken);
            db.EducationalResources.Add(new EducationalResource
            {
                Title = demoResource.Title,
                Description = demoResource.Description,
                UniversityId = university.Id,
                CourseId = course.Id,
                Type = demoResource.Type,
                OriginalFileName = demoResource.FileName,
                StoredFileName = storedFileName,
                ContentType = ContentTypeFor(demoResource.FileName),
                FileSize = content.Length,
                UploadedByUserId = uploader.Id,
                UploadedAt = DateTimeOffset.UtcNow.AddDays(-demoResource.DaysAgo),
                ViewCount = demoResource.Views
            });

            try
            {
                await db.SaveChangesAsync(cancellationToken);
            }
            catch
            {
                await storage.DeleteAsync(storedFileName, CancellationToken.None);
                throw;
            }

            result.Resources++;
        }

        logger.LogInformation(
            "Demo data: {Universities} universities, {Courses} courses, {Users} users, {Resources} resources added; {RestoredFiles} files restored.",
            result.Universities, result.Courses, result.Users, result.Resources, result.RestoredFiles);
        return result;
    }

    private static async Task<string> SaveAsync(
        IFileStorageService storage,
        byte[] content,
        string fileName,
        CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream(content);
        return await storage.SaveAsync(stream, Path.GetExtension(fileName), cancellationToken);
    }

    private static string ContentTypeFor(string fileName) => Path.GetExtension(fileName) switch
    {
        ".pdf" => "application/pdf",
        ".png" => "image/png",
        _ => "text/plain"
    };

    private static byte[] Text(string content) => Encoding.UTF8.GetBytes(content);

    private static byte[] Pdf(params string[] lines)
    {
        var stream = new StringBuilder("BT /F1 16 Tf 72 760 Td");
        foreach (var line in lines)
            stream.Append($" ({line.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)")}) Tj 0 -28 Td");
        stream.Append(" ET");

        var objects = new[]
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R /Resources << /Font << /F1 5 0 R >> >> >>",
            $"<< /Length {stream.Length} >>\nstream\n{stream}\nendstream",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>"
        };

        var pdf = new StringBuilder("%PDF-1.4\n");
        var offsets = new List<int>();
        for (var index = 0; index < objects.Length; index++)
        {
            offsets.Add(pdf.Length);
            pdf.Append($"{index + 1} 0 obj\n{objects[index]}\nendobj\n");
        }

        var xrefOffset = pdf.Length;
        pdf.Append($"xref\n0 {objects.Length + 1}\n0000000000 65535 f \n");
        foreach (var offset in offsets)
            pdf.Append($"{offset:D10} 00000 n \n");
        pdf.Append($"trailer\n<< /Size {objects.Length + 1} /Root 1 0 R >>\nstartxref\n{xrefOffset}\n%%EOF\n");

        return Encoding.ASCII.GetBytes(pdf.ToString());
    }

    private static byte[] Png()
    {
        const int width = 120;
        const int height = 160;
        var boxes = new[] { (35, 10, 50, 25), (35, 68, 50, 25), (35, 125, 50, 25) };
        var raw = new byte[height * (1 + width * 3)];
        for (var y = 0; y < height; y++)
        {
            var row = y * (1 + width * 3);
            for (var x = 0; x < width; x++)
            {
                var onBorder = boxes.Any(box =>
                    x >= box.Item1 && x < box.Item1 + box.Item3 && y >= box.Item2 && y < box.Item2 + box.Item4 &&
                    (x < box.Item1 + 2 || x >= box.Item1 + box.Item3 - 2 || y < box.Item2 + 2 || y >= box.Item2 + box.Item4 - 2));
                var onConnector = x is >= 59 and <= 60 && ((y >= 35 && y < 68) || (y >= 93 && y < 125));
                var (r, g, b) = onBorder || onConnector ? ((byte)37, (byte)99, (byte)235) : ((byte)255, (byte)255, (byte)255);
                raw[row + 1 + x * 3] = r;
                raw[row + 2 + x * 3] = g;
                raw[row + 3 + x * 3] = b;
            }
        }

        using var compressed = new MemoryStream();
        using (var zlib = new System.IO.Compression.ZLibStream(compressed, System.IO.Compression.CompressionLevel.Optimal))
            zlib.Write(raw);

        using var png = new MemoryStream();
        png.Write([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);
        var header = new byte[13];
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(header, width);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4), height);
        header[8] = 8;
        header[9] = 2;
        WriteChunk(png, "IHDR", header);
        WriteChunk(png, "IDAT", compressed.ToArray());
        WriteChunk(png, "IEND", []);
        return png.ToArray();
    }

    private static void WriteChunk(Stream output, string type, byte[] data)
    {
        var typeBytes = Encoding.ASCII.GetBytes(type);
        Span<byte> number = stackalloc byte[4];
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(number, data.Length);
        output.Write(number);
        output.Write(typeBytes);
        output.Write(data);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(number, Crc32([.. typeBytes, .. data]));
        output.Write(number);
    }

    private static uint Crc32(byte[] bytes)
    {
        var crc = 0xFFFFFFFFu;
        foreach (var value in bytes)
        {
            crc ^= value;
            for (var bit = 0; bit < 8; bit++)
                crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320u : crc >> 1;
        }

        return ~crc;
    }

    public sealed class DemoSeedResult
    {
        public int Universities { get; set; }
        public int Courses { get; set; }
        public int Users { get; set; }
        public int Resources { get; set; }
        public int RestoredFiles { get; set; }
    }

    private sealed record DemoUniversity(string Name, string City, DemoCourse[] Courses);

    private sealed record DemoCourse(string Code, string Name);

    private sealed record DemoUser(string Username, string Email);

    private sealed record DemoResource(
        string Title,
        string Description,
        string UniversityName,
        string CourseCode,
        ResourceType Type,
        string UploaderEmail,
        string FileName,
        int DaysAgo,
        Func<byte[]> CreateContent,
        int Views = 0);
}
