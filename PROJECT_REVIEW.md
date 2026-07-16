# PROJECT_REVIEW.md - Review nghiep vu va kien truc

Ngay lap: 27/06/2026

Vai tro review: Solution Architect + Business Analyst cho ung dung quan ly nha tro thay the Excel.

---

## 0. Review toàn diện sẵn sàng vận hành - 12/07/2026

Phạm vi: Nhà/Phòng, Khách thuê, Hợp đồng, Dịch vụ, Giá/Hình thức, Chỉ số, Hóa đơn,
Thanh toán/Công nợ, Cọc, Chuyển phòng, Trả phòng, Báo cáo và toàn vẹn database.
Đợt này chỉ review/chẩn đoán và smoke trên database tạm; chưa sửa code nghiệp vụ.

### 0.1. Executive summary

**Kết luận:** lõi tính tiền đã có nhiều nền tảng tốt, nhưng dự án **chưa sẵn sàng nhập dữ liệu thật
hoặc thay Excel hoàn toàn**. Có thể dùng để pilot có kiểm soát sau khi xử lý Phase 1, nhưng chưa nên
vận hành song song nhiều thao tác hoặc cho phép người dùng kết thúc/trả/chuyển phòng tự do.

Năm rủi ro lớn nhất:

1. Hợp đồng có thể bị đóng mà không tạo/kiểm tra hóa đơn, chỉ số và cọc kỳ cuối.
2. Lịch sử giá phòng gắn theo `Phong`, có thể lấy giá của hợp đồng cũ thay cho giá thỏa thuận hợp đồng mới.
3. Chưa bảo vệ chồng thời gian hợp đồng; hợp đồng tương lai cũng bị gắn ngay `DangHieuLuc` và làm phòng `DangThue`.
4. Thu tiền và ledger cọc không khóa dòng/số dư, có thể sai khi hai request chạy đồng thời.
5. Số khách `TheoNguoi` và thông tin nhận diện hóa đơn không có lịch sử theo kỳ, nên lập bù/in lại có thể đổi kết quả.

Các phần ổn định nhất theo bằng chứng hiện tại:

- `Database/schema.sql` tạo sạch thành công: 19 bảng, 74 constraint, 7 dịch vụ mẫu và đủ cột trọng yếu.
- `BillingPeriodCalculator` xử lý giao khoảng ngày và năm nhuận đúng về mặt công thức.
- Công thức chỉ số thường/reset có guard ở code và CHECK constraint; chỉ số theo hợp đồng có unique scope.
- `HopDongDichVu` và `LichSuHinhThucDichVu` đã resolve theo kỳ cho các service tính tiền chính.
- Hóa đơn đã tạo giữ snapshot số lượng, đơn giá và thành tiền; lập hóa đơn đơn lẻ có transaction và unique kỳ.

Các module phải xử lý trước dữ liệu thật: vòng đời hợp đồng/phòng, trả/chuyển phòng, lịch sử giá phòng,
thanh toán/cọc đồng thời, lịch sử nhân khẩu, snapshot nhận diện hóa đơn và constraint tài chính.

### 0.2. Ma trận tổng hợp A-M

| Luồng | Quy tắc/code hiện tại | Trạng thái | Mức độ | Hậu quả chính | Đề xuất nhỏ nhất |
|---|---|---|---|---|---|
| A. Nhà/Phòng | Có FK Nhà-Phòng và chặn xóa phòng đã dùng; trạng thái phòng vẫn sửa tay | Có rủi ro | High | Phòng `Trong` dù còn hợp đồng hoặc ngược lại | Khóa sửa tay, thêm reconcile trạng thái |
| B. Khách thuê | CRUD + upload extension/size; chưa có duplicate/file lifecycle | Thiếu | High | Trùng hồ sơ, file mồ côi, ảnh giả | Service upload atomic + kiểm tra magic bytes |
| C. Hợp đồng | Tạo có transaction nhưng chỉ check một trạng thái, sửa/kết thúc còn lỏng | Thiếu | Critical | Chồng kỳ, sửa lịch sử, bỏ qua quyết toán | Lifecycle service + overlap query/constraint |
| D. Dịch vụ | `PhongDichVu`/`HopDongDichVu` theo kỳ đã có | Đúng phần lớn | Medium | Một số UI/query vận hành chưa theo kỳ đầy đủ | Tập trung resolver dùng chung |
| E. Giá/Hình thức | Hình thức theo kỳ tốt hơn; giá phòng scope theo phòng | Có rủi ro | Critical | Sai tiền hợp đồng mới | Chuyển lịch sử giá thuê sang scope hợp đồng |
| F. Chỉ số | Reset/unique/hợp đồng đã có; lưu bulk không atomic, ngày đọc chưa khóa | Có rủi ro | High | Chuỗi công tơ đứt, sửa audit sau chốt | Transaction + guard hóa đơn/ngày đọc |
| G. Hóa đơn | Snapshot tiền tốt; thiếu snapshot nhận diện, xóa khoản phát sinh lỗi | Có rủi ro | High | In lại đổi nội dung, không xóa được hóa đơn hợp lệ | Bổ sung snapshot + unlink/revert có transaction |
| H. Thanh toán/Công nợ | Có bút toán phi tiền mặt; chưa khóa invoice | Có rủi ro | Critical | Thu vượt/lost update, lệch `SoTienDaThu` | `SELECT ... FOR UPDATE` + reconcile |
| I. Tiền cọc | Có ledger và transaction flow chính; ghi tay/race còn hở | Có rủi ro | Critical | Âm số dư thực, cấn nhầm hóa đơn | Khóa ledger, validate cùng hợp đồng |
| J. Chuyển phòng | Transaction toàn flow; thiếu guard phòng đích/khoản phát sinh/cuối tháng | Thiếu | High | Chồng phòng, bỏ khoản tiền, không chuyển cuối tháng | Recheck trong tx + tách flow cuối tháng |
| K. Trả/kết thúc | Flow trả phòng có transaction nhưng full-month no-invoice bị bỏ qua; `KetThuc` bypass | Thiếu | Critical | Mất tiền kỳ cuối/chỉ số/cọc | Một lifecycle service duy nhất |
| L. Báo cáo/vận hành | Có công nợ, nhắc nợ, kiểm tra dữ liệu; kỳ mặc định và due date chưa đúng | Có rủi ro | High | Chốt nhầm kỳ, báo quá hạn sai | Mặc định N-1 + dùng ngày đến hạn đã chốt |
| M. Database | Fresh schema pass; thiếu nhiều CHECK/unique logic và lock strategy | Thiếu | High | Dữ liệu xấu lọt qua request/race | Bổ sung constraint + update SQL apply-once |

### 0.3. Phát hiện chi tiết

#### REVIEW-001 - Trả cuối tháng có thể đóng hợp đồng mà không có hóa đơn kỳ cuối

- **Mức độ / loại:** Critical - bug xác nhận bằng code và service smoke.
- **Module:** K, G, F - `TraPhongService`.
- **Hiện trạng:** `canSinhHd = soNgayO < soNgayTrongThang && hdThangNay == null`; ngày cuối tháng luôn
  cho `canSinhHd=false`, kể cả chưa có hóa đơn. Service vẫn đổi hợp đồng `DaKetThuc`, phòng `Trong` và tất toán cọc.
- **Bằng chứng:** `Services/TraPhongService.cs`, `TinhPreviewAsync` dòng 41-62 và `ThucHienAsync` dòng 110-215.
  Smoke `CASE_RETURN_FULL_MONTH`: `CanCreate=False; Invoices=0; Contract=DaKetThuc; Room=Trong`.
- **Tái hiện:** hợp đồng ở trọn 07/2026, chưa lập hóa đơn, trả ngày 31/07/2026.
- **Hậu quả:** mất toàn bộ tiền phòng/dịch vụ kỳ cuối; thiếu chỉ số không còn chặn; cọc có thể hoàn sai.
- **Sửa nhỏ nhất:** nếu chưa có hóa đơn kỳ trả thì luôn preview/tạo hóa đơn kỳ cuối; chỉ bỏ tạo khi đã có hóa đơn hợp lệ.
- **Cần user quyết định:** Không.

#### REVIEW-002 - Nút Kết thúc/Hủy hợp đồng bỏ qua flow trả phòng

- **Mức độ / loại:** Critical - bug xác nhận bằng code.
- **Module:** C, K, A, H, I.
- **Hiện trạng:** `HopDongController.KetThuc/Huy` update trạng thái rồi gọi `PhongService.XuLyKetThucHopDongAsync`,
  không transaction và không kiểm tra hóa đơn, chỉ số, khoản phát sinh, nợ, cọc hay ngày kết thúc thực tế.
- **Bằng chứng:** `Controllers/HopDongController.cs`, action `KetThuc` dòng 173-184 và `Huy` dòng 186-197.
- **Tái hiện:** hợp đồng đang ở, còn thiếu chỉ số/nợ/cọc; POST `/HopDong/KetThuc/{id}`.
- **Hậu quả:** phòng về `Trong` trong khi nghĩa vụ tài chính chưa hoàn tất; luồng kiểm tra dữ liệu không còn thấy hợp đồng.
- **Sửa nhỏ nhất:** bỏ `KetThuc` trực tiếp cho hợp đồng đã bắt đầu; route qua lifecycle service. `Huy` chỉ dành cho hợp đồng
  chưa nhận phòng/chưa có dữ liệu nghiệp vụ và phải atomic với trạng thái phòng.
- **Cần user quyết định:** Có - chốt chính xác điều kiện “Hủy” khác “Trả phòng”.

#### REVIEW-003 - Lịch sử giá phòng của hợp đồng cũ ghi đè giá hợp đồng mới

- **Trạng thái 12/07/2026:** Đã xử lý ở phiên 51.
- **Mức độ / loại:** Critical - bug xác nhận bằng service smoke.
- **Module:** E, G, C.
- **Hiện trạng:** hóa đơn tra `LichSuThayDoiGia` bằng `LoaiDoiTuong='Phong'` + `PhongId`, fallback mới dùng
  `HopDong.TienThueThoaThuan`. Lịch sử cũ của phòng tiếp tục áp dụng cho hợp đồng mới.
- **Bằng chứng:** `Services/HoaDonService.cs` dòng 163; `ChuyenPhongService.LayGiaPhongAsync`;
  `TraPhongService.LayGiaPhongAsync`; `GiaService` cập nhật `Phong.GiaThueMacDinh`.
  Smoke: giá hợp đồng `3,000,000` nhưng preview lấy `2,500,000` từ lịch sử phòng.
- **Tái hiện:** tăng giá phòng ở hợp đồng A, kết thúc A, tạo hợp đồng B cùng phòng với giá thỏa thuận khác, lập hóa đơn B.
- **Hậu quả:** sai tiền phòng mọi kỳ của hợp đồng mới.
- **Sửa nhỏ nhất:** lịch sử thay đổi tiền thuê đang hiệu lực phải scope theo `HopDong`; `Phong.GiaThueMacDinh` chỉ là giá mặc định.
- **Kết quả:** ba flow hóa đơn/trả phòng/chuyển phòng tra giá theo `HopDongId`; UI đổi giá nằm ở chi tiết hợp đồng; `Phong.GiaThueMacDinh` không bị lịch sử hợp đồng cập nhật. Smoke A/B cùng phòng pass: lịch sử A `2,500,000`, giá thỏa thuận B `3,000,000`, preview và hóa đơn B đều `3,000,000`.
- **Cần user quyết định:** Không - đã chốt tại `DECISIONS.md`, Phase 1 ngày 12/07/2026.

#### REVIEW-004 - Thu tiền đồng thời có thể thu vượt hoặc ghi đè `SoTienDaThu`

- **Trạng thái 12/07/2026:** Đã xử lý ở phiên 52.
- **Mức độ / loại:** Critical - rủi ro code cần concurrency smoke sau fix.
- **Module:** H, M.
- **Hiện trạng:** `HoaDonService.ThuTienAsync` đọc hóa đơn trước transaction; trong transaction không `FOR UPDATE`, rồi
  tính giá trị tuyệt đối `SoTienDaThuMoi` từ snapshot cũ. `CongNoSettlementService` cũng không khóa các hóa đơn được phân bổ.
- **Bằng chứng:** `Services/HoaDonService.cs` dòng 284-316; `Services/CongNoSettlementService.cs` dòng 20-53 và 69-137.
- **Tái hiện:** hai request cùng thu phần còn lại của một hóa đơn.
- **Hậu quả:** có thể có hai dòng `ThanhToan` nhưng `SoTienDaThu` chỉ phản ánh một, hoặc tổng thu vượt nợ.
- **Sửa nhỏ nhất:** mở transaction trước, `SELECT ... FOR UPDATE`, tính từ `SUM(ThanhToan)`/giá trị khóa và update có điều kiện.
- **Cần user quyết định:** Không.
- **Kết quả:** `HoaDonService.ThuTienAsync` mở transaction trước khi đọc và khóa hóa đơn; `CongNoSettlementService` khóa các hóa đơn được phân bổ. Concurrency smoke hai request cùng thu toàn bộ: một thành công, một bị chặn; `ThanhToan.Rows=1`, tổng thanh toán và `HoaDon.SoTienDaThu` đều bằng `100`.

