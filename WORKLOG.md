# WORKLOG.md - Nhật Ký Làm Việc

## Dự án: Quản lý nhà trọ (ASP.NET Core MVC)

File này ghi lại tiến trình theo thời gian: đã làm gì, lỗi nào đã sửa, trạng thái build/test hiện tại. Phiên mới nên đọc `DECISIONS.md` trước, rồi đọc file này.

---

## Trạng Thái Hiện Tại

| Mục | Trạng thái |
|---|---|
| Giai đoạn | REVIEW-001 đến REVIEW-027 hoàn tất; REVIEW-028 đã chuẩn bị importer/release và áp migration 12 production; còn gate import thật và switch/start release |
| Build | Phiên 71: Debug/Release/publish sạch 0 warning/0 error; Browser QA artifact publish pass trên desktop/mobile |
| Restore | Đã restore NuGet thành công sau khi trỏ cache vào thư mục workspace |
| Database | Phiên 72: migration 12 đã áp production qua MigrationRunner sau backup/restore verify; journal 1..12, tổng 78, bốn bảng mở sổ rỗng và dữ liệu nghiệp vụ không đổi. |
| GitHub repo | `https://github.com/dangthuy83/QuanLyNhaTro2026.git` |
| Quyết định quan trọng | `HopDong.TienThueThoaThuan` là giá gốc riêng; lịch sử tăng/giảm giá thuê scope theo `HopDong`; `Phong.GiaThueMacDinh` chỉ gợi ý hợp đồng mới. |

### Việc cần làm tiếp

| # | Việc | Ghi chú | Ưu tiên |
|---|---|---|---|
| 0 | Duyệt Phase 1 của review phiên 49 | Đã duyệt và triển khai REVIEW-001 đến REVIEW-007 theo từng nhóm hẹp | Hoàn tất |
| 0.1 | Chặn mọi đường đóng hợp đồng bỏ qua quyết toán | REVIEW-001/002/010 đã xong | Hoàn tất |
| 0.2 | Lock thanh toán/cọc | REVIEW-003/004/005 đã xong | Hoàn tất |
| 0.3 | Chống overlap và bảo toàn lịch sử khách/chỉ số/hóa đơn | REVIEW-006 đến REVIEW-018 đã xong | Hoàn tất batch hiện tại |
| 1 | Rà dữ liệu test sau smoke test | Dữ liệu có tiền tố `TEST_Codex_*`, `TEST_P*`, `TEST_METER_*`, `TEST_KHACH_*`, `TEST_MOVE_*`, `TEST_RETURN_*`, `TEST_LEDGER_*`, `TEST_DEBT_EDGE_*`, `TEST_QUICKPAY_*`, `TEST_BULK_METER_*`, `TEST_BULK_INVOICE_PREVIEW_*`, `TEST_UI_CONTRACT_SCOPE_*` | Thấp |
| 2 | Theo dõi edge case công nợ trên dữ liệu vận hành thật | Smoke test nhiều hóa đơn nợ, trả phòng có nợ cũ và chặn xóa hóa đơn mang nợ kỳ trước đã pass | Trung bình |
| 3 | Rà lại `Database/schema.sql` encoding | File schema hiển thị mojibake trong terminal; cần chuẩn hóa nếu muốn đọc comment tiếng Việt | Trung bình |
| 4 | Chốt nhãn `LoaiDoiTuong` của `LichSuThayDoiGia` | Đã thống nhất `HopDong` và `DichVu` trong code/model/schema ở REVIEW-003/012 | Hoàn tất |
| 5 | Rà UI ledger cọc sau vận hành thực tế | Theo dõi thêm nhu cầu lọc/in phiếu sau khi dùng thật | Thấp |
| 6 | Rà UI in phiếu thu sau vận hành | Bản HTML print tối thiểu đã có; theo dõi nhu cầu thêm logo/thông tin chủ nhà/mẫu phiếu riêng | Thấp |
| 7 | Rà tiếp flow preview chốt hóa đơn hàng loạt sau vận hành | Đã thêm filter theo Nhà, tìm phòng/khách, lọc trạng thái dòng và chọn tất cả dòng sẵn sàng theo bộ lọc | Thấp |
| 8 | Nhắc nợ giai đoạn 2/3 | Sau này mới cân nhắc copy mẫu tin, log đã nhắc, Telegram/ZNS/SMS; chưa làm bây giờ | Thấp |
| 9 | Nâng cấp UI bằng Syncfusion | Làm sau nghiệp vụ lõi; xem `PROJECT_REVIEW.md` mục 8 | Trung bình |
| 10 | Rà tiếp UI chỉ số nhiều đoạn sau vận hành thật | Phiên 38 đã smoke test qua MVC form thật; in-app browser plugin bị lỗi hạ tầng `failed to write kernel assets` nên chưa có screenshot browser | Thấp |
| 11 | Rà UI khoản phát sinh sau pilot | Đã có bản tối thiểu cho hợp đồng/hóa đơn/trả phòng; theo dõi nhu cầu ảnh hiện trạng, danh mục tài sản, hoặc báo cáo riêng | Thấp |
| 12 | Rà màn sẵn sàng vận hành sau khi có dữ liệu thật | Màn `KiemTraDuLieu/Index` đã build pass; cần dùng với dữ liệu thật để tinh chỉnh bộ lọc/badge nếu phát sinh cách vận hành mới | Thấp |

### Quy ước GitHub

- Remote chuẩn của dự án là `origin = https://github.com/dangthuy83/QuanLyNhaTro2026.git`.
- Khi có thay đổi code hoặc tài liệu đáng kể và đã xác nhận xong, commit rồi push lên `origin/main` để đồng bộ lịch sử làm việc.
- Nếu thay đổi chỉ mang tính thử nghiệm hoặc local tạm thời, giữ lại ở workspace cho đến khi chốt.

### Bug cũ đã xác nhận không còn trong backlog

| Bug cũ | Trạng thái hiện tại |
|---|---|
| `HoaDonService.LapHoaDonAsync` chưa rõ có tra `LayGiaApDungAsync` | Đã có tra giá áp dụng cho phòng và dịch vụ |
| `ChuyenPhongService` dùng `DonGia * 1` cho dịch vụ `TheoChiSo` | Đã sửa: lấy `ChiSoDienNuoc`, tính `ChiSoCuoi - ChiSoDau`, lưu `ChiSoDienNuocId` |
| `GetCongNoAsync` dùng `dynamic` dễ lỗi cast | Đã typed sang `BaoCaoCongNoViewModel` |
| Chưa có guard chống hóa đơn trùng | Đã có `GetByHopDongKyAsync` và throw khi trùng kỳ |
| `KetQuaTraPhongViewModel.TenPhong` trả `Phòng #{id}` | Đã lấy tên phòng thật, fallback chỉ dùng khi không tìm thấy phòng |

---

## Phiên Làm Việc

### Phiên 72 - Áp migration 12 production trong downtime

- Người duyệt production write: Đỗ Đăng Thuỷ. Phạm vi chỉ gồm preflight, backup/restore verify,
  `MigrationRunner apply-next` sequence 12 và hậu kiểm; không import, không start app/release.
- Preflight xác nhận server `X2`, schema `quanlynhatro`, journal liên tục 1..11 (11 dòng, tổng 66),
  sequence 12/hash đúng manifest, bốn bảng mở sổ chưa tồn tại và
  `HopDong/HoaDon/ThanhToan/GiaoDichCoc/ThuChi=0`. Hai session MySQL Workbench được người dùng
  ngắt trước khi tiếp tục; gate downtime sau đó giữ `OTHER_CLIENTS=0`.
- Backup `quanlynhatro_pre_migration12_20260717_212738.sql`: 55.244 byte, SHA-256
  `CD9ACECE1D98A7FD19208F874C0507DE8DE8996B5988302894745DCE3AE8F855`. Restore sang
  `qlnt_m12_verify_20260717_212819` khớp 21/21 bảng, 7/7 trigger, 21/21 exact row counts và
  journal 11/11.
- `MigrationRunner status` báo đúng một pending sequence 12; `apply-next` chạy đúng một lần và áp
  `20260716_review027_opening_balances`; status sau không còn pending.
- Hậu kiểm production pass: journal 12 dòng liên tục 1..12, tổng 78; sequence 12 đúng tên/hash;
  bốn bảng mở sổ và hai cột ledger đủ, 13/13 constraint cùng index đủ; bảng mở sổ đều rỗng,
  21 bảng cũ giữ row count, 7 trigger giữ nguyên và năm bảng nghiệp vụ vẫn 0. Không rollback.
- Không chạy OpeningBalanceImporter, ứng dụng, release hoặc migration khác. Backup và DB restore
  tạm được giữ để làm bằng chứng; working tree REVIEW-028 được giữ nguyên đến bước commit riêng.

### Phiên 71 - REVIEW-028: chuẩn bị cutover và release, dừng trước production

- Xác minh `HEAD=main=origin/main=742f098`, worktree đầu phiên sạch. DB vận hành được đọc trong
  transaction `READ ONLY`: 1 nhà, 11 phòng, 13 khách, 7 dịch vụ; hợp đồng/cư trú/dịch vụ hợp
  đồng/chỉ số/hóa đơn/thanh toán/cọc/thu-chi đều 0; journal liên tục 1..11 và migration 12 pending.
- Thêm `tools/OpeningBalanceImporter`: input JSON + file chứng từ nguồn, SHA-256 tính từ file,
  validate tuyệt đối không ghi, apply xác nhận SHA và atomic cả batch qua `MoSoService`. Log chỉ
  in hash/tổng dòng; không đọc appsettings và không in CCCD, điện thoại hay connection string.
- Mở rộng model/service cho lịch sử cư trú có hiệu lực, nguồn dịch vụ/cọc/công nợ/chỉ số riêng,
  đúng một đại diện tại cutover, kiểm tra room/service/tenant tồn tại và không suy diễn trường thiếu.
- Backup nhất quán DB vận hành: 58.217 byte, SHA-256
  `EEE8EB475E0C5A353E72D3B51FFFB55C733DFD04DAE0908C24D66296561E669A`; restore khớp 21/21
  bảng, 7/7 trigger, toàn bộ row count và journal 1..11.
- MigrationRunner trên DB tạm áp đúng migration 12, rerun trả `No pending migration`. Blocker thiếu
  chỉ số bị chặn và không tạo hash/row. Dataset rehearsal tạo 3 hợp đồng, 3 cư trú, 9 đăng ký dịch
  vụ, 3 mốc chỉ số, cọc 3.700.000, công nợ 800.000; `HoaDon/ThanhToan/ThuChi=0`.
- Crash giả lập sau hợp đồng đầu rollback về 0 row của batch; retry áp đủ 2 hợp đồng và replay sau
  commit bị chặn. Hậu kiểm journal 1..12, snapshot công nợ chưa gắn, continuity không trùng ngày,
  reconcile issue = 0. Fresh schema: 25 bảng, 7 trigger, 4 bảng mở sổ, baseline sequence 12.
- Build Debug/Release và publish pass 0 warning/0 error. Web package cuối không chứa config local,
  tools, Database hay smoke build artifact. Browser QA pass login, phòng, hợp đồng, ledger cọc,
  công nợ, chỉ số, preview hóa đơn, readiness, Excel download; console sạch, mobile không overflow.
- Production HTTPS artifact khởi động được trên localhost nhưng Browser không nhận certificate dev
  chưa trust; trust bị từ chối vì thay đổi hệ thống lâu dài. UI QA dùng cùng binary publish trên HTTP
  Development riêng; đây không thay thế kiểm thử certificate/LAN production.
- Chưa có file dữ liệu thật. Không áp migration 12, không import và không đổi/start release trên DB
  vận hành. Chờ ba approval riêng theo `DEPLOYMENT.md`; không push.

### Phiên 69 - REVIEW-026: diễn tập quyết toán cọc/công nợ

- Xác minh `HEAD=main=origin/main=b8bcba7` trước diễn tập. DB vận hành chỉ chạy transaction
  `READ ONLY`: `HopDong/HoaDon/ThanhToan/GiaoDichCoc/ThuChi=0`, journal liên tục 1..11.
- Tạo backup `quanlynhatro_review026_pre_settlement_20260716_221534.sql`, 58.270 byte,
  SHA-256 `C5DCC6BF23B2910D9116EF85F88785B0B828E23ADFDA019C16763B2744DBC329`.
  Restore sang DB tạm khớp 21 bảng, 7 trigger, row count và journal 1..11.
- Dataset được user duyệt và chỉ chạy trên DB tạm: trả phòng cọc dư hoàn 300.000; trả phòng
  thiếu cọc trừ 2.000.000 và còn nợ 1.900.000; chuyển Phòng 202 -> 301 kết chuyển đúng
  1.000.000 nợ cũ, tổng công nợ chuỗi còn 4.500.000 và thu thêm cọc 1.000.000; hóa đơn đã
  `TruCoc`, `KetChuyenNo` hoặc mang `TienNoKyTruoc` đều bị chặn xóa/reissue.
- Rehearsal tái hiện `KetQuaTraPhongViewModel.TienHoanCoc=-1.900.000` khi thiếu cọc dù số
  hoàn thực tế là 0. Chuẩn hóa preview/result để `TienHoanCoc` không âm và dùng trường riêng
  `KhachConNoThem`; cập nhật hai view trả phòng đọc đúng hai trường.
- Browser QA phát hiện và sửa tiếp lỗi Razor render literal `.ToString("N0")`. Sau build lại,
  màn Confirm hiển thị `Khách còn nợ thêm 2,000,000 đ`, guard settlement và nút disabled đúng;
  đổi ngày trả phòng cập nhật URL/preview, console không có warning/error.
- Hậu kiểm DB tạm: 5 hợp đồng, 6 hóa đơn, 5 thanh toán, 11 dòng ledger cọc, 6 chỉ số;
  reconcile issue = 0, payment/ledger mismatch = 0, journal vẫn 1..11. DB vận hành hậu kiểm
  cuối vẫn không có dữ liệu nghiệp vụ và journal 1..11. Không chạy archive, không replay
  migration và không push. DB restore tạm đã được xóa sau khi lưu evidence.

### Phiên 68 - REVIEW-025: diễn tập sẵn sàng vận hành

- Xác minh baseline `HEAD=fde8668`; không push. Inventory DB vận hành chạy trong transaction
  `READ ONLY`: 1 nhà, 11 phòng trống, 13 khách, 7 dịch vụ; chưa có hợp đồng, chỉ số, hóa đơn,
  thanh toán, thu chi hoặc khóa sổ; journal liên tục 1..11.
- Tạo backup nhất quán `quanlynhatro_review025_pre_rehearsal_20260716_213448.sql`, 57.676 byte,
  SHA-256 `406E8550D84F00A4CFB0394EBEB309F2E1E4324D9FD041E54B3559E786F19729`.
  Restore drill pass 21/21 bảng, 7/7 trigger, row count và journal khớp.
- Sau xác nhận của user, chạy dataset kỳ 06/2026 trên DB tạm: ba hợp đồng, bốn cư trú,
  tám đăng ký dịch vụ và không có `Gửi xe`. Blocker thiếu chỉ số của hợp đồng C hoạt động;
  sau khi nhập đủ ba chỉ số, hóa đơn A/B/C lần lượt là 2.370.000 / 2.226.667 / 2.940.000 đồng.
- Ghi thanh toán A 1.000.000 tiền mặt, B 2.226.667 chuyển khoản; B đã thu đủ, A thu một phần,
  C chưa thu. Báo cáo công nợ, nhắc nợ, reconcile và chi tiết ba hóa đơn trả HTTP 200.
- Tạo chi 300.000 tháng 6, khóa sổ, xác nhận sửa/xóa/thêm tháng 6 đều bị chặn; tạo bút toán
  đảo 300.000 tháng 7 tham chiếu `ThuChiGocId=1` và ghi rõ `#1`. Reconcile lệch trạng thái = 0,
  overlap = 0, journal DB tạm vẫn 1..11.
- Tái hiện lỗi HTTP 500 ở Excel công nợ/thu-chi: ClosedXML `AdjustToContents()` đọc thư mục
  font user bị từ chối quyền. Thay bằng độ rộng cột xác định; build sạch và hậu kiểm phiếu thu
  HTML, phiếu thu Excel, công nợ Excel, thu-chi Excel đều HTTP 200.
- In-app Browser/Chrome không truy cập được localhost (`ERR_EMPTY_RESPONSE`), nên rehearsal
  dùng HTTP host-side đã xác thực; không có screenshot/visual QA. DB vận hành hậu kiểm cuối
  vẫn 0 dữ liệu nghiệp vụ và journal 1..11; DB restore tạm được xóa sau khi lưu evidence.

### Phiên 67 - Áp migration journal và REVIEW-022 trên DB vận hành

- Xác minh Git trước apply: `HEAD=8f6845b`, `main` khớp `origin/main`, worktree sạch.
- Dry-run DB vận hành chạy trong transaction `READ ONLY`: `MigrationJournal=0`,
  `InvalidThuChiRows=0`, chưa có `ThuChiGocId`, `ThuChiKySo` hoặc trigger REVIEW-022;
  bảng `ThuChi` có 0 dòng.
- Tạo dump nhất quán `quanlynhatro_pre_review022_20260716_204132.sql`, SHA-256
  `07CA4ADE4997BB289AEFB76BC3342E102CE0794622EDDB716DF8512E464251FA`.
- Restore drill DB tạm pass: 19/19 bảng, 4/4 trigger và row count của mọi bảng khớp;
  DB tạm đã được xóa sau kiểm tra.
- Sau xác nhận riêng của user, runner bootstrap đúng prefix evidence 1..10, không replay SQL cũ;
  migration 11 được xác nhận pending trước apply.
- `apply-next` chỉ áp `20260716_review022_monthly_book_lock`; journal sau apply có 11 dòng
  liên tục (`BootstrapEvidence=10`, `Runner=1`). Hậu kiểm pass: `ThuChiKySo=1`,
  `ThuChiGocId=1`, index/FK=1/1, 3 CHECK, 3 trigger BEFORE INSERT/UPDATE/DELETE,
  procedure tạm không còn và không phát sinh dòng `ThuChi`/`ThuChiKySo`.
- Không chạy `archive_pre_20260710`, không sửa/chạy lại REVIEW-014..019 và không push.

### Phiên 66 - REVIEW-020 đến REVIEW-024: kỳ mặc định, reconcile, khóa sổ, bảo vệ LAN và migration journal

Đã triển khai theo đề xuất user duyệt ngày 16/07/2026:

- REVIEW-020: dùng helper kỳ N-1 cho hóa đơn, chỉ số, dashboard và sẵn sàng vận hành.
- REVIEW-021: thêm reconcile snapshot/ledger/link SELECT-only, không auto-fix.
- REVIEW-022: validation `ThuChi`, service transaction, khóa sổ tháng không unlock, bút toán
  điều chỉnh tham chiếu dòng gốc, bảng `ThuChiKySo`, FK/index và ba trigger chống direct SQL.
- REVIEW-023: một-admin cookie auth, production HTTPS/HSTS, health tối thiểu, ảnh CCCD ngoài
  `wwwroot`, hướng dẫn LAN/backup/restore. QA mobile phát hiện overflow và đã thêm menu trượt.
- REVIEW-024: `MigrationJournal`, manifest SHA-256 1..11 và runner status/bootstrap/apply-next;
  archive không được thực thi.

Kiểm chứng:

```text
dotnet build --no-restore: 0 warning, 0 error
MigrationRunner/AdminPasswordHasher build: 0 warning, 0 error
Fresh schema MySQL 8: 21 tables, 1 FreshBaseline marker, 3 ThuChi triggers
REVIEW-022: add-path + rerun pass; locked direct UPDATE blocked; invalid-data blocker trước DDL pass
REVIEW-022 service: update tháng khóa blocked; correction tháng mở pass; thiếu tham chiếu #gốc blocked
REVIEW-024: bootstrap evidence 1..10, apply-next 11, journal_rows=11
Browser: login/logout/protected routes, HoaDon/KiemTra mặc định 6/2026, reconcile read-only,
         desktop + 390x844, console 0 warning/error; mobile scrollWidth 461 -> 375 sau fix
Health: HTTP 200 {"status":"healthy"}
```

DB vận hành chỉ được đọc: `MigrationJournal` chưa tồn tại, `photo_tokens=0`,
`legacy_photo_tokens=0`. Không áp `20260716_review022_monthly_book_lock.sql`, không bootstrap
journal, không chạy archive, không sửa REVIEW-014..019 và không push. Việc áp migration/journal
phải có backup, dry-run và xác nhận riêng trong phiên mới.

### Phiên 65 - REVIEW-019: continuity chỉ số ngoài hợp đồng

Đã phân tích/tái hiện và triển khai riêng REVIEW-019 theo quyết định đã chốt:

- Mọi reset, đồng hồ hỏng, thay mới hoặc quay vòng ngoài hợp đồng phải dùng `LoaiGhiNhan=Reset`; không cho gap chỉ số ngầm. `ChiSoNgoaiHopDong` có cùng metadata/công thức reset với `ChiSoDienNuoc`.
- `MeterContinuityService` resolve chuỗi chung chỉ số hợp đồng cũ -> ngoài hợp đồng -> hợp đồng mới. Create/update phải nối cả mốc trước và dữ liệu sau, chặn hai mốc cùng phòng/dịch vụ/ngày.
- `ChiSoNgoaiHopDongService` và `ChiSoService` khóa dòng phòng rồi kiểm tra dependency/ghi/xóa trong cùng transaction. Chỉ xóa tail chưa có dependency; mốc đang nuôi dòng sau hoặc chỉ số hợp đồng và chỉ số đã lên hóa đơn bị chặn.
- UI ngoài hợp đồng khóa mốc `Từ số` khi đã resolve được, thêm loại ghi nhận/reset metadata và hướng dẫn không dùng gap ngầm.
- Thêm migration apply-once `20260715_review019_off_contract_meter_continuity.sql`; blocker chạy trước DDL, không suy đoán/backfill reset từ lý do cũ và rerun-safe.

