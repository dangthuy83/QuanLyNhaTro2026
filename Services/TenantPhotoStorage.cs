using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;

namespace QuanLyNhaTro.Services;

public sealed class TenantPhotoOptions
{
    public const string SectionName = "TenantPhotos";
    public string UploadDirectory { get; set; } = "uploads";
    public long MaxFileSize { get; set; } = 5 * 1024 * 1024;
    public long MaxPixels { get; set; } = 40_000_000;
}

public sealed class TenantPhotoValidationException(string message) : Exception(message);

public enum TenantPhotoDeleteResult
{
    NotPresent,
    Deleted,
    RejectedUnsafePath,
    Failed
}

public sealed class TenantPhotoStorage
{
    private static readonly Dictionary<string, string> ExtensionFormats =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"] = "jpeg",
            [".jpeg"] = "jpeg",
            [".png"] = "png",
            [".webp"] = "webp"
        };

    private readonly string _uploadRoot;
    private readonly string _urlRoot;
    private readonly TenantPhotoOptions _options;
    private readonly ILogger<TenantPhotoStorage> _logger;

    public TenantPhotoStorage(
        IWebHostEnvironment environment,
        IOptions<TenantPhotoOptions> options,
        ILogger<TenantPhotoStorage> logger)
    {
        _options = options.Value;
        _logger = logger;

        var webRoot = Path.GetFullPath(environment.WebRootPath);
        var configured = (_options.UploadDirectory ?? string.Empty)
            .Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar)
            .Trim(Path.DirectorySeparatorChar);
        if (string.IsNullOrWhiteSpace(configured) || Path.IsPathRooted(configured))
            throw new InvalidOperationException("TenantPhotos:UploadDirectory phải là đường dẫn tương đối bên trong wwwroot.");

        _uploadRoot = Path.GetFullPath(Path.Combine(webRoot, configured));
        if (!IsInside(_uploadRoot, webRoot))
            throw new InvalidOperationException("Thư mục ảnh CCCD phải nằm bên trong wwwroot.");

        _urlRoot = "/" + configured.Replace(Path.DirectorySeparatorChar, '/');
    }

    public string UploadRoot => _uploadRoot;
    public string UrlRoot => _urlRoot;

    public async Task<string> StoreAsync(IFormFile file, string label, CancellationToken cancellationToken = default)
    {
        if (file.Length <= 0)
            throw new TenantPhotoValidationException($"{label} không có nội dung.");
        if (file.Length > _options.MaxFileSize)
            throw new TenantPhotoValidationException($"{label} không được vượt quá {_options.MaxFileSize / 1024 / 1024} MB.");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!ExtensionFormats.TryGetValue(extension, out var expectedFormat))
            throw new TenantPhotoValidationException($"{label} chỉ chấp nhận JPG, PNG hoặc WEBP.");

        await using var input = file.OpenReadStream();
        var signatureFormat = await DetectSignatureAsync(input, cancellationToken);
        if (signatureFormat == null || !string.Equals(signatureFormat, expectedFormat, StringComparison.Ordinal))
            throw new TenantPhotoValidationException($"{label} có nội dung không khớp định dạng {extension.ToUpperInvariant()}.");

        input.Position = 0;
        try
        {
            var info = await Image.IdentifyAsync(input, cancellationToken);
            if (info == null || info.Width <= 0 || info.Height <= 0
                || (long)info.Width * info.Height > _options.MaxPixels)
                throw new TenantPhotoValidationException($"{label} không phải ảnh hợp lệ hoặc có kích thước ảnh quá lớn.");

            input.Position = 0;
            using var decoded = await Image.LoadAsync(input, cancellationToken);
        }
        catch (TenantPhotoValidationException)
        {
            throw;
        }
        catch (Exception ex) when (ex is InvalidImageContentException or UnknownImageFormatException
                                   or NotSupportedException or ArgumentException)
        {
            throw new TenantPhotoValidationException($"{label} bị hỏng hoặc không thể giải mã thành ảnh thật.");
        }

        Directory.CreateDirectory(_uploadRoot);
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var finalPath = Path.Combine(_uploadRoot, fileName);
        var stagingPath = finalPath + ".uploading";
        try
        {
            input.Position = 0;
            await using (var output = new FileStream(
                             stagingPath, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                             81920, FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await input.CopyToAsync(output, cancellationToken);
                await output.FlushAsync(cancellationToken);
            }
            File.Move(stagingPath, finalPath);
            return $"{_urlRoot}/{fileName}";
        }
        catch
        {
            TryDeletePhysical(stagingPath);
            TryDeletePhysical(finalPath);
            throw;
        }
    }

    public Task<TenantPhotoDeleteResult> DeleteAsync(string? storedPath)
    {
        if (string.IsNullOrWhiteSpace(storedPath))
            return Task.FromResult(TenantPhotoDeleteResult.NotPresent);
        if (!TryResolveStoredPath(storedPath, out var physicalPath))
        {
            _logger.LogWarning("Từ chối xóa đường dẫn ảnh CCCD không an toàn: {StoredPath}", storedPath);
            return Task.FromResult(TenantPhotoDeleteResult.RejectedUnsafePath);
        }

        try
        {
            if (!File.Exists(physicalPath)) return Task.FromResult(TenantPhotoDeleteResult.NotPresent);
            File.Delete(physicalPath);
            return Task.FromResult(TenantPhotoDeleteResult.Deleted);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogError(ex, "Không thể xóa ảnh CCCD {StoredPath}", storedPath);
            return Task.FromResult(TenantPhotoDeleteResult.Failed);
        }
    }

    public bool TryResolveStoredPath(string storedPath, out string physicalPath)
    {
        physicalPath = string.Empty;
        var normalizedUrl = storedPath.Replace('\\', '/');
        var requiredPrefix = _urlRoot + "/";
        if (!normalizedUrl.StartsWith(requiredPrefix, StringComparison.OrdinalIgnoreCase)) return false;

        var relative = Uri.UnescapeDataString(normalizedUrl[requiredPrefix.Length..]);
        if (string.IsNullOrWhiteSpace(relative) || relative.Contains('\0')) return false;

        var candidate = Path.GetFullPath(Path.Combine(
            _uploadRoot,
            relative.Replace('/', Path.DirectorySeparatorChar)));
        if (!IsInside(candidate, _uploadRoot)) return false;

        physicalPath = candidate;
        return true;
    }

    private static async Task<string?> DetectSignatureAsync(Stream stream, CancellationToken cancellationToken)
    {
        var header = new byte[12];
        var read = 0;
        while (read < header.Length)
        {
            var count = await stream.ReadAsync(header.AsMemory(read, header.Length - read), cancellationToken);
            if (count == 0) break;
            read += count;
        }

        if (read >= 3 && header[0] == 0xff && header[1] == 0xd8 && header[2] == 0xff) return "jpeg";
        if (read >= 8 && header.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })) return "png";
        if (read >= 12
            && header.AsSpan(0, 4).SequenceEqual("RIFF"u8)
            && header.AsSpan(8, 4).SequenceEqual("WEBP"u8)) return "webp";
        return null;
    }

    private static bool IsInside(string candidate, string root)
    {
        var normalizedRoot = root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var normalizedCandidate = candidate.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return normalizedCandidate.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase);
    }

    private static void TryDeletePhysical(string path)
    {
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch
        {
            // Best effort cleanup; the original exception remains the actionable failure.
        }
    }
}