#### REVIEW-005 - Ledger cọc có race và cho cấn nhầm hóa đơn khác hợp đồng

- **Trạng thái 12/07/2026:** Đã xử lý ở phiên 52.
- **Mức độ / loại:** Critical - bug/rủi ro xác nhận bằng code.
- **Module:** I, H, M.
- **Hiện trạng:** `InsertDeltaAsync` đọc `SUM(SoTien)` không khóa; hai lần hoàn/trừ đồng thời có thể cùng pass.
  Khi ghi tay `TruNo` có `hoaDonId`, service không kiểm tra hóa đơn thuộc `hopDongId` của ledger.
- **Bằng chứng:** `Services/GiaoDichCocService.cs` dòng 45-70 và 281-307; `GiaoDichCocRepository.GetSoDuAsync`;
  `Views/GiaoDichCoc/Index.cshtml` cho nhập ID hóa đơn tự do.
- **Tái hiện:** hai request hoàn cùng số dư; hoặc ledger hợp đồng A nhập hóa đơn của hợp đồng B.
- **Hậu quả:** tổng delta thực âm dù từng snapshot không âm; cọc A trả nợ B; chuỗi `SoDuSauGiaoDich` sai.
- **Sửa nhỏ nhất:** khóa một row balance/contract, validate invoice cùng hợp đồng, giới hạn loại giao dịch theo trạng thái hợp đồng.
- **Kết quả:** mọi delta khóa dòng hợp đồng trước khi đọc số dư; giao dịch thủ công có guard loại/ngày/phương thức; `TruNo` kiểm tra hóa đơn cùng hợp đồng. Concurrency smoke hai request hoàn toàn bộ cọc: một thành công, một bị chặn, số dư `0`, không có snapshot âm; ca hóa đơn khác hợp đồng và `DieuChinh` thủ công đều bị chặn.
- **Cần user quyết định:** Không - đã chốt tại `DECISIONS.md`, Phase 1 ngày 12/07/2026.

#### REVIEW-006 - Chưa chống chồng thời gian hợp đồng và chưa có trạng thái hợp đồng tương lai

**Trạng thái: RESOLVED trong Phiên 53.** `HopDongService` và `ChuyenPhongService` khóa dòng `Phong` trong transaction rồi kiểm tra overlap toàn khoảng; hợp đồng tương lai dùng `ChoHieuLuc`, không chiếm trạng thái phòng trước ngày bắt đầu, và chỉ được kích hoạt đến hạn khi service xác nhận không có hợp đồng khác chồng kỳ. Schema có CHECK ngày/trạng thái và index lookup qua apply-once `20260712_contract_overlap_future_status.sql`.

- **Mức độ / loại:** High - bug xác nhận bằng code/schema.
- **Module:** C, A, M.
- **Hiện trạng:** tạo hợp đồng chỉ tìm `TrangThai='DangHieuLuc'`; mọi hợp đồng mới bị set ngay `DangHieuLuc` và phòng
  `DangThue`, không xét khoảng ngày. Schema không có constraint chống overlap.
- **Bằng chứng:** `HopDongService.TaoHopDongAsync` dòng 24-48; `HopDongRepository.GetDangHieuLucByPhongAsync`;
  bảng `HopDong` trong `Database/schema.sql` không có unique/generated active key hay CHECK ngày.
- **Tái hiện:** tạo hợp đồng bắt đầu tháng sau cho phòng đang trống; hoặc dữ liệu cũ có hợp đồng kết thúc nhưng trạng thái active.
- **Hậu quả:** phòng bị thuê sớm; không tạo được hợp đồng hiện tại; query theo ngày có thể trả một trong nhiều hợp đồng.
- **Sửa nhỏ nhất:** overlap query theo `[NgayBatDau, NgayKetThuc]`, trạng thái `ChoHieuLuc` hoặc derive theo ngày, khóa phòng khi tạo.
- **Cần user quyết định:** Có - chốt mô hình trạng thái cho hợp đồng tương lai.

#### REVIEW-007 - Sửa hợp đồng có thể sửa lịch sử và tạo dữ liệu không hợp lệ

**Trạng thái: RESOLVED trong Phiên 53 theo phạm vi hẹp.** Edit tải/khóa bản gốc trong transaction, bỏ qua hidden `PhongId`/`TrangThai`, giữ `TienThueThoaThuan`, validate khoảng ngày và khách đại diện. Khi đã có hóa đơn, chỉ số, ledger cọc, thanh toán hoặc khoản phát sinh, service chỉ cập nhật `GhiChu` và giữ nguyên các trường/danh sách khách có ảnh hưởng lịch sử. Lịch sử hiệu lực nhân khẩu chưa mở rộng và tiếp tục thuộc REVIEW-008.

- **Mức độ / loại:** High - bug xác nhận bằng code.
- **Module:** C, G, I.
- **Hiện trạng:** `SuaHopDongAsync` update ngày, giá, cọc, trạng thái và thay toàn bộ khách, không kiểm tra ngày kết thúc,
  overlap, hóa đơn/thanh toán/ledger đã có, hoặc khách đại diện thuộc danh sách. Hidden `PhongId/TrangThai` vẫn có thể tamper.
- **Bằng chứng:** `Services/HopDongService.cs` dòng 123-155; `HopDongRepository.UpdateAsync`;
  `Views/HopDong/Edit.cshtml` dòng 22-24. Model `HopDong` không có validation attribute.
- **Tái hiện:** sửa `NgayKetThuc < NgayBatDau`, đổi tiền cọc sau khi ledger đã ghi, gửi `khachChinhId` không nằm trong checkbox.
- **Hậu quả:** hóa đơn cũ không còn khớp hợp đồng; cọc thỏa thuận lệch ledger; không có/không đúng đại diện.
- **Sửa nhỏ nhất:** load bản gốc trong transaction, whitelist field, guard theo dữ liệu đã chốt, validate đại diện và ngày.
- **Cần user quyết định:** Có - chốt những field nào được phép sửa sau hóa đơn đầu tiên.

#### REVIEW-008 - Dịch vụ TheoNgười không có lịch sử nhân khẩu theo kỳ

**Trạng thái: RESOLVED trong Phiên 57.** `HopDongKhachThue` đã trở thành giai đoạn cư trú có ngày bắt đầu/dự kiến/kết thúc thực tế; tính `TheoNguoi` đếm khách duy nhất có mặt bất kỳ ngày nào trong kỳ. Service khóa hợp đồng để chặn giai đoạn/đại diện chồng nhau, chuyển/trả phòng đóng/mở lịch sử đúng ngày, hủy tương lai không xóa lịch sử. UI dùng tìm kiếm hồ sơ server-side giới hạn 20 kết quả và cho tái sử dụng khách cũ; CCCD trùng được chặn mềm ở service/UI, còn merge/unique DB cứng giữ cho REVIEW-014.

- **Mức độ / loại:** High - khoảng trống mô hình, xác nhận bằng service smoke.
- **Module:** C, D, G.
- **Hiện trạng:** số lượng lấy `COUNT(HopDongKhachThue)` hiện tại, không có `NgayBatDau/NgayKetThuc` thành viên.
- **Bằng chứng:** `FixedServiceQuantityCalculator.ResolveQuantityAsync`; bảng `HopDongKhachThue` trong schema.
  Smoke preview cùng kỳ đổi `SoLuong` từ 2 xuống 1 sau khi xóa một liên kết khách.
- **Tái hiện:** khách rời hợp đồng rồi lập bù hóa đơn kỳ trước chưa chốt.
- **Hậu quả:** sai dịch vụ theo người; không audit được ai ở kỳ nào. Hóa đơn đã chốt không đổi tiền nhưng liên kết lịch sử bị mất.
- **Sửa nhỏ nhất:** thêm hiệu lực ngày/kỳ cho thành viên và resolve count theo kỳ; cấm xóa cứng liên kết đã có kỳ chốt.
- **Cần user quyết định:** Có - tính theo đầu kỳ, cuối kỳ, số ngày thực ở hay thu đủ tháng nếu có mặt bất kỳ ngày nào.

#### REVIEW-009 - Lưu/sửa chỉ số chưa atomic và chưa bảo vệ dữ liệu đã dùng trên hóa đơn

**Trạng thái: RESOLVED trong Phiên 56 theo chính sách Phase 1.** `ChiSoService` lưu toàn batch trong một transaction, khóa bản gốc khi update, không cho đổi phòng/hợp đồng/dịch vụ/kỳ, validate `NgayDoc` thuộc đúng kỳ và hiệu lực hợp đồng. Update/delete bị chặn nếu `ChiTietHoaDon` đã tham chiếu chỉ số; muốn điều chỉnh phải xóa/reissue hóa đơn hợp lệ trước. Service smoke xác nhận lỗi dòng cuối rollback cả batch và các guard lịch sử hoạt động.

- **Mức độ / loại:** High - bug xác nhận bằng code.
- **Module:** F, G, M.
- **Hiện trạng:** `SaveChiSoItemsAsync` insert/update từng dòng không transaction; không kiểm tra `NgayDoc` thuộc kỳ/hợp đồng;
  update dòng đã được `ChiTietHoaDon` tham chiếu vẫn được phép.
- **Bằng chứng:** `Controllers/ChiSoController.cs` dòng 386-516; `ChiSoDienNuocRepository.UpdateAsync`.
- **Tái hiện:** bulk có dòng cuối vi phạm unique; các dòng trước đã lưu. Hoặc sửa `ChiSoCuoi` của chỉ số đã chốt.
- **Hậu quả:** bulk nửa vời; audit chỉ số không còn khớp snapshot số lượng trên hóa đơn.
- **Sửa nhỏ nhất:** transaction toàn batch, validate kỳ/ngày hợp đồng, chặn update/delete khi đã được hóa đơn dùng hoặc tạo correction record.
- **Cần user quyết định:** Có - có cho phép bút toán điều chỉnh chỉ số sau chốt hay chỉ hủy/reissue hóa đơn.

#### REVIEW-010 - Chuyển phòng thiếu guard phòng đích, khoản phát sinh và ca cuối tháng

**Trạng thái: RESOLVED trong Phiên 54.** Guard hợp đồng/phòng đích và overlap đã được đặt dưới khóa transaction từ REVIEW-006; phiên 54 bổ sung khoản phát sinh đến ngày chuyển vào `TongTienPhatSinh` của hóa đơn cũ và liên kết các dòng đó với hóa đơn cũ. Chuyển ngày cuối tháng tạo hợp đồng mới từ ngày đầu tháng sau nhưng không tạo hóa đơn 0 ngày/hóa đơn ghép cho phòng mới trong tháng cũ. Service/concurrency smoke xác nhận hai request cùng nhắm một phòng chỉ một request thành công và request thua rollback nguyên vẹn.

- **Mức độ / loại:** High - bug/luồng thiếu xác nhận bằng code.
- **Module:** J, A, C, G.
- **Hiện trạng:** service không recheck hợp đồng cũ còn active/phòng mới còn trống/không overlap trong transaction;
  không đưa `KhoanPhatSinhHopDong` vào hai hóa đơn; ngày cuối tháng bị throw thay vì có flow riêng.
- **Bằng chứng:** `Services/ChuyenPhongService.cs` dòng 22-50, 58-107, 109-198; không có query khoản phát sinh.
- **Tái hiện:** hai người cùng chọn một phòng trống; tạo khoản phát sinh trước chuyển; chuyển ngày cuối tháng.
- **Hậu quả:** hai hợp đồng active cùng phòng, bỏ sót khoản tiền, vận hành bị chặn ở ca hợp lệ.
- **Sửa nhỏ nhất:** recheck/lock hai phòng và hợp đồng trong tx, đưa khoản phát sinh vào quyết toán cũ, thêm nhánh cuối tháng.
- **Cần user quyết định:** Có - khoản phát sinh trước ngày chuyển thuộc hóa đơn phòng cũ hay hóa đơn gộp chung.

#### REVIEW-011 - Xóa hóa đơn có khoản phát sinh bị FK chặn và chưa có nghiệp vụ hoàn tác

**Trạng thái: RESOLVED trong Phiên 55.** `XoaHoaDonAsync` khóa hóa đơn `FOR UPDATE`, kiểm tra trực tiếp `ThanhToan`, `GiaoDichCoc` và `TienNoKyTruoc`, rồi mới hoàn tác khoản phát sinh `DaDuaVaoHoaDon` về `ChuaXuLy`, tháo liên kết ghép hai chiều, xóa chi tiết và hóa đơn trong cùng transaction. Khoản phát sinh đã ở trạng thái xử lý khác bị chặn. Concurrency smoke xóa đồng thời với thu tiền xác nhận chỉ một thao tác thành công và dữ liệu không lệch.