Dry-run và apply DB vận hành:

```text
TotalOff=0; TotalContract=0; ContinuityMismatch=0; SameDate=0
InsideContract=0; OffAnchorsWithLaterData=0; InvoicedMeterRows=0
Transaction read-only kết thúc bằng ROLLBACK.
Migration applied: ResetColumns=4; Checks=3; không sửa dữ liệu nghiệp vụ.
```

Kết quả kiểm tra hiện tại:

```text
dotnet build --no-restore: 0 warning, 0 error
SERVICE_SMOKE_PASS
MIGRATION_ADD_AND_RERUN_PASS (có ca blocker-before-DDL)
TEMP_DATABASES_REMAINING=0
```

Browser QA màn `ChiSoNgoaiHopDong` đã pass sau khi khởi động host trước khi mở tab:

- Page identity đúng, form và bảng render đầy đủ, filter phòng/dịch vụ giữ đúng giá trị.
- Chuyển `Bình thường -> Reset` hiển thị hướng dẫn và ba trường metadata; `ChiSoTruocReset`/`LyDoDieuChinh` bắt buộc, `ChiSoSauReset` cho phép trống để mặc định 0. Chuyển lại Bình thường ẩn field và bỏ required.
- Không có exception overlay, console warning/error bằng 0, stderr rỗng và host không có log `fail`.

Không sửa/chạy lại dữ liệu hoặc file REVIEW-014 đến REVIEW-018; không làm REVIEW-020+ và không push.

---

### Phiên 64 - REVIEW-018: invariant hóa đơn kỳ trả phòng

Đã phân tích, tái hiện và triển khai riêng REVIEW-018 sau khi được duyệt phương án:

- Preview và execute dùng chung invariant kỳ/ngày ở. Kỳ trả chưa có hóa đơn thì sinh hóa đơn kỳ cuối; hóa đơn khớp thì không sinh trùng; hóa đơn không khớp bị chặn trước mọi thay đổi hợp đồng, phòng, cọc hoặc công nợ.
- Hóa đơn đủ tháng `SoNgayO/SoNgayTrongThang = NULL/NULL` được xem là khớp khi trả cuối tháng và không khớp khi trả giữa tháng. Cặp số đúng `N/N` được chấp nhận; metadata khác bị chặn. Hóa đơn prorata phải khớp chính xác số ngày ở và số ngày trong tháng.
- Execute khóa phòng, hợp đồng và hóa đơn kỳ trả rồi tính/kiểm tra lại trong transaction. Hai request đồng thời chỉ một request hoàn tất; request còn lại rollback sạch.
- Chính sách xóa hóa đơn được dùng chung giữa `HoaDonService` và trả phòng. Hóa đơn không khớp chỉ được hướng dẫn xóa/reissue khi chưa có thanh toán/settlement; phase hiện tại không dùng credit note hay tự điều chỉnh. Hóa đơn kỳ trả đã kết thúc hợp đồng không được xóa ngược.
- `TraPhong/Confirm` hiển thị lý do chặn, link hóa đơn và vô hiệu hóa nút xác nhận; hóa đơn khớp hiển thị rõ không sinh trùng.

Dry-run DB vận hành trước triển khai, chỉ đọc:

```text
HopDong=0; HoaDon=0; CandidateContracts=0; CandidateInvoices=0
Transaction kết thúc bằng ROLLBACK; không có câu lệnh ghi.
```

Kết quả kiểm tra:

```text
CASE_1_NO_INVOICE_PRORATA_PASS Days=10/30
CASE_2_FULL_NULL_NULL_PASS Invoice=2;Count=1
CASE_3_MID_NULL_NULL_PASS Preview=BLOCKED;Execute=BLOCKED;StateUnchanged=True
CASE_4_PRORATA_MATCH_PASS Count=1
CASE_5_PRORATA_MISMATCH_PASS Preview=BLOCKED;Execute=BLOCKED;StateUnchanged=True
CASE_5B_FULL_WRONG_METADATA_PASS Preview=BLOCKED;Execute=BLOCKED;StateUnchanged=True
CASE_6_PAID_SETTLEMENT_BLOCK_PASS Guidance=True;StateUnchanged=True
CASE_7_CONCURRENT_RETURN_PASS Results=OK|BLOCKED;Invoices=1
CASE_8_DELETE_REISSUE_SERIALIZATION_PASS FinalInvoiceProtected=True
REVIEW_018_SMOKE_PASS
TEMP_DATABASES_DROPPED
```

Browser QA `TraPhong/Confirm` pass cho hóa đơn đủ tháng `NULL/NULL` bị chặn khi trả giữa tháng, đổi ngày sang cuối tháng thì được phép, và hóa đơn prorata khớp được phép. Không có overlay, console warning/error sạch. Host dừng sạch với marker `BROWSER_QA_TEMP_DATABASE_DROPPED`; cleanup độc lập xác nhận `Dropped=0`.

Không đổi schema/migration và không sửa/chạy lại dữ liệu/file REVIEW-014 đến REVIEW-017.

---

### Phiên 63 - REVIEW-017: snapshot ngày đến hạn và báo quá hạn

Đã chốt và triển khai riêng REVIEW-017:

- Hóa đơn kỳ N đến hạn theo `HopDong.NgayThanhToanHangThang` trong tháng N+1; tháng thiếu ngày dùng ngày cuối tháng. `HoaDonSnapshotService` tính và lưu `HoaDon.NgayDenHan` cho mọi đường lập hóa đơn thường, chuyển phòng và trả phòng.
- Cho nhập/sửa ngày thanh toán `1-31`. Hợp đồng đã có dữ liệu nghiệp vụ vẫn được đổi ngày thanh toán và ghi chú; ngày mới chỉ áp dụng cho hóa đơn tạo sau lần sửa, hóa đơn cũ giữ nguyên snapshot. Hợp đồng mới khi chuyển phòng kế thừa ngày thanh toán cũ.
- `HoaDonRepository.GetCongNoAsync` tính quá hạn từ snapshot; màn công nợ, nhắc nợ, chi tiết hóa đơn và Excel công nợ hiển thị ngày đến hạn.
- Tạo apply-once `Database/updates/20260715_invoice_due_date_snapshot.sql`: dry-run nguồn, blocker trước DDL, backfill riêng snapshot còn NULL, đổi `NOT NULL`, thêm CHECK ngày nằm trong tháng N+1 và rerun-safe. Fresh schema được cập nhật cùng invariant.

Dry-run DB vận hành trước triển khai, chỉ đọc:

```text
ServerDate=2026-07-15
HopDong=0; HoaDon=0; HoaDonConNo=0
InvalidPaymentDay=0; ExistingNgayDenHanColumn=0
Outstanding=0; OverdueDaysDifferent=0; FalseOverdueNow=0; DebtAffected=0
Edge cases: kỳ 01/2026 ngày 31 -> 28/02/2026; kỳ 03/2026 ngày 31 -> 30/04/2026; kỳ 11/2026 ngày 31 -> 31/12/2026
Transaction read-only kết thúc bằng ROLLBACK; không có câu lệnh ghi.
```

Apply/hậu kiểm DB vận hành:

```text
TotalInvoices=0; InvalidDueDateSource=0; NgayDenHanColumnCountBefore=0
MissingNgayDenHan=0; InvalidNgayDenHan=0
HoaDon.NgayDenHan: date, IS_NULLABLE=NO
CK_HoaDon_NgayDenHan=1
```

Kết quả kiểm tra:

```text
Application build succeeded: 0 warning, 0 error
FRESH_SERVICE_CONCURRENCY_PASS FirstDue=2026-06-30;SecondDue=2026-07-05;Concurrent=OK|BLOCKED
MIGRATION_BLOCKER_PASS DataUnchanged=True;SchemaUnchanged=True
MIGRATION_RERUN_PASS DueColumn=1;DueCheck=1;Nullable=NO;Backfill=2026-06-30
REVIEW_017_SMOKE_PASS
TEMP_DATABASES_DROPPED
```

Browser QA dùng Browser plugin tại `/BaoCao/CongNo` và `/NhacNo` trên DB tạm:

- Công nợ hiển thị `30/06/2026 -> 15 ngày`, `05/07/2026 -> 10 ngày`, `05/08/2026 -> Chưa quá hạn`; lọc `Đã quá hạn` còn đúng hai dòng.
- Nhắc nợ mặc định có đúng hai hóa đơn quá hạn; đổi bộ lọc sang `Chưa quá hạn` còn đúng hóa đơn đến hạn `05/08/2026`.
- Page identity, DOM không rỗng, không có error overlay, screenshot và tương tác lọc đều pass; console không có warning/error.
- Host tự dừng và `BROWSER_QA_TEMP_DATABASE_DROPPED` chạy trong `finally`.

Không sửa hoặc chạy lại dữ liệu/file REVIEW-014/015/016; không làm review khác. Git baseline đầu phiên `32932d6`, khớp `origin/main`; chưa push.

---

### Phiên 62 - REVIEW-016: constraint tài chính/thời gian và overlap ở DB

Đã chốt và triển khai riêng REVIEW-016:

- Dải năm nghiệp vụ `2000-2100` cho kỳ và ngày nghiệp vụ; không áp vào ngày sinh/ngày cấp CCCD. Thêm `BusinessDataLimits` và dùng ở các đường nhập kỳ/ngày chính để trả lỗi trước DB.
- Thêm 32 CHECK cho giá/tiền, kỳ/ngày, công thức tổng hóa đơn và chi tiết, trạng thái thanh toán, loại giao dịch và hình thức thanh toán. `ThanhToan.HinhThuc` chuyển thành `NOT NULL`.
- Thêm bốn trigger `BEFORE INSERT/UPDATE` chặn overlap hợp đồng và overlap đại diện. Trigger khóa dòng cha `Phong`/`HopDong` trước khi query overlap; update khóa cha theo thứ tự ID.
- Tạo apply-once `Database/updates/20260714_financial_time_invariants.sql`: dry-run theo nhóm, blocker trước mọi DDL, thêm constraint theo tên nếu thiếu, tạo lại trigger an toàn và không có câu lệnh sửa/xóa dữ liệu nghiệp vụ.

Dry-run DB vận hành trước triển khai, chỉ đọc:

```text
Nha=1; Phong=11
HopDong/HopDongKhachThue/ChiSo/HoaDon/ChiTiet/ThanhToan/GiaoDichCoc/KhoanPhatSinh/ThuChi/LichSuGia=0
Mọi nhóm vi phạm năm-tháng, ngày, tiền, công thức, trạng thái, overlap, đại diện và tổng thanh toán=0
ExistingCheckConstraints=23
```

Apply/hậu kiểm DB vận hành:

```text
REAL_MIGRATION_APPLIED Checks=32;Triggers=4;HinhThucNullable=NO
```

Kết quả kiểm tra:

```text
Build succeeded (0 error; NU1900 chỉ do vulnerability feed bị chặn)
FRESH_SCHEMA_CHECK_PASS Checks=32;Triggers=4;HinhThucNullable=NO
SERVICE_OVERLAP_PASS FirstContract=1;Second=BLOCKED
REPRESENTATIVE_TRIGGER_PASS Overlap=BLOCKED
CONCURRENT_CONTRACT_TRIGGER_PASS Results=OK|BLOCKED
CONCURRENT_REPRESENTATIVE_TRIGGER_PASS Results=OK|BLOCKED
MIGRATION_BLOCKER_PASS DataUnchanged=True;SchemaUnchanged=True
MIGRATION_RERUN_PASS Checks=32;Triggers=4;HinhThucNullable=NO
REVIEW_016_SMOKE_PASS
TEMP_DATABASES_DROPPED
```

Không có thay đổi UI nên Browser QA không áp dụng. Không sửa hoặc chạy lại dữ liệu/file REVIEW-014/015; không làm review khác và không tự sửa dữ liệu vận hành.

Git baseline đầu phiên: `78a4f59`, khớp `origin/main`. Chưa push.

---

### Phiên 61 - REVIEW-015: đồng bộ trạng thái Phòng và bảo toàn lịch sử Nhà–Phòng

Đã triển khai:

- Giữ `Phong.TrangThai` làm snapshot/cache; danh sách phòng, Details, dashboard và query gán dịch vụ suy trạng thái thuê theo hợp đồng tại ngày hiện tại. Danh sách phòng trống không còn tin riêng snapshot và loại cả phòng sửa chữa lẫn phòng có hợp đồng hiện tại/tương lai.
- Thêm `PhongLifecycleService` dùng chung để khóa phòng, kiểm tra hợp đồng hiệu lực/tương lai và đồng bộ snapshot. Tạo, kích hoạt, hủy, chuyển và trả hợp đồng chạy guard trong transaction với thứ tự khóa ổn định.
- Edit phòng chỉ cho đặt tay `DangSuaChua`; chặn khi có hợp đồng hiệu lực hoặc tương lai. Service tải lại dòng đã khóa, không tin `NhaId/TrangThai` từ request và chặn đổi Nhà nếu đã có hợp đồng, chỉ số, thu chi hoặc lịch sử nghiệp vụ; phòng chưa sử dụng vẫn đổi Nhà/sửa chữa được.
- Thêm `/Phong/Reconcile` chỉ đọc, báo trạng thái snapshot/theo hợp đồng, hợp đồng hiệu lực/tương lai, overlap, xung đột sửa chữa, trạng thái lạ và khóa đổi Nhà. Màn hình không có lệnh sửa hay ghi dữ liệu.

Dry-run DB vận hành trước triển khai, không ghi dữ liệu:

```text
TotalRooms=11
ActiveButSnapshotNotDangThue=0
NoActiveButSnapshotDangThue=0
RepairWithActiveOrFutureContract=0
RoomsWithMultipleActiveContractsToday=0
HistoryRoomsStillMovableViaUiService=0
UnknownRoomStatuses=0
```

Kết quả kiểm tra:

```text
dotnet build --no-restore: pass, 0 warning, 0 error
SCHEMA_SMOKE_PASS; Tables=19; Constraints=80; SeedServices=7
REVIEW_015_SMOKE_PASS
REVIEW_010_SMOKE_PASS
ACTIVE_CANNOT_MANUAL_EMPTY / ACTIVE_CANNOT_REPAIR
FUTURE_STAYS_EMPTY_BEFORE_START / FUTURE_CONTRACT_NOT_TRANSFER_TARGET
DUE_CONTRACT_ACTIVATED / CANCEL_SYNCS_ROOM_EMPTY / RETURN_SYNCS_ROOM_EMPTY
USED_ROOM_HOUSE_BLOCKED / UNUSED_ROOM_CAN_MOVE_AND_REPAIR
STATUS_TAMPER_IGNORED / CONCURRENT_CREATE_REPAIR_SERIALIZED
RECONCILE_DETECTS_MISMATCH / RECONCILE_READ_ONLY
TEMP_DATABASES_DROPPED
```

Browser QA Create/Edit/Details/Index/Reconcile và dashboard trên DB tạm pass; phòng có lịch sử khóa chọn Nhà, request sửa chữa xung đột trả lỗi thân thiện, phòng chưa dùng đổi Nhà và đặt sửa chữa được, trạng thái/count theo hợp đồng đúng và console không có warning/error. App/cổng/DB browser tạm đã dọn.

Không đổi schema nên không tạo apply-once và không sửa `Database/updates/README.md`. Không làm REVIEW-016, không sửa hoặc chạy lại dữ liệu/file REVIEW-014 và không tự reconcile dữ liệu vận hành.

Git baseline đầu phiên: `d51fd53`; user đã commit/push thủ công trước REVIEW-015.

---

### Phiên 60 - REVIEW-014: chống trùng hồ sơ khách và vòng đời ảnh CCCD an toàn

Đã triển khai:

- CCCD được `Trim`, rỗng thành `NULL`, chặn trùng ở service/UI và bằng generated `CCCDNormalized` + unique DB; lỗi race duplicate key được trả về hồ sơ cũ. Không merge/xóa/sửa CCCD tự động.
- SĐT trùng chỉ hiển thị cảnh báo và danh sách link hồ sơ; muốn lưu hồ sơ riêng phải xác nhận, không thêm unique DB cứng.
- `TenantPhotoStorage` dùng tên GUID do server sinh trong upload root cấu hình, giới hạn 5 MB, kiểm tra extension, magic bytes, kích thước pixel và decode ảnh thật bằng ImageSharp 2.1.13.
- Service không tin hidden/request path. File mới được cleanup nếu create/update DB lỗi; update lỗi giữ ảnh cũ; update thành công chỉ xóa ảnh cũ sau commit.
- Delete khóa hồ sơ, chặn mọi khách có `HopDongKhachThue` bằng lỗi nghiệp vụ; hồ sơ chưa dùng được xóa DB trước rồi mới cleanup ảnh an toàn. Path traversal/đường dẫn ngoài root bị từ chối.
- Thêm apply-once `Database/updates/20260713_tenant_identity_photo_lifecycle.sql`, cập nhật schema baseline, README và cấu hình mẫu.

Dry-run/apply DB vận hành:

```text
REAL_DRY_RUN TotalKhachThue=13;BlankOrNullCCCD=0;DuplicateCCCDGroups=0;DuplicatePhoneGroups=0;ProfilesWithPhotoPath=0;DuplicatePhotoPaths=0;MissingReferencedPhotos=0;OrphanFiles=0
REAL_MIGRATION_APPLIED
REAL_DRY_RUN ...;NormalizedColumns=1;UniqueConstraints=1
```

Kết quả kiểm tra:

```text
dotnet build --no-restore: pass, 0 warning, 0 error
SCHEMA_SMOKE_PASS
MIGRATION_RERUN_PASS
MIGRATION_DUPLICATE_BLOCKER_PASS Rows=2
IMAGE_SECURITY_PASS Text=True;FakeDecode=True;Oversize=True;Traversal=True
CREATE_DB_FAILURE_CLEANUP_PASS
EDIT_PHOTO_LIFECYCLE_PASS TrustedPath=True;FailureKeepsOld=True;SuccessCleansOld=True
DUPLICATE_RULES_PASS CccdBlocked=True;PhoneWarningOnly=True
DELETE_GUARDS_PASS UsedBlocked=True;UnusedDeleted=True;PhotoClean=True
CONCURRENT_CCCD_PASS Results=OK|BLOCKED
REVIEW_014_SMOKE_PASS
TEMP_DATABASES_DROPPED
```

Browser QA Create/Edit/Delete trên database tạm pass: CCCD trùng hiện link hồ sơ cũ; Edit không có hidden path ảnh; khách có lịch sử bị chặn xóa thân thiện; khách chưa dùng xóa thành công; console không có warning/error. Database/browser process tạm đã dọn.

Phạm vi giữ nguyên: không làm REVIEW-015/016, không redesign rộng, không merge/cleanup hồ sơ hoặc file vận hành.

### Phiên 59 - REVIEW-013: snapshot nhận diện chứng từ hóa đơn

Đã triển khai:

- `HoaDon` lưu snapshot nhà, phòng và khách đại diện/CCCD; `ChiTietHoaDon` lưu tên/đơn vị dịch vụ; khoản phát sinh lưu mô tả/số tiền thực tế tại lúc gắn hóa đơn.
- `HoaDonSnapshotService` là điểm ghi chung cho lập hóa đơn thường, chuyển phòng và trả phòng. Không có đại diện hoặc đại diện chồng thời gian trong kỳ bị chặn; các giai đoạn đại diện kế tiếp hợp lệ chọn giai đoạn mới nhất giao kỳ.
- Chi tiết hóa đơn, danh sách, phiếu thu HTML/Excel và báo cáo công nợ dùng snapshot nhận diện. Đổi dữ liệu gốc sau chốt không làm thay đổi chứng từ cũ.
- Xóa hóa đơn hợp lệ trả khoản phát sinh về `ChuaXuLy` đồng thời xóa snapshot liên kết, để lần chốt lại lấy đúng mô tả/số tiền mới.
- Thêm apply-once `Database/updates/20260713_invoice_identity_snapshot.sql` có dry-run fail-closed và backfill không xóa/merge dữ liệu; cập nhật baseline và README.

Dry-run và apply DB vận hành:

```text
REAL_DRY_RUN TotalInvoices=0;MissingScope=0;NoRepresentative=0;ManyRepresentatives=0;TotalDetails=0;InvalidServiceIdentity=0;LinkedCharges=0;BlankChargeDescriptions=0
REAL_MIGRATION_APPLIED
REAL_DRY_RUN ...;SnapshotColumns=11;SnapshotConstraints=1
```

Kết quả kiểm tra:

```text
dotnet build --no-restore: pass, 0 error; 1 warning NU1900 do môi trường
SCHEMA_SMOKE_PASS
MIGRATION_BACKFILL_RERUN_PASS
SERVICE_EXCEL_SNAPSHOT_PASS
SEQUENTIAL_REPRESENTATIVE_PASS
REPRESENTATIVE_GUARD_PASS
TEMP_DB_DROPPED
git diff --check: pass
```

Browser QA `/HoaDon` sau migration pass: trang render đúng, trạng thái chưa có hóa đơn hợp lệ và console không có warning/error; process test đã dừng.

Phạm vi giữ nguyên: không làm REVIEW-014/015/016; không merge hồ sơ khách; không thêm unique cứng CCCD; không tạo/xóa dữ liệu hóa đơn thật.

### Phiên 58 - REVIEW-012: khóa lịch sử giá theo hóa đơn và kỳ hiệu lực

Đã triển khai:

