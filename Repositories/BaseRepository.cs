using System.Data;

namespace QuanLyNhaTro.Repositories;

/// <summary>
/// Base class cung cấp IDbConnection cho tất cả repository.
/// Connection được inject qua DI (Transient) — tự đóng sau mỗi request.
/// </summary>
public abstract class BaseRepository
{
    protected readonly IDbConnection _db;

    protected BaseRepository(IDbConnection db)
    {
        _db = db;
    }
}
