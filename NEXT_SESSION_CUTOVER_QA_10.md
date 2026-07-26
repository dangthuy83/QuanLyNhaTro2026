# Prompt phiên kế tiếp: CUTOVER-QA-HOPDONG10-602-20260726

Tiếp tục dự án tại `I:\CODEX\QuanLyNhaTro`.

## Mục tiêu duy nhất

Hoàn tất acceptance QA qua UI cho workflow đóng trước cutover của **Hợp đồng #10 / Phòng 602**,
trước khi tiếp tục acceptance hóa đơn hoặc xét release ứng dụng.

## Baseline đã có

- Production chỉ có `MigrationJournal` 01..12; sequence 13 vẫn pending. Không được ghi vào
  production dưới bất kỳ hình thức nào.
- QA A4 `QuanLyNhaTro_BILLING_UI_ACCEPTANCE_QA_20260726_A4` có status 01..13, được giữ nguyên
  làm evidence: invoice 08/2026 bị block vì các hợp đồng thiếu chỉ số Điện. Không dùng A4 cho
  cutover và không drop/reset A4.
- App QA A4 đang chạy thủ công tại `http://127.0.0.1:5097`. Không dừng listener/PID này nếu
  không thật sự cần. Nếu UI cutover cần app riêng, dùng listener/port và profile Data Protection
  tạm riêng cho QA cutover; không thay cấu hình service/NSSM.
- Contract #10 / room 602 phải được loại khỏi collection 08/2026 qua workflow guard/audit, không
  tạo hóa đơn, thanh toán, chỉ số cuối, `ChiSoNgoaiHopDong`, khoản phát sinh, debt settlement hay
  chứng từ lịch sử giả.
- Contract values đã chốt cho QA acceptance: trả phòng thực tế `30/06/2026`; tiền phòng và dịch
  vụ đã thanh toán đến `01/06/2026`; công nợ `0`; hoàn cọc `2.700.000 đ` ngày `03/07/2026`; nguồn
  đối chiếu `Sổ thu của gia đình`.

## Bắt buộc đọc trước khi hành động

- `DECISIONS.md`
- `WORKLOG.md`
- `PROJECT_REVIEW.md`
- `Database/updates/README.md`
- `Database/migration-manifest.json`
- `Database/updates/20260726_advance_rent_collection_periods.sql`
- `Controllers/CutoverController.cs`
- `Services/DongHopDongTruocCutoverService.cs`
- `Models/DongHopDongTruocCutoverViewModel.cs`
- `Views/Cutover/DongHopDongTruocCutover.cshtml`
- controller/service/repository của `HopDong`, `PhongLifecycle`, `GiaoDichCoc`, `HoaDon`,
  `ChiSoDienNuoc`, `HopDongKhachThue`, `HopDongDichVu` và audit cutover.

Trước khi ghi bất kỳ QA nào, kiểm tra `git status` và chạy `MigrationRunner status` chỉ đọc để
xác nhận baseline. Không reset/clean/stash/check-out. Không sửa/xóa/stage hai file bảo vệ
`opening-balance.json` và `tools/OpeningBalanceImporter/templates/opening-balance.sample.json`.

## QA database và giới hạn

- Tạo một QA clone mới từ snapshot production read-only, ví dụ
  `QuanLyNhaTro_CUTOVER_QA_20260726_B1`; apply đúng sequence 13 bằng `MigrationRunner` và xác
  nhận status 01..13. Không dùng production cho restore/apply/POST.
- Chỉ SELECT để inventory DB production. Mọi POST và business write chỉ được thực hiện trên B1
  qua UI/workflow ứng dụng. Không dùng SQL trực tiếp để tạo hay sửa trạng thái nghiệp vụ.
- Không thay đổi công thức tiền, waterfall cọc/tín dụng/nợ, meter continuity, schema ngoài
  migration 13, auth/security, route/POST contract, dependency, NSSM/service, publish/deploy.
  Nếu cần đổi bất kỳ mục nào, dừng và xin duyệt riêng.
- Không chạy Trả phòng, Chuyển phòng, nhập chỉ số hoặc tạo hóa đơn cho hợp đồng khác trên B1.

## Acceptance cần thực hiện

1. Ghi inventory B1 trước POST, tối thiểu:
   - HĐ #10 và Phòng 602: trạng thái, ngày kết thúc/trả phòng, số dư cọc.
   - hai dòng `HopDongKhachThue` hiệu lực; sáu dòng `HopDongDichVu` chưa kết thúc.
   - count/chi tiết `HoaDon`, `ThanhToan`, `KhoanPhatSinhHopDong` chưa xử lý,
     `CongNoMoSo`, `ChiSoDienNuoc`, `ChiSoNgoaiHopDong`, `GiaoDichTinDungTienPhong`,
     `ThuChi`, `GiaoDichCoc` và `AuditDongHopDongTruocCutover` liên quan HĐ #10.
2. Qua In-app Browser đã đăng nhập admin, mở route UI cutover cho HĐ #10. Xác nhận form render
   sạch, không HTTP 500, không console error; xác nhận các giá trị contract ở trên, hai checkbox
   bắt buộc, và source/lý do không rỗng. Ghi URL/action thực tế.
3. Submit đúng một POST cutover qua UI. Không gửi payment hoặc receipt riêng.
4. Xác minh kết quả UI và DB B1 sau POST:
   - HĐ #10 `DaKetThuc`, `NgayKetThuc=NgayTraPhongThucTe=30/06/2026`;
     Phòng 602 `Trong`.
   - đúng hai cư trú kết thúc 30/06/2026; đúng sáu dịch vụ kết thúc từ 01/07/2026.
   - có đúng một `GiaoDichCoc` `HoanCoc=-2.700.000`, `SoDuSauGiaoDich=0`, ngày 03/07/2026,
     nguồn đối chiếu đúng; không có `ThanhToan` hoặc `ThuChi` giả.
   - đúng một audit với các ba mốc/amount/source/actor, `IdempotencyKey=DONG_TRUOC_CUTOVER:10`.
   - vẫn không có invoice, meter, off-contract meter, charge, opening debt hay credit liên quan
     HĐ #10.
5. Kiểm tra replay bằng cách UI hỗ trợ: sau POST, form/submit phải bị khóa hoặc route báo đã xử
   lý. Đối chiếu DB vẫn đúng một audit và một `HoanCoc`; không tạo dòng trùng.
6. Chỉ sau khi pass, mở `/HoaDon/ChotHangLoat?thang=8&nam=2026` trên B1 để xác nhận HĐ #10 không
   còn là dòng active có thể chốt. Không tạo invoice cho các hợp đồng còn lại.
7. Ghi evidence thật ngay vào `WORKLOG.md`: baseline, DB QA, URL/input, trước/sau, replay,
   console, cleanup/trạng thái giữ lại và các mục chưa kiểm chứng.

## Điều kiện dừng

- Dừng và báo blocker nếu B1 không tái hiện chính xác preconditions #10, form yêu cầu giá trị
  khác contract, một guard fail, hoặc hậu kiểm DB lệch bất kỳ invariant nào.
- Không xóa B1 sau phiên nếu POST thành công; giữ nguyên B1 để acceptance kế tiếp quyết định.
- Không release app, không apply/cutover production, không stage/commit/push khi hoàn tất.

## Báo cáo cuối phiên

Nêu ngắn: baseline, QA DB/migration, inventory trước, UI form/POST, đối chiếu DB sau, replay,
blocker/defect nếu có, QA B1 và A4 còn giữ lại, cùng các gate vẫn đóng.
