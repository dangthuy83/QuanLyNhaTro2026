using System.Data;
using MySqlConnector;
using QuanLyNhaTro.Models;
using QuanLyNhaTro.Repositories;

namespace QuanLyNhaTro.Services;

public sealed class DuplicateCccdException(KhachThue existingProfile)
    : Exception("CCCD đã thuộc một hồ sơ khác.")
{
    public KhachThue ExistingProfile { get; } = existingProfile;
}

public sealed class DuplicatePhoneConfirmationException(IReadOnlyList<KhachThue> matchingProfiles)
    : Exception("Số điện thoại đang được dùng ở hồ sơ khác.")
{
    public IReadOnlyList<KhachThue> MatchingProfiles { get; } = matchingProfiles;
}

public sealed class TenantInUseException()
    : Exception("Khách đã có lịch sử cư trú hoặc dữ liệu nghiệp vụ liên quan nên không thể xóa hồ sơ.");

public sealed record TenantDeleteResult(bool PhotoCleanupComplete);

public class KhachThueService(
    IDbConnection db,
    KhachThueRepository repository,
    TenantPhotoStorage photoStorage)
{
    public async Task<KhachThue?> TimHoSoTrungCccdAsync(string? cccd, int? excludeId = null)
    {
        var normalized = Normalize(cccd);
        return normalized == null ? null : await repository.GetByCccdAsync(normalized, excludeId);
    }

    public async Task<IReadOnlyList<KhachThue>> TimHoSoTrungSoDienThoaiAsync(
        string? phone,
        int? excludeId = null)
    {
        var normalized = Normalize(phone);
        return normalized == null
            ? []
            : (await repository.GetByPhoneAsync(normalized, excludeId)).ToList();
    }

    public async Task<int> CreateAsync(
        KhachThue khach,
        IFormFile? frontPhoto,
        IFormFile? backPhoto,
        bool confirmDuplicatePhone,
        CancellationToken cancellationToken = default)
    {
        NormalizeProfile(khach);
        await EnforceDuplicateRulesAsync(khach, null, confirmDuplicatePhone);

        string? newFront = null;
        string? newBack = null;
        try
        {
            newFront = await StoreOptionalAsync(frontPhoto, "Ảnh CCCD mặt trước", cancellationToken);
            newBack = await StoreOptionalAsync(backPhoto, "Ảnh CCCD mặt sau", cancellationToken);
            khach.AnhCCCDMatTruoc = newFront;
            khach.AnhCCCDMatSau = newBack;

            var connection = (MySqlConnection)db;
            if (connection.State != ConnectionState.Open) await connection.OpenAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            try
            {
                await EnsureCccdAvailableAsync(khach.CCCD, null, transaction);
                var id = await repository.InsertAsync(khach, transaction);
                await transaction.CommitAsync(cancellationToken);
                return id;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
        catch (MySqlException ex) when (ex.Number == 1062)
        {
            await CleanupNewPhotosAsync(newFront, newBack);
            var duplicate = await TimHoSoTrungCccdAsync(khach.CCCD);
            if (duplicate != null) throw new DuplicateCccdException(duplicate);
            throw;
        }
        catch
        {
            await CleanupNewPhotosAsync(newFront, newBack);
            throw;
        }
    }

    public async Task UpdateAsync(
        int id,
        KhachThue submitted,
        IFormFile? frontPhoto,
        IFormFile? backPhoto,
        bool confirmDuplicatePhone,
        CancellationToken cancellationToken = default)
    {
        NormalizeProfile(submitted);
        var existing = await repository.GetByIdAsync(id)
                       ?? throw new KeyNotFoundException("Không tìm thấy hồ sơ khách thuê.");
        await EnforceDuplicateRulesAsync(submitted, id, confirmDuplicatePhone);

        string? newFront = null;
        string? newBack = null;
        string? oldFront = null;
        string? oldBack = null;
        try
        {
            newFront = await StoreOptionalAsync(frontPhoto, "Ảnh CCCD mặt trước", cancellationToken);
            newBack = await StoreOptionalAsync(backPhoto, "Ảnh CCCD mặt sau", cancellationToken);

            var connection = (MySqlConnection)db;
            if (connection.State != ConnectionState.Open) await connection.OpenAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            try
            {
                existing = await repository.GetByIdForUpdateAsync(id, transaction)
                           ?? throw new KeyNotFoundException("Không tìm thấy hồ sơ khách thuê.");
                await EnsureCccdAvailableAsync(submitted.CCCD, id, transaction);

                oldFront = existing.AnhCCCDMatTruoc;
                oldBack = existing.AnhCCCDMatSau;
                submitted.Id = id;
                submitted.AnhCCCDMatTruoc = newFront ?? oldFront;
                submitted.AnhCCCDMatSau = newBack ?? oldBack;
                await repository.UpdateAsync(submitted, transaction);
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
        catch (MySqlException ex) when (ex.Number == 1062)
        {
            await CleanupNewPhotosAsync(newFront, newBack);
            var duplicate = await TimHoSoTrungCccdAsync(submitted.CCCD, id);
            if (duplicate != null) throw new DuplicateCccdException(duplicate);
            throw;
        }
        catch
        {
            await CleanupNewPhotosAsync(newFront, newBack);
            throw;
        }

        if (newFront != null && !string.Equals(newFront, oldFront, StringComparison.OrdinalIgnoreCase))
            await photoStorage.DeleteAsync(oldFront);
        if (newBack != null && !string.Equals(newBack, oldBack, StringComparison.OrdinalIgnoreCase))
            await photoStorage.DeleteAsync(oldBack);
    }

    public async Task<TenantDeleteResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var connection = (MySqlConnection)db;
        if (connection.State != ConnectionState.Open) await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        KhachThue existing;
        try
        {
            existing = await repository.GetByIdForUpdateAsync(id, transaction)
                       ?? throw new KeyNotFoundException("Không tìm thấy hồ sơ khách thuê.");
            if (await repository.HasBusinessUsageAsync(id, transaction))
                throw new TenantInUseException();

            await repository.DeleteAsync(id, transaction);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (MySqlException ex) when (ex.Number == 1451)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw new TenantInUseException();
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        var frontResult = await photoStorage.DeleteAsync(existing.AnhCCCDMatTruoc);
        var backResult = await photoStorage.DeleteAsync(existing.AnhCCCDMatSau);
        var cleanupComplete = frontResult is TenantPhotoDeleteResult.NotPresent or TenantPhotoDeleteResult.Deleted
                              && backResult is TenantPhotoDeleteResult.NotPresent or TenantPhotoDeleteResult.Deleted;
        return new TenantDeleteResult(cleanupComplete);
    }

    private async Task EnforceDuplicateRulesAsync(
        KhachThue khach,
        int? excludeId,
        bool confirmDuplicatePhone)
    {
        var duplicateCccd = await TimHoSoTrungCccdAsync(khach.CCCD, excludeId);
        if (duplicateCccd != null) throw new DuplicateCccdException(duplicateCccd);

        var duplicatePhones = await TimHoSoTrungSoDienThoaiAsync(khach.SoDienThoai, excludeId);
        if (duplicatePhones.Count > 0 && !confirmDuplicatePhone)
            throw new DuplicatePhoneConfirmationException(duplicatePhones);
    }

    private async Task EnsureCccdAvailableAsync(
        string? cccd,
        int? excludeId,
        MySqlTransaction transaction)
    {
        if (cccd == null) return;
        var duplicate = await repository.GetByCccdAsync(cccd, excludeId, transaction);
        if (duplicate != null) throw new DuplicateCccdException(duplicate);
    }

    private async Task<string?> StoreOptionalAsync(
        IFormFile? file,
        string label,
        CancellationToken cancellationToken)
        => file is not { Length: > 0 }
            ? null
            : await photoStorage.StoreAsync(file, label, cancellationToken);

    private async Task CleanupNewPhotosAsync(params string?[] paths)
    {
        foreach (var path in paths.Where(x => x != null).Distinct(StringComparer.OrdinalIgnoreCase))
            await photoStorage.DeleteAsync(path);
    }

    private static void NormalizeProfile(KhachThue khach)
    {
        khach.HoTen = khach.HoTen.Trim();
        khach.CCCD = Normalize(khach.CCCD);
        khach.SoDienThoai = Normalize(khach.SoDienThoai);
    }

    private static string? Normalize(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}
