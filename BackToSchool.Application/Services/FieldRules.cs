using System.Text.RegularExpressions;

namespace BackToSchool.Application.Services;

/// <summary>
/// Reglas de los campos (largos y formatos). Son las mismas que valida la pantalla
/// (BackToSchool.Web/src/app/core/forms/field-rules.ts): si se cambia una, cambiarla en los dos.
/// </summary>
internal static partial class FieldRules
{
    public const int CustomerNameMin = 3, CustomerNameMax = 150;
    public const int TaxIdMin = 5, TaxIdMax = 15;
    public const int PhoneLength = 8;
    public const int NotesMax = 500;
    public const int ManualTextMin = 3, ManualTextMax = 150;
    public const int StudentLabelMax = 100;
    public const int CancelReasonMin = 5, CancelReasonMax = 250;
    public const decimal PriceMax = 99_999m;
    public const int SchoolNameMin = 3, SchoolNameMax = 150;
    public const int ProductNameMin = 3, ProductNameMax = 250;
    public const int ProductCodeMax = 20;
    public const int GarmentMin = 2, GarmentMax = 150;
    public const int SizeMax = 20;

    [GeneratedRegex(@"^\d+$")]
    private static partial Regex DigitsRegex();

    [GeneratedRegex(@"\p{L}")]
    private static partial Regex HasLetterRegex();

    [GeneratedRegex(@"^[\p{L}\p{M}0-9 .,&'\-]+$")]
    private static partial Regex NameRegex();

    [GeneratedRegex(@"^[\p{L}\p{M} .\-]+$")]
    private static partial Regex LettersRegex();

    public static bool IsDigits(string value) => DigitsRegex().IsMatch(value);

    /// <summary>Nombre: letras (con tildes), números, espacios y . , &amp; ' - ; debe tener al menos una letra.</summary>
    public static bool IsName(string value) => NameRegex().IsMatch(value) && HasLetterRegex().IsMatch(value);

    public static bool IsLetters(string value) => LettersRegex().IsMatch(value) && HasLetterRegex().IsMatch(value);

    /// <summary>Precio en bolivianos: entre 0 y el máximo, con hasta 2 decimales.</summary>
    public static bool IsValidPrice(decimal price) => price >= 0 && price <= PriceMax && decimal.Round(price, 2) == price;

    /// <summary>Errores del texto según largo y formato; agrega los mensajes a la lista.</summary>
    public static void CheckLength(List<string> errors, string? value, string label, int min, int max, bool required)
    {
        var v = value?.Trim() ?? string.Empty;
        if (v.Length == 0)
        {
            if (required) errors.Add($"{label}: es obligatorio.");
            return;
        }
        if (v.Length < min) errors.Add($"{label}: mínimo {min} caracteres.");
        if (v.Length > max) errors.Add($"{label}: máximo {max} caracteres.");
    }
}
