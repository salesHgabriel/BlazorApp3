using System.Collections.Concurrent;
using System.Reflection;

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

    private async Task<TResult> DispatchAsync<TResult>(IMessage message, Invoker<TResult> invoker, CancellationToken ct)
    {
        await using var scope = scopes.CreateAsyncScope();
        var services = scope.ServiceProvider;

        var handler = services.GetService(invoker.HandlerType)
            ?? throw new InvalidOperationException($"Nenhum handler registrado para {message.GetType().Name}.");

        Func<Task<object?>> pipeline = async () => await invoker.InvokeAsync(handler, message, ct);
        foreach (var behavior in services.GetServices<IPipelineBehavior>().Reverse())
        {
            var next = pipeline;
            pipeline = () => behavior.HandleAsync(message, next, ct);
        }
        return (TResult)(await pipeline())!;
    }

    private static Invoker<TResult> GetInvoker<TResult>(Type messageType, Type openInvoker) =>
        (Invoker<TResult>)Invokers.GetOrAdd(messageType,
            t => Activator.CreateInstance(openInvoker.MakeGenericType(t, typeof(TResult)))!);

    // Invokers tipados evitam reflection/dynamic a cada chamada
    private abstract class Invoker<TResult>
    {
        public abstract Type HandlerType { get; }
        public abstract Task<TResult> InvokeAsync(object handler, IMessage message, CancellationToken ct);
    }

    private sealed class QueryInvoker<TQuery, TResult> : Invoker<TResult> where TQuery : IQuery<TResult>
    {
        public override Type HandlerType => typeof(IQueryHandler<TQuery, TResult>);
        public override Task<TResult> InvokeAsync(object handler, IMessage message, CancellationToken ct) =>
            ((IQueryHandler<TQuery, TResult>)handler).HandleAsync((TQuery)message, ct);
    }

    private sealed class CommandInvoker<TCommand, TResult> : Invoker<TResult> where TCommand : ICommand<TResult>
    {
        public override Type HandlerType => typeof(ICommandHandler<TCommand, TResult>);
        public override Task<TResult> InvokeAsync(object handler, IMessage message, CancellationToken ct) =>
            ((ICommandHandler<TCommand, TResult>)handler).HandleAsync((TCommand)message, ct);
    }
}

public static class CqrsServiceCollectionExtensions
{
    /// <summary>Registra o dispatcher e todos os handlers do assembly (uma classe pode tratar várias mensagens).</summary>
    public static IServiceCollection AddCqrs(this IServiceCollection services, Assembly assembly)
    {
        services.AddSingleton<IDispatcher, Dispatcher>();

        var handlerInterfaces = new[] { typeof(IQueryHandler<,>), typeof(ICommandHandler<,>) };
        foreach (var type in assembly.GetTypes().Where(t => t is { IsClass: true, IsAbstract: false }))
        {
            foreach (var iface in type.GetInterfaces().Where(i => i.IsGenericType && handlerInterfaces.Contains(i.GetGenericTypeDefinition())))
                services.AddScoped(iface, type);
        }
        return services;
    }

    /// <summary>Behaviors executam na ordem de registro (o primeiro é o mais externo).</summary>
    public static IServiceCollection AddPipelineBehavior<TBehavior>(this IServiceCollection services)
        where TBehavior : class, IPipelineBehavior =>
        services.AddScoped<IPipelineBehavior, TBehavior>();
}
