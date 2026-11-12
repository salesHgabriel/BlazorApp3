namespace BlazorApp3.Application.Cqrs;

internal sealed class CommandInvoker<TCommand, TResult> : HandlerInvoker<TResult> where TCommand : ICommand<TResult>
{
    public override Type HandlerType => typeof(ICommandHandler<TCommand, TResult>);

    public override Task<TResult> InvokeAsync(object handler, IMessage message, CancellationToken ct) =>
        ((ICommandHandler<TCommand, TResult>)handler).HandleAsync((TCommand)message, ct);
}
