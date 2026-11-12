namespace BlazorApp3.Application.Cqrs;

/// <summary>Escrita que retorna um valor (ex.: Id criado).</summary>
public interface ICommand<TResult> : IMessage;

/// <summary>Escrita sem retorno (atalho para <c>ICommand&lt;Unit&gt;</c>; mesma interface, por isso no mesmo arquivo).</summary>
public interface ICommand : ICommand<Unit>;
