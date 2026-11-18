using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace CheckPoint.Api.Validation;

public static class TurkishProvinces
{
    public const string InvalidMessage = "Geçerli bir il seçin.";

    private static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");

    public static readonly IReadOnlyList<string> All =
    [
        "Adana", "Adıyaman", "Afyonkarahisar", "Ağrı", "Aksaray", "Amasya", "Ankara", "Antalya", "Ardahan",
        "Artvin", "Aydın", "Balıkesir", "Bartın", "Batman", "Bayburt", "Bilecik", "Bingöl", "Bitlis", "Bolu",
        "Burdur", "Bursa", "Çanakkale", "Çankırı", "Çorum", "Denizli", "Diyarbakır", "Düzce", "Edirne", "Elazığ",
        "Erzincan", "Erzurum", "Eskişehir", "Gaziantep", "Giresun", "Gümüşhane", "Hakkari", "Hatay", "Iğdır",
        "Isparta", "İstanbul", "İzmir", "Kahramanmaraş", "Karabük", "Karaman", "Kars", "Kastamonu", "Kayseri",
        "Kırıkkale", "Kırklareli", "Kırşehir", "Kilis", "Kocaeli", "Konya", "Kütahya", "Malatya", "Manisa",
        "Mardin", "Mersin", "Muğla", "Muş", "Nevşehir", "Niğde", "Ordu", "Osmaniye", "Rize", "Sakarya", "Samsun",
        "Siirt", "Sinop", "Sivas", "Şanlıurfa", "Şırnak", "Tekirdağ", "Tokat", "Trabzon", "Tunceli", "Uşak",
        "Van", "Yalova", "Yozgat", "Zonguldak"
    ];

    /// <summary>Returns the canonical province name for <paramref name="value"/>, ignoring case and surrounding spaces.</summary>
    public static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        return All.FirstOrDefault(province =>
                   string.Compare(province, trimmed, Turkish, CompareOptions.IgnoreCase) == 0)
               ?? All.FirstOrDefault(province =>
                   string.Compare(province, trimmed, CultureInfo.InvariantCulture,
                       CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) == 0);
    }
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class TurkishProvinceAttribute : ValidationAttribute
{
    public TurkishProvinceAttribute() : base(TurkishProvinces.InvalidMessage)
    {
    }

    public override bool IsValid(object? value) =>
        value is not string text || string.IsNullOrWhiteSpace(text) || TurkishProvinces.Normalize(text) is not null;
}
