using System.Collections.Concurrent;

namespace BlazorApp3.Application.Cqrs;

/// <summary>
/// Dispatcher próprio (sem pacotes). Cada mensagem roda em um escopo de DI novo, então os handlers
/// recebem AppDbContext/UserManager "frescos" — essencial em Blazor Server, onde o escopo do circuito vive muito.
/// </summary>
public sealed class Dispatcher(IServiceScopeFactory scopes) : IDispatcher
{
    private static readonly ConcurrentDictionary<Type, object> Invokers = new();

    public Task<TResult> QueryAsync<TResult>(IQuery<TResult> query, CancellationToken ct = default) =>
        DispatchAsync(query, GetInvoker<TResult>(query.GetType(), typeof(QueryInvoker<,>)), ct);

    public Task<TResult> SendAsync<TResult>(ICommand<TResult> command, CancellationToken ct = default) =>
        DispatchAsync(command, GetInvoker<TResult>(command.GetType(), typeof(CommandInvoker<,>)), ct);

    private async Task<TResult> DispatchAsync<TResult>(IMessage message, HandlerInvoker<TResult> invoker, CancellationToken ct)
    {
        await using var scope = scopes.CreateAsyncScope();
        var services = scope.ServiceProvider;

        var handler = services.GetService(invoker.HandlerType)
            ?? throw new InvalidOperationException($"Nenhum handler registrado para {message.GetType().Name}.");

        // Monta a cadeia: behavior1 → behavior2 → ... → handler
        Func<Task<object?>> pipeline = async () => await invoker.InvokeAsync(handler, message, ct);
        foreach (var behavior in services.GetServices<IPipelineBehavior>().Reverse())
        {
            var next = pipeline;
            pipeline = () => behavior.HandleAsync(message, next, ct);
        }
        return (TResult)(await pipeline())!;
    }

    private static HandlerInvoker<TResult> GetInvoker<TResult>(Type messageType, Type openInvoker) =>
        (HandlerInvoker<TResult>)Invokers.GetOrAdd(messageType,
            t => Activator.CreateInstance(openInvoker.MakeGenericType(t, typeof(TResult)))!);
}