- `GiaService` khóa dòng `HopDong`/`PhongDichVu` và toàn bộ chuỗi lịch sử giá trong transaction trước khi thêm/xóa.
- Chặn thêm hoặc xóa mốc giá khi đã có hóa đơn từ kỳ áp dụng trở đi. Giá thuê kiểm tra đúng hợp đồng; giá dịch vụ kiểm tra hợp đồng đã đăng ký đúng `PhongDichVu` tại kỳ hóa đơn.
- Mốc giá tương lai không còn ghi ngay vào `PhongDichVu.DonGia`; giá base dịch vụ chỉ đồng bộ theo tháng hiện tại. `HopDong.TienThueThoaThuan` tiếp tục bất biến.
- Thêm/xóa mốc giữa chuỗi cập nhật `GiaCu` của mốc kế tiếp trong cùng transaction; lỗi ở bước sau rollback toàn bộ.
- Màn lịch sử giá resolve đúng giá hiện tại, hiển thị lỗi service khi lưu/xóa bị chặn và phục hồi đầy đủ view model sau lỗi.
- `PhongDichVuRepository.GetByIdAsync` map thêm `Phong`, sửa tiêu đề màn giá từ thiếu tên phòng thành `Phòng 101 — Bảo trì thang máy`.
- Sửa comment model `LichSuThayDoiGia` về scope thực tế `HopDong | DichVu`.

Dry-run database vận hành trước khi sửa:

```text
REAL_DRY_RUN FutureHistory=0;ServiceBaseMismatch=0;InvoicedHistory=0;BrokenChains=0
```

Không có dữ liệu cần cleanup, không đổi schema và không tạo apply-once.

Kết quả kiểm tra:

```text
dotnet build --no-restore: pass, 0 error; 1 warning NU1900 do môi trường
SCHEMA_SMOKE_PASS
CASE1_2_FUTURE_CURRENT_RESOLVER_PASS Current=100;Future=120
CASE4_DELETE_FUTURE_CHAIN_PASS NextGiaCu=120
CURRENT_PERIOD_SYNC_PASS Current=450
CASE3_INVOICE_GUARDS_PASS RentInsert=True;RentDelete=True;ServiceInsert=True
CASE5_CONCURRENCY_PASS Results=BLOCKED|OK
CASE6_ROLLBACK_PASS InsertedRows=0;NextGiaCu=300
CASE7_PREVIEW_STABILITY_PASS Old=100;Current=100
REVIEW_012_SMOKE_PASS
TEMP_DATABASE_DROPPED
git diff --check: pass
```

Browser QA tại `http://127.0.0.1:5128/Gia/DichVu?phongDichVuId=1` pass: page identity và nội dung render đúng; tiêu đề đủ phòng/dịch vụ; cảnh báo giá tương lai/khóa hóa đơn hiển thị; submit giá không đổi trả lỗi nghiệp vụ trên form nhưng vẫn giữ giá hiện tại `50,000 đ`; console không có warning/error. Process test đã dừng.

Phạm vi giữ nguyên: không làm REVIEW-013/014/015/016; không sửa dữ liệu giá thật; không thay đổi schema.

### Phiên 57 - REVIEW-008: lịch sử cư trú theo ngày và dịch vụ TheoNgười

Đã triển khai:

- Mở rộng `HopDongKhachThue` thành từng giai đoạn cư trú với `NgayBatDau`, `NgayKetThucDuKien`, `NgayKetThuc` thực tế nullable và `LaDaiDien`; unique theo hợp đồng/khách/ngày bắt đầu, có CHECK và index hiệu lực.
- `FixedServiceQuantityCalculator` đếm khách duy nhất có giai đoạn giao với kỳ. Khách ở một ngày vẫn tính đủ một suất; ngày dự kiến chỉ cảnh báo và không tham gia tính tiền.
- Thêm `CuTruService` khóa hợp đồng, chặn giai đoạn cùng khách chồng nhau và chặn đại diện kép; không có đường xóa cứng lịch sử.
- Tạo hợp đồng yêu cầu ít nhất một khách và đúng một đại diện ban đầu. Hủy hợp đồng tương lai giữ nguyên lịch sử cư trú.
- Chuyển phòng đóng giai đoạn cũ đúng ngày chuyển, mở giai đoạn mới từ hôm sau; trả phòng đóng mọi giai đoạn đang mở đúng ngày trả.
- Màn tạo hợp đồng và thêm người ở tìm server-side theo tên/SĐT/CCCD/biển số, debounce 300 ms, giới hạn 20 kết quả; khách cũ được chọn lại để tạo giai đoạn mới.
- `KhachThue/Details` có card lịch sử cư trú phía dưới. `KhachThue/Index` có phòng hiện tại, dự kiến rời, trạng thái và bộ lọc.
- CCCD trùng được chặn ở `KhachThueService`/UI và link về hồ sơ cũ; không merge và không tạo unique DB cứng ngoài phạm vi REVIEW-014.
- Thêm apply-once `Database/updates/20260713_tenant_residency_history.sql`, đồng thời cập nhật `Database/schema.sql` và README.

Dry-run database vận hành trước apply:

```text
TotalHopDongKhachThue=0
NoRepresentativeContracts=0
ManyRepresentativeContracts=0
DuplicateTenantInContract=0
DuplicateCCCD=0
InvalidContractDates=0
```

Kết quả kiểm tra:

```text
dotnet build --no-restore: pass, 0 error; 1 warning NU1900 do vulnerability feed môi trường.
SCHEMA_SMOKE_PASS ResidencyColumns=3
MIGRATION_SMOKE_PASS Before=2;After=2;BackfillStart=2026-01-01;BackfillEnd=2026-03-31
CASE1_ONE_DAY_FULL_UNIT_PASS Quantity=1
CASE2_3_HISTORY_AND_RETURN_PASS June=1;July=1;Periods=2
CASE4_5_OVERLAP_REPRESENTATIVE_PASS
CONCURRENCY_PASS Results=OK|BLOCKED
CASE6_TRANSFER_RETURN_PASS OldEnd=2026-07-10;NewStart=2026-07-11;ReturnEnd=2026-07-20
CASE7_8_BACKFILL_SEARCH_PASS HistoryPreserved=True;SearchRows=20
REVIEW_008_SMOKE_PASS
TEMP_DATABASE_DROPPED
REAL_APPLY_PASS Rows=0;MissingNgayBatDau=0
git diff --check: pass
```

Browser QA tại `http://127.0.0.1:5126` pass: danh sách khách render đủ bộ lọc; tìm `Đặng` ở màn tạo hợp đồng trả 3 kết quả server-side, chọn hồ sơ #12 tạo đúng một dòng đã chọn; không có console warning/error. Process test đã dừng.

Phạm vi giữ nguyên: không làm REVIEW-012/013/014 ngoài guard CCCD tối thiểu; không merge hồ sơ, không thêm unique cứng CCCD, không mở rộng Syncfusion/nhắn tin/auth.

### Phiên 56 - REVIEW-009: atomic batch và khóa chỉ số đã chốt

Đã triển khai thay đổi hẹp cho chỉ số điện/nước:

- Thêm `ChiSoService.LuuBatchAsync` bọc toàn bộ insert/update trong một MySQL transaction; bất kỳ validation/unique/DB error nào đều rollback toàn batch.
- Update khóa bản gốc `ChiSoDienNuoc FOR UPDATE`, giữ nguyên scope phòng, hợp đồng, dịch vụ và kỳ; không tin các hidden field từ form.
- `NgayDoc` là bắt buộc, phải thuộc đúng `Thang/Nam`; chỉ số có `HopDongId` còn phải nằm trong `[NgayBatDau, NgayKetThuc]` và đúng phòng hợp đồng.
- Update/delete khóa và kiểm tra `ChiTietHoaDon`; chỉ số đã được hóa đơn dùng bị chặn. Phase 1 dùng đường xóa/reissue hóa đơn hợp lệ trước, chưa tạo correction record.
- Controller nhập đơn lẻ, theo phòng và hàng loạt đều gọi service chung. Form mặc định ngày đọc theo cuối kỳ, được clamp theo ngày bắt đầu/kết thúc hợp đồng; batch chỉ hiển thị hợp đồng giao với kỳ đang nhập.
- Repository bổ sung overload insert/update/delete nhận connection + transaction; đường nghiệp vụ mới không ghi từng dòng ngoài transaction.

Kiểm tra thực tế trên database tạm:

- Dòng thứ hai vi phạm unique: cả hai dòng rollback, kỳ test còn 0 bản ghi.
- Chỉ số đã dùng trên hóa đơn: update/delete đều bị chặn và số liệu giữ nguyên.
- `NgayDoc` ngoài kỳ hoặc ngoài hiệu lực hợp đồng bị chặn.
- Chỉ số chưa dùng được update hợp lệ; sửa hidden scope phòng bị chặn.
- `REVIEW_009_SMOKE_PASS`; database tạm đã drop trong `finally`.
- Không thay đổi schema, không có migration và không đụng database vận hành.

Phạm vi giữ nguyên: chưa làm REVIEW-008 hoặc correction record; không mở rộng chỉ số ngoài hợp đồng, UI diện rộng, Syncfusion, nhắn tin hay auth/multi-user.

### Phiên 55 - REVIEW-011: hoàn tác an toàn khi xóa hóa đơn

Đã hoàn thiện `HoaDonService.XoaHoaDonAsync` theo quyết định Phase 1:

- Khóa dòng `HoaDon` bằng `SELECT ... FOR UPDATE` trước mọi guard và thao tác xóa, serialize với flow thu tiền đang dùng cùng khóa.
- Chặn xóa nếu có bất kỳ dòng `ThanhToan`, `GiaoDichCoc.HoaDonId`, `SoTienDaThu > 0` hoặc `TienNoKyTruoc > 0`; không chỉ tin dữ liệu tổng hợp trên hóa đơn.
- Khoản phát sinh liên kết hóa đơn chỉ được hoàn tác từ `DaDuaVaoHoaDon` về `ChuaXuLy` và xóa `HoaDonId`. Nếu đã ở trạng thái xử lý khác thì chặn toàn bộ thao tác.
- Khóa và tháo liên kết `HoaDonGhepId` ở hóa đơn còn lại trước khi xóa; sau đó xóa chi tiết và hóa đơn trong cùng transaction.

Kiểm tra thực tế:

- Service smoke database tạm pass: hóa đơn hợp lệ được xóa, chi tiết biến mất, khoản phát sinh trở lại `ChuaXuLy`, hóa đơn ghép còn lại có link `NULL`.
- Guard smoke pass cho payment row dù `SoTienDaThu` bị lệch bằng 0, `TienNoKyTruoc`, ledger cọc và khoản phát sinh đã xử lý.
- Concurrency smoke xóa đồng thời với thu tiền pass: chỉ một thao tác thành công; kết quả hoặc hóa đơn bị xóa sạch, hoặc hóa đơn và thanh toán cùng tồn tại nhất quán.
- Database tạm đã drop trong `finally`. Không thay đổi schema, không có migration và không đụng database vận hành.
- `dotnet build --no-restore` pass 0 error; warning duy nhất `NU1900` do sandbox không truy cập vulnerability feed.

Phạm vi giữ nguyên: chưa làm REVIEW-008/009; không mở rộng credit note/soft-delete hóa đơn, UI diện rộng, Syncfusion, nhắn tin hoặc auth/multi-user.

### Phiên 54 - REVIEW-010: hoàn thiện chuyển phòng

Đã triển khai chính sách chuyển phòng được duyệt:

- Giữ guard từ REVIEW-006: khóa lại hợp đồng cũ và hai dòng phòng theo thứ tự, kiểm tra overlap phòng đích trong cùng transaction.
- Khoản phát sinh `ChuaXuLy` đến hết `NgayChuyenDi` được khóa, cộng vào `HoaDon.TongTienPhatSinh`/`TongCong` của hợp đồng cũ và chuyển `DaDuaVaoHoaDon` với `HoaDonId` của hóa đơn cũ. Khoản phát sinh sau ngày chuyển không bị lấy nhầm.
- Chuyển giữa tháng tiếp tục sinh hai hóa đơn ghép và kết chuyển nợ cũ sang hóa đơn hợp đồng mới.
- Chuyển ngày cuối tháng sinh duy nhất hóa đơn phòng cũ; dịch vụ cố định của tháng cũ được tính trên hóa đơn này để không bỏ sót tiền. Hợp đồng mới bắt đầu ngày đầu tháng sau, không có hóa đơn 0 ngày cho kỳ cũ. Nợ cũ chưa settlement tiếp tục được hóa đơn đầu tiên của hợp đồng mới mang sang theo `HopDongTruocId`.
- Hợp đồng mới dùng `ChoHieuLuc` nếu ngày bắt đầu còn ở tương lai và chưa đổi phòng đích sang `DangThue`; nếu đã đến ngày thì dùng `DangHieuLuc`.
- View chuyển phòng giải thích rõ scope khoản phát sinh và nhánh cuối tháng.

Kiểm tra thực tế:

- `dotnet build --no-restore`: pass, 0 error; 1 warning `NU1900` do sandbox không truy cập vulnerability feed.
- Service smoke database tạm pass: chuyển giữa tháng tạo hai hóa đơn ghép và gắn khoản phát sinh đúng hóa đơn cũ; khoản sau ngày chuyển giữ `ChuaXuLy`; chuyển cuối tháng chỉ tạo hóa đơn cũ và hợp đồng mới bắt đầu đầu tháng sau.
- Guard/rollback smoke pass: phòng đích đã chiếm bị chặn và hợp đồng nguồn giữ nguyên; hai request đồng thời cùng chuyển vào một phòng chỉ một request thành công.
- Database tạm đã drop trong `finally`. REVIEW-010 không thay đổi schema nên không có migration mới và không đụng database vận hành.

Phạm vi giữ nguyên: chưa làm REVIEW-008/009/011; không mở rộng UI diện rộng, Syncfusion, nhắn tin hoặc auth/multi-user.

### Phiên 53 - REVIEW-006/007: chống chồng kỳ và khóa sửa lịch sử hợp đồng

Đã triển khai thay đổi hẹp theo quyết định Phase 1:

- `HopDongService.TaoHopDongAsync` khóa dòng `Phong` trong transaction, kiểm tra mọi hợp đồng không hủy có khoảng `[NgayBatDau, NgayKetThuc]` giao nhau rồi mới insert. Hai request đồng thời cùng phòng được serialize bằng khóa phòng.
- Hợp đồng bắt đầu sau hôm nay được gán `ChoHieuLuc`; không đổi `Phong.TrangThai` sang `DangThue`. `KichHoatHopDongDenHanAsync` chỉ chuyển sang `DangHieuLuc` sau khi đến ngày và kiểm tra overlap lại dưới khóa phòng; controller hợp đồng, dashboard, phòng và kiểm tra dữ liệu dùng cùng service này.
- `ChuyenPhongService` khóa lại hợp đồng cũ và hai dòng phòng theo thứ tự, rồi kiểm tra overlap phòng mới trước khi ghi.
- Edit tải bản gốc `FOR UPDATE`; không tin hidden `PhongId`/`TrangThai`, giữ giá gốc `TienThueThoaThuan`, validate `NgayKetThuc >= NgayBatDau`, khách đại diện thuộc danh sách và khoảng sửa không overlap.
- Khi đã có hóa đơn, chỉ số, ledger cọc, thanh toán hoặc khoản phát sinh, Edit chỉ cập nhật `GhiChu`; phòng, ngày, cọc, giá gốc và danh sách khách được giữ nguyên. Effective-date nhân khẩu chưa triển khai và tiếp tục thuộc REVIEW-008.
- `Database/schema.sql` thêm CHECK khoảng ngày/trạng thái và index `IX_HopDong_Phong_KhoangNgay`; file apply-once mới là `Database/updates/20260712_contract_overlap_future_status.sql`.

Kiểm tra thực tế:

- `dotnet build --no-restore`: pass, 0 error; 1 warning `NU1900` vì sandbox không truy cập vulnerability feed.
- Schema smoke database tạm: 19 bảng, 77 constraint, 7 dịch vụ mẫu, PASS; database tạm đã drop trong `finally`.
- Service/concurrency smoke: cả 7 ca bắt buộc pass (future/ChoHieuLuc, overlap, race chỉ một thành công, khoảng nối tiếp, hidden field, ngày/đại diện, khóa lịch sử + sửa ghi chú); database tạm đã drop trong `finally`.
- Dry-run database vận hành: `InvalidDateRanges=0`, `InvalidStatuses=0`, `OverlappingPairs=0`.
- Apply-once database vận hành: `Constraints=2`, `Indexes=1`; không backfill/xóa dữ liệu và không replay hai migration cũ.
- `git diff --check`: pass.

Phạm vi giữ nguyên: chưa làm REVIEW-008/009/010/011; không mở rộng Syncfusion, nhắn tin, auth/multi-user hoặc refactor UI diện rộng.

### Phiên 52 - REVIEW-004/005: khóa thanh toán và ledger cọc

Ngày: 12/07/2026

Đã triển khai:

- `HoaDonService.ThuTienAsync` mở transaction trước khi đọc hóa đơn và dùng `SELECT ... FOR UPDATE`; kiểm tra số còn lại dựa trên bản ghi đã khóa.
- `CongNoSettlementService` khóa toàn bộ hóa đơn được phân bổ theo thứ tự kỳ/ID, kể cả đường thanh toán một hóa đơn từ cọc.
- Mọi delta ledger cọc khóa dòng `HopDong` trước khi đọc tổng số dư, ngăn hai request cùng tiêu thụ một số dư.
- Giao dịch cọc thủ công chỉ cho `ThuThemCoc`, `HoanCoc`, `TruNo`; chặn ngày trước khi hợp đồng bắt đầu hoặc sau hiện tại; `DieuChinh` chỉ còn cho flow nội bộ.
- Thu/hoàn cọc thủ công và xử lý chênh lệch chuyển phòng lưu `PhuongThuc = TienMat/ChuyenKhoan`. `TruNo` bắt buộc chọn hóa đơn còn nợ cùng hợp đồng.
- UI ledger thay input ID hóa đơn tự do bằng dropdown hóa đơn còn nợ cùng hợp đồng; thêm phương thức và hiển thị phương thức trong lịch sử.
- Cập nhật `Database/schema.sql`, thêm apply-once `Database/updates/20260712_deposit_payment_method.sql` và cập nhật README.

Kết quả kiểm tra:

```text
dotnet build --no-restore: Build succeeded, 0 errors (NU1900 do sandbox).
SCHEMA_SMOKE_PASS Tables=19;Constraints=75;SeedServices=7
MIGRATION_SMOKE PhuongThucColumns=1
PAYMENT_CONCURRENCY Results=OK|Hoa don da thu du.;Rows=1;Sum=100;InvoicePaid=100
DEPOSIT_CONCURRENCY một request OK, một request bị chặn; Balance=0;NegativeSnapshots=0
DEPOSIT_GUARDS hóa đơn khác hợp đồng bị chặn; DieuChinh thủ công bị chặn
REVIEW004005_SMOKE_PASS
TEMP_DATABASE_DROPPED
```

Phạm vi giữ nguyên: chưa làm REVIEW-006/007/008/011; không mở rộng UI diện rộng, nhắn tin, auth/multi-user hoặc Syncfusion.

### Phiên 51 - REVIEW-003: lịch sử giá thuê theo hợp đồng

Ngày: 12/07/2026

Đã triển khai:

- Đổi mọi đường tính tiền phòng trong `HoaDonService`, `TraPhongService` và `ChuyenPhongService` sang tra `LichSuThayDoiGia` bằng `LoaiDoiTuong='HopDong'` + `HopDongId`.
- `GiaService` quản lý thay đổi giá thuê theo hợp đồng và không còn cập nhật `Phong.GiaThueMacDinh`; giá gốc `HopDong.TienThueThoaThuan` được giữ nguyên khi sửa thông tin hợp đồng.
- Chuyển nút thay đổi giá thuê từ chi tiết phòng sang chi tiết hợp đồng; form sửa hợp đồng hiển thị giá gốc chỉ đọc.
- Cập nhật `Database/schema.sql` và thêm apply-once `Database/updates/20260712_contract_scoped_rent_history.sql`. Script ánh xạ lịch sử `Phong` sang hợp đồng hiệu lực tại kỳ áp dụng, rồi giữ dòng nguồn dưới nhãn `PhongLegacy` để audit nhưng không tính tiền.
- Thêm `appsettings.example.json` chỉ chứa placeholder, không đưa thông tin kết nối thật vào Git. `appsettings.json` và `appsettings.Development.json` tiếp tục không được track.

Kết quả kiểm tra:

```text
dotnet build --no-restore: Build succeeded, 0 errors (NU1900 do sandbox).
SCHEMA_SMOKE_PASS Tables=19;Constraints=74;SeedServices=7
MIGRATION_SMOKE OldContractRows=1;NewContractRows=0;LegacyRows=1
SERVICE_SMOKE OldContractHistory=2500000;NewContractAgreed=3000000;PreviewRent=3000000;InvoiceRent=3000000
REVIEW003_SMOKE_PASS
TEMP_DATABASE_DROPPED
```

Phạm vi giữ nguyên: chưa làm REVIEW-004/005/008/011; không mở rộng Syncfusion, nhắn tin, auth/multi-user hoặc refactor UI diện rộng.

### Phiên 50 - Chốt nghiệp vụ Critical và triển khai nhóm Phase 1 đầu tiên

Ngày: 12/07/2026

Quyết định đã được duyệt và ghi vào `DECISIONS.md`: ranh giới Hủy/Trả phòng, scope giá thuê theo hợp đồng, hóa đơn kỳ cuối, xóa/reissue hóa đơn, không thu dư trong Phase 1, guard ledger cọc, lịch sử nhân khẩu, due date, snapshot chứng từ, hợp đồng tương lai và phạm vi localhost.

Đã triển khai nhóm REVIEW-001/002:

- `TraPhongService` luôn preview/tạo hóa đơn kỳ cuối khi kỳ trả phòng chưa có hóa đơn, kể cả trả ngày cuối tháng.
- Nếu trả giữa tháng nhưng hóa đơn hiện có không khớp số ngày ở, flow trả phòng bị chặn để xóa/hủy và lập lại đúng.
- `HopDong/KetThuc` không còn cập nhật trực tiếp hợp đồng/phòng; endpoint chuyển vào flow `TraPhong`.
- Bỏ nút `Kết thúc HĐ` trực tiếp. Nút `Hủy HĐ` chỉ hiện cho hợp đồng tương lai.
- `HopDongService.HuyHopDongAsync` khóa hợp đồng trong transaction và chỉ cho hủy trước ngày bắt đầu khi chưa có hóa đơn, chỉ số, cọc, thanh toán hoặc khoản phát sinh.

