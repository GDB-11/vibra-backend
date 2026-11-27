using Vibra.Modules.Identity.Application.Contracts;
using Vibra.Shared.Api;
using Vibra.Shared.Domain;

namespace Vibra.Modules.Identity.Api.Validators;

internal sealed class RefreshRequestValidator : IRequestValidator<RefreshRequest>
{
    public IReadOnlyList<FieldError> Validate(RefreshRequest request) =>
    [
        .. InputRules.Required(request.RefreshToken, "refreshToken", "El token de renovación es obligatorio.",
            InputRules.MaxRefreshTokenLength, "El token de renovación no es válido."),
    ];
}
