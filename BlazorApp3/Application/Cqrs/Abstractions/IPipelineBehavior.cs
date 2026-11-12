namespace BlazorApp3.Application.Cqrs;

/// <summary>Comportamento transversal executado em volta de todo handler (logging, validação, cache...).</summary>
public interface IPipelineBehavior
{
    Task<object?> HandleAsync(IMessage message, Func<Task<object?>> next, CancellationToken ct);
}
