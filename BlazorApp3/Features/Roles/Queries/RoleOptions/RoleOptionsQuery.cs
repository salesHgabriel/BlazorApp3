using BlazorApp3.Application.Cqrs;
using BlazorApp3.Services;

namespace BlazorApp3.Features.Roles;

/// <summary>Papéis como opções de seleção (filtros e editor de usuário).</summary>
public sealed record RoleOptionsQuery : IQuery<List<SelectOption<string>>>;
