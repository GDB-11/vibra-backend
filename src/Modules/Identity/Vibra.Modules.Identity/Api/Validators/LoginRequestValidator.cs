using Vibra.Modules.Identity.Application.Contracts;
using Vibra.Shared.Api;
using Vibra.Shared.Domain;

namespace Vibra.Modules.Identity.Api.Validators;

internal sealed class LoginRequestValidator : IRequestValidator<LoginRequest>
{
    public IReadOnlyList<FieldError> Validate(LoginRequest request) =>
    [
        // El servicio recorta el correo, así que el límite se mide igual.
        .. InputRules.Required(request.Email?.Trim(), "email", "El correo es obligatorio.",
            InputRules.MaxEmailLength, $"El correo no puede superar los {InputRules.MaxEmailLength} caracteres."),
        .. InputRules.Password(request.Password),
    ];
}