Xác minh hiện tại:

```text
dotnet build --no-restore: PASS, 0 error; 1 warning NU1900 do vulnerability feed của môi trường.
git diff --check: PASS.
Service-level smoke database tạm: PASS.
PHASE1_GROUP1_SMOKE_PASS FullMonthInvoice=1 Contract=DaKetThuc Room=Trong EligibleCancel=DaHuy DepositCancel=Blocked.
Database tạm đã DROP trong `finally`; không đụng dữ liệu vận hành.
```

Không thay đổi schema trong nhóm này; không tạo apply-once SQL.

---

### Phiên 49 - Review toàn diện logic, dữ liệu và sẵn sàng vận hành

Ngày: 12/07/2026

Phạm vi:

- Đọc đầy đủ `DECISIONS.md`, `WORKLOG.md`, `PROJECT_REVIEW.md`, `Database/schema.sql`, toàn bộ
  `Database/updates/` kể cả archive và `Program.cs`.
- Rà bằng `rg` toàn bộ controller/service/repository/model liên quan các luồng A-M.
- Chỉ chẩn đoán và cập nhật tài liệu; không sửa nghiệp vụ, schema hoặc dữ liệu vận hành.

Git đầu phiên:

```text
main = origin/main = 1c1273b feat: expand tenant and contract details
Worktree clean; không có commit local chưa push.
```

Kết quả xác minh:

```text
dotnet build --no-restore
Build succeeded. 0 Warning(s), 0 Error(s).

dotnet test --no-restore -p:NuGetAudit=false
Exit code 0; repo không có test output đáng kể.

Schema smoke database TEST_REVIEW_SCHEMA_*:
Tables=19; Constraints=74; SeedServices=7; SCHEMA_SMOKE_PASS.
Database tạm đã DROP trong finally.

Business smoke database TEST_REVIEW_BUSINESS_*:
CASE_RETURN_FULL_MONTH: không tạo hóa đơn nhưng hợp đồng DaKetThuc, phòng Trong.
CASE_PRICE_HISTORY: giá hợp đồng 3,000,000 nhưng preview lấy 2,500,000 từ lịch sử phòng cũ.
CASE_PEOPLE_HISTORY: số lượng TheoNguoi của cùng kỳ đổi 2 -> 1 khi sửa liên kết khách.
CASE_DELETE_CHARGE: xóa hóa đơn có khoản phát sinh bị FK chặn; hóa đơn còn nguyên.
Database tạm đã DROP trong finally.
```

Kết luận:

- Chưa sẵn sàng nhập dữ liệu thật/thay Excel hoàn toàn.
- Ghi 24 phát hiện `REVIEW-001` đến `REVIEW-024` trong `PROJECT_REVIEW.md` mục 0.
- Critical ưu tiên: trả phòng cuối tháng, kết thúc/hủy bypass, scope lịch sử giá phòng, race thanh toán và race/cấn nhầm cọc.
- High tiếp theo: overlap hợp đồng, sửa hợp đồng sau chốt, lịch sử nhân khẩu, atomic chỉ số, chuyển phòng,
  xóa hóa đơn có khoản phát sinh, snapshot chứng từ, constraint DB và due date công nợ.
- Browser/visual QA không cần để xác nhận các lỗi backend này; concurrency smoke thật được hoãn đến sau khi có fix khóa dòng.
- Restore harness phát sinh `NU1900` do môi trường không truy cập vulnerability feed; đây không phải lỗi compile.

Tài liệu cập nhật:

- `PROJECT_REVIEW.md`: executive summary, ma trận A-M, 24 phát hiện, câu hỏi và kế hoạch 4 phase.
- `DECISIONS.md`: chỉ cập nhật “Mục Chưa Chốt”; không tự chốt nghiệp vụ mới.
- `WORKLOG.md`: bằng chứng build/schema/business smoke và trạng thái Git.

Không làm:

- Không sửa code/schema nghiệp vụ.
- Không chạy/xóa dữ liệu thật.
- Không đưa `appsettings.json` vào Git.
- Không mở rộng Syncfusion, nhắc nợ tự động, Telegram/ZNS/SMS hoặc refactor diện rộng.

### Phiên 48 - Thông tin CCCD, phương tiện và mặc định hợp đồng

Ngày: 12/07/2026

- Bổ sung `KhachThue.NgayCapCCCD`, `NgheNghiep`, `LoaiXe`, `BienSoXe` xuyên suốt schema/model/repository/form/danh sách/chi tiết.
- Sắp xếp thông tin khách thuê theo thứ tự vận hành: họ tên, ngày sinh, điện thoại, CCCD, ngày cấp, biển số xe, loại xe, nghề nghiệp, ghi chú.
- Danh sách hợp đồng hiển thị thêm ngày kết thúc.
- Form tạo hợp đồng tự điền `TienThueThoaThuan` và `TienCoc` bằng `Phong.GiaThueMacDinh` khi chọn phòng, nhưng vẫn là input cho phép sửa.
- Xác nhận `HopDongService.TaoHopDongAsync` đã tự chuyển phòng sang `DangThue` trong cùng transaction tạo hợp đồng.
- Cập nhật DB hiện tại và thêm script idempotent `Database/updates/20260712_khach_thue_identity_vehicle.sql`.
- DB smoke pass round-trip đầy đủ 4 cột mới và rollback sạch dữ liệu `TEST_*`.
- `dotnet build --no-restore` pass 0 warning, 0 error trước restore smoke; `NU1900` sau restore là giới hạn vulnerability feed của môi trường.

---

### Phiên 47 - Sửa lỗi lưu ảnh CCCD khách thuê

Ngày: 12/07/2026

- Nguyên nhân: `KhachThueController` ghép đường dẫn từ `Directory.GetCurrentDirectory()` và giả định sẵn `wwwroot/uploads`; khi working directory lúc chạy khác thư mục dự án, upload mới phát sinh lỗi lưu file.
- Chuyển sang `IWebHostEnvironment.WebRootPath`, tự tạo thư mục `uploads`, dùng tên file ngẫu nhiên an toàn.
- Chỉ nhận JPG/JPEG/PNG/WEBP, giới hạn 5 MB mỗi ảnh và trả lỗi rõ ràng ngay trên form.
- Smoke ghi ảnh PNG vào `WebRootPath/uploads` pass và đã dọn file test.
- `dotnet build --no-restore` thành công, 0 error; `NU1900` là cảnh báo môi trường do không truy cập được NuGet vulnerability feed.

---

### Phiên 46 - Lịch sử hình thức dịch vụ theo kỳ

Ngày: 11/07/2026

Đã làm:

- Thêm `LichSuHinhThucDichVu` và `ChiSoDauChuyenDoiDichVu` vào schema chuẩn; không tạo backfill SQL vì database chưa có dữ liệu vận hành và `Database/schema.sql` là baseline.
- Chặn sửa trực tiếp hình thức khi dịch vụ đã từng gắn phòng; thêm màn thay đổi theo kỳ và lịch sử riêng.
- Service thay đổi chạy transaction, chặn thiếu hóa đơn kỳ trước, chặn hóa đơn từ kỳ áp dụng trở đi, kiểm tra chỉ số kỳ cuối khi bỏ cách tính theo chỉ số và bắt chỉ số đầu mọi phòng khi chuyển sang theo chỉ số.
- `HopDongDichVuRepository` resolve hình thức theo kỳ cho hóa đơn, trả phòng, chuyển phòng và nhập chỉ số hợp đồng.
- Nhập chỉ số theo phòng resolve theo kỳ và lấy mốc chỉ số đầu chuyển đổi; chỉ số ngoài hợp đồng validate hình thức theo tháng của ngày ghi nhận.
- Dashboard kiểm tra dữ liệu resolve hình thức theo kỳ thay vì đọc trạng thái hiện tại của `DichVu`.

Kết quả kiểm tra:

```text
dotnet build --no-restore
Build succeeded.
1 Warning(s) NU1900 (môi trường không truy cập được NuGet vulnerability feed)
0 Error(s)
```

Giới hạn cần ghi rõ khi bàn giao: Browser plugin có thể lỗi hạ tầng; ưu tiên schema/DB/service smoke và HTTP smoke nếu listener ổn định.

---

### Phiên 2-5 - Phase 1 MVP

Ngày: 23/06/2026

Đã làm:

- Tạo cấu trúc ASP.NET Core MVC, `Program.cs`, `appsettings.json`, layout Bootstrap 5.
- Tạo model entity và view model ban đầu.
- Tạo repository Dapper.
- Tạo `HoaDonService`, `PhongService`.
- Tạo các controller/view cho phòng, khách thuê, hợp đồng, chỉ số, hóa đơn, dịch vụ.
- Tạo dashboard.

Deliverable:

- `QuanLyNhaTro.csproj`
- `Program.cs`
- `Models/`
- `Repositories/`
- `Services/HoaDonService.cs`
- `Services/PhongService.cs`
- `Controllers/`
- `Views/`

---

### Phiên 6 - Phase 2: Thu Chi, Excel, Công Nợ

Ngày: 24/06/2026

Đã làm:

- Tạo `ThuChiRepository`, `ThuChiController`, views thu chi.
- Tạo `ExcelService` xuất phiếu thu, thu chi, công nợ.
- Tạo `BaoCaoCongNoViewModel`, `BaoCaoController`, view công nợ.
- Thêm query công nợ trong `HoaDonRepository`.

Deliverable:

- `Repositories/ThuChiRepository.cs`
- `Controllers/ThuChiController.cs`
- `Views/ThuChi/`
- `Services/ExcelService.cs`
- `Models/BaoCaoCongNoViewModel.cs`
- `Controllers/BaoCaoController.cs`
- `Views/BaoCao/CongNo.cshtml`

---

### Phiên 7 - Phase 3: Chuyển Phòng, Thay Đổi Giá

Ngày: 24/06/2026

Đã làm:

- Tạo `ChuyenPhongViewModel`.
- Tạo flow chuyển phòng: kết thúc hợp đồng cũ, tạo hợp đồng mới, sinh hóa đơn phòng cũ/phòng mới, liên kết `HoaDonGhepId`.
- Tạo `LichSuThayDoiGiaRepository`.
- Tạo `GiaController` và view thay đổi giá.
- Bổ sung DI cho module mới.

Deliverable:

- `Models/ChuyenPhongViewModel.cs`
- `Services/ChuyenPhongService.cs`
- `Controllers/ChuyenPhongController.cs`
- `Views/ChuyenPhong/Create.cshtml`
- `Repositories/LichSuThayDoiGiaRepository.cs`
- `Models/ThayDoiGiaViewModel.cs`
- `Models/PhongDichVuViewModel.cs`
- `Controllers/GiaController.cs`
- `Views/Gia/ThayDoiGia.cshtml`

---

### Phiên 8 - Phase 3: Trả Phòng Và Patch Giá Áp Dụng

Ngày: 24/06/2026

Đã làm:

- Tạo `TraPhongViewModel` và `KetQuaTraPhongViewModel`.
- Tạo `TraPhongService`: preview không ghi DB, execute có transaction.
- Tạo `TraPhongController`.
- Tạo views confirm/kết quả trả phòng.
- Bổ sung `HoaDonRepository.GetByHopDongThangNamAsync` và `GetHoaDonCuoiCungAsync`.
- Patch tra giá áp dụng cho chuyển phòng và trả phòng.

Deliverable:

- `Models/TraPhongViewModel.cs`
- `Services/TraPhongService.cs`
- `Controllers/TraPhongController.cs`
- `Views/TraPhong/Confirm.cshtml`
- `Views/TraPhong/KetQua.cshtml`

Phát hiện cuối phiên:

- Một số bug trong backlog Phase 4: lệch schema/code, chuyển phòng tính sai dịch vụ theo chỉ số, query công nợ dynamic, cần xác nhận build.

---

### Phiên 9 - Đồng Bộ Code Theo Schema Và Sửa Build

Ngày: 27/06/2026

Đã làm:

- Chốt `Database/schema.sql` là nguồn chuẩn.
- Sửa model theo schema:
  - `DichVu`: bỏ `HienThi`, `GhiChu`; giữ `TenDichVu`, `LoaiTinhPhi`, `DonViTinh`.
  - `KhachThue`: dùng `AnhCCCDMatTruoc`, `AnhCCCDMatSau`; bỏ các field ngoài schema như `GioiTinh`, `NgayCap`, `NoiCap`, `DiaChiThuongTru`.
  - `ChiSoDienNuoc`: dùng `PhongId`, `NgayDoc`; bỏ `HopDongId`, `NgayNhap`.
  - `PhongDichVu`: dùng `DangApDung`; bỏ `NgayTao`.
  - `HoaDon` và `HopDongKhachThue`: bỏ `NgayTao` khỏi model vì schema không có.
- Sửa repository/query theo schema:
  - `DichVuRepository`
  - `KhachThueRepository`
  - `ChiSoDienNuocRepository`
  - `NhaRepository`
  - `PhongDichVuRepository`
  - `HopDongKhachThueRepository`
  - `ChiTietHoaDonRepository`
  - `HoaDonRepository`
- Sửa view liên quan:
  - `Views/DichVu/`
  - `Views/KhachThue/`
  - `Views/ChiSo/`
  - `Views/ChuyenPhong/Create.cshtml`
  - `Views/Phong/Details.cshtml`
- Sửa Dapper multi-mapping ở `HopDongRepository` và `PhongDichVuRepository`.
- Dọn warning DI không dùng.
- Sửa `ChuyenPhongService`:
  - Dịch vụ `TheoChiSo` lấy từ `ChiSoDienNuoc`.
  - `SoLuong = ChiSoCuoi - ChiSoDau`.
  - Lưu `ChiSoDienNuocId` vào `ChiTietHoaDon`.
  - Dịch vụ cố định vẫn chỉ tính ở hóa đơn phòng mới.
  - Tiếp tục tra lịch sử giá dịch vụ.

Kết quả kiểm tra:

```text
dotnet build --no-restore
Build succeeded.
0 Warning(s)
0 Error(s)
```

Sau khi restore smoke project tạm, chạy lại `dotnet build --no-restore` vẫn build thành công nhưng có 1 warning `NU1900` vì sandbox không truy cập được `https://api.nuget.org/v3/index.json` để lấy dữ liệu vulnerability.

Chưa làm:

- Chưa chạy `dotnet run` với MySQL thật.
- Chưa test thao tác UI/runtime.

---

### Phiên 10 - Chuẩn Hóa Tài Liệu

Ngày: 27/06/2026

Đã làm:

- Viết lại `DECISIONS.md` bằng tiếng Việt UTF-8 rõ ràng.
- Viết lại `WORKLOG.md` bằng tiếng Việt UTF-8 rõ ràng.
- Cập nhật backlog theo trạng thái code mới.
- Gỡ các bug đã sửa khỏi danh sách cần làm.
- Ghi lại quyết định lấy `Database/schema.sql` làm nguồn chuẩn.

Deliverable:

- `DECISIONS.md`
- `WORKLOG.md`

---

### Phiên 11 - Review Nghiệp Vụ Và Kiến Trúc

Ngày: 27/06/2026

Đã làm:

- Rà soát tài liệu `DECISIONS.md`, `WORKLOG.md`, `Database/schema.sql`.
- Đối chiếu thêm các service/controller/repository liên quan tới hóa đơn, chỉ số, chuyển phòng, trả phòng, thanh toán, hợp đồng và Excel.
- Ghi nhận các rủi ro ưu tiên cao vào `PROJECT_REVIEW.md`.
- Bổ sung vào `DECISIONS.md` các vấn đề chưa chốt: quy ước ngày không trọn tháng, đồng hồ reset/hỏng, ledger cọc/công nợ.

Phát hiện chính:

- Cần chốt quy ước ngày vào/ra/chuyển phòng để tránh sai tiền pro-rata.
- Cần validate `ChiSoCuoi >= ChiSoDau` và thiết kế trường hợp đồng hồ reset.
- `TraPhongService` cần sửa cách tính dịch vụ `TheoChiSo` khi trả phòng giữa tháng.
- `LapHoaDon`, xóa hóa đơn, tạo/sửa hợp đồng cần transaction.
- Cần ledger cho cọc/công nợ để không mất dấu dòng tiền.
- Báo cáo công nợ cần sửa cách tính ngày quá hạn cho tháng 12.

Deliverable:

- `PROJECT_REVIEW.md`
- Cập nhật `DECISIONS.md`
- Cập nhật `WORKLOG.md`

Ghi chú cuối phiên:

- Đã thống nhất chưa nâng giao diện Syncfusion ngay.
- Syncfusion sẽ được dùng sau cho các màn nhiều dữ liệu như công nợ, hóa đơn, nhập chỉ số hàng loạt, preview chốt hóa đơn.
- License ưu tiên: `Essential Studio UI Edition Binary License`.

---

### Phiên 12 - Chốt Quy Ước Ngày Và Sửa Hóa Đơn Không Trọn Tháng

Ngày: 27/06/2026

Đã làm:

- Chốt quy ước ngày vào/ngày ra/chuyển phòng:
  - `NgayBatDau` là ngày đầu tiên tính tiền.
  - Ngày trả/chuyển phòng là ngày cuối cùng tính cho phòng cũ.
  - Phòng mới bắt đầu từ `NgayChuyenDi + 1 ngày`.
  - `SoNgayO` là giao giữa kỳ hóa đơn và khoảng ở thực tế, không suy ra đơn giản bằng `.Day`.
- Thêm `BillingPeriodCalculator` để tính số ngày trong kỳ và tiền phòng pro-rata.
- Sửa `HoaDonService.LapHoaDonAsync` tự tính hóa đơn không trọn tháng theo `HopDong.NgayBatDau`/`NgayKetThuc` nếu caller không truyền `SoNgayO`.
- Sửa `ChuyenPhongService` tính ngày phòng cũ/phòng mới bằng khoảng ngày rõ ràng.
- Sửa `TraPhongService`:
  - Preview và execute dùng cùng quy tắc tính ngày.
  - Hóa đơn trả phòng tính dịch vụ `TheoChiSo` theo `ChiSoDienNuoc`, lưu `ChiSoDienNuocId`.
  - Dịch vụ `CoDinh` thu trọn một lần trong hóa đơn tháng cuối nếu có sinh hóa đơn.
  - Preview hiển thị thêm tổng dịch vụ tháng cuối.
- Cập nhật `DECISIONS.md` và `PROJECT_REVIEW.md`; giữ Syncfusion trong backlog sau nghiệp vụ lõi.

Kết quả kiểm tra:

```text
dotnet build --no-restore
Build succeeded.
0 Warning(s)
0 Error(s)
```

Deliverable:

- `Services/BillingPeriodCalculator.cs`
- `Services/HoaDonService.cs`
- `Services/ChuyenPhongService.cs`
- `Services/TraPhongService.cs`
- `Models/TraPhongViewModel.cs`
- `Views/TraPhong/Confirm.cshtml`
- `DECISIONS.md`
- `PROJECT_REVIEW.md`
- `WORKLOG.md`

Chưa làm:

- Chưa chạy app với MySQL thật.
- Chưa test thao tác UI/runtime.
- Chưa xử lý validate chỉ số/reset meter, transaction cho lập/xóa hóa đơn và ledger cọc/công nợ.

---

### Phiên 13 - Chặn Chỉ Số Điện/Nước Âm

Ngày: 27/06/2026

Đã làm:

- Sửa `ChiSoController`:
  - GET nhập chỉ số tính và truyền `ChiSoDau` cho từng dịch vụ theo chỉ số.
  - POST validate `ChiSoCuoi >= ChiSoDau` trước khi insert/update.
  - Nếu chỉ số cuối nhỏ hơn chỉ số đầu, không lưu và báo cần dùng cơ chế reset meter riêng.
- Sửa `Views/ChiSo/Nhap.cshtml`:
  - Hiển thị chỉ số đầu ngay cả khi nhập mới.
  - Đặt `min` của ô chỉ số cuối bằng chỉ số đầu.
  - Hiển thị lỗi `TempData["Error"]`.
- Thêm `ChiSoConsumptionCalculator` và dùng trong:
  - `HoaDonService`
  - `ChuyenPhongService`
  - `TraPhongService`
- Sửa `ChiSoDienNuoc.SoLuongTieuThu` không trả số âm khi gặp dữ liệu cũ không hợp lệ.
- Cập nhật `Database/schema.sql` thêm `CK_ChiSo_KhongAm CHECK (ChiSoCuoi >= ChiSoDau)`.
- Cập nhật `DECISIONS.md` và `PROJECT_REVIEW.md`.

Kết quả kiểm tra:

```text
dotnet build --no-restore
Build succeeded.
0 Warning(s)
0 Error(s)
```

Chưa làm:

- Chưa thiết kế reset meter cho đồng hồ reset/hỏng/quay vòng.
- Chưa chạy app với MySQL thật hoặc apply schema lên DB hiện hữu.

---

### Phiên 14 - Reset Meter Cho Đồng Hồ Reset/Hỏng/Quay Vòng

Ngày: 27/06/2026

Quyết định nghiệp vụ:

- Chưa tách riêng `HongDongHo` / `ThayDongHo` / `QuayVong`; gom trước vào `LoaiGhiNhan = Reset`.
- `BinhThuong`: đồng hồ chạy liên tục, sản lượng `ChiSoCuoi - ChiSoDau`, bắt buộc `ChiSoCuoi >= ChiSoDau`.
- `Reset`: dùng cho reset về 0, hỏng phải thay, thay đồng hồ, hoặc quay vòng số; sản lượng `(ChiSoTruocReset - ChiSoDau) + (ChiSoCuoi - ChiSoSauReset)`.
- `ChiSoSauReset` bỏ trống được hiểu là 0.
- Reset bắt buộc có `LyDoDieuChinh`; hỏng đồng hồ không có số đọc thật chưa cho nhập sản lượng ước tính tự do.

