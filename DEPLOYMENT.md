# Triển khai vận hành

Baseline này dành cho một quản trị viên. Tất cả route nghiệp vụ cần cookie đăng nhập; chỉ
`/Account/Login`, `/Account/AccessDenied` và `/healthz` được truy cập ẩn danh. Không mở app
trực tiếp ra Internet.

## 1. Cấu hình bí mật

Không commit `appsettings.json`, mật khẩu, connection string hoặc chứng thư. Tạo hash mật
khẩu một lần bằng công cụ cục bộ (ký tự nhập không hiện trên màn hình):

```powershell
dotnet run --project tools/AdminPasswordHasher/AdminPasswordHasher.csproj
```

Đặt kết quả vào secret/environment `AdminAuth__PasswordHash`; đặt tên đăng nhập trong
`AdminAuth__Username`. Production sẽ từ chối khởi động nếu thiếu password hash.

Ảnh CCCD mới được lưu dưới `private-data/tenant-photos`, ngoài `wwwroot`, và chỉ được đọc
qua controller sau đăng nhập. Thư mục `private-data` phải được backup cùng database nhưng
không được web server/static-file middleware phục vụ trực tiếp.

## 2. Localhost và LAN HTTPS

Profile phát triển tiếp tục bind `http://localhost:5000`. Khi chạy trong LAN, dùng địa chỉ
HTTPS cụ thể và chứng thư tin cậy trên các thiết bị nội bộ, ví dụ:

```powershell
$env:ASPNETCORE_ENVIRONMENT='Production'
$env:ASPNETCORE_URLS='https://192.168.1.20:5443'
$env:ASPNETCORE_Kestrel__Certificates__Default__Path='C:\secure\quanlynhatro.pfx'
$env:ASPNETCORE_Kestrel__Certificates__Default__Password='...'
$env:AllowedHosts='192.168.1.20;quanlynhatro.lan'
$env:AdminAuth__Username='admin'
$env:AdminAuth__PasswordHash='...'
dotnet QuanLyNhaTro.dll
```

Chỉ mở đúng cổng LAN cần dùng trong firewall sau khi HTTPS và đăng nhập đã được kiểm tra.
Không port-forward trên router. `/healthz` chỉ trả `{"status":"healthy"}` hoặc trạng thái 503,
không lộ connection string, phiên bản hay dữ liệu nghiệp vụ.

## 3. Migration có journal

Các file trong `Database/updates/archive_pre_20260710` chỉ là lịch sử và tuyệt đối không
được chạy trên baseline hiện hành. Runner chỉ đọc connection string từ biến môi trường,
kiểm tra SHA-256, thứ tự liên tục và journal:

```powershell
$env:QUANLYNHATRO_CONNECTION='Server=...;Database=QuanLyNhaTro;User ID=...;Password=...'
dotnet run --project tools/MigrationRunner/MigrationRunner.csproj -- status
dotnet run --project tools/MigrationRunner/MigrationRunner.csproj -- bootstrap
dotnet run --project tools/MigrationRunner/MigrationRunner.csproj -- apply-next
```

- `status` chỉ đọc.
- `bootstrap` là thao tác một lần cho DB cũ chưa có journal: chỉ ghi một prefix liên tục mà
  từng migration đã được chứng minh bằng schema evidence; không replay SQL.
- `apply-next` chỉ chạy đúng migration kế tiếp sau khi checksum và thứ tự hợp lệ.
- DB mới tạo bằng `Database/schema.sql` có marker `FreshBaseline` bao phủ migration 1..11.
- Luôn chạy/lưu dry-run của migration, backup và có xác nhận riêng trước `bootstrap` hoặc
  `apply-next` trên DB vận hành.

## 4. Backup và restore drill

Trước migration hoặc phát hành, tạo backup database nhất quán và copy `private-data` vào
vùng backup có kiểm soát truy cập. Không coi backup là hợp lệ trước khi restore thử:

1. Restore vào database tạm có tên khác, trên cùng major MySQL.
2. Chạy `MigrationRunner status` với connection string của DB tạm.
3. Kiểm tra số dòng các bảng lõi và mở app tạm chỉ trên localhost.
4. Xác nhận đăng nhập, hóa đơn, thanh toán, ledger cọc và ảnh CCCD mẫu đọc được.
5. Xóa DB/thư mục restore tạm theo quy trình vận hành sau khi ghi lại kết quả.

Không dùng DB vận hành làm đích restore drill và không tự động sửa dữ liệu khi reconcile.