- **Mức độ / loại:** High - bug xác nhận bằng service smoke.
- **Module:** G, H.
- **Hiện trạng:** `XoaHoaDonAsync` chỉ xóa chi tiết rồi hóa đơn, không trả `KhoanPhatSinhHopDong` về `ChuaXuLy`, không xử lý
  `HoaDonGhepId`; FK `KhoanPhatSinhHopDong.HoaDonId` chặn delete.
- **Bằng chứng:** `HoaDonService.XoaHoaDonAsync` dòng 327-354; schema FK `FK_KhoanPhatSinh_HoaDon`.
  Smoke: `BlockedByFk=True; InvoiceStillExists=1`.
- **Tái hiện:** lập hóa đơn có khoản phát sinh, chưa thu, bấm xóa.
- **Hậu quả:** flow xóa hợp lệ báo lỗi DB; không thể chốt lại kỳ; hóa đơn ghép có thể để link treo.
- **Sửa nhỏ nhất:** trong transaction unlink/revert khoản phát sinh, unlink cặp ghép, kiểm tra mọi settlement liên quan rồi xóa.
- **Cần user quyết định:** Có - xóa hay dùng trạng thái `DaHuy` + hóa đơn thay thế để audit.

#### REVIEW-012 - Thay đổi/xóa lịch sử giá không chặn kỳ đã có hóa đơn

**Trạng thái: RESOLVED trong Phiên 58.** `GiaService` khóa dòng đối tượng và chuỗi lịch sử giá trong transaction, chặn thêm/xóa nếu đúng scope đã có hóa đơn từ kỳ áp dụng trở đi, sửa liên kết `GiaCu/GiaMoi` atomically và chỉ đồng bộ `PhongDichVu.DonGia` theo giá hiệu lực tháng hiện tại. UI resolve giá hiện tại theo kỳ, hiển thị lỗi guard, và `PhongDichVuRepository.GetByIdAsync` trả đủ tên phòng/dịch vụ.

- **Mức độ / loại:** High - bug xác nhận bằng code.
- **Module:** E, G.
- **Hiện trạng:** `GiaService.LuuThayDoiAsync/XoaThayDoiAsync` không query hóa đơn liên quan. Thay đổi tương lai còn cập nhật ngay
  `Phong.GiaThueMacDinh`/`PhongDichVu.DonGia`, làm giá “hiện tại” UI không còn là giá kỳ hiện tại.
- **Bằng chứng:** `Services/GiaService.cs` dòng 13-119 và 122-188.
- **Tái hiện:** đã có hóa đơn 07/2026, thêm/xóa lịch sử áp dụng 07/2026; hoặc nhập giá áp dụng 01/2027 từ 07/2026.
- **Hậu quả:** preview lập bù thay đổi; form hợp đồng/phòng gợi ý sai thời điểm; chuỗi `GiaCu/GiaMoi` khó audit.
- **Sửa nhỏ nhất:** guard hóa đơn từ kỳ áp dụng, resolve “giá hiện hành hôm nay” thay vì cập nhật base bằng giá tương lai.
- **Cần user quyết định:** Không sau khi scope giá thuê ở REVIEW-003 được chốt.

#### REVIEW-013 - Snapshot hóa đơn chưa đủ để bảo toàn nội dung lịch sử

**Trạng thái: RESOLVED trong Phiên 59.** Mọi đường lập hóa đơn thường/chuyển phòng/trả phòng dùng chung `HoaDonSnapshotService`; hóa đơn lưu nhận diện nhà/phòng/đại diện, chi tiết lưu tên-đơn vị dịch vụ, khoản phát sinh lưu mô tả-số tiền tại lúc liên kết. Phiếu thu HTML/Excel, chi tiết hóa đơn, danh sách và báo cáo công nợ đọc snapshot thay vì join ngược dữ liệu nhận diện hiện tại. Apply-once đã dry-run/apply trên DB vận hành và schema/migration/service/Excel smoke DB tạm đã pass.

- **Mức độ / loại:** High - khoảng trống dữ liệu xác nhận bằng schema/query.
- **Module:** G, L.
- **Hiện trạng:** hóa đơn snapshot tiền nhưng phiếu thu/report lấy tên phòng, nhà, khách, tên/đơn vị dịch vụ từ bảng hiện tại.
- **Bằng chứng:** `HoaDonController.XuatPhieuThu/InPhieuThu`; `ChiTietHoaDonRepository.GetByHoaDonAsync` join `DichVu` hiện tại;
  schema `HoaDon/ChiTietHoaDon` không có snapshot tên phòng/khách/dịch vụ/đơn vị.
- **Tái hiện:** đổi tên phòng/dịch vụ, sửa đại diện hoặc chuyển phòng rồi in lại hóa đơn cũ.
- **Hậu quả:** chứng từ cũ đổi nội dung dù số tiền giữ nguyên; khó đối chiếu pháp lý/vận hành.
- **Sửa nhỏ nhất:** snapshot mã/tên phòng, nhà, khách đại diện, tên/đơn vị dịch vụ và mô tả khoản phát sinh khi chốt.
- **Cần user quyết định:** Không - mức tối thiểu đã chốt tại `DECISIONS.md` và triển khai trong Phiên 59.

#### REVIEW-014 - Hồ sơ khách và ảnh CCCD thiếu chống trùng và quản lý vòng đời file

**Trạng thái: RESOLVED trong Phiên 60.** CCCD không rỗng được normalize bằng `TRIM`, chặn ở service/UI và unique generated column trong DB; SĐT chỉ cảnh báo có xác nhận. `TenantPhotoStorage` kiểm tra extension/kích thước/magic bytes/decode, tự sinh tên trong upload root và từ chối path traversal. `KhachThueService` giữ path từ DB, cleanup file mới khi DB lỗi, chỉ xóa ảnh cũ sau commit và chặn xóa hồ sơ đã có lịch sử cư trú. Apply-once đã dry-run sạch, áp trên DB vận hành và hậu kiểm đủ 1 generated column/1 unique constraint.

- **Mức độ / loại:** High - bug/rủi ro xác nhận bằng code; fake-image cần smoke bảo mật.
- **Module:** B.
- **Hiện trạng:** chỉ kiểm tra extension/kích thước, không kiểm tra magic bytes; file được lưu trước DB; thay ảnh/xóa khách không xóa
  file cũ; hidden path ảnh có thể bị sửa; không có cảnh báo CCCD/SĐT trùng; xóa khách đang gắn hợp đồng dựa vào FK và có thể trả 500.
- **Bằng chứng:** `KhachThueController.TryLuuAnhAsync/LuuAnhAsync/Delete`; `KhachThueRepository`; schema không unique CCCD/SĐT.
- **Tái hiện:** upload text đổi đuôi `.jpg`; DB insert lỗi sau lưu file; thay ảnh nhiều lần; tạo hai hồ sơ cùng CCCD.
- **Hậu quả:** hồ sơ trùng, file mồ côi, nội dung không phải ảnh trong static files, trải nghiệm lỗi khi xóa.
- **Sửa nhỏ nhất:** validate signature/decode ảnh, staging + commit/cleanup, giữ path server-side, duplicate warning và delete service.
- **Cần user quyết định:** Có - CCCD/SĐT là unique cứng hay chỉ cảnh báo cho hồ sơ cũ/không đủ thông tin.

#### REVIEW-015 - Trạng thái phòng có thể bị sửa tay và lệch hợp đồng

**Trạng thái: RESOLVED trong Phiên 61.** `Phong.TrangThai` được giữ làm snapshot/cache nhưng trạng thái thuê hiển thị và các guard hợp đồng đều suy từ hợp đồng theo ngày. Chỉ `DangSuaChua` được đặt tay; hợp đồng hiệu lực hoặc tương lai chặn sửa chữa. Edit phòng không tin `NhaId/TrangThai` từ request, khóa phòng trong transaction và chặn đổi Nhà sau khi có dữ liệu nghiệp vụ. Tạo/kích hoạt/hủy/chuyển/trả hợp đồng dùng chung `PhongLifecycleService` và khóa nhất quán; màn reconcile chỉ đọc báo lệch mà không sửa dữ liệu.

- **Mức độ / loại:** High - bug xác nhận bằng code/UI.
- **Module:** A, C.
- **Hiện trạng:** form sửa phòng cho chọn `Trong/DangThue/DangSuaChua`; repository ghi thẳng. Di chuyển `NhaId` cũng làm toàn bộ
  lịch sử phòng/hóa đơn được báo cáo dưới nhà mới.
- **Bằng chứng:** `Views/Phong/Edit.cshtml` dòng 57-63; `PhongRepository.UpdateAsync`; dashboard đếm trực tiếp `Phong.TrangThai`.
- **Tái hiện:** phòng còn hợp đồng active nhưng sửa thành `Trong`, sau đó chọn làm phòng chuyển đến.
- **Hậu quả:** dashboard sai, có thể tạo/chuyển thêm hợp đồng; báo cáo lịch sử đổi nhà.
- **Sửa nhỏ nhất:** trạng thái thuê derive từ hợp đồng/lifecycle; chỉ cho sửa trạng thái bảo trì khi không có hợp đồng; guard đổi nhà sau dữ liệu.
- **Cần user quyết định:** Có - có cho chuyển một phòng vật lý sang nhà khác hay phải tạo phòng mới.

#### REVIEW-016 - Schema thiếu constraint cho các invariant tài chính và thời gian

**Trạng thái: RESOLVED trong Phiên 62.** Dải năm nghiệp vụ được chốt `2000-2100` (không áp cho ngày sinh/ngày cấp CCCD); schema có 32 CHECK mới cho kỳ/ngày, tiền, công thức snapshot, trạng thái/loại và hình thức thanh toán. `ThanhToan.HinhThuc` là bắt buộc. Overlap hợp đồng và đại diện cư trú được chặn ở DB bằng bốn trigger khóa dòng cha `Phong/HopDong` trước khi kiểm tra khoảng. Apply-once báo chi tiết dry-run, dừng trước DDL nếu có vi phạm, không sửa dữ liệu và chạy lại an toàn.

- **Mức độ / loại:** High - thiếu xác nhận bằng schema.
- **Module:** M.
- **Hiện trạng:** chưa có CHECK cho ngày hợp đồng, tháng/năm hóa đơn/chỉ số, tiền không âm, trạng thái phòng/hợp đồng/hóa đơn,
  hình thức thanh toán, `ThanhToan.SoTien > 0`, `ThuChi`; chưa có DB guard hợp đồng overlap hoặc một đại diện duy nhất.
- **Bằng chứng:** `Database/schema.sql` các bảng `Phong`, `HopDong`, `HoaDon`, `ThanhToan`, `ThuChi`, `HopDongKhachThue`.
- **Tái hiện:** repository/script/import ghi trực tiếp giá trị âm, tháng 13, trạng thái lạ hoặc hai đại diện.
- **Hậu quả:** code path khác/race/import có thể tạo dữ liệu service không xử lý được.
- **Sửa nhỏ nhất:** Đã thực hiện trong `Database/schema.sql` và `Database/updates/20260714_financial_time_invariants.sql`; service dùng chung `BusinessDataLimits` để trả lỗi sớm cho kỳ/ngày chính.
- **Cần user quyết định:** Không - đã chốt dải năm `2000-2100` và trigger + khóa cha.

#### REVIEW-017 - Báo quá hạn bỏ qua `NgayThanhToanHangThang`

**Trạng thái: RESOLVED trong Phiên 63.** Hóa đơn kỳ N snapshot `HoaDon.NgayDenHan` theo `HopDong.NgayThanhToanHangThang` trong tháng N+1; tháng thiếu ngày dùng ngày cuối tháng. `HoaDonSnapshotService` là nguồn ghi chung cho lập hóa đơn thường, chuyển phòng và trả phòng. Công nợ/nhắc nợ/Excel đọc snapshot và tính `GREATEST(0, DATEDIFF(CURDATE(), NgayDenHan))`.

Ngày thanh toán hợp đồng được nhập/sửa trong khoảng `1-31`; được phép sửa sau khi đã có dữ liệu nghiệp vụ nhưng chỉ ảnh hưởng hóa đơn tạo sau lần sửa. Hóa đơn cũ giữ nguyên snapshot; hợp đồng mới khi chuyển phòng kế thừa ngày của hợp đồng cũ. Apply-once `20260715_invoice_due_date_snapshot.sql` có dry-run, blocker trước DDL, backfill riêng snapshot còn thiếu, CHECK ngày thuộc tháng N+1 và rerun-safe.

Dry-run DB vận hành có `HopDong=0`, `HoaDon=0`, không có dòng backfill. Migration đã áp, hậu kiểm `NgayDenHan DATE NOT NULL`, `CK_HoaDon_NgayDenHan=1`. Fresh schema/service/concurrency/Excel và migration blocker-rerun smoke pass; Browser QA công nợ/nhắc nợ với dữ liệu tạm pass, console sạch và DB tạm drop trong `finally`.

#### REVIEW-018 - Hóa đơn đã tồn tại khi trả giữa tháng chưa có chính sách điều chỉnh

