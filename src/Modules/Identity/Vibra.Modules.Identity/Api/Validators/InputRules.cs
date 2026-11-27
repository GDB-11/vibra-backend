using System.Globalization;
using System.Net.Mail;
using System.Text;
using Vibra.Shared.Domain;

namespace Vibra.Modules.Identity.Api.Validators;

/// <summary>
/// Reglas de entrada de Identity. Las de fortaleza de la contraseña no están aquí: las aplica
/// ASP.NET Core Identity al crear la cuenta.
/// </summary>
internal static class InputRules
{
    public const int MaxEmailLength = 254;
    public const int MaxDisplayNameLength = 100;
    public const int MaxRefreshTokenLength = 128;

    // Limita el trabajo de calcular el hash de entradas enormes.
    public const int MaxPasswordLength = 128;

    /// <summary>Correo de una cuenta nueva. Se comprueba sin los espacios de los extremos.</summary>
    public static IEnumerable<FieldError> NewEmail(string? value)
    {
        var email = value?.Trim();

        if (string.IsNullOrEmpty(email))
            return Error("email", "El correo es obligatorio.");

        return IsEmail(email) ? [] : Error("email", "El correo no es válido.");
    }

    /// <summary>Nombre visible. Admite cualquier idioma y emojis, pero no texto invisible o engañoso.</summary>
    public static IEnumerable<FieldError> DisplayName(string? value)
    {
        const string field = "displayName";
        var name = value?.Trim();

        if (string.IsNullOrEmpty(name))
            return Error(field, "El nombre es obligatorio.");

        if (name.Length > MaxDisplayNameLength)
            return Error(field, $"El nombre no puede superar los {MaxDisplayNameLength} caracteres.");

        var runes = name.EnumerateRunes().ToList();

        if (runes.Any(IsForbidden))
            return Error(field, "El nombre contiene caracteres no permitidos.");

        return runes.Any(IsVisible) ? [] : Error(field, "El nombre debe incluir al menos un carácter visible.");
    }

    /// <summary>
    /// Para campos que solo se comparan con datos guardados: obligatorios y con tamaño máximo.
    /// No aplica reglas nuevas que dejen fuera a cuentas existentes.
    /// </summary>
    public static IEnumerable<FieldError> Required(
        string? value, string field, string requiredMessage, int maxLength, string tooLongMessage)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Error(field, requiredMessage);

        return value.Length > maxLength ? Error(field, tooLongMessage) : [];
    }

    /// <summary>La contraseña nunca se recorta ni se modifica.</summary>
    public static IEnumerable<FieldError> Password(string? value) =>
        Required(value, "password", "La contraseña es obligatoria.", MaxPasswordLength,
            $"La contraseña no puede superar los {MaxPasswordLength} caracteres.");

    private static bool IsEmail(string email) =>
        email.Length <= MaxEmailLength
        && !email.EnumerateRunes().Any(rune => Rune.IsWhiteSpace(rune) || IsForbidden(rune)
            || Rune.GetUnicodeCategory(rune) == UnicodeCategory.Format)
        && MailAddress.TryCreate(email, out var address)
        && address.Address == email
        && IsPublicDomain(address.Host);

    // Exige un punto: "a@b" es sintaxis válida, pero no una dirección que pueda recibir correo.
    private static bool IsPublicDomain(string domain)
    {
        var labels = domain.Split('.');

        return labels.Length >= 2
            && labels.All(label => label.Length is > 0 and <= 63
                && !label.StartsWith('-')
                && !label.EndsWith('-')
                && label.All(character => char.IsLetterOrDigit(character) || character == '-'
                    || char.GetUnicodeCategory(character) is UnicodeCategory.NonSpacingMark
                        or UnicodeCategory.SpacingCombiningMark))
            && labels[^1].Length >= 2
            && !labels[^1].All(char.IsDigit);
    }

    // Controles, saltos de línea, cambios de dirección y espacios de ancho cero. Se permiten
    // U+200C y U+200D, que usan algunos emojis y sistemas de escritura.
    private static bool IsForbidden(Rune rune) =>
        Rune.IsControl(rune)
        || rune.Value is 0x00AD or 0x061C or 0x180E or 0x200B or 0x200E or 0x200F
            or (>= 0x2028 and <= 0x202E) or (>= 0x2060 and <= 0x206F) or 0xFEFF;

    private static bool IsVisible(Rune rune) =>
        // Estos cuentan como letra o símbolo, pero se ven en blanco.
        rune.Value is not (0x115F or 0x1160 or 0x2800 or 0x3164 or 0xFFA0)
        && (Rune.IsLetter(rune) || Rune.IsNumber(rune) || Rune.IsPunctuation(rune) || Rune.IsSymbol(rune));

    private static FieldError[] Error(string field, string message) => [new(field, message)];
}
