using Microsoft.AspNetCore.Identity;

namespace Vibra.Modules.Identity.Application.Errors;

/// <summary>Mensajes en español de las reglas de contraseña, que llegan al cliente.</summary>
internal sealed class SpanishIdentityErrorDescriber : IdentityErrorDescriber
{
    public override IdentityError PasswordTooShort(int length) =>
        Create(nameof(PasswordTooShort), $"La contraseña debe tener al menos {length} caracteres.");

    public override IdentityError PasswordRequiresDigit() =>
        Create(nameof(PasswordRequiresDigit), "La contraseña debe incluir un número.");

    public override IdentityError PasswordRequiresLower() =>
        Create(nameof(PasswordRequiresLower), "La contraseña debe incluir una letra minúscula.");

    public override IdentityError PasswordRequiresUpper() =>
        Create(nameof(PasswordRequiresUpper), "La contraseña debe incluir una letra mayúscula.");

    public override IdentityError PasswordRequiresNonAlphanumeric() =>
        Create(nameof(PasswordRequiresNonAlphanumeric), "La contraseña debe incluir un símbolo.");

    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) =>
        Create(
            nameof(PasswordRequiresUniqueChars),
            $"La contraseña debe usar al menos {uniqueChars} caracteres distintos.");

    private static IdentityError Create(string code, string description) =>
        new() { Code = code, Description = description };
}