**Trạng thái: RESOLVED trong Phiên 64.** Quyết định đã chốt là xóa/reissue nếu hóa đơn không khớp và còn đủ điều kiện xóa; phase hiện tại không dùng credit note.

`TraPhongService` dùng chung một invariant cho preview và execute. Không có hóa đơn kỳ trả thì sinh hóa đơn kỳ cuối; hóa đơn hiện hữu khớp thì không sinh trùng; hóa đơn không khớp bị chặn trước mọi thay đổi trạng thái hợp đồng, phòng, cọc hoặc công nợ. Execute khóa phòng, hợp đồng và hóa đơn kỳ trả, rồi tính/kiểm tra lại từ dữ liệu đã khóa để tránh TOCTOU. `HoaDonService` và flow trả phòng dùng chung chính sách xác định hóa đơn có thể xóa; hóa đơn kỳ trả đã được dùng để kết thúc hợp đồng không thể bị xóa ngược.

Hóa đơn đủ tháng bình thường có `SoNgayO/SoNgayTrongThang = NULL/NULL`; cặp này được phép khi trả đúng cuối tháng nhưng bị chặn khi trả giữa tháng. Cặp số đúng `N/N` cũng được chấp nhận cho dữ liệu cũ/flow trả phòng; metadata khác bị xem là không khớp. Màn `TraPhong/Confirm` hiển thị lý do và link hóa đơn, vô hiệu hóa nút xác nhận khi bị chặn.

Dry-run DB vận hành chỉ đọc có `HopDong=0`, `HoaDon=0`, không có dữ liệu liên quan và kết thúc bằng rollback. Không đổi schema. Fresh-schema service/concurrency smoke đủ các ca missing/full-month/prorata/mismatch/settlement/two-request pass; Browser QA trạng thái chặn và cho phép pass, console sạch; mọi DB tạm được drop trong `finally`.

#### REVIEW-019 - Chỉ số ngoài hợp đồng có thể phá chuỗi audit

**Trạng thái 15/07/2026: RESOLVED trong Phiên 65.** Chính sách đã chốt: mọi reset/thay/hỏng/quay vòng đồng hồ ngoài hợp đồng phải dùng `LoaiGhiNhan=Reset`, không cho gap chỉ số ngầm. Migration hẹp thêm bốn metadata reset vào `ChiSoNgoaiHopDong`; không sửa/backfill dữ liệu nghiệp vụ.

- **Mức độ / loại:** Medium - rủi ro xác nhận bằng code.
- **Module:** F.
- **Kết quả:** `MeterContinuityService` resolve chung mốc trước/sau từ cả `ChiSoDienNuoc` và `ChiSoNgoaiHopDong`. Create/update/delete đi qua service transaction, khóa dòng phòng, chặn trùng ngày, gap, đoạn chèn không nối dữ liệu sau, ngày ngoài hợp đồng sai phạm vi và xóa mốc đã có dependency.
- **Dữ liệu bất biến:** chỉ số hợp đồng đã được `ChiTietHoaDon` tham chiếu không được sửa/xóa; mọi mốc hợp đồng/ngoài hợp đồng đang có sự kiện phía sau cũng không được xóa. Tail ngoài hợp đồng chưa có dependency vẫn được xóa cứng; chưa cần soft-delete/correction tổng quát.
- **Reset:** `ChiSoNgoaiHopDong` dùng cùng công thức và metadata reset với chỉ số hợp đồng. Reset thiếu chỉ số trước/sau hoặc lý do bị chặn ở calculator/service và CHECK; `BinhThuong` không được mang metadata reset.
- **Migration/DB:** `20260715_review019_off_contract_meter_continuity.sql` dry-run và blocker trước DDL, rerun-safe. Dry-run DB vận hành có cả hai bảng chỉ số và các nhóm anomaly/dependency/invoice đều bằng 0; transaction đã rollback. Migration đã áp, hậu kiểm 4 cột reset và 3 CHECK.
- **Kiểm tra:** fresh-schema service smoke, migration add-path/blocker/rerun, reset formula, create/delete dependency và concurrency pass; DB tạm hậu kiểm còn 0. Browser QA màn `ChiSoNgoaiHopDong` pass cho trạng thái Bình thường/Reset, required metadata, layout, page identity và filter; không có exception overlay, console warning/error hoặc lỗi host.
- **Cần user quyết định:** Không - đã chốt chính sách continuity/reset ngày 15/07/2026.

#### REVIEW-020 - Màn vận hành mặc định sai kỳ thu tiền trả sau

**Trạng thái 16/07/2026: RESOLVED trong Phiên 66.** Các màn hóa đơn, chỉ số,
dashboard và sẵn sàng vận hành dùng chung `DefaultBillingPeriodResolver` để mặc định kỳ
N-1; tham số kỳ explicit vẫn được giữ nguyên. Browser QA xác nhận ngày 16/07/2026 mở
`HoaDon` và `KiemTraDuLieu` ở kỳ 6/2026.

- **Mức độ / loại:** Medium - UX có thể gây lỗi nghiệp vụ.
- **Module:** G, F, L.
- **Hiện trạng:** Hóa đơn, chỉ số, preview và dashboard mặc định `DateTime.Today.Month/Year`, trong khi tháng N+1 thu kỳ N.
- **Bằng chứng:** `HoaDonController`, `ChiSoController`, `HomeController`, `KiemTraDuLieuController`.
- **Tái hiện:** đầu tháng 8 mở preview và bấm chốt mặc định, hệ thống đưa kỳ 8 thay vì kỳ sử dụng 7.
- **Hậu quả:** chốt nhầm kỳ tương lai, thiếu chỉ số hoặc pro-rata sai kỳ.
- **Sửa nhỏ nhất:** helper `DefaultBillingPeriod = Today.AddMonths(-1)` cho màn thu/chốt; vẫn cho chọn kỳ khác rõ ràng.
- **Cần user quyết định:** Không.

#### REVIEW-021 - KiemTraDuLieu và báo cáo chưa reconcile dữ liệu denormalized/snapshot

**Trạng thái 16/07/2026: RESOLVED trong Phiên 66.** `KiemTraDuLieu` có khối reconcile
SELECT-only cho quyết toán kỳ cuối, tổng thanh toán, chuỗi ledger cọc, trạng thái phòng,
tổng/snapshot hóa đơn, liên kết chỉ số và khoản phát sinh. Kết quả chỉ cảnh báo và link tới
đối tượng; không có nút sửa hoặc ghi DB.

- **Mức độ / loại:** Medium - thiếu kiểm soát vận hành.
- **Module:** L, H, I.
- **Hiện trạng:** chỉ xét hợp đồng `DangHieuLuc`; không thấy hợp đồng vừa kết thúc còn thiếu hóa đơn. Chưa kiểm tra
  `HoaDon.SoTienDaThu = SUM(ThanhToan)`, `GiaoDichCoc.SoDuSauGiaoDich`, trạng thái phòng-hợp đồng, tổng chi tiết-snapshot.
- **Bằng chứng:** `KiemTraDuLieuRepository.GetRowsAsync`; `KiemTraDuLieuController`.
- **Tái hiện:** direct `KetThuc` hoặc sửa DB tạo lệch snapshot; màn vẫn không cảnh báo.
- **Hậu quả:** lỗi tài chính tồn tại âm thầm đến cuối kỳ.
- **Sửa nhỏ nhất:** thêm các query reconcile read-only và nhóm “hợp đồng kết thúc chưa quyết toán”.
- **Cần user quyết định:** Không.

#### REVIEW-022 - ThuChi cho phép dữ liệu âm/sai loại và sửa/xóa không audit

**Trạng thái 16/07/2026: RESOLVED; migration DB vận hành ĐÃ ÁP.**
Model/service/CHECK hiện hành chặn loại, tiền và ngày sai; `ThuChiKySo` cùng ba trigger
khóa INSERT/UPDATE/DELETE trực tiếp. Không có unlock UI. Điều chỉnh tháng đã khóa là dòng
mới ở tháng mở có `ThuChiGocId`; migration add-path/rerun/blocker và direct-SQL smoke pass.
DB vận hành đã dry-run sạch, backup/restore drill pass và apply qua migration runner số 11;
hậu kiểm bảng/cột/index/FK, ba CHECK và ba trigger đều đủ, không backfill giao dịch.

- **Mức độ / loại:** Medium - bug/rủi ro xác nhận bằng code/schema.
- **Module:** L, M.
- **Hiện trạng:** model không validation, schema không CHECK; controller tin `ModelState`; giao dịch đã ghi có thể sửa/xóa cứng.
- **Bằng chứng:** `ThuChiController`; `ThuChiRepository`; bảng `ThuChi` trong schema.
- **Tái hiện:** POST `LoaiGiaoDich=Khac`, `SoTien=-100000`, hoặc sửa giao dịch tháng đã đối chiếu.
- **Hậu quả:** báo cáo thu chi/cân đối sai và mất audit.
- **Sửa nhỏ nhất:** validate + CHECK, soft delete/correction hoặc nhật ký thay đổi.
- **Cần user quyết định:** Không - đã chốt khóa theo tháng, không unlock.

#### REVIEW-023 - Chưa có lớp bảo vệ vận hành nếu app được mở ngoài máy quản lý

**Trạng thái 16/07/2026: RESOLVED cho mô hình một admin trong LAN.** Fallback cookie auth
bảo vệ toàn bộ route nghiệp vụ; production yêu cầu password hash, HTTPS/HSTS và Secure
cookie. `/healthz` ẩn danh chỉ trả trạng thái tối thiểu. Ảnh CCCD lưu ngoài `wwwroot`, đọc
qua controller có auth/no-store. `DEPLOYMENT.md` ghi LAN HTTPS, backup/restore drill và
không port-forward. Browser QA pass login/logout/protected route; responsive overflow được
phát hiện và sửa bằng menu trượt ở viewport 390x844.

- **Mức độ / loại:** High nếu triển khai LAN/Internet; khoảng trống vận hành đã biết.
- **Module:** L, M, `Program.cs`.
- **Hiện trạng:** không đăng nhập/phân quyền; mọi route tài chính và upload đều truy cập được; chưa thấy health check, backup/restore drill,
  audit người thao tác hay policy HTTPS/HSTS trong production.
- **Bằng chứng:** `Program.cs`; `DECISIONS.md` xác nhận hiện chỉ một chủ nhà và chưa cần auth.
- **Tái hiện:** mở cổng app cho thiết bị khác trong LAN.
- **Hậu quả:** bất kỳ ai truy cập được có thể sửa/xóa dữ liệu tài chính và xem ảnh CCCD.
- **Sửa nhỏ nhất:** nếu chỉ localhost thì bind localhost; nếu LAN thì thêm auth tối thiểu, HTTPS, backup và audit actor trước go-live.
- **Cần user quyết định:** Không - đã chốt LAN HTTPS, không Internet.

#### REVIEW-024 - Migration hiện tại có guard, nhưng archive không được coi là idempotent cho baseline mới

**Trạng thái 16/07/2026: RESOLVED; journal DB vận hành ĐÃ BOOTSTRAP/APPLY.**
`migration-manifest.json` đánh số 1..11 và khóa SHA-256; runner cung cấp `status`, bootstrap
theo schema evidence prefix liên tục và `apply-next` chống lệch thứ tự/checksum. Fresh
schema có marker bao phủ 1..11. Smoke DB tạm chứng minh bootstrap 1..10 rồi chỉ apply số 11;
DB vận hành sau backup/restore drill đã bootstrap evidence 1..10 và apply-next số 11, journal
có 11 dòng liên tục; không file archive nào được thực thi.

- **Mức độ / loại:** Medium - lưu ý vận hành database.
- **Module:** M.
- **Hiện trạng:** `Database/updates/20260712_khach_thue_identity_vehicle.sql` có `INFORMATION_SCHEMA` guard. File
  `archive_pre_20260710/20260628_add_giao_dich_coc.sql` không idempotent; các file archive chỉ dành lịch sử và không chạy trên baseline.
- **Bằng chứng:** toàn bộ `Database/updates/` và `Database/updates/README.md`.
- **Tái hiện:** chạy lại toàn bộ archive trên DB tạo từ schema mới.
- **Hậu quả:** lỗi table/constraint exists và có thể dừng deployment giữa chừng.
- **Sửa nhỏ nhất:** deployment checklist chỉ chạy file trực tiếp trong `updates/` theo mốc đã ghi; thêm bảng migration journal khi có dữ liệu thật.
- **Cần user quyết định:** Không.

### 0.4. Câu hỏi nghiệp vụ cần chốt

