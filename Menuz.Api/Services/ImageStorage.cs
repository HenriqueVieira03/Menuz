namespace Menuz.Api.Services;

/// <summary>
/// Guarda as fotos dos produtos enviadas pela gerência em wwwroot/uploads e as serve como arquivos estáticos.
/// No banco fica o caminho relativo ("uploads/abc.jpg"); o app monta a URL completa com o endereço do servidor,
/// assim as imagens continuam funcionando se o IP do servidor mudar.
/// </summary>
public class ImageStorage(IWebHostEnvironment env, ILogger<ImageStorage> logger)
{
    public const string Folder = "uploads";
    public const long MaxBytes = 5 * 1024 * 1024; // 5 MB

    private static readonly Dictionary<string, string> Extensions = new()
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp"
    };

    private string WebRoot => env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
    private string Directory => Path.Combine(WebRoot, Folder);

    /// <summary>Valida e salva a imagem. Retorna o caminho relativo ou uma mensagem de erro.</summary>
    public async Task<(string? Path, string? Error)> SaveAsync(IFormFile file, CancellationToken ct)
    {
        if (file.Length == 0) return (null, "Arquivo vazio.");
        if (file.Length > MaxBytes) return (null, "A imagem pode ter no máximo 5 MB.");

        // Confere o conteúdo real (assinatura do arquivo), não só a extensão ou o tipo informado.
        await using var input = file.OpenReadStream();
        var header = new byte[12];
        var read = await input.ReadAsync(header, ct);
        var contentType = Detect(header.AsSpan(0, read));
        if (contentType is null) return (null, "Formato não suportado. Use JPG, PNG ou WEBP.");

        System.IO.Directory.CreateDirectory(Directory);
        var name = $"{Guid.NewGuid():N}{Extensions[contentType]}";

        input.Position = 0;
        await using (var output = File.Create(Path.Combine(Directory, name)))
            await input.CopyToAsync(output, ct);

        return ($"{Folder}/{name}", null);
    }

    /// <summary>Apaga uma imagem enviada anteriormente (ignora links externos).</summary>
    public void DeleteIfLocal(string? relativePath)
    {
        if (!IsLocal(relativePath)) return;

        var full = Path.GetFullPath(Path.Combine(WebRoot, relativePath!));
        if (!full.StartsWith(Path.GetFullPath(Directory), StringComparison.OrdinalIgnoreCase)) return;

        try
        {
            if (File.Exists(full)) File.Delete(full);
        }
        catch (IOException ex)
        {
            logger.LogWarning(ex, "Não foi possível apagar a imagem {Path}", relativePath);
        }
    }

    /// <summary>Caminho de upload válido: "uploads/&lt;nome&gt;" sem subpastas nem "..".</summary>
    public static bool IsLocal(string? path) =>
        path is not null
        && path.StartsWith(Folder + "/", StringComparison.Ordinal)
        && path.IndexOfAny(new[] { '\\', ':' }) < 0
        && !path.Contains("..")
        && path.Count(c => c == '/') == 1;

    private static string? Detect(ReadOnlySpan<byte> h)
    {
        if (h.Length >= 3 && h[0] == 0xFF && h[1] == 0xD8 && h[2] == 0xFF) return "image/jpeg";
        if (h.Length >= 8 && h[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A })) return "image/png";
        if (h.Length >= 12 && h[..4].SequenceEqual("RIFF"u8) && h[8..12].SequenceEqual("WEBP"u8)) return "image/webp";
        return null;
    }
}
