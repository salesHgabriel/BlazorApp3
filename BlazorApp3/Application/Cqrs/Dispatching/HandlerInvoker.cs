namespace BlazorApp3.Application.Cqrs;

/// <summary>
/// Ponte tipada entre a mensagem e seu handler. Criada uma vez por tipo de mensagem e reutilizada,
/// evitando reflection/dynamic a cada chamada.
/// </summary>
internal abstract class HandlerInvoker<TResult>
{
    public abstract Type HandlerType { get; }
    public abstract Task<TResult> InvokeAsync(object handler, IMessage message, CancellationToken ct);
}