1. `Huy` chỉ được dùng trước ngày bắt đầu và khi chưa có hóa đơn/chỉ số/cọc, hay còn trường hợp hủy sau khi đã nhận phòng?
2. Thay đổi giá thuê giữa hợp đồng có luôn thuộc riêng `HopDong`, còn `Phong.GiaThueMacDinh` chỉ là giá gợi ý cho hợp đồng mới?
3. Khách vào/ra giữa kỳ của dịch vụ `TheoNguoi` tính đủ tháng, theo số ngày, theo đầu kỳ hay cuối kỳ?
4. Hóa đơn đủ tháng đã chốt nhưng khách trả giữa tháng: giữ nguyên, hủy/reissue, hay tạo khoản điều chỉnh/credit?
5. Có cho trả dư/ứng trước không? Nếu có, số dư thuộc hợp đồng, chuỗi khách qua chuyển phòng hay khách thuê đại diện?
6. Ngày đến hạn chuẩn của hóa đơn kỳ N là ngày nào trong tháng N+1; xử lý ngày 29-31 thế nào?
7. Giao dịch cọc thủ công có cho backdate/điều chỉnh không; có cần lưu phương thức thu/hoàn và chứng từ không?
8. Hóa đơn cần snapshot tối thiểu những thông tin nhận diện nào: nhà, phòng, khách đại diện, CCCD, dịch vụ, đơn vị?
9. App sẽ chạy chỉ trên localhost, trong LAN hay có truy cập Internet?
10. `ThuChi` có khóa sổ theo tháng và dùng bút toán điều chỉnh thay cho sửa/xóa không?

### 0.5. Kế hoạch triển khai đề xuất

#### Phase 1 - Critical/High làm sai tiền hoặc bỏ quyết toán

- **Phạm vi file:** `TraPhongService`, `HopDongController/HopDongService`, `ChuyenPhongService`, `HoaDonService`,
  `GiaService/LichSuThayDoiGiaRepository`, `GiaoDichCocService`, `CongNoSettlementService`.
- **Schema:** có - scope lịch sử giá thuê theo hợp đồng, constraint ngày/trạng thái tối thiểu, chuẩn bị lock/balance nếu cần.
- **DB hiện tại:** cần file apply-once mới trong `Database/updates/`; đồng thời cập nhật `schema.sql`.
- **Smoke:** full-month return no invoice; direct end blocked; old/new contract price; concurrent double payment/deposit;
  transfer room conflict; delete/reissue invoice with charges.
- **Hoàn thành khi:** không còn đường làm hợp đồng non-active mà bỏ hóa đơn/chỉ số/cọc; tiền thuê mới không bị lịch sử cũ ghi đè;
  các request đồng thời không làm lệch tổng tiền.

#### Phase 2 - Toàn vẹn, transaction và lịch sử

- **Phạm vi file:** `HopDongService`, `ChiSoController/Repository`, `HopDongKhachThue`, `KhachThueController`,
  `Database/schema.sql`, các repository thanh toán/cọc.
- **Schema:** có - effective dates nhân khẩu, CHECK/index/unique logic, snapshot nhận diện hóa đơn.
- **DB hiện tại:** có update SQL và kế hoạch backfill rõ ràng; chưa có dữ liệu thật nên ưu tiên baseline sạch nếu user xác nhận reset.
- **Smoke:** overlap/future contract; edit after invoice; meter batch rollback; edit linked reading blocked; tenant-count historical;
  upload failure cleanup và duplicate identity.
- **Hoàn thành khi:** model/schema/service cùng enforce invariant; không còn batch nửa vời; dữ liệu chốt không bị sửa ngược.

#### Phase 3 - Luồng vận hành còn thiếu

- **Phạm vi file:** `ChuyenPhongService/View`, `TraPhongService/View`, `HoaDonService`, `KiemTraDuLieuRepository`,
  `KhoanPhatSinhHopDongRepository`.
- **Schema:** REVIEW-017 đã thêm snapshot `NgayDenHan`; REVIEW-018 không đổi schema; REVIEW-019 thêm hẹp metadata reset cho `ChiSoNgoaiHopDong`. Các mục Phase 3 còn lại chỉ đổi schema khi có quyết định riêng.
- **DB hiện tại:** cần update nếu thêm cột/bảng.
- **Smoke:** chuyển cuối tháng/nhiều lần; trả giữa tháng có hóa đơn; khoản phát sinh trước/sau trả; cọc thiếu;
  hợp đồng kết thúc chưa quyết toán phải hiện trên dashboard kiểm tra.
- **Hoàn thành khi:** mọi ca trả/chuyển có đường thao tác rõ, rollback toàn bộ và báo đúng số tiền còn phải xử lý.

#### Phase 4 - UX, báo cáo, hiệu năng và vận hành

**Cập nhật 16/07/2026:** REVIEW-020..024 đã hoàn tất trong repo và DB vận hành. Backup đã
restore-validate; journal bootstrap evidence 1..10 và migration khóa sổ số 11 đã apply/hậu kiểm.

- **Phạm vi file:** controllers/view mặc định kỳ, `HoaDonRepository.GetCongNoAsync`, `KiemTraDuLieu`, `ExcelService`,
  `ThuChi`, `Program.cs`/deployment docs.
- **Schema:** có thể thêm `NgayDenHan`, audit actor, lock-period; không bắt buộc cho tối ưu query thuần túy.
- **DB hiện tại:** update nếu thêm cột/index.
- **Smoke:** tháng 12/chuyển năm/năm nhuận; Excel numeric/date/text; dataset lớn; backup-restore; auth theo mô hình triển khai.
- **Hoàn thành khi:** mặc định kỳ N-1, báo quá hạn đúng, reconcile sạch, query đủ nhanh và deployment có backup/bảo vệ truy cập.

### 0.6. Bằng chứng kiểm tra phiên 49

```text
Git: main = origin/main = 1c1273b; worktree sạch trước review.
dotnet build --no-restore: pass 0 warning, 0 error trước khi restore harness.
Schema smoke DB tạm: 19 tables, 74 constraints, 7 seed services, PASS; DB tạm đã drop.
Business smoke DB tạm:
- Full-month return: đóng hợp đồng/phòng nhưng 0 hóa đơn.
- Contract price 3,000,000: preview lấy 2,500,000 từ lịch sử phòng cũ.
- TheoNguoi cùng kỳ: quantity đổi 2 -> 1 sau khi sửa liên kết khách.
- Xóa hóa đơn có khoản phát sinh: FK block, hóa đơn còn nguyên.
Database tạm đã drop; không đụng dữ liệu vận hành.
```

Giới hạn: chưa chạy concurrency smoke thực sự cho REVIEW-004/005 và chưa visual Browser QA; các điểm đó được phân loại
đúng là rủi ro code cần smoke sau fix, không trình bày như kết quả runtime đã xác nhận.

### 0.7. Tiến độ Phase 1 sau khi duyệt nghiệp vụ

- Nhóm REVIEW-001/002 đã được triển khai local: hóa đơn kỳ trả phòng chưa tồn tại luôn được tạo, direct `KetThuc` không còn đổi trạng thái, `Huy` có transaction và guard dữ liệu nghiệp vụ.
- Build và `git diff --check` pass. Service-level smoke database tạm pass: `FullMonthInvoice=1`, hợp đồng `DaKetThuc`, phòng `Trong`, hủy hợp lệ thành `DaHuy`, hủy khi có ledger cọc bị chặn. Database tạm đã drop trong `finally`.
- REVIEW-001/002 được xem là resolved theo phạm vi nhóm Phase 1 này; các guard overlap/trạng thái tương lai đầy đủ tiếp tục thuộc REVIEW-006.
- REVIEW-003 đã resolved ở phiên 51: build, schema smoke, apply-once migration smoke và service-level smoke đều pass; database tạm đã drop.
- REVIEW-004/005 đã resolved ở phiên 52: build, schema/apply-once smoke và concurrency service smoke đều pass; database tạm đã drop.
- Nhóm Critical/High REVIEW-001 đến REVIEW-011 đã xử lý xong theo phạm vi Phase 1; REVIEW-008 hoàn tất ở Phiên 57.
- REVIEW-006/007 resolved ở phiên 53, REVIEW-010 ở phiên 54, REVIEW-011 ở phiên 55 và REVIEW-009 ở phiên 56. Các nhóm đều có build và service/concurrency smoke database tạm pass, database tạm được drop trong `finally`.
- REVIEW-008 resolved ở phiên 57: dry-run database vận hành sạch (`HopDongKhachThue=0`, mọi nhóm bất thường bằng 0), apply-once đã chạy; schema/migration smoke và 8 ca service/concurrency/search pass, Browser QA tìm/chọn hồ sơ cũ pass không có console error.

### 0.8. Tiến độ Phase 2

- REVIEW-012 resolved ở phiên 58. Dry-run database vận hành: `FutureHistory=0`, `ServiceBaseMismatch=0`, `InvoicedHistory=0`, `BrokenChains=0`; không có dữ liệu cần sửa và không đổi schema nên không tạo apply-once.
- Schema/service smoke database tạm pass: giá tương lai không đổi giá hiện tại, resolver đúng kỳ, guard giá thuê/dịch vụ đã có hóa đơn, xóa mốc tương lai sửa chuỗi đúng, concurrent cùng kỳ cho kết quả `BLOCKED|OK`, lỗi giữa transaction rollback toàn bộ và preview kỳ cũ/hiện tại giữ nguyên. Database tạm đã drop trong `finally`.
- Browser QA màn `Gia/DichVu` pass: tiêu đề đủ phòng/dịch vụ, cảnh báo bất biến giá hiển thị, submit giá không đổi trả lỗi nghiệp vụ trên form, không có console warning/error.
- REVIEW-013 resolved ở phiên 59. Dry-run DB vận hành trước apply: `TotalInvoices=0` và mọi nhóm bất thường bằng `0`; apply-once thành công, hậu kiểm có đủ `SnapshotColumns=11`, `SnapshotConstraints=1`.
- Schema/backfill/rerun/service/Excel smoke pass; đổi tên nhà/phòng/khách/CCCD/dịch vụ/khoản phát sinh sau chốt không làm đổi snapshot. Đại diện kế tiếp không chồng được chấp nhận, đại diện đồng thời bị chặn; DB tạm drop trong `finally`.
- Browser QA `/HoaDon` sau migration render đúng, trạng thái chưa có hóa đơn hợp lệ và console không có warning/error. REVIEW-014/015/016 tiếp tục tách thành các batch riêng.
- REVIEW-014 resolved ở phiên 60. Dry-run DB vận hành: `TotalKhachThue=13`, không có CCCD rỗng/trùng, SĐT trùng, path ảnh trùng, ảnh thiếu hay file mồ côi; apply-once thành công và hậu kiểm `NormalizedColumns=1`, `UniqueConstraints=1`.
- Schema/migration rerun/blocker, upload security, transaction/file lifecycle, duplicate/concurrency và delete guard smoke đều pass; DB tạm drop trong `finally`. Browser QA Create/Edit/Delete trên DB tạm pass, console sạch. REVIEW-015/016 chưa làm.
- REVIEW-015 resolved ở phiên 61. Dry-run DB vận hành chỉ đọc có `TotalRooms=11`; tất cả nhóm phòng lệch `DangThue`, sửa chữa xung đột, nhiều hợp đồng đồng thời, phòng có lịch sử còn đổi được Nhà và trạng thái lạ đều bằng `0`; không có dữ liệu bị sửa.
- Build/schema/service/concurrency/reconcile smoke pass và mọi DB tạm được drop trong `finally`; không đổi schema nên không có apply-once. Browser QA Create/Edit/Details/Index/Reconcile và dashboard pass, console sạch. REVIEW-016 chưa làm.
- REVIEW-016 resolved ở phiên 62. Dry-run DB vận hành chỉ đọc có `Nha=1`, `Phong=11`, các bảng giao dịch/hợp đồng/hóa đơn liên quan bằng `0`; mọi nhóm vi phạm kỳ/ngày, tiền, công thức, trạng thái, overlap và đại diện đều bằng `0`, không sửa dữ liệu.
- Apply-once `20260714_financial_time_invariants.sql` đã áp dụng trên DB vận hành; hậu kiểm `Checks=32`, `Triggers=4`, `ThanhToan.HinhThuc IS_NULLABLE=NO`. Fresh schema, migration blocker/rerun, service overlap và race hợp đồng/đại diện đều pass; mọi DB tạm drop trong `finally`. Không có thay đổi UI nên Browser QA không áp dụng.
- REVIEW-017 resolved ở phiên 63. Dry-run DB vận hành chỉ đọc có `HopDong=0`, `HoaDon=0`, không có dữ liệu cần backfill; apply-once `20260715_invoice_due_date_snapshot.sql` đã áp và hậu kiểm `HoaDon.NgayDenHan DATE NOT NULL`, `CK_HoaDon_NgayDenHan=1`.
- Fresh schema/snapshot/service/concurrency/Excel và migration blocker-rerun smoke pass. Sửa ngày hợp đồng `31 -> 5` giữ hóa đơn cũ đến hạn `30/06/2026`, hóa đơn mới đến hạn `05/07/2026`; race hóa đơn cùng kỳ cho `OK|BLOCKED`. Browser QA công nợ/nhắc nợ kiểm tra cả quá hạn/chưa quá hạn và console sạch; mọi DB tạm drop trong `finally`.

## 1. Ket luan tong quan

Da xu ly trong phien 46:

