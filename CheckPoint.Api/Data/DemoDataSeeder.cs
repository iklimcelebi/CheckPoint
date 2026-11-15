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
            new("MAT101", "Matematik I")
        ]),
        new("Ege Üniversitesi", "İzmir",
        [
            new("BIL105", "Programlamaya Giriş"),
            new("BIL204", "Algoritma Analizi")
        ]),
        new("Orta Doğu Teknik Üniversitesi", "Ankara",
        [
            new("CENG242", "Programlama Dilleri"),
            new("MATH119", "Kalkülüs I")
        ])
    ];

    private static readonly DemoUser[] Users =
    [
        new("ayse_demo", "ayse.demo@example.com"),
        new("mehmet_demo", "mehmet.demo@example.com")
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
            () => Text("Kalkülüs I — Limit Soruları (Demo)\n\n1) lim x→0 sin(x)/x = ?\n2) lim x→∞ (1 + 1/x)^x = ?\n\nBu dosya yalnızca demo amaçlı üretilmiştir."))
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
                UploadedAt = DateTimeOffset.UtcNow.AddDays(-demoResource.DaysAgo)
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
        Func<byte[]> CreateContent);
}
