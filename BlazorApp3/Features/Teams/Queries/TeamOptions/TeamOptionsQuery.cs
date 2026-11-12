using BlazorApp3.Application.Cqrs;
using BlazorApp3.Services;

namespace BlazorApp3.Features.Teams;

/// <summary>Times como opções de seleção (filtros e editor de usuário).</summary>
public sealed record TeamOptionsQuery : IQuery<List<SelectOption<int>>>;
