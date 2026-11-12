namespace BlazorApp3.Services;

public sealed record UploadedFile(string FileName, string Url, long Size, string ContentType);
