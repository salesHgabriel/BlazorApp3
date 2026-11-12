using System.Reflection;

namespace BlazorApp3.Application.Cqrs;

public static class CqrsServiceCollectionExtensions
{
    /// <summary>Registra o dispatcher e todos os handlers encontrados no assembly.</summary>
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
