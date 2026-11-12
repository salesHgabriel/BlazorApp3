namespace BlazorApp3.Application.Cqrs;

internal sealed class QueryInvoker<TQuery, TResult> : HandlerInvoker<TResult> where TQuery : IQuery<TResult>
{
    public override Type HandlerType => typeof(IQueryHandler<TQuery, TResult>);

    public override Task<TResult> InvokeAsync(object handler, IMessage message, CancellationToken ct) =>
        ((IQueryHandler<TQuery, TResult>)handler).HandleAsync((TQuery)message, ct);
}
