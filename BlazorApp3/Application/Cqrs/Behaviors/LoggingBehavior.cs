using System.Diagnostics;
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
