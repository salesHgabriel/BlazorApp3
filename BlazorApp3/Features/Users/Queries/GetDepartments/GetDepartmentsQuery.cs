using BlazorApp3.Application.Cqrs;

namespace BlazorApp3.Features.Users;

/// <summary>Departamentos distintos já cadastrados (filtro e sugestões do editor).</summary>
public sealed record GetDepartmentsQuery : IQuery<List<string>>;
