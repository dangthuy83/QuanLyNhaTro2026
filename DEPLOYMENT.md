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

Mật khẩu quản trị phải có ít nhất 8 ký tự. Công cụ chỉ in hash ASP.NET Identity; không lưu mật khẩu
và không tự cập nhật cấu hình của ứng dụng.

Đặt kết quả vào secret/environment `AdminAuth__PasswordHash`; đặt tên đăng nhập trong
`AdminAuth__Username`. Production sẽ từ chối khởi động nếu thiếu password hash.

Màn đăng nhập có lựa chọn `Ghi nhớ trên thiết bị này`. Khi được chọn, cookie mã hóa tồn tại tối đa
365 ngày và được gia hạn khi tiếp tục sử dụng; không chọn thì dùng phiên 8 giờ. Đăng xuất luôn xóa
cookie của trình duyệt hiện tại.

### Mật khẩu local khi chạy F5 trong VS Code

Project có `UserSecretsId` riêng cho Development. Tạo hash và lưu vào user-secrets trên máy phát
triển; không đưa hash vào `appsettings*.json` hoặc source control:

```powershell
dotnet build tools/AdminPasswordHasher/AdminPasswordHasher.csproj
$localAdminHash = dotnet run --project tools/AdminPasswordHasher/AdminPasswordHasher.csproj --no-build
dotnet user-secrets set "AdminAuth:Username" "admin" --project QuanLyNhaTro.csproj
dotnet user-secrets set "AdminAuth:PasswordHash" $localAdminHash --project QuanLyNhaTro.csproj
Remove-Variable localAdminHash
```

Sau đó dừng phiên debug cũ và bấm F5 lại. Các lệnh này chỉ đổi cấu hình local của VS Code/
Development; không cập nhật environment của service NSSM.

Ảnh CCCD mới được lưu dưới `private-data/tenant-photos`, ngoài `wwwroot`, và chỉ được đọc
qua controller sau đăng nhập. Thư mục `private-data` phải được backup cùng database nhưng
không được web server/static-file middleware phục vụ trực tiếp.

## 2. Localhost và giao thức LAN

Profile phát triển tiếp tục bind `http://localhost:5000`. Mặc định Production vẫn yêu cầu
HTTPS. Dùng địa chỉ HTTPS cụ thể và chứng thư tin cậy trên các thiết bị nội bộ, ví dụ:

```powershell
$env:ASPNETCORE_ENVIRONMENT='Production'
$env:Security__UseHttps='true'
$env:ASPNETCORE_URLS='https://192.168.1.20:5443'
$env:ASPNETCORE_Kestrel__Certificates__Default__Path='C:\secure\quanlynhatro.pfx'
$env:ASPNETCORE_Kestrel__Certificates__Default__Password='...'
$env:AllowedHosts='192.168.1.20;quanlynhatro.lan'
$env:AdminAuth__Username='admin'
$env:AdminAuth__PasswordHash='...'
dotnet QuanLyNhaTro.dll
```

Ngoại lệ đã duyệt ngày 18/07/2026 cho production gia đình có 1-2 người dùng là HTTP nội bộ:

```powershell
$env:ASPNETCORE_ENVIRONMENT='Production'
$env:Security__UseHttps='false'
dotnet QuanLyNhaTro.dll --urls "http://0.0.0.0:5001"
```

Khi `Security__UseHttps=false`, ứng dụng không bật HSTS/HTTPS redirect và cookie đăng nhập dùng
chính giao thức của request. Thông tin đăng nhập và dữ liệu truyền qua HTTP không được mã hóa;
chỉ dùng trên LAN/Wi-Fi gia đình tin cậy. Chỉ mở đúng port LAN cần dùng trong firewall và tuyệt
đối không port-forward trên router. `/healthz` chỉ trả `{"status":"healthy"}` hoặc trạng thái
503, không lộ connection string, phiên bản hay dữ liệu nghiệp vụ.

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