Đã làm:

- Mở rộng `ChiSoDienNuoc` với `LoaiGhiNhan`, `ChiSoTruocReset`, `ChiSoSauReset`, `LyDoDieuChinh`.
- Cập nhật `ChiSoDienNuocRepository` insert/update các cột mới.
- Cập nhật `ChiSoConsumptionCalculator` tính đúng sản lượng cho `BinhThuong` và `Reset`.
- Cập nhật `ChiSoController` validate theo `LoaiGhiNhan` và dùng helper tính sản lượng để chặn dữ liệu sai.
- Cập nhật `Views/ChiSo/Nhap.cshtml` thêm chọn loại ghi nhận và các trường reset.
- Cập nhật `Database/schema.sql` thêm cột và CHECK constraint theo loại ghi nhận.
- Cập nhật `DECISIONS.md` và `PROJECT_REVIEW.md`.

Kết quả kiểm tra:

```text
dotnet build --no-restore
Build succeeded.
0 Warning(s)
0 Error(s)
```

Chưa làm:

- Chưa chạy app với MySQL thật hoặc apply schema lên DB hiện hữu.
- Chưa có migration ALTER TABLE cho database đang tồn tại; hiện `Database/schema.sql` là schema chuẩn cho DB tạo mới.

---

### Phiên 15 - Transaction Cho Lập/Xóa Hóa Đơn

Ngày: 27/06/2026

Đã làm:

- Thêm overload transaction cho `HoaDonRepository.InsertAsync(...)`.
- Thêm overload transaction cho `HoaDonRepository.GetByHopDongKyAsync(...)`.
- Thêm overload transaction cho `ChiTietHoaDonRepository.InsertAsync(...)`.
- Sửa `HoaDonService.LapHoaDonAsync`:
  - Tính toán hóa đơn/dịch vụ giữ nguyên.
  - Mở transaction trước khi ghi DB.
  - Re-check trùng kỳ trong transaction.
  - Insert `HoaDon` và toàn bộ `ChiTietHoaDon` trong cùng transaction.
  - Rollback nếu bất kỳ dòng chi tiết nào lỗi.
- Thêm `HoaDonService.XoaHoaDonAsync`:
  - Mở transaction trước khi xóa.
  - Kiểm tra hóa đơn tồn tại và chưa có tiền thu.
  - Xóa `ChiTietHoaDon` và `HoaDon` trong cùng transaction.
  - Rollback nếu xóa hóa đơn lỗi sau khi đã xóa chi tiết.
- Sửa `HoaDonController.Delete` chỉ gọi service và xử lý thông báo.
- Thêm `HopDongService`:
  - `TaoHopDongAsync` insert hợp đồng, insert danh sách khách thuê, cập nhật phòng sang `DangThue` trong cùng transaction.
  - `SuaHopDongAsync` update hợp đồng, xóa liên kết khách cũ, insert liên kết khách mới trong cùng transaction.
  - Chặn tạo hợp đồng mới nếu phòng đã có hợp đồng `DangHieuLuc`.
- Thêm overload transaction cho `HopDongRepository`, `HopDongKhachThueRepository`, `PhongRepository`.
- Sửa `HopDongController` gọi service cho tạo/sửa hợp đồng và chuẩn hóa lại file controller về UTF-8 rõ ràng.
- Đăng ký `HopDongService` trong DI.
- Sửa `HoaDonRepository.GetCongNoAsync` dùng `DATE_ADD(STR_TO_DATE(...), INTERVAL 1 MONTH)` để kỳ tháng 12 không sinh ngày tháng 13 khi tính `SoNgayQuaHan`.

Kết quả kiểm tra:

```text
dotnet build --no-restore
Build succeeded.
0 Warning(s)
0 Error(s)
```

Chưa làm:

- Chưa thêm UI quản lý `Nha`; hiện `NhaRepository` có nhưng không có `NhaController`/`Views/Nha`/menu.
- Chưa thêm dropdown `NhaId` trong form tạo/sửa phòng; DB mới tạo bảng `Nha` đang rỗng có thể làm tạo phòng lỗi FK.
- Chưa xử lý transaction cho kết thúc/hủy hợp đồng; hiện vẫn dùng flow cũ.

---

### Phiên 16 - UI Quản Lý Nhà Và Chọn Nhà Khi Tạo Phòng

Ngày: 28/06/2026

Đã làm:

- Thêm `NhaController` với các thao tác thêm/sửa/xóa nhà.
- Thêm `Views/Nha/Index.cshtml` theo Bootstrap hiện tại, dùng modal thêm/sửa và chặn xóa nhà đang có phòng.
- Thêm menu `Nha` vào sidebar.
- Sửa `Views/Phong/Create.cshtml` và `Views/Phong/Edit.cshtml` để chọn `NhaId` bằng dropdown từ `NhaRepository`.
- Khi chưa có nhà nào, màn tạo phòng hiển thị cảnh báo và link sang màn quản lý nhà.
- Thêm validate server-side trong `PhongController` để không tạo/sửa phòng với `NhaId <= 0` hoặc nhà không tồn tại.
- Sửa `PhongRepository.UpdateAsync` cập nhật `NhaId`; `GetAllAsync/GetByIdAsync` join `Nha` để hiển thị tên nhà trong danh sách/chi tiết phòng.
- Chỉnh bảng danh sách phòng hiển thị cột Nhà và trạng thái đúng cột.
- Đối chiếu schema và đổi `Phong.DienTich` sang `decimal?` để khớp `Database/schema.sql` (`DienTich DECIMAL NULL`).

Kết quả kiểm tra:

```text
dotnet build --no-restore
Build succeeded.
0 Warning(s)
0 Error(s)
```

Chưa làm:

- Chưa chạy app với MySQL thật hoặc test thủ công UI bằng trình duyệt.
- Không đụng ledger cọc/công nợ theo phạm vi phiên này.

---

### Phiên 17 - Smoke Test Với MySQL Thật

Ngày: 28/06/2026

Đã làm:

- Chạy app tại `http://127.0.0.1:5097` với connection string MySQL hiện có trong `appsettings.json`.
- Mở Dashboard qua browser, xác nhận app truy vấn DB thành công và không có console error.
- Tạo dữ liệu test qua UI:
  - `Nha`: `TEST_Codex_Nha_20260627235312`.
  - `Phong`: `TEST_P235312`, chọn đúng Nhà, diện tích `18.5`, giá niêm yết `2,500,000`.
  - `KhachThue`: `TEST_Codex_Khach_TEST_P235312`.
  - `HopDong`: bắt đầu `01/06/2026`, tiền thuê `2,500,000`, cọc `1,000,000`.
  - `HoaDon`: kỳ `6/2026`, tổng cộng `2,500,000`.
  - `ThanhToan`: thu đủ `2,500,000`, hình thức `ChuyenKhoan`.
- Xác nhận danh sách phòng hiển thị đúng Nhà và trạng thái chuyển sang `Dang thue`.
- Xác nhận Dashboard sau thu tiền: 1 phòng, 1 đang thuê, phải thu `2,500,000`, đã thu `2,500,000`, còn lại `0`.
- Kiểm tra browser console và server stderr: không có lỗi.
- Dừng web process test sau khi hoàn tất.

Kết quả kiểm tra:

```text
dotnet build --no-restore
Build succeeded.
0 Warning(s)
0 Error(s)
```

Chưa làm:

- Chưa test gán dịch vụ theo phòng, nhập chỉ số điện/nước, đồng hồ reset.
- Chưa test chuyển phòng/trả phòng trên MySQL thật.
- Chưa dọn dữ liệu test `TEST_Codex_*`.

---

### Phiên 18 - Test Dịch Vụ, Chỉ Số Và Reset Meter Với MySQL Thật

Ngày: 28/06/2026

Đã làm:

- Chạy app tại `http://127.0.0.1:5097` với MySQL thật.
- Tạo phòng test `TEST_METER_N_000821`, gán dịch vụ:
  - Điện `TheoChiSo`, đơn giá `3,500`.
  - Internet `CoDinh`, đơn giá `100,000`.
- Tạo khách `TEST_KHACH_N_000821` và hợp đồng `#2`.
- Nhập chỉ số thường:
  - Kỳ `6/2026`: `ChiSoDau = 0`, `ChiSoCuoi = 100`.
  - Kỳ `7/2026`: `ChiSoDau = 100`, `ChiSoCuoi = 150`.
- Lập hóa đơn `7/2026` cho hợp đồng `#2`; kết quả:
  - Tiền phòng `2,500,000`.
  - Điện `50.00 x 3,500 = 175,000`.
  - Internet `1.00 x 100,000 = 100,000`.
  - Tổng cộng `2,775,000`.
- Tạo phòng test reset `TEST_METER_R_000821`, gán cùng dịch vụ Điện/Internet.
- Tạo khách `TEST_KHACH_R_000821` và hợp đồng `#3`.
- Nhập chỉ số reset:
  - Kỳ `6/2026`: `ChiSoDau = 0`, `ChiSoCuoi = 980`.
  - Kỳ `7/2026`: chọn `Reset`, `ChiSoDau = 980`, `ChiSoTruocReset = 999`, `ChiSoSauReset = 0`, `ChiSoCuoi = 35`.
  - UI reset hiển thị field reset và đổi `min` của `ChiSoCuoi` về `0`.
- Lập hóa đơn `7/2026` cho hợp đồng `#3`; kết quả:
  - Tiền phòng `2,500,000`.
  - Điện `54.00 x 3,500 = 189,000`.
  - Internet `1.00 x 100,000 = 100,000`.
  - Tổng cộng `2,789,000`.
- Đọc trực tiếp MySQL xác nhận:
  - Dòng Điện hóa đơn thường có `ChiSoDienNuocId = 2`, `LoaiGhiNhan = BinhThuong`, `ChiSoDau = 100`, `ChiSoCuoi = 150`.
  - Dòng Điện hóa đơn reset có `ChiSoDienNuocId = 4`, `LoaiGhiNhan = Reset`, `ChiSoDau = 980`, `ChiSoCuoi = 35`, `ChiSoTruocReset = 999`, `ChiSoSauReset = 0`.
  - Dòng Internet không có `ChiSoDienNuocId`, đúng vì là dịch vụ cố định.
- Kiểm tra browser console: không có lỗi.

Kết quả kiểm tra:

```text
dotnet build --no-restore
Build succeeded.
0 Warning(s)
0 Error(s)
```

Chưa làm:

- Chưa test chuyển phòng/trả phòng trên MySQL thật.
- Chưa dọn dữ liệu test `TEST_METER_*`, `TEST_KHACH_*`.

---

### Phiên 19 - Test Chuyển Phòng Và Trả Phòng Với Dịch Vụ Theo Chỉ Số/Cố Định

Ngày: 28/06/2026

Đã làm:

- Seed dữ liệu test bằng console tạm rồi thao tác chính qua UI:
  - Chuyển phòng: `TEST_MOVE_OLD_072235` -> `TEST_MOVE_NEW_072235`, khách `TEST_MOVE_KHACH_072235`.
  - Trả phòng: `TEST_RETURN_072235`, khách `TEST_RETURN_KHACH_072235`.
  - Các phòng đều có Điện `TheoChiSo` và Internet `CoDinh`.
- Test chuyển phòng qua UI:
  - Hợp đồng cũ `#4`, ngày chuyển đi `10/08/2026`.
  - Phòng mới `TEST_MOVE_NEW_072235`, tiền thuê mới `2,600,000`, cọc mới `1,500,000`.
  - Kết quả UI: tạo hợp đồng mới `#6`, bắt đầu `11/08/2026`, khách được copy sang hợp đồng mới.
- Đọc trực tiếp MySQL xác nhận hóa đơn chuyển phòng:
  - Hóa đơn phòng cũ `#4`: `SoNgayO = 10/31`, tiền phòng `806,452`, chỉ tính Điện `30 x 3,500 = 105,000`, tổng `911,452`, không tính Internet cố định.
  - Hóa đơn phòng mới `#5`: `SoNgayO = 21/31`, tiền phòng `1,761,290`, Điện `20 x 4,000 = 80,000`, Internet `120,000`, tổng `1,961,290`.
  - Hai hóa đơn có `HoaDonGhepId` trỏ qua nhau.
  - Hợp đồng cũ chuyển `DaChuyenPhong`, phòng cũ `Trong`, hợp đồng mới `DangHieuLuc`, phòng mới `DangThue`.
- Test trả phòng qua UI:
  - Hợp đồng `#5`, ngày trả `10/08/2026`.
  - Preview đúng: `10/31` ngày, tiền phòng `806,452`, dịch vụ tháng cuối `205,000`, tổng nợ `1,011,452`, hoàn cọc `988,548`.
  - Execute trả phòng thành công, sinh hóa đơn tháng cuối `#6`.
- Đọc trực tiếp MySQL xác nhận hóa đơn trả phòng:
  - `SoNgayO = 10/31`, tiền phòng `806,452`.
  - Điện `30 x 3,500 = 105,000`, có `ChiSoDienNuocId`.
  - Internet `1 x 100,000 = 100,000`, không có `ChiSoDienNuocId`.
  - Tổng dịch vụ `205,000`, tổng cộng `1,011,452`.
  - Hợp đồng trả phòng chuyển `DaKetThuc`, phòng `Trong`.
- Browser console không có warning/error; server stderr rỗng.

Kết quả kiểm tra:

```text
dotnet build --no-restore
Build succeeded.
0 Warning(s)
0 Error(s)
```

Ghi chú:

- Flow chuyển phòng hiện cần chỉ số của phòng mới tồn tại trước khi chuyển nếu muốn hóa đơn phòng mới có dịch vụ theo chỉ số. Vì phòng mới chưa có hợp đồng nên UI hiện chưa có đường nhập chỉ số phòng mới trước khi chuyển; phiên này seed chỉ số phòng mới trực tiếp vào DB để test service.
- Chưa dọn dữ liệu test `TEST_MOVE_*`, `TEST_RETURN_*`.

---

### Phiên 20 - UI Nhập Chỉ Số Theo Phòng Cho Flow Chuyển Phòng

Ngày: 28/06/2026

Đã làm:

- Thêm `ChiSoDienNuocRepository.GetByPhongKyAsync(phongId, thang, nam)` để đọc chỉ số trực tiếp theo phòng/kỳ, khớp schema `ChiSoDienNuoc.PhongId`.
- Sửa `ChiSoController`:
  - Thêm action GET/POST `NhapTheoPhong`.
  - Dùng dịch vụ `TheoChiSo` đang gán cho phòng qua `PhongDichVuRepository`, không bắt nhập dịch vụ chưa áp dụng cho phòng.
  - Gom logic validate/lưu chỉ số thường/reset dùng chung cho nhập theo hợp đồng và nhập theo phòng.
  - Chặn update chỉ số không thuộc đúng phòng/kỳ đang nhập.
- Thêm `Views/ChiSo/NhapTheoPhong.cshtml`.
- Sửa `Views/ChuyenPhong/Create.cshtml`:
  - Thêm link `Nhap chi so phong moi`.
  - Link tự cập nhật theo `PhongMoiId` và tháng/năm từ `NgayChuyenDi`.
  - Khi lưu chỉ số từ flow chuyển phòng, quay lại màn chuyển phòng qua `returnHopDongId`.
- Không đụng ledger cọc/công nợ.

Kết quả kiểm tra:

```text
dotnet build --no-restore
Build succeeded.
0 Warning(s)
0 Error(s)
```

QA với app chạy MySQL thật tại `http://127.0.0.1:5097`:

- Mở `/ChiSo/NhapTheoPhong?phongId=4&thang=9&nam=2026`: render đúng form nhập chỉ số theo phòng, có nút lưu, console không có warning/error.
- Mở `/ChuyenPhong/Create?hopDongId=6`, chọn phòng mới `TEST_MOVE_OLD_072235`: link sinh đúng `/ChiSo/NhapTheoPhong?phongId=4&thang=6&nam=2026&returnHopDongId=6`.
- Click link từ màn chuyển phòng sang màn nhập chỉ số theo phòng thành công, console không có warning/error.

Chưa làm:

- Chưa nhập/lưu thêm dữ liệu chỉ số mới trong QA phiên này để tránh phát sinh dữ liệu test không cần thiết.
- Chưa làm ledger cọc/công nợ theo đúng phạm vi đã hoãn.

---

### Phiên 21 - Ledger Cọc Và Xử Lý Công Nợ Phi Tiền Mặt

Ngày: 28/06/2026

Quyết định nghiệp vụ:

- Thêm `GiaoDichCoc` làm ledger cọc theo hợp đồng.
- `HopDong.TienCoc` là số cọc thỏa thuận; số cọc thực đang giữ lấy từ tổng ledger.
- `GiaoDichCoc.SoTien` là delta có dấu:
  - `ThuCoc`, `ThuThemCoc`: tăng số dư.
  - `HoanCoc`, `TruNo`: giảm số dư.
  - `DieuChinh`: tăng/giảm để chuyển cọc hoặc điều chỉnh chênh lệch.
- Chưa thêm bảng `CongNoLedger` riêng trong phiên này. Để tránh double-count công nợ, dùng `ThanhToan` phi tiền mặt:
  - `KetChuyenNo` khi nợ cũ được snapshot sang hóa đơn/hợp đồng mới.
  - `TruCoc` khi trả phòng dùng cọc để tất toán nợ.

Đã làm:

- Thêm model/repository/service cho `GiaoDichCoc`.
- Thêm `CongNoSettlementService` để phân bổ bút toán xử lý nợ vào các hóa đơn còn nợ và cập nhật `HoaDon.SoTienDaThu`/`TrangThaiThanhToan` trong transaction.
- Khi tạo hợp đồng, tự ghi `ThuCoc` nếu có tiền cọc.
- Khi chuyển phòng:
  - Chuyển số dư cọc thực tế từ hợp đồng cũ sang hợp đồng mới bằng `DieuChinh`.
  - Nếu có nợ xuyên hợp đồng, ghi `ThanhToan.HinhThuc = KetChuyenNo` trên hóa đơn cũ để tránh báo cáo công nợ tính trùng với `TienNoKyTruoc` của hóa đơn mới.
  - Cập nhật `DaXuLyChenhLechCoc` theo số dư cọc thực tế so với cọc thỏa thuận của hợp đồng mới.
- Khi trả phòng:
  - Tính preview theo số dư cọc thực tế.
  - Ghi `TruNo` trong ledger và `ThanhToan.HinhThuc = TruCoc` để cấn trừ nợ bằng cọc.
  - Ghi `HoanCoc` cho phần cọc còn lại.
  - Cập nhật `HopDong.TienCocHoanLai`.
- Thêm `GiaoDichCocController` và `Views/GiaoDichCoc/Index.cshtml` để xem ledger và ghi nhận giao dịch thủ công.
- Thêm link ledger cọc từ chi tiết hợp đồng.
- Thêm migration lịch sử, nay lưu tại `Database/updates/archive_pre_20260710/20260628_add_giao_dich_coc.sql`.
- Sửa lỗi nhỏ ở `ChuyenPhongController`: khi render lại form lỗi, danh sách phòng trống phải loại theo `PhongId` cũ, không phải `HopDongCuId`.

Kết quả kiểm tra:

```text
dotnet build --no-restore
Build succeeded.
0 Warning(s)
0 Error(s)
```

Chưa làm:

- Chưa apply migration vào MySQL thật vì connection string hiện trong `appsettings.json` là placeholder.
- Chưa smoke test runtime ledger cọc trên DB thật.
- Chưa dựng `CongNoLedger` riêng; hiện xử lý rủi ro double-count bằng bút toán `ThanhToan` phi tiền mặt.

---

### Phiên 22 - Smoke Test Ledger Cọc Với MySQL Thật

Ngày: 28/06/2026

Đã làm:

- Apply migration `GiaoDichCoc` trên DB thật trước phiên test.
- Chạy app Development tại `http://127.0.0.1:5098` với MySQL thật.
- Smoke test dữ liệu tiền tố `TEST_LEDGER_182206`.
- Test tạo hợp đồng có cọc:
  - Hợp đồng `#7` tự sinh ledger `ThuCoc = 1,000,000`.
- Test thu thêm/hoàn cọc thủ công:
  - `ThuThemCoc = +200,000`.
  - `HoanCoc = -100,000`.
  - Số dư cọc còn `1,100,000`.
- Test chuyển phòng có nợ và chênh cọc:
  - Hợp đồng cũ `#8` sang hợp đồng mới `#9`.
  - Hóa đơn nợ cũ `#7` được tất toán bằng `ThanhToan.HinhThuc = KetChuyenNo`.
  - Hóa đơn mới `#9` có `TienNoKyTruoc = 800,000`.
  - Số dư cọc chuyển sang hợp đồng mới là `1,000,000`.
  - `DaXuLyChenhLechCoc = false` vì cọc thỏa thuận mới là `1,500,000`.
- Test trả phòng còn nợ:
  - Hợp đồng `#10` trừ nợ vào cọc `700,000`.
  - Ledger có `TruNo = -700,000` và `HoanCoc = -300,000`.
  - `ThanhToan.HinhThuc = TruCoc`.
  - Công nợ còn lại `0`.
- Rà báo cáo công nợ sau chuyển phòng:
  - Hóa đơn nợ cũ đã `KetChuyenNo` không còn trong báo cáo.
  - Hóa đơn mới mang `TienNoKyTruoc` vẫn còn trong báo cáo, đúng nghiệp vụ.

Kết quả kiểm tra:

```text
dotnet build --no-restore
Build succeeded.
0 Warning(s)
0 Error(s)
```

Ghi chú:

- App test cổng `5098` đã dừng sau khi test.
- Dữ liệu smoke test `TEST_LEDGER_182206` còn trong DB thật để đối chiếu nếu cần; có thể dọn sau.

---

### Phiên 23 - UI Xử Lý Chênh Lệch Cọc Khi Chuyển Phòng

Ngày: 28/06/2026

Đã làm:

- Thêm trạng thái tính toán vào `GiaoDichCocViewModel`:
  - Hợp đồng có sinh từ chuyển phòng hay không.
  - Có cần xử lý chênh lệch cọc hay không.
  - Chênh lệch giữa `HopDong.TienCoc` và số dư cọc ledger.
- Thêm `GiaoDichCocService.XuLyChenhLechChuyenPhongAsync`:
  - Chỉ cho xử lý hợp đồng có `HopDongTruocId`.
  - Nếu số dư cọc thấp hơn cọc thỏa thuận, ghi `ThuThemCoc`.
  - Nếu số dư cọc cao hơn cọc thỏa thuận, ghi `HoanCoc`.
  - Nếu đã khớp, chỉ cập nhật `DaXuLyChenhLechCoc = true`.
  - Toàn bộ chạy trong transaction.
- Thêm action `GiaoDichCocController.XuLyChenhLech`.
- Trên màn ledger cọc, thêm khối cảnh báo chênh lệch và nút xử lý tự động.
- Trên chi tiết hợp đồng mới sau chuyển phòng, nếu còn chênh lệch cọc thì hiển thị cảnh báo và link sang ledger.
- Cập nhật `DECISIONS.md` để bỏ UI chênh lệch cọc khỏi nhóm còn thiếu.

Kết quả kiểm tra:

```text
dotnet build --no-restore
Build succeeded.
0 Warning(s)
0 Error(s)
```

QA với app chạy MySQL thật tại `http://127.0.0.1:5099`:

- Mở ledger hợp đồng `#9` từ smoke test trước.
- UI hiển thị đúng chênh lệch `+500,000` vì số dư cọc đang giữ `1,000,000`, cọc thỏa thuận hợp đồng mới `1,500,000`.
- Bấm `Xu ly chenh lech` thành công:
  - Sinh ledger `ThuThemCoc = +500,000`.
  - Số dư cọc sau xử lý `1,500,000`.
  - Cảnh báo chuyển sang trạng thái đã xử lý.
  - Chi tiết hợp đồng `#9` không còn cảnh báo chênh lệch cọc.
- Browser console không có warning/error liên quan.

---

### Phiên 24 - Hiển Thị Bút Toán Phi Tiền Mặt Trên Hóa Đơn

Ngày: 28/06/2026

Đã làm:

- Sửa `Views/HoaDon/Details.cshtml` để lịch sử thanh toán hiển thị badge riêng cho:
  - `TienMat`: tiền mặt.
  - `ChuyenKhoan`: chuyển khoản.
  - `KetChuyenNo`: kết chuyển nợ.
  - `TruCoc`: trừ cọc.
- Nếu hóa đơn có `KetChuyenNo` hoặc `TruCoc`, hiển thị cảnh báo rằng đây là bút toán xử lý công nợ, không phải tiền mặt/chuyển khoản mới thu.
- Cập nhật comment `ThanhToan.HinhThuc` để ghi rõ các giá trị nghiệp vụ đang dùng.
- Cập nhật `DECISIONS.md` và `PROJECT_REVIEW.md` về nguyên tắc hiển thị bút toán phi tiền mặt.

Kết quả kiểm tra:

```text
dotnet build --no-restore
Build succeeded.
0 Warning(s)
0 Error(s)
```

QA với app chạy MySQL thật tại `http://127.0.0.1:5100`:

- Mở `/HoaDon/Details/7`: hóa đơn kết chuyển nợ hiển thị badge/cảnh báo phi tiền mặt, console không có warning/error.
- Mở `/HoaDon/Details/10`: hóa đơn trừ cọc hiển thị badge/cảnh báo phi tiền mặt, console không có warning/error.
- Click menu `Hóa đơn` từ chi tiết hóa đơn về danh sách thành công, console vẫn sạch.
- App test cổng `5100` đã dừng sau khi test.
- Sau QA, nhãn hiển thị được đổi từ không dấu sang tiếng Việt có dấu (`Kết chuyển nợ`, `Trừ cọc`) và `dotnet build --no-restore` vẫn pass; chưa chạy lại runtime smoke test do lượt chạy escalated bị giới hạn.

---

### Phiên 25 - Chống Double-Count Công Nợ Khi Kết Chuyển Kỳ

Ngày: 29/06/2026

Đã làm:

- Rà lại edge cases công nợ sau ledger:
  - Hợp đồng có nhiều hóa đơn nợ.
  - Hóa đơn mới mang `TienNoKyTruoc`.
  - Trả phòng sinh hóa đơn cuối và có nợ cũ.
  - Xóa hóa đơn đang giữ nợ đã kết chuyển.
- Sửa `HoaDonService.LapHoaDonAsync`:
  - `TienNoKyTruoc` dương lấy theo tổng nợ các hóa đơn trước kỳ, không chỉ hóa đơn gần nhất.
  - Sau khi tạo hóa đơn mới, ghi `ThanhToan.HinhThuc = KetChuyenNo` để tất toán các hóa đơn cũ tương ứng.
  - Vẫn giữ hỗ trợ `TienNoKyTruoc` âm khi khách trả dư kỳ trước.
- Sửa `TraPhongService.ThucHienAsync`:
  - Hóa đơn trả phòng sinh mới cũng kết chuyển tổng nợ cũ vào `TienNoKyTruoc`.
  - Các hóa đơn cũ được tất toán bằng `KetChuyenNo` trước khi dùng cọc `TruCoc`, tránh trừ cọc trên tổng nợ bị double-count.
- Chặn xóa hóa đơn có `TienNoKyTruoc > 0` trong `HoaDonService.XoaHoaDonAsync`.
- Ẩn nút xóa trên chi tiết hóa đơn nếu hóa đơn đang mang `TienNoKyTruoc > 0`.
- Cập nhật `DECISIONS.md` và `PROJECT_REVIEW.md`.

Kết quả kiểm tra:

```text
dotnet build --no-restore
Build succeeded.
0 Warning(s)
0 Error(s)
```

Chưa làm:

- Chưa runtime smoke test với MySQL thật cho các biến thể mới trong phiên này. Cần test tiếp: nhiều hóa đơn nợ -> lập hóa đơn mới, trả phòng có nợ cũ, và thử xóa hóa đơn mang `TienNoKyTruoc > 0`.

---

### Phiên 26 - Smoke Test Edge Cases Công Nợ Sau Kết Chuyển

Ngày: 29/06/2026

Đã làm:

- Tạo console smoke test tạm trong `.agents/SmokeDebtEdges` để gọi trực tiếp service thật và DB MySQL thật; thư mục `.agents/` đang được `.gitignore`.
- Seed dữ liệu test tiền tố `TEST_DEBT_EDGE_20260629204600`.
- Chạy 3 edge cases đã đề xuất:
  - Hợp đồng có 2 hóa đơn nợ -> lập hóa đơn kỳ mới.
  - Trả phòng có nợ cũ và sinh hóa đơn cuối.
  - Thử xóa hóa đơn đang mang `TienNoKyTruoc > 0`.

Kết quả smoke test trên MySQL thật:

- Case 1:
  - Hợp đồng `#11`, hóa đơn nợ cũ `#11`, `#12`, hóa đơn mới `#13`.
  - `TienNoKyTruoc` hóa đơn mới = `800,000`.
  - Hóa đơn cũ còn nợ = `0`.
  - Tổng `KetChuyenNo` trên hóa đơn cũ = `800,000`.
  - Báo cáo công nợ chỉ còn hóa đơn mới.
  - Kết quả: pass.
- Case 2:
  - Hợp đồng `#12`, hóa đơn nợ cũ `#14`, `#15`, hóa đơn trả phòng `#16`.
  - Tiền phòng tháng cuối = `300,000`.
  - `TienNoKyTruoc` hóa đơn trả phòng = `600,000`.
  - Tổng hóa đơn trả phòng = `900,000`.
  - Cọc trừ vào hóa đơn trả phòng bằng `TruCoc` = `700,000`.
  - Hóa đơn cũ còn nợ = `0`; hóa đơn trả phòng còn nợ = `200,000`.
  - `KetQuaTraPhongViewModel.KhachConNoThem = 200,000`.
  - Kết quả: pass.
- Case 3:
  - Thử xóa hóa đơn `#13` đang mang `TienNoKyTruoc > 0`.
  - Service chặn đúng với thông báo `Khong the xoa hoa don dang mang no ky truoc da ket chuyen.`
  - Hóa đơn vẫn tồn tại.
  - Kết quả: pass.

Kết quả kiểm tra:

```text
dotnet run --project .agents\SmokeDebtEdges\SmokeDebtEdges.csproj
Case1.Passed = true
Case2.Passed = true
Case3.Passed = true
```

Ghi chú:

- Dữ liệu smoke test `TEST_DEBT_EDGE_20260629204600` còn trong DB thật để đối chiếu; có thể dọn sau nếu muốn.
- Console smoke test nằm trong `.agents/` nên không commit vào repo.

---

### Phiên 27 - Cải Thiện UI Báo Cáo Công Nợ

Ngày: 29/06/2026

Đã làm:

- Thêm thông tin `NhaId`, `TenNha` vào `BaoCaoCongNoViewModel` và query công nợ.
- Bổ sung filter GET cho màn `BaoCao/CongNo`:
  - Lọc theo Nhà.
  - Lọc theo trạng thái hợp đồng.
  - Lọc theo nhóm quá hạn.
  - Tìm nhanh theo nhà, phòng, khách thuê, số điện thoại.
- Đồng bộ filter với xuất Excel công nợ.
- Cập nhật view báo cáo công nợ:
  - Thêm thanh filter.
  - Thêm cột Nhà và Trạng thái.
  - Thêm card số hóa đơn.
  - Bảng dùng `table-responsive`.
- Cập nhật `ExcelService.XuatExcelCongNo` để xuất thêm Nhà và Quá hạn.
- Sửa CSS layout `#page-content { min-width: 0; }` để bảng rộng chỉ cuộn trong vùng bảng, không kéo ngang toàn trang.

Kết quả kiểm tra:

```text
dotnet build --no-restore
Build succeeded.
0 Warning(s)
0 Error(s)
```

QA với app chạy MySQL thật tại `http://127.0.0.1:5102`:

- Mở `/BaoCao/CongNo`: render đúng filter, cột Nhà, summary card; console không có warning/error.
- Lọc `tuKhoa=TEST_DEBT_EDGE_20260629204600` và `quaHan=QuaHan30`: còn đúng 1 hóa đơn, summary và bảng khớp.
- Link xuất Excel giữ query filter: `/BaoCao/XuatCongNo?quaHan=QuaHan30&tuKhoa=TEST_DEBT_EDGE_20260629204600`.
- Gọi route xuất Excel trả `200`, content type `.xlsx`, file size `6909` bytes.
- Sau CSS fix, trang không còn horizontal scroll toàn cục; bảng tự cuộn ngang bên trong `table-responsive`.
- App test cổng `5102` đã dừng sau khi test.

---

### Phiên 28 - Thu Tiền Nhanh Trên Danh Sách Hóa Đơn

Ngày: 30/06/2026

Đã làm:

- Thêm thao tác thu tiền nhanh ngay trên `Views/HoaDon/Index.cshtml` cho hóa đơn `ChuaThu`/`ThuMotPhan`.
- Mỗi hóa đơn còn nợ có nút mở modal nhỏ để nhập số tiền, hình thức và ghi chú.
- Modal tự điền số còn lại, có nút `Thu đủ`, truyền `thang/nam` để sau khi thu quay lại đúng kỳ đang xem.
- Danh sách hóa đơn hiển thị rõ:
  - Badge `Nợ kỳ trước` khi `TienNoKyTruoc > 0`.
  - Badge `Kết chuyển nợ`/`Trừ cọc` nếu lịch sử thanh toán có bút toán phi tiền mặt.
  - Số còn lại ngay dưới trạng thái.
- `HoaDonController.Index` nạp thêm `DanhSachThanhToan` cho từng hóa đơn trong kỳ để hiển thị badge công nợ.
- `HoaDonController.ThuTien` nhận thêm `returnTo`, `thang`, `nam` để hỗ trợ redirect về danh sách.
- `HoaDonService.ThuTienAsync` chặn:
  - Số tiền <= 0.
  - Hình thức ngoài `TienMat`/`ChuyenKhoan`.
  - Hóa đơn đã thu đủ.
  - Thu vượt số còn lại.
- Sửa input số tiền ở danh sách và chi tiết dùng `step="1"` thay vì `step="1000"` để browser không chặn các số hợp lệ như `10,000`.

Kết quả kiểm tra:

```text
dotnet build --no-restore
Build succeeded.
0 Warning(s)
0 Error(s)
```

QA với app chạy MySQL thật tại `http://127.0.0.1:5103`:

- Seed dữ liệu smoke test tiền tố `TEST_QUICKPAY_20260629211502`.
- Mở `/HoaDon?thang=6&nam=2026`: danh sách render hóa đơn test, badge `Nợ kỳ trước`, `Trừ cọc` và nút thu nhanh.
- Mở modal thu nhanh hóa đơn `#17`: tự điền còn phải thu `97,000`, input có `max=97000`, hiển thị cảnh báo nợ kỳ trước/bút toán phi tiền mặt.
- Phát hiện lỗi HTML validation do `min=1` + `step=1000`; đã sửa thành `step=1` và build lại.
- Submit thu nhanh `10,000` thành công theo server log: POST `/HoaDon/ThuTien` trả `302` về `/HoaDon?thang=6&nam=2026`.
- Verify DB thật sau submit:
  - Hóa đơn `#17`, phòng `TEST_QUICKPAY_20260629211502 P`.
  - `TongCong = 120,000`.
  - `SoTienDaThu = 33,000`.
  - `ConLai = 87,000`.
  - Dòng smoke `TienMat = 10,000`.
  - Dòng `TruCoc = 23,000` vẫn hiển thị riêng.
- Browser plugin ban đầu mở trang/modal và chụp screenshot được, console không có warning/error. Sau cú submit cuối, Browser runtime bị lỗi reconnect `failed to write kernel assets`; phần xác nhận sau submit được kiểm chứng bằng server log và DB query trực tiếp.

Ghi chú:

- Dữ liệu smoke test `TEST_QUICKPAY_20260629211502` còn trong DB thật để đối chiếu; có thể dọn sau.
- Console verify/seed nằm trong `.agents/` và không commit.

---

### Phiên 29 - UI Nhập Chỉ Số Hàng Loạt

Ngày: 30/06/2026

Đã làm:

- Thêm `ChiSoHangLoatViewModel` để render bảng nhập chỉ số theo kỳ.
- Thêm action `ChiSoController.NhapHangLoat` GET/POST.
- Màn `Views/ChiSo/NhapHangLoat.cshtml` hiển thị một dòng cho mỗi phòng đang thuê + dịch vụ `TheoChiSo`.
- Mỗi dòng có:
  - Checkbox chọn lưu.
  - Chỉ số đầu lấy từ kỳ trước hoặc chỉ số hiện tại nếu đã nhập.
  - Chỉ số cuối.
  - Loại ghi nhận `BinhThuong`/`Reset`.
  - Trường reset: chỉ số trước reset, sau reset, lý do.
  - Sản lượng tính ngay trên UI.
  - Badge trạng thái.
- Client-side JS tự tính sản lượng và tô lỗi nhanh khi:
  - `BinhThuong` có chỉ số cuối nhỏ hơn chỉ số đầu.
  - `Reset` thiếu chỉ số trước reset, chỉ số trước reset nhỏ hơn đầu, cuối nhỏ hơn sau reset, hoặc thiếu lý do.
- POST bulk dùng lại cùng validate hiện có của `ChiSoController` và `ChiSoConsumptionCalculator`.
- Refactor nhẹ private method trong `ChiSoController`:
  - Tách validate chỉ số ra `ValidateChiSoAsync`.
  - Tách thao tác lưu ra `SaveChiSoItemsAsync`.
  - Nhập đơn lẻ và nhập theo phòng vẫn dùng luồng cũ.
- Thêm nút `Nhap hang loat` vào `Views/ChiSo/Index.cshtml`.

Kết quả kiểm tra:

```text
dotnet build --no-restore
Build succeeded.
0 Warning(s)
0 Error(s)
```

QA với app chạy MySQL thật tại `http://127.0.0.1:5104`:

- Browser plugin bị lỗi bootstrap `failed to write kernel assets`, nên fallback sang HTTP + DB verify.
- Seed dữ liệu smoke test tiền tố `TEST_BULK_METER_20260630205847`.
- GET `/ChiSo/NhapHangLoat?thang=7&nam=2026` trả `200`, render đúng title và dòng test.
- POST `/ChiSo/NhapHangLoat` một dòng test qua form thật với anti-forgery token thành công; server log ghi `302` về `/ChiSo?thang=7&nam=2026`.
- Verify DB thật:
  - Phòng `#14`, dịch vụ `#6`, kỳ `7/2026`.
  - `ChiSoDau = 100.00`.
  - `ChiSoCuoi = 125.50`.
  - `LoaiGhiNhan = BinhThuong`.
  - `SanLuong = 25.50`.

Ghi chú:

- Dữ liệu smoke test `TEST_BULK_METER_20260630205847` còn trong DB thật để đối chiếu; có thể dọn sau.
- Console seed/verify nằm trong `.agents/` và không commit.

---

### Phiên 30 - Preview Chốt Hóa Đơn Hàng Loạt

Ngày: 30/06/2026

Đã làm:

- Thêm màn `HoaDon/ChotHangLoat` để preview hóa đơn theo kỳ trước khi ghi DB thật.
- Màn preview hiển thị các hợp đồng `DangHieuLuc`, tiền phòng, dịch vụ, nợ kỳ trước, tổng dự kiến và trạng thái dữ liệu.
- Trạng thái dữ liệu gồm:
  - Đã có hóa đơn.
  - Thiếu chỉ số cho dịch vụ `TheoChiSo`.
  - Thiếu dịch vụ.
  - Có nợ kỳ trước.
  - Sẵn sàng chốt.
- Thêm POST chốt hàng loạt: chỉ tạo hóa đơn cho các hợp đồng được chọn và vẫn sẵn sàng sau khi recompute server-side.
- Tách `HoaDonService.TinhHoaDonDuKienAsync` để preview và `LapHoaDonAsync` dùng chung logic tính tiền phòng, dịch vụ, chỉ số, giá áp dụng và nợ kỳ trước.
- `LapHoaDonAsync` giờ chặn thiếu chỉ số của dịch vụ `TheoChiSo`, tránh lập hóa đơn bị thiếu dòng dịch vụ.
- Thêm nút `Preview chốt hàng loạt` từ danh sách hóa đơn.

Kết quả kiểm tra:

```text
dotnet build --no-restore
Build succeeded.
0 Warning(s)
0 Error(s)
```

QA với app chạy MySQL thật tại `http://127.0.0.1:5105`:

- Seed dữ liệu smoke test tiền tố `TEST_BULK_INVOICE_PREVIEW_20260630212750`.
- GET `/HoaDon/ChotHangLoat?thang=8&nam=2026` trả `200`, render đúng dòng test và trạng thái `Sẵn sàng chốt`.
- POST form thật có anti-forgery token tới `/HoaDon/ChotHangLoat`, chọn hợp đồng test `#15`, server log trả `302` về preview.
- Verify DB thật:
  - Hóa đơn mới `#19`, kỳ `8/2026`.
  - `TienPhong = 1,200,000`.
  - `TongTienDichVu = 150,000`.
  - `TienNoKyTruoc = 80,000`.
  - `TongCong = 1,430,000`.
  - Có 2 dòng `ChiTietHoaDon`.
  - Hóa đơn cũ `#18` đã được tất toán bằng `KetChuyenNo = 80,000`.

Ghi chú:

- Dữ liệu smoke test `TEST_BULK_INVOICE_PREVIEW_20260630212750` còn trong DB thật để đối chiếu; có thể dọn sau.
- Console seed/verify nằm trong `.agents/` và không commit.

---

### Phiên 31 - In Phiếu Thu HTML

Ngày: 30/06/2026

Đã làm:

- Thêm action `HoaDonController.InPhieuThu`.
- Thêm `PhieuThuHtmlViewModel` để gom hóa đơn, hợp đồng, phòng, khách thuê, chi tiết hóa đơn và lịch sử thanh toán.
- Thêm view `Views/HoaDon/InPhieuThu.cshtml`:
  - Bố cục phiếu thu HTML gọn để in A4.
  - Nút `In phiếu thu` gọi `window.print()`.
  - Nút quay lại chi tiết hóa đơn và nút xuất Excel.
  - Bảng chi tiết tiền phòng, dịch vụ, nợ/dư kỳ trước, tổng cộng, đã thu, còn lại.
  - Lịch sử thanh toán và cảnh báo riêng cho `KetChuyenNo`/`TruCoc`.
  - CSS print ẩn sidebar/toolbar và canh giấy A4.
- Thêm nút `In phiếu thu` vào màn chi tiết hóa đơn.

Kết quả kiểm tra:

```text
dotnet build --no-restore
Build succeeded.
0 Warning(s)
0 Error(s)
```

QA với app chạy MySQL thật tại `http://127.0.0.1:5106`:

- GET `/HoaDon/InPhieuThu/19` trả `200`.
- HTML có tiêu đề `PHIẾU THU TIỀN PHÒNG`.
- HTML có nút in dùng `window.print()`.
- HTML hiển thị đúng `Phiếu #19`.

Ghi chú:

- Không seed thêm dữ liệu mới; dùng hóa đơn smoke test `#19` từ phiên preview chốt hóa đơn hàng loạt.
- App test cổng `5106` đã dừng sau khi kiểm tra.

---

### Phiên 32 - Nhắc Nợ Tối Thiểu Giai Đoạn 1

Ngày: 30/06/2026

Phạm vi đã chốt:

- Chỉ làm màn nhắc nợ cho chủ nhà/quản lý.
- Không sinh nội dung tin nhắn mẫu.
- Không ghi log đã nhắc.
- Không gửi tự động qua Telegram/ZNS/SMS.
- Giai đoạn 2/3 treo lại cho sau.

