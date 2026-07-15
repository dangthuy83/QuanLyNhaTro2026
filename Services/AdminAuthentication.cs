using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace QuanLyNhaTro.Services;

public sealed class AdminAuthOptions
{
    public const string SectionName = "AdminAuth";
    public string Username { get; set; } = "admin";
    public string? PasswordHash { get; set; }
}

public sealed class AdminCredentialService(IOptions<AdminAuthOptions> options)
{
    private readonly PasswordHasher<string> _hasher = new();

    public bool Verify(string username, string password)
    {
        var configured = options.Value;
        if (string.IsNullOrWhiteSpace(configured.PasswordHash)
            || !string.Equals(username?.Trim(), configured.Username, StringComparison.Ordinal))
            return false;
        return _hasher.VerifyHashedPassword(configured.Username, configured.PasswordHash, password)
            is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }
}