- Tach `LichSuHinhThucDichVu` khoi lich su gia, resolve hinh thuc theo ky su dung.
- Chan sua truc tiep sau khi dich vu da gan phong; thay doi co ky ap dung va ly do.
- Them guard hoa don ky truoc/hoa don tu ky ap dung, guard chi so ky cuoi va chi so dau chuyen doi tung phong.
- Phu cac luong hoa don, nhap chi so, tra/chuyen phong va kiem tra du lieu; hoa don cu giu snapshot.

Da xu ly trong phien 48:

- Mo rong ho so khach thue voi ngay cap CCCD, nghe nghiep, loai xe va bien so xe.
- Form hop dong lay gia phong lam mac dinh cho gia thue va tien coc; danh sach hien ngay ket thuc.
- Xac nhan trang thai phong tu dong sang `DangThue` trong transaction tao hop dong.

Du an dang di dung huong cho muc tieu thay Excel:

- Da tach duoc cac nhom nghiep vu chinh: phong, khach thue, hop dong, chi so, hoa don, thanh toan, thu chi, chuyen phong, tra phong.
- Da co cac quy tac quan trong: thu tien tra sau, snapshot gia, snapshot no ky truoc, hoa don khong tron thang, hoa don ghep khi chuyen phong.
- Schema da co cac khoa ngoai va unique key co gia tri, dac biet `UQ_HoaDon` va `UQ_ChiSo`.

Tuy nhien, truoc khi dung thay Excel hoan toan, can uu tien sua cac diem co the gay sai tien that:

1. Chi so dien nuoc chua chan truong hop chi so moi nho hon chi so cu.
2. Coc va cong no chua co ledger nen co nguy co mat dau dong tien.
3. Lap/xoa hoa don can transaction de tranh du lieu nua voi.
4. Bao cao cong no co bug thang 12 khi tinh ngay qua han bang `hd.Thang + 1`.

Da xu ly trong phien 12:

- Chot quy uoc ngay vao/ngay ra/chuyen phong cho hoa don khong tron thang.
- Sua `HoaDonService`, `ChuyenPhongService`, `TraPhongService` dung chung cach tinh so ngay theo khoang ngay.
- Sua `TraPhongService` tinh dich vu `TheoChiSo` bang chi so thuc te, khong con `DonGia * 1`.

Da xu ly trong phien 13:

- Chan `ChiSoCuoi < ChiSoDau` o form nhap chi so, server-side controller, service tinh hoa don va `Database/schema.sql`.
- Them `ChiSoConsumptionCalculator` de cac service khong tao tien dich vu am neu DB da co du lieu xau.

Da xu ly trong phien 14:

- Thiet ke va trien khai `LoaiGhiNhan = Reset` cho dong ho reset/hong/thay/quay vong.
- Bo sung `ChiSoTruocReset`, `ChiSoSauReset`, `LyDoDieuChinh` vao `ChiSoDienNuoc`.
- Cap nhat cong thuc san luong trong `ChiSoConsumptionCalculator`, form nhap chi so, controller validate va `Database/schema.sql`.

Da xu ly trong phien 15:

- Boc transaction cho `HoaDonService.LapHoaDonAsync`: check trung ky, insert `HoaDon`, insert cac dong `ChiTietHoaDon`, commit/rollback cung nhau.
- Boc transaction cho xoa hoa don: check hoa don chua thu, xoa `ChiTietHoaDon`, xoa `HoaDon`, commit/rollback cung nhau.
- Boc transaction cho tao/sua hop dong: `HopDong`, `HopDongKhachThue`, cap nhat trang thai phong khi tao hop dong cung commit/rollback.
- Them guard khong cho tao hop dong moi neu phong da co hop dong `DangHieuLuc`.
- Sua bug tinh ngay qua han cong no thang 12 trong `HoaDonRepository.GetCongNoAsync`.

Da xu ly trong phien 16:

- Them UI quan ly `Nha`: `NhaController`, `Views/Nha/Index.cshtml`, menu sidebar.
- Sua form tao/sua `Phong` bat buoc chon `NhaId`, validate server-side khong cho `NhaId <= 0` hoac Nha khong ton tai.
- Khi chua co Nha, man tao Phong hien canh bao va link sang man quan ly Nha.
- `PhongRepository.GetAllAsync/GetByIdAsync` join `Nha` de hien thi ten Nha trong danh sach/chi tiet Phong.

Da xu ly trong phien 20:

- Them UI nhap chi so theo `PhongId` + ky, dung cho phong chua co hop dong moi trong flow chuyen phong.
- Man chuyen phong co link nhap chi so phong moi, tu dong gan thang/nam theo `NgayChuyenDi` va quay lai flow chuyen phong sau khi luu.
- Nhap chi so chi hien dich vu `TheoChiSo` dang gan cho phong qua `PhongDichVu`.

Da xu ly trong phien 21:

- Them ledger coc `GiaoDichCoc` de audit thu coc, thu them, hoan coc, tru no va dieu chinh/chuyen coc.
- Tao hop dong ghi `ThuCoc`; chuyen phong ghi `DieuChinh` am/duong de chuyen so du coc thuc te sang hop dong moi.
- Tra phong ghi `TruNo`/`HoanCoc` va cap nhat `HopDong.TienCocHoanLai`.
- Chong double-count cong no chuyen hop dong bang `ThanhToan.HinhThuc = KetChuyenNo`; tru no vao coc bang `ThanhToan.HinhThuc = TruCoc`.

Da xu ly trong phien 24:

- Man chi tiet hoa don hien thi rieng `KetChuyenNo` va `TruCoc` bang badge/canh bao de phan biet but toan cong no voi tien mat/chuyen khoan moi thu.

Da xu ly trong phien 25:

- Hoa don thuong va hoa don tra phong neu mang no cu vao `TienNoKyTruoc` se ket chuyen/tat toan cac hoa don cu bang `ThanhToan.HinhThuc = KetChuyenNo`, tranh double-count tren bao cao cong no.
- Khong cho xoa hoa don dang mang `TienNoKyTruoc > 0` bang flow xoa don gian, vi hoa don do dang giu khoan no da ket chuyen.

Da xu ly trong phien 27:

- `BaoCao/CongNo` da co filter Bootstrap toi thieu theo Nha, trang thai hop dong, nhom qua han va tu khoa.
- Xuat Excel cong no dung chung bo loc voi man hinh va co them cot Nha/Qua han.

Da xu ly trong phien 28:

- `HoaDon/Index` da co modal thu tien nhanh cho hoa don chua thu/thu mot phan.
- Thu nhanh dung lai `HoaDonService.ThuTienAsync`, co guard khong thu vuot so con lai va redirect ve dung ky dang xem.
- Danh sach hoa don hien ro `TienNoKyTruoc` va badge `KetChuyenNo`/`TruCoc` neu co but toan phi tien mat.

Da xu ly trong phien 29:

- `ChiSo/NhapHangLoat` da co bang nhap chi so hang loat theo ky cho phong dang thue co dich vu `TheoChiSo`.
- UI tinh san luong tai cho, canh bao dong sai chi so/reset, va POST dung lai validate server-side hien co.

Da nang tiep trong phien 34:

- Ky dau chua co du lieu chi so cu cho phep nhap `ChiSoDau` theo so hien co tren dong ho, khong ep ve 0.
- Khi da co ky truoc hoac dang sua dong da nhap, `ChiSoDau` van duoc khoa de giu chuoi audit.

Da xu ly trong phien 35:

- Them bang `ChiSoNgoaiHopDong` de audit dien/nuoc phat sinh khi phong trong, sua phong hoac chu nha su dung.
- Cac dong nay khong lien ket hoa don va khong tinh cho khach thue.
- `DenChiSo` moi nhat cua `ChiSoNgoaiHopDong` duoc dung lam moc `ChiSoDau` cho ky/hop dong sau neu moi hon chi so ky truoc.

Da xu ly trong phien 36:

- Doi thiet ke `ChiSoDienNuoc` sang co `HopDongId` nullable de tach nhieu doan chi so cung phong/dich vu/thang theo tung hop dong.
- `NgayDoc` duoc dung lam moc ban giao thuc te de noi khach cu -> chi so ngoai hop dong -> khach moi trong cung thang.
- `TraPhongService` va `ChuyenPhongService` khong con bo qua dich vu `TheoChiSo` khi thieu chi so; thieu chi so la loi chan.
- `ChiSoNgoaiHopDong` bi chan neu ngay ghi nhan dang nam trong mot hop dong cua phong.

Da nang tiep trong phien 38:

- Sau khi tra phong thanh cong, UI co link sang man `ChiSoNgoaiHopDong` va truyen san `phongId`.
- Man `ChiSoNgoaiHopDong` goi y `TuChiSo` tu moc chi so gan nhat cua phong/dich vu.
- Man nhap chi so don/theo phong/hang loat hien nguon moc `ChiSoDau` de biet dang noi tu ky truoc, chi so ngoai hop dong hay moc ban giao gan nhat.

Da xu ly trong phien 39:

- Them `DichVu.DonGiaMacDinh` de giam thao tac nhap lieu khi gan dich vu cho phong; gia tinh hoa don van nam o `PhongDichVu.DonGia`.
- Them `KhoanPhatSinhHopDong` cho cac khoan mot lan gan voi hop dong nhu den bu hu hong, mat chia khoa, phu thu/phat.
- `HoaDonService` dua khoan phat sinh chua xu ly vao hoa don ky phu hop va tach tong tien nay o `HoaDon.TongTienPhatSinh`.
- `TraPhongService` tinh khoan phat sinh chua xu ly vao tong no tra phong; coc tru no hoa don truoc, phan con lai moi tru khoan phat sinh chua vao hoa don.
- UI toi thieu da co man quan ly khoan phat sinh tu chi tiet hop dong, hien thi trong chi tiet hoa don, phieu thu HTML/Excel va preview chot hoa don.

Da xu ly trong phien 40:

- Them `DichVu.CachTinhCoDinh` de giu nguyen `LoaiTinhPhi = CoDinh` nhung tach cach tinh so luong `TheoPhong` hoac `TheoNguoi`.
- `HoaDonService`, `TraPhongService` va `ChuyenPhongService` tinh dich vu `CoDinh + TheoNguoi` bang so khach dang gan trong `HopDongKhachThue`.
- Neu hop dong chua gan khach ma co dich vu co dinh tinh theo nguoi, preview/chot hoa don bi chan de tranh tinh `SoLuong = 0`.
- REVIEW-008 da xu ly bien dong nhan khau theo ngay bang cac giai doan `HopDongKhachThue`; tinh `TheoNguoi` theo giao ky va khong xoa lich su khi khach roi.

Da xu ly trong phien 41:

- Them man `Phong/GanDichVuHangLoat` de gan/bat lai cung mot dich vu cho nhieu phong, giam thao tac sau khi cau hinh danh muc dich vu theo nguoi.
- Sau phien 45, man nay chi dat gia khi gan moi; phong da co dich vu giu nguyen don gia va doi gia qua luong lich su theo ky.
- Man nay canh bao hop dong hieu luc chua co khach khi dich vu tinh theo nguoi, va hien thanh tien du kien theo so khach x don gia de review truoc khi luu.

Da smoke test trong phien 42:

- Tao du lieu `TEST_FSB_20260707213224`, gan dich vu `CoDinh + TheoNguoi` cho 2 phong dang thue qua repository hang loat.
- Xac nhan `PhongDichVu.DonGia = 120000`, preview ky 07/2026 tinh phong co 2 khach thanh `SoLuong = 2`, `ThanhTien = 240000`.
- Xac nhan hop dong chua gan khach bi bao loi/chặn preview de tranh tinh `SoLuong = 0`.
- Da sinh file cleanup safe-mode-friendly: `Database/cleanup/TEST_FSB_20260707213224_cleanup.sql`.

Da xu ly trong phien 43:

- Them man `KiemTraDuLieu/Index` read-only de ra soat hop dong dang hieu luc truoc khi van hanh/chot hoa don.
- Man nay tong hop thieu khach, thieu dich vu, don gia bang 0, thieu chi so theo ky va trang thai da co hoa don.
- Man nay goi lai `HoaDonService.TinhHoaDonDuKienAsync`, khong tao cong thuc tinh tien rieng trong controller/view.
- Co link nhanh sang hop dong, gan dich vu hang loat, nhap chi so va preview chot hoa don.

Da xu ly trong phien 44:

- Chuan hoa nhan tieng Viet co dau trong cac view van hanh chinh de UI de doc hon khi dung that.
- Doi cac nhan/muc hien thi con lo ma noi bo nhu `ThuThemCoc`, `HoanCoc`, `TruNo`, `Trong`, `HD`, `Preview` sang nhan nguoi dung de hieu.
- Chuan hoa ky hieu tien te con sot tu `d` sang `đ` trong cac man preview, hoa don va cau hinh dich vu phong.
- Them helper hien thi trang thai khoan phat sinh va loai giao dich coc trong view, tranh in thang ma trang thai ra UI.

Da xu ly trong phien 45:

- Them `HopDongDichVu` de tach cau hinh dich vu cua phong khoi dich vu tung hop dong dang ky; hieu luc duoc luu theo ky su dung.
- Them `DichVu.BatBuocKhiThue`; du lieu mau dat Dien la bat buoc, Nuoc la tuy chon. Tao phong mac dinh chon tat ca dich vu, tao hop dong/chuyen phong mac dinh chon dich vu phong va cho bo dich vu tuy chon.
- `HoaDonService`, `TraPhongService`, `ChuyenPhongService`, `ChiSoController` va `KiemTraDuLieuRepository` da chuyen sang lay dich vu hop dong theo ky.
- Sua bo giai gia: ky truoc lan tang gia dau tien dung `GiaCu`; chan trung thay doi cung ky; ghi/xoa lich su gia va gia hien tai trong transaction.
- Sua xoa phong: phong chi co cau hinh duoc xoa cung `PhongDichVu`; phong da co hop dong/chi so/thu chi bi chan va hien thong bao nghiep vu.
- Chot baseline database moi tu `Database/schema.sql`; cac update cu duoc chuyen vao `Database/updates/archive_pre_20260710`.
- Smoke database sach tao du 17 bang; smoke service pass dich vu theo ky, hoa don chi lay dich vu hop dong, gia lich su va xoa phong an toan. HTTP smoke cac GET/POST chinh pass; Browser plugin van loi ha tang `failed to write kernel assets` nen chua co visual screenshot.

Da xu ly trong phien 30:

- `HoaDon/ChotHangLoat` da co man preview Bootstrap theo ky cho cac hop dong dang hieu luc.
- Preview hien tien phong, dich vu, no ky truoc, tong du kien va badge trang thai du lieu: da co hoa don, thieu chi so, thieu dich vu, co no ky truoc, san sang chot.
- POST chot hang loat recompute server-side va chi tao hoa don cho cac dong duoc chon va san sang.
- `HoaDonService.TinhHoaDonDuKienAsync` duoc tach de preview va lap hoa don dung chung logic tinh tien; thieu chi so `TheoChiSo` la loi chan chot hoa don.

Da nang tiep trong phien 33:

- Preview chot hoa don hang loat co filter theo Nha, tu khoa phong/khach/SĐT/ma hop dong va trang thai dong.
- Co the xem nhanh dong san sang chot, dong can kiem tra, thieu chi so, da co hoa don hoac thieu dich vu.
- Checkbox chon tat ca chi chon cac dong san sang dang hien thi theo bo loc.

Da xu ly trong phien 31:

- `HoaDon/InPhieuThu` da co phieu thu HTML toi thieu de in bang `window.print()`.
- Phieu thu dung snapshot hoa don, chi tiet dich vu, lich su thanh toan va hien canh bao rieng cho `KetChuyenNo`/`TruCoc`.
- Man chi tiet hoa don da co nut `In phieu thu` ben canh xuat Excel.

Da xu ly trong phien 32:

- `NhacNo/Index` da co man nhac no toi thieu cho chu nha/quan ly.
- Man nhac no dung du lieu cong no hien co, mac dinh tap trung khach dang o va hoa don da qua han.
- Chua sinh tin nhan mau, chua log da nhac, chua gui tu dong; cac buoc nay de giai doan sau.

---

## 2. Cac lo hong can sua ngay

### 2.0. Thieu UI quan ly Nha va chon Nha khi tao Phong - DA XU LY PHIEN 16

Phat hien cuoi phien 15:

- Schema co bang `Nha`, model `Nha` va `NhaRepository`.
- Chua co `NhaController`, chua co `Views/Nha`, sidebar chua co menu Nha.
- `PhongController.Create/Edit` co load `ViewBag.DanhSachNha`, nhung view `Views/Phong/Create.cshtml` va `Views/Phong/Edit.cshtml` chua hien select `NhaId`.
- Sau khi tao lai DB, bang `Nha` rong. Neu tao phong tu UI hien tai, `Phong.NhaId` co nguy co mac dinh 0 va loi FK vi khong co `Nha.Id = 0`.

Ket qua phien 16:

1. Da them module `NhaController` + `Views/Nha/Index.cshtml` de them/sua/xoa nha o muc du dung.
2. Da them menu Nha vao sidebar.
3. Da sua form tao/sua Phong de bat buoc chon `NhaId` tu danh sach nha.
4. Da them canh bao/link sang man Nha khi chua co Nha nao.
5. Da them validate server-side trong `PhongController` de khong tao/sua Phong voi `NhaId = 0` hoac Nha khong ton tai.

### 2.1. Chot quy uoc tinh ngay khong tron thang - DA XU LY PHIEN 12

Quy uoc da chot:

```text
HopDong.NgayBatDau la ngay dau tien tinh tien.
Ngay tra phong / NgayChuyenDi la ngay cuoi cung khach o phong cu.
Phong moi bat dau tinh tu NgayBatDauMoi = NgayChuyenDi + 1 ngay.
SoNgayO = giao giua ky thang [ngay 1, ngay 1 thang sau)
          va khoang o thuc te [ngay bat dau, ngay ket thuc + 1).
TienPhong = ROUND(GiaPhong / SoNgayTrongThang * SoNgayO).
```

Vi du:

```text
Thang 6/2026 co 30 ngay.
Khach vao 10/06, o het thang: SoNgayO = 21.
Tra phong 10/06, hop dong da o tu truoc: SoNgayO = 10.
Chuyen phong 10/06: phong cu 10 ngay, phong moi tu 11/06 = 20 ngay.
Chuyen phong 30/06: phong moi co 0 ngay trong ky 6/2026, khong sinh hoa don phong moi cho ky nay bang flow giua thang.
Thang 2/2028 co 29 ngay, khach vao 15/02: SoNgayO = 15.
```

Ket qua code:

- Them `BillingPeriodCalculator.CountOccupiedDays(...)`.
- `HoaDonService.LapHoaDonAsync` tu nhan ra thang dau/thang cuoi khong tron theo `HopDong.NgayBatDau`/`NgayKetThuc`.
- `ChuyenPhongService` tinh ngay phong cu/phong moi bang khoang ngay, khong lay truc tiep `.Day`.
- `TraPhongService` preview va execute cung dung khoang ngay, dong thoi tinh dung dich vu theo chi so.

### 2.2. Chan chi so moi nho hon chi so cu - DA XU LY PHIEN 13

Rui ro da xu ly:

```text
SoLuong = ChiSoCuoi - ChiSoDau
ThanhTien = SoLuong * DonGia
```

Neu `ChiSoCuoi < ChiSoDau`, hoa don co the bi am tien dich vu.

Giai phap da ap dung:

```sql
ALTER TABLE ChiSoDienNuoc
ADD CONSTRAINT CK_ChiSo_KhongAm CHECK (ChiSoCuoi >= ChiSoDau);
```

- `ChiSoController` validate `ChiSoCuoi >= ChiSoDau` truoc khi insert/update.
- `Views/ChiSo/Nhap.cshtml` dat `min` bang chi so dau cua ky.
- `HoaDonService`, `ChuyenPhongService`, `TraPhongService` dung `ChiSoConsumptionCalculator`.
- Neu gap du lieu cu bi am, service nem loi va khong sinh hoa don.

Thong bao nghiep vu:

```text
Chi so moi khong duoc nho hon chi so cu. Neu dong ho reset/hong, hay dung che do "dong ho reset".
```

### 2.3. Bo sung co che dong ho reset/hong/quay vong - DA XU LY PHIEN 14

Quyet dinh: gom cac tinh huong reset/hong/thay dong ho/quay vong vao mot loai nghiep vu `Reset` truoc.
Chua tach `HongDongHo` / `ThayDongHo` / `QuayVong` thanh enum rieng vi cong thuc tinh san luong giong nhau; ly do cu the luu o `LyDoDieuChinh`.

Cot da bo sung vao `ChiSoDienNuoc`:

```sql
ALTER TABLE ChiSoDienNuoc
ADD COLUMN LoaiGhiNhan VARCHAR(20) NOT NULL DEFAULT 'BinhThuong',
ADD COLUMN ChiSoTruocReset DECIMAL(10,2) NULL,
ADD COLUMN ChiSoSauReset DECIMAL(10,2) NULL,
ADD COLUMN LyDoDieuChinh VARCHAR(255) NULL;
```

Cong thuc da chot:

```text
BinhThuong: SoLuong = ChiSoCuoi - ChiSoDau
Reset:      SoLuong = (ChiSoTruocReset - ChiSoDau) + (ChiSoCuoi - ChiSoSauReset)
```

Quy tac validate:

- `BinhThuong`: `ChiSoCuoi >= ChiSoDau`.
- `Reset`: bat buoc co `ChiSoTruocReset`, `ChiSoTruocReset >= ChiSoDau`, `ChiSoCuoi >= ChiSoSauReset`; neu `ChiSoSauReset` bo trong thi tinh la 0.
- `Reset`: bat buoc co `LyDoDieuChinh` de audit.
- Cac gia tri chi so tren dong ho khong duoc am.

Vi du:

```text
Binh thuong: dau 120, cuoi 180 => 60.
Reset ve 0: dau 980, truoc reset 999, sau reset 0, cuoi 35 => 54.
Thay dong ho moi bat dau o 50: dau 980, truoc reset 1000, sau reset 50, cuoi 83 => 53.
Quay vong 9999 ve 0: dau 9980, truoc reset 9999, sau reset 0, cuoi 12 => 31.
Hong dong ho khong co so doc that: chua cho nhap san luong uoc tinh tu do; can chot chi so truoc reset chap nhan duoc hoac thiet ke loai UocTinh sau.
```

### 2.4. Sua flow tra phong giua thang - DA XU LY PHIEN 12

Quy uoc da chot va da sua:

- Dich vu `TheoChiSo`: lay `ChiSoDienNuoc`, tinh san luong theo chi so, luu `ChiSoDienNuocId`.
- Dich vu `CoDinh`: thu tron mot lan trong ky co hoa don, khong pro-rata theo ngay.
- Don gia dich vu tra lich su gia ap dung theo ky.
- Preview tra phong hien them tong dich vu thang cuoi de khop voi tong no du kien.

### 2.5. Them ledger cho coc - DA XU LY TOI THIEU PHIEN 21

`HopDong.TienCoc`, `TienCocHoanLai`, `DaXuLyChenhLechCoc` khong con la noi duy nhat theo doi coc. Phien 21 da them `GiaoDichCoc` lam ledger toi thieu.

Bang da them:

```sql
CREATE TABLE GiaoDichCoc (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    HopDongId INT NOT NULL,
    LoaiGiaoDich VARCHAR(30) NOT NULL,
    SoTien DECIMAL(12,0) NOT NULL,
    SoDuSauGiaoDich DECIMAL(12,0) NOT NULL DEFAULT 0,
    NgayGiaoDich DATE NOT NULL,
    HoaDonId INT NULL,
    GhiChu VARCHAR(255) NULL,
    NgayTao DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (HopDongId) REFERENCES HopDong(Id),
    FOREIGN KEY (HoaDonId) REFERENCES HoaDon(Id)
);
```

Loai giao dich de xuat:

```text
ThuCoc | ThuThemCoc | HoanCoc | TruNo | DieuChinh
```

Nguyen tac da chot: `SoTien` la delta co dau. `ThuCoc`/`ThuThemCoc` tang so du; `HoanCoc`/`TruNo` giam so du; `DieuChinh` dung cho chuyen coc/chinh lech. `SoDuSauGiaoDich` la snapshot audit nhanh.

### 2.6. Chong double count cong no da chuyen ky - DA XU LY PHIEN 25

`HoaDon.TienNoKyTruoc` la snapshot dung ve mat hien thi. Nhung khi bao cao tong no nhieu ky, can tranh tinh ca hoa don cu con no va hoa don moi da carry no cu.

Rui ro cu:

```sql
SUM(TongCong - SoTienDaThu)
```

co the double count neu ky moi da gom no ky truoc vao `TienNoKyTruoc`.

Huong xu ly da ap dung:

- Chua them `CongNoLedger` rieng.
- Khi no cu duoc dua vao `TienNoKyTruoc` cua hoa don moi, he thong ghi `ThanhToan.HinhThuc = KetChuyenNo` tren cac hoa don cu de chung khong con xuat hien trong bao cao cong no.
- Phien 25 mo rong quy tac nay cho ca hoa don lap thuong va hoa don tra phong sinh moi, khong chi rieng flow chuyen phong.
- Hoa don dang mang `TienNoKyTruoc > 0` khong duoc xoa bang flow xoa don gian de tranh mat khoan no da ket chuyen.
- Khi tra phong dung coc de tru no, he thong ghi `GiaoDichCoc.LoaiGiaoDich = TruNo` va dong thoi ghi `ThanhToan.HinhThuc = TruCoc` de hoa don duoc tat toan dung.
- Neu sau nay can audit cong no day du hon, co the them `CongNoLedger`, nhung phien 25 da xu ly rui ro double-count trong bao cao hien co.

---

## 3. Toi uu rieng cho Dapper/MySQL

### 3.1. Transaction cho lap hoa don - DA XU LY PHIEN 15