Đã làm:

- Thêm `NhacNoController.Index`.
- Tái dùng `HoaDonRepository.GetCongNoAsync()` và `BaoCaoCongNoViewModel`, không tạo bảng/schema mới.
- Thêm màn `Views/NhacNo/Index.cshtml`:
  - Mặc định lọc khách đang ở + hóa đơn đã quá hạn.
  - Filter theo Nhà, trạng thái khách/hợp đồng, nhóm nợ và từ khóa.
  - Nhóm nợ gồm `Đã quá hạn`, `Quá hạn trên 30 ngày`, `Chưa quá hạn`, `Tất cả còn nợ`.
  - Summary card: nợ theo bộ lọc, tổng nợ quá hạn, nợ quá hạn của khách đang ở, số hóa đơn quá hạn trên 30 ngày.
  - Bảng ưu tiên nhắc, phòng, khách, SĐT, kỳ, còn nợ, số ngày quá hạn, trạng thái.
  - Link nhanh tới chi tiết hóa đơn và in phiếu thu.
- Thêm menu `Nhắc nợ` vào sidebar.

Kết quả kiểm tra:

```text
dotnet build --no-restore
Build succeeded.
0 Warning(s)
0 Error(s)
```

QA với app chạy MySQL thật tại `http://127.0.0.1:5107`:

- GET `/NhacNo` trả `200`.
- HTML có tiêu đề `Nhắc nợ`.
- HTML có filter `Nhóm nợ`.
- HTML có link `Báo cáo công nợ`.

Ghi chú:

- Không seed thêm dữ liệu mới; dùng dữ liệu công nợ hiện có.
- App test cổng `5107` đã dừng sau khi kiểm tra.

---

### Phiên 33 - Nâng Filter Preview Chốt Hóa Đơn Hàng Loạt

Ngày: 30/06/2026

Đã làm:

- Nâng màn `HoaDon/ChotHangLoat` để vận hành dễ rà trước khi chốt:
  - Filter theo `Nhà`.
  - Tìm theo tên phòng, mã hợp đồng, tên khách thuê hoặc số điện thoại.
  - Lọc trạng thái dòng: tất cả, sẵn sàng chốt, cần kiểm tra, thiếu chỉ số, đã có hóa đơn, thiếu dịch vụ.
  - Summary card và footer tính theo bộ lọc hiện tại.
  - Checkbox chọn tất cả chỉ chọn các dòng sẵn sàng đang hiển thị theo bộ lọc.
- POST chốt hàng loạt giữ lại bộ lọc khi redirect về preview.
- Controller preview nạp thêm danh sách khách thuê cho từng hợp đồng để tìm/hiển thị khách ngay trên dòng.
- `HopDongRepository.GetAllAsync/GetByIdAsync` trả thêm `Phong.NhaId` để lọc theo Nhà.

Kết quả kiểm tra:

```text
dotnet build --no-restore
Build succeeded.
0 Warning(s)
0 Error(s)
```

QA với app chạy MySQL thật tại `http://127.0.0.1:5113`:

- GET `/HoaDon/ChotHangLoat?thang=8&nam=2026` trả `200`.
- GET `/HoaDon/ChotHangLoat?thang=8&nam=2026&trangThaiDong=SanSang` trả `200`.
- GET `/HoaDon/ChotHangLoat?thang=8&nam=2026&trangThaiDong=CanKiemTra&tuKhoa=TEST_BULK_INVOICE_PREVIEW_20260630212750` trả `200`.
- HTML có filter `Trạng thái dòng`, ô `Tìm phòng/khách` và checkbox `checkAllReady`.

Ghi chú:

- Smoke test runtime cần override connection string bằng `SslMode=None` trong biến môi trường của lệnh test vì process nền PowerShell bị lỗi MySQL SSL credential nếu dùng default SSL.
- Không seed thêm dữ liệu mới; dùng dữ liệu smoke test `TEST_BULK_INVOICE_PREVIEW_20260630212750` còn sẵn.
- App test cổng `5113` đã dừng sau khi kiểm tra.

---

### Phiên 34 - Cho Nhập Chỉ Số Đầu Ở Kỳ Đầu

Ngày: 30/06/2026

Vấn đề:

- Màn nhập chỉ số đang lấy `ChiSoDau = kyTruoc.ChiSoCuoi ?? 0` và khóa chỉ số đầu.
- Khi bắt đầu dùng app từ dữ liệu thực tế, đồng hồ điện/nước thường không bắt đầu từ 0 nên không thể nhập đúng kỳ đầu.

Quy tắc đã chốt:

- Nếu phòng/dịch vụ chưa có chỉ số hiện tại và chưa có chỉ số kỳ trước, cho nhập `ChiSoDau` theo số hiện có trên đồng hồ.
- Nếu đã có kỳ trước, `ChiSoDau` tự nối từ `ChiSoCuoi` gần nhất trước đó và không nhập tay.
- Nếu đang sửa dòng đã nhập, giữ `ChiSoDau` đã lưu để tránh lệch chuỗi audit.

Đã làm:

- Sửa `ChiSoController` để nhận `chiSoDaus` từ form nhưng chỉ dùng khi không có dòng hiện tại và không có kỳ trước.
- Màn `ChiSo/Nhap` và `ChiSo/NhapTheoPhong` hiển thị ô nhập `ChiSoDau` cho kỳ đầu; các trường hợp còn lại vẫn khóa như cũ.
- Màn `ChiSo/NhapHangLoat` thêm ô nhập `ChiSoDau` cho từng dòng kỳ đầu và cập nhật tính sản lượng client-side theo số đầu vừa nhập.
- Validate server-side chặn `ChiSoDau < 0`.

Kết quả kiểm tra:

```text
dotnet build --no-restore
Build succeeded.
0 Warning(s)
0 Error(s)
```

QA với app chạy MySQL thật tại `http://127.0.0.1:5114`:

- GET `/ChiSo/NhapHangLoat?thang=1&nam=2020` trả `200`, HTML có `data-start-input`.
- GET `/ChiSo/Nhap?hopDongId=15&thang=1&nam=2020` trả `200`, HTML có `data-start-input`.
- GET `/ChiSo/NhapTheoPhong?phongId=15&thang=1&nam=2020` trả `200`, HTML có `data-start-input`.

Ghi chú:

- Không POST lưu dữ liệu smoke test cho kỳ cũ để tránh tạo chỉ số 1/2020 làm thay đổi chuỗi kỳ trước của dữ liệu thật.
- Smoke test runtime dùng override `SslMode=None` trong biến môi trường giống phiên 33.
- App test cổng `5114` đã dừng sau khi kiểm tra.

---

### Phiên 35 - Chỉ Số Ngoài Hợp Đồng Cho Phòng Trống/Sửa Phòng

Ngày: 01/07/2026

Quyết định đã chốt với chủ dự án:

- Dùng phương án thiết kế 2: thêm bảng riêng `ChiSoNgoaiHopDong`.
- Điện/nước phát sinh khi phòng trống, sửa phòng hoặc chủ nhà sử dụng không tính vào hóa đơn khách thuê.
- `DenChiSo` mới nhất của dòng ngoài hợp đồng được dùng làm mốc `ChiSoDau` cho kỳ/hợp đồng sau nếu mới hơn chỉ số kỳ trước.

Đã làm:

- Cập nhật `Database/schema.sql` thêm bảng `ChiSoNgoaiHopDong`:
  - `PhongId`, `DichVuId`, `TuChiSo`, `DenChiSo`, `SanLuong`, `NgayGhiNhan`, `LyDo`, `GhiChu`.
  - CHECK chống chỉ số âm và `DenChiSo < TuChiSo`.
  - Index `IX_ChiSoNgoaiHopDong_Moc` theo phòng/dịch vụ/ngày.
- Thêm `ChiSoNgoaiHopDong` model và `ChiSoNgoaiHopDongRepository`.
- Thêm `ChiSoNgoaiHopDongController` và màn `Views/ChiSoNgoaiHopDong/Index.cshtml`:
  - Tạo dòng chỉ số ngoài hợp đồng.
  - Filter theo phòng/dịch vụ.
  - Xóa dòng audit nếu nhập nhầm.
- Thêm menu `Chỉ số ngoài HĐ` và link từ màn `ChiSo/Index`.
- Sửa `ChiSoController`:
  - Khi nhập chỉ số, nếu có dòng ngoài hợp đồng mới hơn chỉ số kỳ trước thì dùng `DenChiSo` làm `ChiSoDau`.
  - Nếu không có kỳ trước và không có ngoài hợp đồng thì vẫn cho nhập tay `ChiSoDau` như phiên 34.

Kết quả kiểm tra:

```text
dotnet build --no-restore
Build succeeded.
0 Warning(s)
0 Error(s)
```

QA với app chạy MySQL thật:

- Đã apply `CREATE TABLE IF NOT EXISTS ChiSoNgoaiHopDong` lên DB runtime.
- GET `/ChiSoNgoaiHopDong` trả `200`.
- POST `/ChiSoNgoaiHopDong/Create` tạo dòng test tương lai `TEST_CHISO_NGOAI_HD_20990115` thành công.
- GET `/ChiSo/NhapTheoPhong?phongId=15&thang=1&nam=2099` trả `200` và dùng `DenChiSo = 1245.00` làm mốc chỉ số đầu.
- POST xóa dòng test thành công; không giữ dữ liệu smoke test mới.

Ghi chú:

- Smoke test runtime dùng biến môi trường connection string có `SslMode=None;AllowPublicKeyRetrieval=True` cho process test, không sửa file config repo.
- Helper apply schema tạm đã được dọn, không commit.

---

### Phiên 36 - Chỉ Số Theo Hợp Đồng Cho Nhiều Đoạn Cùng Tháng

Ngày: 01/07/2026

Quyết định đã chốt với chủ dự án:

- Đi thẳng phương án B vì hệ thống chưa chạy trên dữ liệu thật.
- `ChiSoDienNuoc` có thêm `HopDongId` nullable để hỗ trợ nhiều dòng cùng phòng/dịch vụ/tháng nhưng khác hợp đồng.
- `HopDongId IS NULL` chỉ còn là mốc theo phòng/tạm trước khi có hợp đồng hoặc fallback dữ liệu cũ.
- `NgayDoc` là ngày đọc/bàn giao thực tế, dùng để nối chỉ số khách cũ -> ngoài hợp đồng -> khách mới trong cùng tháng.
- Khi trả phòng/chuyển phòng, thiếu chỉ số dịch vụ `TheoChiSo` là lỗi chặn, không được bỏ qua dòng dịch vụ.

Đã làm:

- Cập nhật `Database/schema.sql`:
  - Thêm `ChiSoDienNuoc.HopDongId`.
  - Thêm FK `FK_ChiSo_HopDong`.
  - Đổi unique scope sang `(PhongId, DichVuId, Thang, Nam, ChiSoScopeKey)` với `ChiSoScopeKey = COALESCE(HopDongId, 0)`.
  - Thêm index `IX_ChiSo_HopDong_Ky`.
- Cập nhật model/repository:
  - `ChiSoDienNuoc` có `HopDongId`.
  - `ChiSoDienNuocRepository` ưu tiên chỉ số đúng `HopDongId`, fallback dòng theo phòng khi cần.
  - Thêm lookup mốc chỉ số gần nhất theo ngày đọc.
  - `ChiSoNgoaiHopDongRepository` có lookup mốc ngoài hợp đồng theo ngày.
- Cập nhật `ChiSoController`:
  - Nhập theo hợp đồng lưu `HopDongId`.
  - Nhập theo phòng vẫn lưu `HopDongId = NULL`.
  - Nhập hàng loạt group theo phòng + hợp đồng.
  - Tự nối `ChiSoDau` theo chuỗi cùng hợp đồng, sau đó theo mốc chỉ số/ngoài hợp đồng gần nhất trước ngày bắt đầu hợp đồng.
  - Form nhập chỉ số cho phép chọn `NgayDoc`.
- Cập nhật `TraPhongService` và `ChuyenPhongService`:
  - Query chỉ số theo `HopDongId`, fallback mốc theo phòng.
  - Thiếu chỉ số `TheoChiSo` thì throw lỗi nghiệp vụ thay vì `continue`.
- Cập nhật `ChiSoNgoaiHopDongController`:
  - Chặn ghi chỉ số ngoài hợp đồng vào ngày đang thuộc một hợp đồng của phòng.
- Cập nhật `DECISIONS.md` và `PROJECT_REVIEW.md`.

Kết quả kiểm tra:

```text
dotnet build --no-restore
Build succeeded.
0 Warning(s)
0 Error(s)
```

QA với MySQL thật:

- Đã apply schema runtime cho `ChiSoDienNuoc.HopDongId`, `ChiSoScopeKey`, FK/index/unique scope mới.
- Seed dữ liệu smoke test tiền tố `TEST_METER_CONTRACT_SCOPE_20260701230229`.
- Tạo cùng một phòng/dịch vụ/tháng 6/2026:
  - Hợp đồng cũ có chỉ số 100 -> 150, `NgayDoc = 10/06/2026`.
  - Chỉ số ngoài hợp đồng 150 -> 160, `NgayGhiNhan = 15/06/2026`.
  - Hợp đồng mới có chỉ số 160 -> 180, `NgayDoc = 30/06/2026`.
- Verify `ChiSoDienNuocRepository.GetByHopDongKyAsync` của hợp đồng mới lấy đúng dòng `HopDongId = 19`.
- Verify `HoaDonService.TinhHoaDonDuKienAsync` cho hợp đồng mới kỳ 6/2026:
  - `PreviewSoLuong = 20.00`.
  - `PreviewThanhTien = 80,000`.
  - `PreviewLoi = []`.

Ghi chú:

- HTTP runtime UI smoke bị blocker môi trường: process nền log có lúc báo app started nhưng không giữ listener ổn định để `Invoke-WebRequest` connect. Đã dừng process test còn treo.
- Restore/build sau smoke project tạm có warning `NU1900` do không truy cập được vulnerability feed NuGet trong sandbox; không ảnh hưởng lỗi compile/app chính.

---

### Phiên 37 - Migration SQL Cho Chỉ Số Theo Hợp Đồng

Ngày: 01/07/2026

Đã làm:

- Thêm migration lịch sử, nay lưu tại `Database/updates/archive_pre_20260710/20260701_contract_scoped_meter_readings.sql`.
- Migration dùng stored procedure tạm và `INFORMATION_SCHEMA` để tự kiểm tra trước khi:
  - Thêm `ChiSoDienNuoc.HopDongId`.
  - Thêm generated column `ChiSoScopeKey = COALESCE(HopDongId, 0)`.
  - Thêm index FK cho `PhongId`, `DichVuId`.
  - Drop unique cũ `UQ_ChiSo`.
  - Tạo unique mới `(PhongId, DichVuId, Thang, Nam, ChiSoScopeKey)`.
  - Thêm index `IX_ChiSo_HopDong_Ky`.
  - Thêm FK `FK_ChiSo_HopDong`.

Ghi chú vận hành:

- DB tạo mới dùng `Database/schema.sql`.
- DB đang tồn tại cần chạy file update này một lần trong MySQL Workbench hoặc MySQL CLI sau khi backup.
- DB runtime đã smoke test ở phiên 36 đã được apply schema tương đương.

---

### Phiên 38 - UI Vận Hành Cho Chỉ Số Nhiều Đoạn

Ngày: 02/07/2026

Đã làm:

- Smoke test UI/MVC form với MySQL thật cho flow:
  - Khách cũ trả phòng trong cùng tháng, nhập chỉ số cuối với `NgayDoc = 10/06/2026`.
  - Ghi `ChiSoNgoaiHopDong` cho đoạn phòng trống/sửa phòng.
  - Tạo hợp đồng khách mới cùng phòng trong cùng tháng.
  - Nhập chỉ số kỳ đầu khách mới, xác nhận `ChiSoDau` lấy đúng từ `DenChiSo` ngoài hợp đồng.
  - Preview/chốt hóa đơn khách mới, xác nhận dịch vụ theo chỉ số chỉ tính phần khách mới dùng.
- Thêm link `Ghi chỉ số ngoài hợp đồng` trên màn trả phòng thành công, truyền sẵn `phongId`.
- Màn `ChiSoNgoaiHopDong` gợi ý `TuChiSo` từ mốc chỉ số cuối gần nhất của phòng/dịch vụ.
- Màn nhập chỉ số theo hợp đồng, theo phòng và hàng loạt hiển thị nguồn mốc `ChiSoDau`.

Kết quả kiểm tra:

```text
dotnet build --no-restore
Build succeeded.
0 Warning(s)
0 Error(s)
```

Warning `NU1900` do sandbox không truy cập được NuGet vulnerability feed, không phải lỗi compile.

QA với app chạy MySQL thật tại `http://127.0.0.1:5121`:

- GET `/` trả `200`.
- GET `/ChiSoNgoaiHopDong?phongId=18&dichVuId=1` trả `200`, có gợi ý mốc gần nhất.
- GET `/ChiSo/Nhap?hopDongId=21&thang=6&nam=2026` trả `200`, hiển thị `Nguồn: Đã nhập trong kỳ này`.
- GET `/ChiSo/NhapHangLoat?thang=6&nam=2026` trả `200`.
- Smoke flow dữ liệu `TEST_UI_CONTRACT_SCOPE_20260702213715`:
  - Hợp đồng cũ `#20`, hợp đồng mới `#21`, hóa đơn mới `#21`.
  - Preview khớp `20.00 x 4,000 = 80,000`.
  - Chi tiết hóa đơn khớp `20.00` và `80,000`.

Ghi chú:

- In-app browser plugin bị lỗi hạ tầng `failed to write kernel assets`, nên QA dùng HTTP MVC form thật với antiforgery token thay vì screenshot browser.
- App test cổng `5121` đã dừng sau khi kiểm tra.

---

### Phiên 39 - Giá Dịch Vụ Mặc Định Và Khoản Phát Sinh Hợp Đồng

Ngày: 06/07/2026

Quyết định nghiệp vụ:

- `DichVu.DonGiaMacDinh` chỉ là giá gợi ý khi gán dịch vụ cho phòng.
- `PhongDichVu.DonGia` vẫn là đơn giá thực tế áp dụng và là nguồn tính hóa đơn.
- Khi sửa giá mặc định của dịch vụ, không tự cập nhật các phòng đã gán dịch vụ.
- Khoản khách làm hỏng đồ/mất chìa khóa/phụ thu một lần được quản lý bằng `KhoanPhatSinhHopDong`, không nhét vào `ThuChi` và không tạo dịch vụ giả.
- Khoản phát sinh chưa xử lý được đưa vào hóa đơn kỳ phù hợp hoặc cộng vào tổng nợ khi trả phòng để có thể trừ cọc.

Đã làm:

- Cập nhật `Database/schema.sql`:
  - Thêm `DichVu.DonGiaMacDinh`.
  - Thêm `HoaDon.TongTienPhatSinh`.
  - Thêm bảng `KhoanPhatSinhHopDong`.
- Thêm migration lịch sử, nay lưu tại `Database/updates/archive_pre_20260710/20260706_default_service_price_and_contract_charges.sql`.
- Thêm model/repository/controller/view cho khoản phát sinh hợp đồng.
- Màn `DichVu` cho nhập/sửa đơn giá mặc định.
- Màn tạo phòng tự điền đơn giá dịch vụ từ `DichVu.DonGiaMacDinh`.
- `HoaDonService` đưa khoản phát sinh chưa xử lý tới cuối kỳ vào hóa đơn và liên kết lại `HoaDonId`.
- `TraPhongService` cộng khoản phát sinh chưa xử lý vào tổng nợ trả phòng; cọc trừ nợ hóa đơn trước, phần còn lại mới trừ khoản phát sinh chưa vào hóa đơn.
- Chi tiết hóa đơn, phiếu thu HTML, xuất phiếu thu Excel và preview chốt hóa đơn hiển thị khoản phát sinh.
- Chi tiết hợp đồng có khu vực/link quản lý khoản phát sinh.

Kết quả kiểm tra:

```text
dotnet build --no-restore
Build succeeded.
0 Warning(s)
0 Error(s)
```

DB runtime:

- Đã apply additive migration bằng runner tạm trong `obj/`.
- Rerun migration báo các cột/bảng/index đã tồn tại.
- Warning `NU1900` khi chạy runner do sandbox không truy cập được NuGet vulnerability feed.

Ghi chú:

- HTTP smoke bằng `Start-Job` vẫn bị lỗi hạ tầng Windows EventLog của sandbox khi Kestrel cố ghi event log; không để lại process/cổng treo.
- Ghi chú này chỉ áp dụng cho baseline cũ; file hiện nằm trong `Database/updates/archive_pre_20260710` và không chạy trên database tạo mới.

---

### Phiên 40 - Cách Tính Dịch Vụ Cố Định Theo Người

Ngày: 07/07/2026

Quyết định nghiệp vụ:

- Giữ `DichVu.LoaiTinhPhi` chỉ gồm `CoDinh` và `TheoChiSo`.
- Thêm `DichVu.CachTinhCoDinh` cho riêng dịch vụ cố định:
  - `TheoPhong`: `SoLuong = 1`.
  - `TheoNguoi`: `SoLuong = COUNT(HopDongKhachThue)` theo hợp đồng.
- `TheoChiSo` giữ nguyên logic sản lượng từ `ChiSoDienNuoc`.
- Nếu dịch vụ `CoDinh + TheoNguoi` nhưng hợp đồng chưa gắn khách, preview/chốt hóa đơn bị chặn để tránh tính 0.
- Chưa làm biến động nhân khẩu theo ngày vì `HopDongKhachThue` chưa có mốc ngày riêng.

Đã làm:

