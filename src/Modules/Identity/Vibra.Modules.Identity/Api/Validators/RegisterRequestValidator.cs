using Vibra.Modules.Identity.Application.Contracts;
using Vibra.Shared.Api;
using Vibra.Shared.Domain;

namespace Vibra.Modules.Identity.Api.Validators;

internal sealed class RegisterRequestValidator : IRequestValidator<RegisterRequest>
{
    public IReadOnlyList<FieldError> Validate(RegisterRequest request) =>
    [
        .. InputRules.NewEmail(request.Email),
        .. InputRules.Password(request.Password),
        .. InputRules.DisplayName(request.DisplayName),
    ];
}
