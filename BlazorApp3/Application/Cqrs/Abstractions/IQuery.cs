namespace BlazorApp3.Application.Cqrs;

/// <summary>Leitura: não altera estado.</summary>
public interface IQuery<TResult> : IMessage;