- Cập nhật `Database/schema.sql` thêm `DichVu.CachTinhCoDinh`, CHECK constraint và seed mẫu: `Nước` mặc định `TheoNguoi`, các dịch vụ cố định còn lại giữ `TheoPhong`.
- Thêm migration lịch sử, nay lưu tại `Database/updates/archive_pre_20260710/20260707_fixed_service_quantity_method.sql`.
- Cập nhật `DichVu` model, `DichVuRepository` và các repository join `DichVu`.
- Màn `DichVu` cho chọn cách tính cố định và khóa lựa chọn này khi loại tính phí là `TheoChiSo`.
- Màn tạo/sửa/chi tiết phòng hiển thị thêm nhãn cách tính cố định cho dịch vụ đã gán.
- Thêm `FixedServiceQuantityCalculator` dùng chung cho dịch vụ `CoDinh`.
- Cập nhật `HoaDonService.TinhHoaDonDuKienAsync`, `TraPhongService` và `ChuyenPhongService` để tính `CoDinh + TheoNguoi` theo số khách trong `HopDongKhachThue`.
- Preview chốt hóa đơn hàng loạt và chốt đơn lẻ dùng lại logic này nên sẽ hiển thị `SoLuong` đúng và chặn dòng thiếu khách.

Kết quả kiểm tra:

```text
dotnet build --no-restore
Build succeeded.
0 Warning(s)
0 Error(s)

dotnet test --no-restore
Exit code 0; no meaningful test output in this repo.
```

Ghi chú:

- Chưa chạy HTTP smoke vì blocker sandbox Windows EventLog đã được ghi nhận từ phiên 39.
- Ghi chú này chỉ áp dụng cho baseline cũ; database mới lấy trực tiếp `CachTinhCoDinh` từ `Database/schema.sql`.
- Sau phản hồi runtime, migration đã được chỉnh để câu `UPDATE DichVu` dùng điều kiện theo key `Id`, tránh lỗi MySQL Workbench Safe Updates `Error Code: 1175`.

---

### Phiên 41 - Gán/Cập Nhật Dịch Vụ Hàng Loạt Cho Phòng

Ngày: 07/07/2026

Đã làm:

- Thêm màn `Phong/GanDichVuHangLoat` để chọn dịch vụ, lọc theo Nhà/trạng thái phòng và tick nhiều phòng để gán/cập nhật cùng lúc.
- Mặc định màn này ưu tiên dịch vụ `CoDinh + TheoNguoi` và lọc phòng `DangThue`, phù hợp bước cấu hình nước/vệ sinh/máy giặt theo người.
- Bảng hiển thị Nhà/phòng, hợp đồng hiệu lực, số khách, trạng thái dịch vụ đã gán/chưa gán/đang tắt và đơn giá hiện tại.
- Với dịch vụ `CoDinh + TheoNguoi`, màn hình cảnh báo phòng có hợp đồng hiệu lực nhưng `SoKhach = 0`.
- Màn hình có cột `So luong du kien` và `Thanh tien du kien`; khi sửa đơn giá, thành tiền dự kiến tự cập nhật ngay trên trình duyệt.
- Khi lưu, hệ thống ghi vào `PhongDichVu` bằng upsert:
  - Phòng chưa có dịch vụ thì gán mới.
  - Phòng đã có dịch vụ thì cập nhật `DonGia` và bật `DangApDung = 1`.
- Thêm link `Gan dich vu hang loat` từ danh sách phòng.

Test flow cần chạy sau:

1. Vào từng nhóm phòng đang thuê, gán các dịch vụ theo người cần áp dụng bằng màn hàng loạt.
2. Kiểm tra lại đơn giá trong `PhongDichVu`, vì hóa đơn dùng giá theo phòng chứ không dùng `DichVu.DonGiaMacDinh`.
3. Preview chốt hóa đơn một kỳ gần nhất để xác nhận dòng như `Nước: số khách x đơn giá`.

Kết quả kiểm tra:

```text
dotnet build --no-restore
Build succeeded.
1 Warning(s)
0 Error(s)

dotnet test --no-restore
Exit code 0; no meaningful test output in this repo.
```

Ghi chú:

- Chưa chạy HTTP smoke vì blocker Windows EventLog của sandbox vẫn là rủi ro môi trường đã biết.

### Phiên 42 - Smoke Test Dịch Vụ Cố Định Theo Người

Ngày: 07/07/2026

Đã làm:

- Tạo dữ liệu smoke test tiền tố `TEST_FSB_20260707213224` trên MySQL thật.
- Seed 1 dịch vụ `CoDinh + TheoNguoi`, 2 phòng đang thuê, 2 hợp đồng hiệu lực:
  - Phòng OK có 2 khách gắn với hợp đồng.
  - Phòng thiếu khách không có dòng `HopDongKhachThue`.
- Gán dịch vụ hàng loạt qua repository thật để ghi vào `PhongDichVu.DonGia`.
- Gọi `HoaDonService.TinhHoaDonDuKienAsync` cho kỳ 07/2026.

Kết quả smoke:

```text
BulkRows=2
BulkRowOk: SoKhach=2; DonGia=120000
BulkRowMissingCustomers: SoKhach=0; DonGia=120000
PreviewOk: Loi=0; SoLuong=2; DonGia=120000; ThanhTien=240000
PreviewMissingCustomersErrors=... chua gan khach thue.
PASS_BULK_AND_PREVIEW=True
```

File dọn dữ liệu test:

- `Database/cleanup/TEST_FSB_20260707213224_cleanup.sql`
- File dùng ID hằng trực tiếp và khóa ngoại có index, tránh `DELETE ... IN (SELECT ... FROM temp table)` để phù hợp MySQL Workbench khi bật Safe Updates.

### Phiên 43 - Màn Sẵn Sàng Vận Hành

Ngày: 07/07/2026

Đã làm:

- Thêm màn `KiemTraDuLieu/Index` để rà các hợp đồng `DangHieuLuc` theo kỳ trước khi chốt hóa đơn.
- Thêm `KiemTraDuLieuRepository` tổng hợp dữ liệu cấu hình: nhà/phòng/hợp đồng, số khách, dịch vụ đang áp dụng, dịch vụ theo chỉ số, chỉ số đã nhập trong kỳ và dịch vụ có đơn giá không hợp lệ.
- Màn này gọi lại `HoaDonService.TinhHoaDonDuKienAsync` cho từng hợp đồng, nên các lỗi chặn như thiếu chỉ số hoặc dịch vụ cố định theo người thiếu khách vẫn đi qua logic hóa đơn chuẩn.
- UI có filter theo tháng/năm, nhà, từ khóa và trạng thái: sẵn sàng, cần xử lý, thiếu khách, thiếu dịch vụ, thiếu đơn giá, thiếu chỉ số, đã có hóa đơn.
- Thêm link nhanh sang chi tiết hợp đồng, gán dịch vụ hàng loạt, nhập chỉ số và preview chốt hóa đơn.
- Thêm menu sidebar `San Sang Van Hanh`.

Kết quả kiểm tra:

```text
dotnet build --no-restore
Build succeeded.
1 Warning(s)
0 Error(s)

dotnet test --no-restore
Exit code 0; no meaningful test output in this repo.
```

Ghi chú:

- Warning build là `NU1900` do sandbox không truy cập được `https://api.nuget.org/v3/index.json`, không phải lỗi compile.
- Chưa chạy HTTP smoke vì blocker Windows EventLog của sandbox vẫn là rủi ro môi trường đã biết.

---

### Phiên 44 - Chuẩn Hóa Nhãn Tiếng Việt Có Dấu Trong View

Ngày: 08/07/2026

Đã làm:

- Rà và chuẩn hóa các nhãn không dấu trong các view vận hành chính: kiểm tra dữ liệu, gán dịch vụ hàng loạt, nhà/phòng, nhập chỉ số, chuyển/trả phòng, khoản phát sinh, ledger cọc và hóa đơn.
- Đổi các nhãn/mã nội bộ đang lộ ra UI như `ThuThemCoc`, `HoanCoc`, `TruNo`, `Trong`, `HD`, `Preview` sang tiếng Việt có dấu dễ đọc hơn.
- Chuẩn hóa ký hiệu tiền tệ còn sót từ `d` sang `đ` ở các màn hình preview, hóa đơn và cấu hình dịch vụ phòng.
- Thêm helper hiển thị trạng thái khoản phát sinh và loại giao dịch cọc trong view để không in thẳng mã trạng thái ra giao diện.

Kết quả kiểm tra:

```text
dotnet build --no-restore
Build succeeded.
0 Warning(s)
0 Error(s)

dotnet test --no-restore
Exit code 0; no meaningful test output in this repo.
```

Ghi chú:

- Chưa chạy HTTP smoke vì blocker Windows EventLog của sandbox vẫn là rủi ro môi trường đã biết.
- `appsettings.json` vẫn là thay đổi cấu hình local, không thuộc phạm vi commit.

---

### Phiên 45 - Dịch Vụ Theo Hợp Đồng, Giá Theo Kỳ Và Xóa Phòng An Toàn

Ngày: 11/07/2026

Đã làm:

- Thêm `DichVu.BatBuocKhiThue` và bảng `HopDongDichVu` có `KyBatDau`/`KyKetThuc`.
- Tạo phòng mặc định chọn toàn bộ dịch vụ; sửa phòng cho phép thêm/bật/tắt dịch vụ, nhưng giá hiện hữu chỉ đổi qua màn giá theo kỳ.
- Tạo hợp đồng và chuyển phòng cho chọn dịch vụ riêng; dịch vụ bắt buộc không thể bỏ. Thêm màn cập nhật dịch vụ hợp đồng từ một kỳ sử dụng.
- Đổi nguồn dịch vụ của hóa đơn, nhập chỉ số theo hợp đồng, trả phòng, chuyển phòng và màn kiểm tra dữ liệu sang `HopDongDichVu` theo kỳ.
- Sửa lịch sử giá để kỳ trước lần đổi đầu tiên dùng `GiaCu`; thêm unique theo đối tượng/kỳ và transaction khi thêm/xóa lịch sử.
- Sửa xóa phòng: xóa transaction cấu hình của phòng chưa dùng; chặn phòng đã có hợp đồng/chỉ số/thu chi.
- Chuyển update cũ vào `Database/updates/archive_pre_20260710`; thêm README cho baseline mới.
- Thêm cấu hình test-only `UseEphemeralDataProtection=true` để HTTP smoke trong sandbox không bị chặn bởi Windows DataProtection/EventLog; mặc định production không bật.

Kết quả kiểm tra:

```text
dotnet build --no-restore
Build succeeded.
0 Warning(s)
0 Error(s)

Schema smoke: 17 tables.
Business smoke: contract service periods, invoice service selection, historical price, safe room deletion passed.
HTTP smoke: GET Phong/Create, HopDong/Create, HopDong/Details, HopDong/DichVu = 200.
HTTP POST smoke: mandatory service guard, create room with 5 services, delete unused room,
block used-room deletion, create/update contract services by period = passed.
```

Ghi chú:

- In-app Browser plugin vẫn lỗi hạ tầng `failed to write kernel assets`, nên chưa có screenshot/visual QA.
- Database chính chưa bị thay đổi; cần tạo lại từ `Database/schema.sql` sau khi chốt code.
- Không đưa thay đổi cục bộ `appsettings.json` vào phạm vi commit.

---

## Lỗi Và Fix Đã Xử Lý

| Phiên | Khu vực | Lỗi | Cách xử lý |
|---|---|---|---|
| 7-8 | `ChuyenPhongService`, `TraPhongService` | Tính tiền phòng đọc thẳng giá hiện tại | Thêm tra giá áp dụng theo kỳ |
| 9 | Nhiều model/repository/view | Code dùng field không có trong schema | Sửa code theo `Database/schema.sql` |
| 9 | `HoaDonRepository.GetCongNoAsync` | Backlog cũ nói dùng `dynamic`; code hiện đã typed | Xác nhận dùng `BaoCaoCongNoViewModel` |
| 9 | `HoaDonService` | Backlog cũ nghi chưa check trùng hóa đơn | Xác nhận đã check bằng `GetByHopDongKyAsync` |
| 9 | `ChuyenPhongService` | Dịch vụ `TheoChiSo` tính `DonGia * 1` | Sửa sang lấy `ChiSoDienNuoc` |
| 12 | `HoaDonService`, `ChuyenPhongService`, `TraPhongService` | Quy ước ngày vào/ra/chuyển phòng chưa rõ, dễ sai pro-rata khi không ở từ mùng 1 | Dùng `BillingPeriodCalculator` tính giao giữa kỳ hóa đơn và khoảng ở thực tế |
| 12 | `TraPhongService` | Hóa đơn trả phòng tính dịch vụ theo `DonGia * 1`, sai với `TheoChiSo` | Tính theo `ChiSoDienNuoc`, lưu `ChiSoDienNuocId`, tra lịch sử giá |
| 13 | `ChiSoController`, `HoaDonService`, `ChuyenPhongService`, `TraPhongService` | `ChiSoCuoi < ChiSoDau` có thể tạo tiền dịch vụ âm | Chặn ở form/controller/schema và guard bằng `ChiSoConsumptionCalculator` |
| 14 | `ChiSoDienNuoc`, `ChiSoController`, `ChiSoConsumptionCalculator` | Chưa có cách hợp lệ để nhập đồng hồ reset/hỏng/quay vòng | Thêm `LoaiGhiNhan = Reset`, mốc trước/sau reset và công thức sản lượng riêng |
| 15 | `HoaDonService.LapHoaDonAsync`, `HoaDonService.XoaHoaDonAsync` | Insert/xóa hóa đơn và chi tiết hóa đơn chưa atomic | Bọc transaction cho lập và xóa hóa đơn |
| 15 | `HopDongController`, `HopDongService` | Tạo/sửa hợp đồng gồm nhiều thao tác DB rời rạc | Bọc transaction cho tạo/sửa hợp đồng và chặn phòng có hợp đồng hiệu lực |
| 15 | `HoaDonRepository.GetCongNoAsync` | Tính ngày quá hạn bằng `hd.Thang + 1` có thể thành tháng 13 | Dùng `DATE_ADD` từ ngày đầu kỳ hóa đơn |
| 16 | `PhongController`, `Views/Phong` | Tạo/sửa phòng có nguy cơ gửi `NhaId = 0` khi bảng `Nha` rỗng hoặc form không có dropdown | Thêm dropdown chọn Nhà, cảnh báo khi chưa có Nhà và validate server-side `NhaId` |
| 20 | `ChiSoController`, `Views/ChuyenPhong` | Flow chuyển phòng không có UI nhập chỉ số cho phòng mới vì phòng mới chưa có hợp đồng | Thêm nhập chỉ số theo `PhongId` + kỳ và link từ màn chuyển phòng |
| 21 | `ChuyenPhongService`, `TraPhongService`, `GiaoDichCoc` | Cọc chỉ là số tĩnh trên hợp đồng, nợ chuyển kỳ có nguy cơ double-count | Thêm ledger cọc và bút toán `ThanhToan` phi tiền mặt `KetChuyenNo`/`TruCoc` |
| 21 | `ChuyenPhongController` | Render lại form lỗi loại phòng theo `HopDongCuId` thay vì `PhongId` cũ | Nạp lại hợp đồng cũ và loại đúng `PhongId` |
| 24 | `Views/HoaDon/Details.cshtml` | Dòng `KetChuyenNo`/`TruCoc` in như mã thanh toán thường, dễ hiểu nhầm là tiền mới thu | Hiển thị badge/cảnh báo riêng cho bút toán phi tiền mặt |
| 25 | `HoaDonService`, `TraPhongService` | Hóa đơn mới/trả phòng mang `TienNoKyTruoc` có thể double-count nếu hóa đơn cũ vẫn nằm trong báo cáo công nợ | Kết chuyển nợ cũ bằng `KetChuyenNo`, dùng tổng nợ trước kỳ, chặn xóa hóa đơn đang mang nợ đã kết chuyển |
| 27 | `Views/BaoCao/CongNo.cshtml`, `site.css` | Báo cáo công nợ thiếu filter vận hành và bảng rộng kéo ngang toàn trang | Thêm filter, cột Nhà/trạng thái, đồng bộ Excel và giới hạn scroll ngang trong bảng |
| 28 | `Views/HoaDon/Index.cshtml`, `HoaDonService` | Danh sách hóa đơn chưa có thao tác thu nhanh; input số tiền dùng `step=1000` có thể khiến browser chặn số hợp lệ | Thêm modal thu nhanh, redirect về đúng kỳ, guard không thu vượt số còn lại và đổi input sang `step=1` |
| 29 | `Views/ChiSo/NhapHangLoat.cshtml`, `ChiSoController` | Nhập chỉ số từng phòng còn chậm khi vận hành thay Excel | Thêm màn nhập hàng loạt theo kỳ, tính sản lượng tại chỗ và dùng lại validate reset/server-side hiện có |
| 30 | `HoaDon/ChotHangLoat`, `HoaDonService` | Chốt hóa đơn hàng loạt chưa có bước preview và có nguy cơ bỏ sót dịch vụ theo chỉ số nếu chưa nhập chỉ số | Thêm preview theo kỳ, badge trạng thái dữ liệu, bulk POST chỉ chốt dòng sẵn sàng và tách logic tính dự kiến dùng chung với lập hóa đơn |
| 31 | `HoaDon/InPhieuThu` | Chỉ có xuất phiếu thu Excel, chưa in nhanh trực tiếp từ trình duyệt | Thêm phiếu thu HTML với CSS print A4, nút `window.print()` và cảnh báo bút toán phi tiền mặt |
| 32 | `NhacNo/Index` | Chủ nhà vẫn phải tự lọc công nợ để biết hóa đơn nào cần nhắc | Thêm màn nhắc nợ giai đoạn 1 cho chủ nhà/quản lý, dùng dữ liệu công nợ hiện có và chưa gửi/copy tin nhắn tự động |
| 33 | `HoaDon/ChotHangLoat` | Preview chốt hàng loạt khó vận hành khi nhiều nhà/phòng và khó xem nhanh dòng lỗi | Thêm filter theo Nhà, tìm phòng/khách, lọc trạng thái dòng và chọn tất cả dòng sẵn sàng theo bộ lọc |
| 34 | `ChiSo/Nhap`, `ChiSo/NhapTheoPhong`, `ChiSo/NhapHangLoat` | Kỳ đầu chưa có dữ liệu cũ bị khóa `ChiSoDau = 0`, sai khi đồng hồ thực tế không bắt đầu từ 0 | Cho nhập `ChiSoDau` khi chưa có kỳ trước; các kỳ sau vẫn tự nối từ chỉ số cuối kỳ trước |
| 35 | `ChiSoNgoaiHopDong`, `ChiSoController` | Khi phòng trống/sửa phòng có phát sinh điện/nước, chỉ số đầu khách mới có thể khác chỉ số cuối khách cũ | Thêm bảng audit ngoài hợp đồng và dùng `DenChiSo` mới nhất làm mốc đầu kỳ sau nếu phù hợp |
| 36 | `ChiSoDienNuoc`, `ChiSoController`, `TraPhongService`, `ChuyenPhongService` | Một phòng/dịch vụ/tháng chỉ có một dòng chỉ số nên không tách được khách cũ, phòng trống và khách mới trong cùng tháng | Thêm `HopDongId` nullable cho chỉ số, dùng `NgayDoc` làm mốc bàn giao, cho phép nhiều đoạn theo hợp đồng và chặn thiếu chỉ số khi trả/chuyển phòng |
| 40 | `DichVu`, `HoaDonService`, `TraPhongService`, `ChuyenPhongService` | Dịch vụ cố định luôn tính `SoLuong = 1`, sai với các khoản thu theo số người | Thêm `DichVu.CachTinhCoDinh`, tính `TheoNguoi` bằng số khách trong `HopDongKhachThue` và chặn hóa đơn nếu hợp đồng chưa gắn khách |
| 41 | `PhongDichVu`, `Views/Phong` | Cấu hình danh mục dịch vụ xong nhưng phải gán/cập nhật giá từng phòng thủ công | Thêm màn gán/cập nhật dịch vụ hàng loạt cho nhiều phòng, ghi trực tiếp vào `PhongDichVu.DonGia` |
| 43 | `KiemTraDuLieu/Index` | Trước khi nhập dữ liệu thật/chốt kỳ cần một nơi nhìn nhanh phòng nào thiếu khách, dịch vụ, đơn giá hoặc chỉ số | Thêm màn kiểm tra read-only theo kỳ, dùng lại `HoaDonService.TinhHoaDonDuKienAsync` và link nhanh sang các màn xử lý |
| 44 | `Views/*` | Một số view còn nhãn tiếng Việt không dấu hoặc lộ mã nội bộ, làm giao diện vận hành khó đọc | Chuẩn hóa nhãn tiếng Việt có dấu, ký hiệu `đ`, và helper hiển thị trạng thái/loại giao dịch |
| 45 | `PhongDichVu`, `HopDong`, hóa đơn/chỉ số | Dịch vụ gắn theo phòng làm các hợp đồng kế tiếp không thể đăng ký khác nhau | Thêm `HopDongDichVu` theo kỳ; giữ `PhongDichVu` làm cấu hình và nguồn giá |
| 45 | `LichSuThayDoiGia` | Lập bù kỳ trước lần đổi giá đầu tiên có thể rơi về giá hiện tại | Dùng `GiaCu` của lần đổi đầu tiên, unique đối tượng/kỳ và transaction cập nhật chuỗi giá |
| 45 | `PhongController`, `PhongService` | `DELETE FROM Phong` lỗi khóa ngoại ngay cả với phòng mới có `PhongDichVu` | Xóa cấu hình trong transaction nếu phòng chưa dùng; chặn và báo rõ khi đã có dữ liệu nghiệp vụ |

---

## Hướng Dẫn Cập Nhật File Này

Khi bắt đầu phiên mới:

1. Đọc `DECISIONS.md`.
2. Đọc mục "Trạng Thái Hiện Tại" trong file này.
3. Ưu tiên xử lý việc còn lại theo bảng "Việc cần làm tiếp".

Khi kết thúc phiên:

1. Thêm một block phiên mới.
2. Cập nhật trạng thái build/test.
3. Chuyển bug đã fix khỏi backlog.
4. Ghi quyết định mới nếu có.