`HoaDonService.LapHoaDonAsync` da insert `HoaDon` va cac dong `ChiTietHoaDon` trong cung transaction.

Flow hien tai:

```text
Check trung ky
Insert HoaDon
Insert ChiTietHoaDon
Commit
```

`UQ_HoaDon (HopDongId, Thang, Nam)` da dung, nhung code van can bat loi duplicate key de hien thi thong bao than thien.

### 3.2. Transaction cho xoa hoa don - DA XU LY PHIEN 15

`HoaDonController.Delete` da goi `HoaDonService.XoaHoaDonAsync`. Service xoa chi tiet va hoa don trong cung transaction de tranh tinh huong xoa chi tiet thanh cong nhung xoa hoa don that bai.

### 3.3. Transaction cho tao/sua hop dong - DA XU LY PHIEN 15

`HopDongController.Create` da goi `HopDongService.TaoHopDongAsync` de gom:

1. Insert hop dong.
2. Insert danh sach khach.
3. Cap nhat trang thai phong.

Tat ca nam trong cung transaction de tranh hop dong da tao nhung khach/phong chua cap nhat xong.

`HopDongController.Edit` da goi `HopDongService.SuaHopDongAsync` de update hop dong, xoa lien ket khach cu, insert lien ket moi trong cung transaction.

### 3.4. Chong hai hop dong dang hieu luc cung phong - DA XU LY O SERVICE PHIEN 15

`HopDongService.TaoHopDongAsync` da check truoc khi tao hop dong:

```text
Neu phong da co HopDong.TrangThai = DangHieuLuc thi khong cho tao hop dong moi.
```

O DB MySQL, neu muon rang buoc cung, co the them generated column de unique active contract:

```sql
ALTER TABLE HopDong
ADD COLUMN ActivePhongId INT
    GENERATED ALWAYS AS (
        CASE WHEN TrangThai = 'DangHieuLuc' THEN PhongId ELSE NULL END
    ) STORED,
ADD UNIQUE KEY UQ_HopDong_ActivePhong (ActivePhongId);
```

### 3.5. Bo sung CHECK va index

CHECK nen co:

```sql
CHECK (Thang BETWEEN 1 AND 12)
CHECK (Nam BETWEEN 2000 AND 2100)
CHECK (TienPhong >= 0)
CHECK (TongCong >= 0)
CHECK (SoTien >= 0)
CHECK (SoNgayO IS NULL OR SoNgayO BETWEEN 0 AND 31)
CHECK (SoNgayTrongThang IS NULL OR SoNgayTrongThang BETWEEN 28 AND 31)
CHECK (LoaiTinhPhi IN ('CoDinh', 'TheoChiSo'))
CHECK (TrangThaiThanhToan IN ('ChuaThu', 'ThuMotPhan', 'DaThu'))
```

Index nen co:

```sql
CREATE INDEX IX_HopDong_Phong_TrangThai ON HopDong(PhongId, TrangThai);
CREATE INDEX IX_HoaDon_Ky ON HoaDon(Nam, Thang);
CREATE INDEX IX_HoaDon_TrangThai ON HoaDon(TrangThaiThanhToan);
CREATE INDEX IX_ThanhToan_HoaDon ON ThanhToan(HoaDonId);
CREATE INDEX IX_ChiSo_Phong_Ky ON ChiSoDienNuoc(PhongId, Nam, Thang);
```

### 3.6. Sua tinh ngay qua han thang 12 - DA XU LY PHIEN 15

Truoc day trong `HoaDonRepository.GetCongNoAsync`, cong thuc:

```sql
DATE(CONCAT(hd.Nam,'-',LPAD(hd.Thang+1,2,'0'),'-01'))
```

co the tao thang 13 neu `hd.Thang = 12`.

Da doi sang ham ngay cua MySQL:

```sql
DATE_ADD(STR_TO_DATE(CONCAT(hd.Nam, '-', LPAD(hd.Thang, 2, '0'), '-01'), '%Y-%m-%d'), INTERVAL 1 MONTH)
```

---

## 4. Luu y ClosedXML/Excel

Khi export:

- Ngay thang nen ghi bang `DateTime`, khong ghi string `dd/MM/yyyy`; format cell bang `dd/mm/yyyy`.
- So tien ghi dang numeric, format `#,##0`, khong noi chuoi kem `d`.
- CCCD/so dien thoai phai format text de khong mat so 0 dau.
- Ky hoa don nen tach `Thang`, `Nam`, khong suy dien tu `NgayLap`.
- Lich su thanh toan nen giu so tien dang numeric, khong ghi `"1,000,000 d"` dang text.

Khi import:

- Khong tin vao dinh dang hien thi cua Excel.
- Parse ngay/thang/nam ro rang.
- Validate so tien, chi so, CCCD, so dien thoai truoc khi ghi DB.
- Khong cho import de du lieu tai chinh da co thanh toan neu chua co man xac nhan thay doi.

---

## 5. Goi y UI/UX Bootstrap 5 cho van hanh

### 5.1. Nhap chi so hang loat - DA XU LY PHIEN 29

Mot bang theo ky:

```text
Phong | Dien cu | Dien moi | Tieu thu | Nuoc cu | Nuoc moi | Tieu thu | Ghi chu
```

Tinh tieu thu ngay khi nhap. Dong nao chi so moi nho hon chi so cu thi to do va hien lua chon "Dong ho reset".

Ket qua phien 29:

- Da them man `ChiSo/NhapHangLoat` theo ky.
- Bang hien moi dong theo phong + dich vu `TheoChiSo`.
- Co checkbox chon dong luu, chi so dau/cuoi, loai ghi nhan, thong tin reset va san luong tinh tai cho.
- Server-side van dung chung `ChiSoConsumptionCalculator` va validate reset hien co.

Ket qua phien 34:

- Neu chua co chi so ky truoc, man nhap don/theo phong/hang loat cho nhap `ChiSoDau`.
- Neu da co ky truoc, `ChiSoDau` tu noi tu chi so cuoi gan nhat va khong nhap tay.

Ket qua phien 35:

- Da co man `ChiSoNgoaiHopDong/Index` de nhap/xoa/filter cac moc chi so ngoai hop dong.
- `ChiSoController` uu tien moc ngoai hop dong moi hon ky truoc khi goi y `ChiSoDau`.

Ket qua phien 36:

- `ChiSoDienNuoc` co `HopDongId` de cung phong/dich vu/thang co the co nhieu dong theo hop dong khac nhau.
- Man nhap chi so don/theo phong/hang loat co `NgayDoc` de van hanh dung cac ca tra phong/nhan phong trong cung thang.
- Lap hoa don, tra phong va chuyen phong se dung chi so dung hop dong truoc, fallback moc theo phong neu can.

Ket qua phien 38:

- Sau tra phong co link nhanh sang ghi `ChiSoNgoaiHopDong` cho phong vua tra.
- `ChiSoNgoaiHopDong/Index` goi y `TuChiSo` tu chi so cuoi gan nhat.
- Cac man nhap chi so hien nguon `ChiSoDau`, giup chu nha nhin ro moc dau den tu dau truoc khi luu.

### 5.2. Preview chot hoa don hang loat - DA XU LY PHIEN 30

Ban Bootstrap toi thieu da co tai `HoaDon/ChotHangLoat`. Truoc khi chot, man hinh hien:

```text
Phong | Tien phong | Dien | Nuoc | Dich vu | No cu | Tong | Trang thai du lieu
```

Badge can co:

```text
Thieu chi so dien
Thieu chi so nuoc
Da co hoa don
San sang chot
```

Nut hanh dong:

```text
Chot cac dong da chon
Xem chi tiet
```

Ket qua phien 30:

- Dung `HoaDonService.TinhHoaDonDuKienAsync` de tinh tien du kien, khong nhan doi cong thuc tinh tien.
- Dong co hoa don ton tai hoac thieu chi so se bi khoa checkbox va khong duoc bulk create.
- Dong thieu dich vu hien canh bao de chu nha kiem tra, nhung van co the chot tien phong neu can.
- Khi POST, server recompute tung hop dong truoc khi goi `LapHoaDonAsync`.

Ket qua phien 33:

- Da them filter theo Nha, tim phong/khach/SĐT/ma hop dong va loc trang thai dong.
- Summary/footer tinh theo bo loc hien tai.
- Chon tat ca chi tick cac dong san sang dang hien thi theo bo loc.

### 5.3. Thu tien nhanh tren danh sach hoa don - DA XU LY PHIEN 28

Them thao tac ngay tai danh sach:

```text
So tien | Hinh thuc | Thu du
```

Nut "Thu du" tu dien so con lai.

Ket qua phien 28:

- Da them nut thu nhanh tren tung dong hoa don con no trong `HoaDon/Index`.
- Modal gom `So tien`, `Hinh thuc`, `Ghi chu`, nut `Thu du`.
- Server-side chan hinh thuc khong hop le va chan thu vuot so con lai.
- Sau khi thu, quay lai dung `thang/nam` dang xem.

### 5.4. Man xu ly coc khi chuyen/tra phong

Can co man rieng de ghi nhan:

```text
Coc cu | Coc moi | Chenh lech | Thu them/Hoan lai | Da xu ly
```

Neu tra phong:

```text
Tien coc | Tong no | Tru no vao coc | Hoan lai | Khach con no
```

---

## 6. Huong mo rong sau nay

Nen chuan bi som cho multi-user/multi-tenant:

```sql
CREATE TABLE NguoiDung (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    HoTen VARCHAR(255) NOT NULL,
    Email VARCHAR(255) NULL,
    SoDienThoai VARCHAR(20) NULL,
    MatKhauHash VARCHAR(500) NULL,
    TrangThai VARCHAR(20) NOT NULL DEFAULT 'HoatDong'
);

CREATE TABLE NguoiDungNha (
    NguoiDungId INT NOT NULL,
    NhaId INT NOT NULL,
    VaiTro VARCHAR(30) NOT NULL,
    PRIMARY KEY (NguoiDungId, NhaId)
);
```

Neu lam app cho khach thue xem hoa don:

- Khong cho truy cap truc tiep bang `HoaDonId`.
- Them `HoaDon.PublicToken` hoac tai khoan khach rieng.
- Tach quyen xem hoa don, thanh toan, thong tin hop dong.

---

## 7. Thu tu uu tien de thao luan/lam tiep

1. Ra UI in phieu thu sau van hanh neu can them logo/thong tin chu nha/mau phieu rieng.
2. Ra soat UI khoan phat sinh sau pilot: co can anh hien trang, danh muc tai san trong phong, hay bao cao rieng khong.
3. Nang UI ledger coc neu chu nha can doi soat coc thuong xuyen: filter theo hop dong/phong/loai giao dich, in/xuat so coc.
4. Nhac no giai doan 2/3: copy mau tin, log da nhac, Telegram/ZNS/SMS neu that su can.

---

## 8. Ke hoach nang cap UI bang Syncfusion

Chu truong da thong nhat:

- Hoan thien nghiep vu loi truoc, nang giao dien sau.
- Khong thay toan bo Bootstrap 5 ngay lap tuc.
- Dung Syncfusion co chon loc cho cac man hinh nhieu du lieu, can thao tac nhanh, gan voi muc tieu thay Excel.

License nen dung:

- Lay `Essential Studio UI Edition Binary License` truoc, vi phu hop voi ASP.NET Core MVC UI controls: Grid, DatePicker, NumericTextBox, Dialog, Toast, Chart.
- `Essential Studio Document SDK Developer Binary License` chi can khi muon thay/bo sung ClosedXML bang Syncfusion XlsIO, PDF, Word, PDF Viewer/Editor.

Nguyen tac tich hop:

- Khong hard-code license key vao repository.
- Dung bien moi truong, user-secrets, hoac cau hinh local khong commit.
- Dang ky license trong `Program.cs` bang `SyncfusionLicenseProvider.RegisterLicense(...)`.
- Giu layout Bootstrap hien tai trong giai doan dau, chi thay cac bang/form phuc tap.

Thu tu UI de lam sau khi nghiep vu loi on dinh:

1. Preview chot hoa don hang loat: da co Bootstrap toi thieu; chi doi sang Syncfusion Grid khi can sort/filter/inline thao tac nang cao hon.
2. `ChiSo/Index` va man nhap chi so hang loat: da co Bootstrap toi thieu; chi doi sang Syncfusion Grid khi can inline edit/sort/filter nang cao hon.
3. `HoaDon/Index`: da co thu nhanh Bootstrap toi thieu; chi doi sang Syncfusion Grid khi can sort/filter/export nang cao hon.
4. `BaoCao/CongNo`: da co filter Bootstrap toi thieu; chi doi sang Syncfusion Grid khi can sort/filter/export nang cao hon.
5. Dashboard: them Chart/KPI nhe neu can, sau khi cac man thao tac chinh da tot.

Khong lam truoc khi:

- Sua xong validate chi so va dong ho reset.
- Them transaction cho cac flow tai chinh chinh.
- Co quyet dinh ve ledger coc/cong no.
