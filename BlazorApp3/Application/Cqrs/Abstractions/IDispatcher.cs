namespace BlazorApp3.Application.Cqrs;

/// <summary>Ponto único de envio de queries e commands.</summary>
public interface IDispatcher
{
    Task<TResult> QueryAsync<TResult>(IQuery<TResult> query, CancellationToken ct = default);
    Task<TResult> SendAsync<TResult>(ICommand<TResult> command, CancellationToken ct = default);
}
