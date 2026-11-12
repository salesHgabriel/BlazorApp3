namespace BlazorApp3.Application.Cqrs;

/// <summary>Marcador comum de queries e commands (usado pelos behaviors do pipeline).</summary>
public interface IMessage;

/// <summary>Leitura: não altera estado.</summary>
public interface IQuery<TResult> : IMessage;

/// <summary>Escrita que retorna um valor (ex.: Id criado).</summary>
public interface ICommand<TResult> : IMessage;

/// <summary>Escrita sem retorno.</summary>
public interface ICommand : ICommand<Unit>;

public readonly record struct Unit
{
    public static readonly Unit Value = new();
}

public interface IQueryHandler<in TQuery, TResult> where TQuery : IQuery<TResult>
{
    Task<TResult> HandleAsync(TQuery query, CancellationToken ct);
}

public interface ICommandHandler<in TCommand, TResult> where TCommand : ICommand<TResult>
{
    Task<TResult> HandleAsync(TCommand command, CancellationToken ct);
}

public interface IDispatcher
{
    Task<TResult> QueryAsync<TResult>(IQuery<TResult> query, CancellationToken ct = default);
    Task<TResult> SendAsync<TResult>(ICommand<TResult> command, CancellationToken ct = default);
}

/// <summary>Comportamento transversal executado em volta de todo handler (logging, validação, cache...).</summary>
public interface IPipelineBehavior
{
    Task<object?> HandleAsync(IMessage message, Func<Task<object?>> next, CancellationToken ct);
}

/// <summary>Commands que alteram roles/vínculos e exigem invalidar o cache de permissões.</summary>
public interface IInvalidatesPermissions;

/// <summary>Query paginada: Page/PageSize ficam na própria query (filtros mutáveis ligados à UI).</summary>
public abstract class PagedQuery<TItem> : IQuery<Services.PagedResult<TItem>>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
