using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using BlazorApp3.Security;
using BlazorApp3.Services;

namespace BlazorApp3.Application.Cqrs;

/// <summary>Loga duração e falhas de toda query/command.</summary>
public sealed class LoggingBehavior(ILogger<LoggingBehavior> logger) : IPipelineBehavior
{
    public async Task<object?> HandleAsync(IMessage message, Func<Task<object?>> next, CancellationToken ct)
    {
        var name = message.GetType().Name;
        var sw = Stopwatch.StartNew();
        try
        {
            var result = await next();
            logger.LogDebug("{Message} concluído em {Elapsed} ms", name, sw.ElapsedMilliseconds);
            return result;
        }
        catch (AppException ex)
        {
            logger.LogInformation("{Message} rejeitado: {Reason}", name, ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{Message} falhou após {Elapsed} ms", name, sw.ElapsedMilliseconds);
            throw;
        }
    }
}

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

/// <summary>Invalida o cache de permissões após commands marcados com <see cref="IInvalidatesPermissions"/>.</summary>
public sealed class PermissionCacheBehavior(PermissionService permissions) : IPipelineBehavior
{
    public async Task<object?> HandleAsync(IMessage message, Func<Task<object?>> next, CancellationToken ct)
    {
        var result = await next();
        if (message is IInvalidatesPermissions) permissions.Invalidate();
        return result;
    }
}
