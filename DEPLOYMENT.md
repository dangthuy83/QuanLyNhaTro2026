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

### Kết quả drill REVIEW-025 ngày 16/07/2026

- Backup DB vận hành đã restore sang DB tạm: 21/21 bảng, 7/7 trigger, row count và
  `MigrationJournal` 1..11 khớp nguồn.
- Dataset kỳ 06/2026 chỉ được tạo/cập nhật trên DB tạm. DB vận hành được hậu kiểm bằng
  transaction `READ ONLY` và vẫn không có hợp đồng, chỉ số, hóa đơn, thanh toán hay thu chi.
- Các đầu ra phiếu thu HTML/Excel, công nợ Excel và thu-chi Excel đã trả HTTP 200 dưới launcher
  localhost. Export công nợ/thu-chi dùng độ rộng cột xác định, không gọi tự dò font hệ thống.
- DB restore tạm phải được xóa sau khi lưu evidence; không giữ dataset diễn tập làm dữ liệu thật.

### Kết quả drill REVIEW-026 ngày 16/07/2026

- Backup DB vận hành đã restore sang DB tạm với 21/21 bảng, 7/7 trigger, row count và
  `MigrationJournal` 1..11 khớp nguồn. Không chạy archive hoặc replay migration.
- Bốn ca quyết toán cọc/công nợ chỉ được tạo trên DB tạm: cọc dư, cọc thiếu, chuyển phòng có
  chênh lệch cọc/nợ cũ và guard hóa đơn đã settlement. Reconcile/payment/ledger mismatch đều 0;
  công nợ chuyển phòng không double-count.
- DB vận hành được hậu kiểm bằng transaction `READ ONLY`, vẫn có 0 hợp đồng, hóa đơn, thanh toán,
  giao dịch cọc và thu chi; journal vẫn liên tục 1..11.
- DB restore tạm đã được xóa sau khi lưu evidence; file dump nằm trong `.tmp/review026` và
  không được commit hay coi là dữ liệu vận hành.

### Kết quả drill REVIEW-027 ngày 16/07/2026

- Backup `quanlynhatro_review027_pre_opening_20260716_224055.sql` có 57.676 byte,
  SHA-256 `1B7A7D18D974D40DCD02DDFB6EE745228BA8F4A738DBE69CEC3C56C34521DE35`; restore
  khớp 21 bảng, 7 trigger, từng row count và journal 1..11.
- Migration 12 chỉ áp trên DB tạm. Dataset qua service pass: 4 hợp đồng, 4 ledger `SoDuMoSo`,
  2 snapshot công nợ, 4 mốc chỉ số mở sổ, 4 hóa đơn, 0 thanh toán giả, công nợ 14.030.000,
  reconcile issue = 0; ca thiếu mốc chỉ số bị chặn và không tạo row.
- Fresh schema smoke pass với 25 bảng, 4 bảng mở sổ và baseline journal sequence 12. DB vận hành
  hậu kiểm SELECT-only vẫn 0 hợp đồng/hóa đơn/thanh toán/cọc/thu chi và journal liên tục 1..11.
- Không chạy archive, không replay migration cũ, không giữ dataset rehearsal làm dữ liệu thật.
