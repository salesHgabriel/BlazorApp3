namespace BlazorApp3.Application.Cqrs;

/// <summary>Commands que alteram roles/vínculos e exigem invalidar o cache de permissões.</summary>
public interface IInvalidatesPermissions;
