using System.Text;

namespace CheckPoint.Api.Tests.Infrastructure;

public sealed record TestFile(string FileName, string ContentType, byte[] Content);

public static class TestFiles
{
    public static TestFile Text(string fileName = "notlar.txt", string content = "Veri yapıları ders notları") =>
        new(fileName, "text/plain", Encoding.UTF8.GetBytes(content));

    public static TestFile Pdf(string fileName = "notlar.pdf") =>
        new(fileName, "application/pdf", Encoding.ASCII.GetBytes("%PDF-1.4\n%test\n"));

    public static TestFile Png(string fileName = "sema.png") =>
        new(fileName, "image/png", [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00]);

    public static TestFile OfSize(int size, string fileName = "buyuk.txt")
    {
        var content = new byte[size];
        Array.Fill(content, (byte)'a');
        return new TestFile(fileName, "text/plain", content);
    }
}