### Kết quả pre-production REVIEW-028 ngày 17/07/2026

- DB vận hành chỉ đọc: 21 bảng, 7 trigger, 0 hợp đồng/hóa đơn/thanh toán/cọc/thu-chi và journal
  liên tục 1..11; migration 12 vẫn pending.
- Backup `quanlynhatro_review028_pre_cutover_20260717.sql`: 58.217 byte, SHA-256
  `EEE8EB475E0C5A353E72D3B51FFFB55C733DFD04DAE0908C24D66296561E669A`. Restore tạm khớp
  toàn bộ bảng, trigger, row count và journal.
- Migration 12 apply/rerun-safe trên DB tạm. Importer chặn ca thiếu chỉ số, rollback sạch khi giả
  lập crash giữa batch, retry thành công và chặn replay sau commit.
- Hậu kiểm rehearsal: 3 hợp đồng, 3 cư trú, 9 dịch vụ hợp đồng, cọc 3.700.000, công nợ 800.000,
  3 mốc chỉ số; không có hóa đơn/thanh toán/thu-chi giả; journal 1..12 và reconcile issue 0.
- Artifact publish cuối không chứa config local, `tools`, `Database` hoặc build artifact. Browser QA
  cùng binary pass desktop/mobile và các màn chính. Certificate dev chưa trust nên HTTPS Browser
  QA production/LAN còn là gate bắt buộc với certificate thật.

## 5. Runbook cutover production REVIEW-028

### 5.1 Điều kiện trước downtime

Phải có đủ và được người duyệt xác nhận:

1. Một file chứng từ nguồn thật (khuyến nghị ZIP chỉ đọc) và SHA-256 độc lập.
2. JSON import thật theo `tools/OpeningBalanceImporter/templates/opening-balance.template.json`.
3. Tổng kiểm soát: số hợp đồng, giai đoạn cư trú/đại diện, dịch vụ, số dư cọc, công nợ theo kỳ và
   chứng từ, số mốc điện/nước; tổng tiền cọc và công nợ.
4. Máy/hostname LAN, certificate/PFX đã trust, service account, quyền đọc/ghi
   `private-data/tenant-photos`, firewall chỉ cho subnet LAN và xác nhận không port-forward Internet.
5. Bản publish hiện hành và bản publish mới ở hai thư mục versioned; không overwrite bản cũ.

Nếu thiếu bất kỳ mục nào, dừng trước downtime. Không tự bổ sung, suy đoán hoặc sửa file nguồn.

### 5.2 Kế hoạch downtime

- Đặt cửa sổ 30-45 phút dù rehearsal nhỏ chạy nhanh hơn, để đủ thời gian backup, restore verify,
  migration, import, hậu kiểm và rollback nếu cần.
- Thông báo người vận hành, dừng app/service và xác nhận không còn process ghi DB.
- Trong downtime không nhập chỉ số, chốt hóa đơn, thu tiền, ghi cọc hoặc thu-chi từ máy khác.

### 5.3 Backup và migration 12

1. Tạo backup nhất quán bằng `mysqldump --single-transaction --routines --triggers`, ghi kích thước
   và SHA-256; backup cả `private-data` nếu có.
2. Restore backup sang DB tạm mới; đối chiếu bảng, trigger, exact row count và journal. Lệch là dừng.
3. Trỏ connection string ngoài source, không in vào log:

```powershell
$env:QUANLYNHATRO_CONNECTION='<production connection string from secret store>'
dotnet run --project tools/MigrationRunner -- status
dotnet run --project tools/MigrationRunner -- apply-next
dotnet run --project tools/MigrationRunner -- status
```

Chỉ chạy `apply-next` sau approval migration 12. Status sau phải liên tục 1..12; chạy status lại,
không replay migration 1..11 và không chạy `archive_pre_20260710`.

### 5.4 Validate và import file thật

```powershell
dotnet run --project tools/OpeningBalanceImporter -- validate `
  --input '<approved-batch.json>' --source '<approved-evidence.zip>'

