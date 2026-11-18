namespace BlazorApp3.Services.Files.Storage;

/// <summary>Quem pode ler o arquivo. O provedor decide como aplicar (container/bucket separado, ACL, pasta...).</summary>
public enum StorageVisibility
{
    /// <summary>Uploads exibidos na tela (logos, avatares): servidos em /uploads sem login.</summary>
    Public,
    /// <summary>Arquivos de um usuário (relatórios): só saem por um endpoint que confere o dono.</summary>
    Private
}

/// <summary>Arquivo aberto para leitura. Quem recebe é responsável pelo Dispose do <see cref="Content"/>.</summary>
public sealed record StoredFileStream(Stream Content, string ContentType, long? Length);

/// <summary>
/// Provedor de armazenamento de arquivos. O padrão é o disco local (<see cref="LocalFileStorageProvider"/>).
/// Para usar nuvem (Azure Blob Storage, Amazon S3, Google Cloud Storage, MinIO...): implemente esta interface,
/// registre com <c>services.AddFileStorageProvider&lt;MeuProvider&gt;()</c> e, se houver mais de um, escolha pelo
/// <see cref="Name"/> em <c>Storage:Provider</c>. Exemplos de Azure e S3 no README.
/// </summary>
/// <remarks>
/// <para>As chaves são caminhos relativos com "/" (ex.: <c>uploads/avatars/3f2c....png</c>, <c>reports/9a1b....xlsx</c>),
/// já validadas por <see cref="StorageKey.Normalize"/>: sem "..", sem barra inicial. Use-as como nome do blob/objeto.</para>
/// <para>Erros transitórios devem virar exceção: quem chama em job do Hangfire ganha novas tentativas.</para>
/// </remarks>
public interface IFileStorageProvider
{
    /// <summary>Nome usado em <c>Storage:Provider</c> (comparação sem diferenciar maiúsculas).</summary>
    string Name { get; }

    /// <summary>Grava (ou substitui) o arquivo. Devolve o tamanho gravado em bytes.</summary>
    Task<long> SaveAsync(string key, Stream content, string contentType, StorageVisibility visibility, CancellationToken ct = default);

    /// <summary>Abre o arquivo para leitura; null se não existir.</summary>
    Task<StoredFileStream?> OpenReadAsync(string key, CancellationToken ct = default);

    Task<bool> ExistsAsync(string key, CancellationToken ct = default);

    /// <summary>Apaga o arquivo (não falha se ele não existir).</summary>
    Task DeleteAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// URL para o navegador baixar direto do provedor (blob público, SAS do Azure, URL pré-assinada do S3...), válida por
    /// <paramref name="expiresIn"/>. Devolva null para o app ler pelo <see cref="OpenReadAsync"/> e repassar o conteúdo
    /// (é o que o disco local faz). O app só chama depois de conferir a permissão de quem pediu.
    /// </summary>
    /// <param name="downloadFileName">Nome sugerido no download (Content-Disposition), ou null para exibir no navegador.</param>
    Task<string?> GetDownloadUrlAsync(string key, TimeSpan expiresIn, string? downloadFileName = null, CancellationToken ct = default);
}

/// <summary>Ponto único de armazenamento usado pela aplicação: delega ao provedor escolhido.</summary>
public interface IFileStorage : IFileStorageProvider
{
    /// <summary>Provedor ativo é o disco local (uploads servidos como arquivos estáticos).</summary>
    bool IsLocal { get; }
}

/// <summary>Regras das chaves (iguais para todos os provedores).</summary>
public static class StorageKey
{
    /// <summary>Junta as partes com "/" e valida: sem "..", sem caminho absoluto, sem caracteres de controle.</summary>
    public static string Normalize(params string[] parts)
    {
        var segments = parts
            .SelectMany(p => (p ?? "").Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries))
            .ToList();
        if (segments.Count == 0 || segments.Any(s => s is "." or ".." || s.Any(char.IsControl) || s.Contains(':')))
            throw new ArgumentException("Chave de armazenamento inválida.", nameof(parts));
        return string.Join('/', segments);
    }
}
