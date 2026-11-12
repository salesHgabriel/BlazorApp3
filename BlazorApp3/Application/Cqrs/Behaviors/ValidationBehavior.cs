using System.ComponentModel.DataAnnotations;
using BlazorApp3.Services;

namespace BlazorApp3.Application.Cqrs;

/// <summary>
/// Valida DataAnnotations da mensagem e dos modelos que ela carrega (ex.: CreateUserCommand.Model).
/// Garante a validação no servidor mesmo que a UI seja contornada.
/// </summary>
public sealed class ValidationBehavior : IPipelineBehavior
{
    public Task<object?> HandleAsync(IMessage message, Func<Task<object?>> next, CancellationToken ct)
    {
        var errors = new List<ValidationResult>();
        Validate(message, errors);
        foreach (var prop in message.GetType().GetProperties())
        {
            if (prop.GetIndexParameters().Length > 0) continue;
            var value = prop.GetValue(message);
            if (value is not null && value.GetType().Assembly == typeof(ValidationBehavior).Assembly && !value.GetType().IsEnum)
                Validate(value, errors);
        }

        if (errors.Count > 0)
            throw new AppException(string.Join(" ", errors.Select(e => e.ErrorMessage).Distinct()));
        return next();
    }

    private static void Validate(object instance, List<ValidationResult> errors) =>
        Validator.TryValidateObject(instance, new ValidationContext(instance), errors, validateAllProperties: true);
}