dotnet run --project tools/OpeningBalanceImporter -- apply `
  --input '<approved-batch.json>' --source '<approved-evidence.zip>' `
  --confirm-sha '<approved SHA-256>'
```

- Chỉ chạy `apply` sau approval import thật và khi validate pass với đúng file/hash đã duyệt.
- Không dùng `--simulate-failure-after-contracts` ngoài rehearsal; cờ này bị chặn nếu không có
  `QUANLYNHATRO_REHEARSAL=1`.
- Chạy lại `validate` cùng nguồn sau apply phải bị chặn replay; không sửa JSON để né hash.

### 5.5 Hậu kiểm bắt buộc trước start app

- Journal liên tục 1..12; đúng một `DotMoSo` cho SHA nguồn thật.
- Tổng hợp đồng/cư trú/dịch vụ/chỉ số khớp input và từng mã chứng từ là duy nhất.
- Tổng ledger `SoDuMoSo` khớp cọc thực giữ; không có `ThuCoc` lịch sử giả.
- `CongNoMoSo` khớp tổng nguồn, chưa gắn hóa đơn trước kỳ đầu; `ThanhToan=0` ngay sau mở sổ.
- Không double-count công nợ; snapshot chỉ gắn một lần khi hóa đơn đầu tiên được tạo sau đó.
- Continuity chỉ số sạch và `/KiemTraDuLieu` có reconcile issue = 0.

Lệch bất kỳ tổng nào: không start app, không sửa tay; rollback toàn DB từ backup.

### 5.6 Switch/start release và smoke

Chỉ sau approval switch/start:

1. Cấu hình `ConnectionStrings__DefaultConnection`, `AdminAuth__Username`,
   `AdminAuth__PasswordHash`, `AllowedHosts`, certificate Kestrel và thư mục ảnh bằng environment/
   secret ngoài source. Xác nhận artifact không có appsettings local.
2. Bind HTTPS vào hostname/IP LAN đã duyệt; firewall chỉ mở cho LAN; không port-forward Internet.
3. Start service và kiểm tra `/healthz` trả 200 `{"status":"healthy"}`.
4. Smoke đăng nhập, phòng, hợp đồng, ledger cọc, công nợ, chỉ số, preview chốt hóa đơn và tải Excel.
5. Ghi timestamp, người thực hiện, hash backup/source/publish và tổng hậu kiểm vào WORKLOG.

### 5.7 Rollback

- Dừng ngay app mới nếu health, login, schema, tổng kiểm soát hoặc smoke sai.
- Restore nguyên backup pre-cutover vào DB production theo quy trình đã diễn tập; không cố rollback
  từng DDL/dòng import bằng SQL thủ công.
- Đổi lại thư mục/binary publish cũ và cấu hình cũ, start rồi smoke `/healthz`/login.
- Lưu evidence sự cố; không chạy lại cutover cho đến khi nguyên nhân và file nguồn được duyệt lại.

## 6. Approval gate còn mở

Gate migration đã hoàn tất ngày 17/07/2026 theo phê duyệt của Đỗ Đăng Thuỷ. Backup production
`quanlynhatro_pre_migration12_20260717_212738.sql` có 55.244 byte, SHA-256
`CD9ACECE1D98A7FD19208F874C0507DE8DE8996B5988302894745DCE3AE8F855`; restore verify khớp
21/21 bảng, 7/7 trigger, exact row counts và journal 1..11. MigrationRunner áp đúng sequence 12;
hậu kiểm journal 1..12, tổng sequence 78, bốn bảng mở sổ rỗng và dữ liệu nghiệp vụ không đổi.

Hai xác nhận riêng còn bắt buộc, theo đúng thứ tự:

1. Import file thật sau validate và duyệt tổng kiểm soát.
2. Đổi/start bản release production sau hậu kiểm DB.

Không replay migration 12, không gộp hai xác nhận còn lại và không coi approval migration là quyền
import dữ liệu hoặc start release.
