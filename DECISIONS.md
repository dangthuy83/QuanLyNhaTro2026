# DECISIONS.md - Quyết Định Và Nguyên Tắc Ổn Định

## Dự án: Quản lý nhà trọ (ASP.NET Core MVC)

File này ghi các quyết định đã chốt. Mỗi phiên mới nên đọc file này trước khi sửa code. Nếu đảo quyết định đã chốt, ghi lý do vào `WORKLOG.md`.

---

## Mục Chưa Chốt

| # | Vấn đề | Ngữ cảnh | Ưu tiên |
|---|---|---|---|
| 1 | `Huy` chỉ được dùng trước ngày bắt đầu và khi chưa có hóa đơn/chỉ số/cọc, hay có trường hợp hủy sau khi đã nhận phòng? | Vòng đời hợp đồng; phân biệt Hủy và Trả phòng | Cao |
| 4 | Hóa đơn đủ tháng đã chốt nhưng khách trả giữa tháng: giữ nguyên, hủy/reissue hay tạo khoản điều chỉnh/credit? | Trả phòng và snapshot hóa đơn | Cao |
| 5 | Có cho khách trả dư/ứng trước không; nếu có số dư thuộc hợp đồng, chuỗi khách qua chuyển phòng hay hồ sơ khách? | Thanh toán, công nợ, chuyển phòng | Cao |
| 6 | Ngày đến hạn của hóa đơn kỳ N là ngày nào trong tháng N+1; nếu cấu hình ngày 29-31 nhưng tháng ngắn hơn thì xử lý thế nào? | Báo cáo công nợ và nhắc nợ | Cao |
| 7 | Giao dịch cọc thủ công có cho backdate/điều chỉnh không; có cần lưu phương thức thu/hoàn và chứng từ? | Ledger cọc và audit tiền thật | Cao |
| 8 | Hóa đơn cần snapshot tối thiểu thông tin nào: nhà, phòng, khách đại diện, CCCD, tên/đơn vị dịch vụ? | Bảo toàn chứng từ lịch sử | Cao |
| 9 | Hợp đồng tương lai dùng trạng thái `ChoHieuLuc` riêng hay trạng thái được suy ra từ ngày? | Chống chồng kỳ và trạng thái phòng | Cao |
| 10 | Có cho chuyển một phòng vật lý sang Nhà khác sau khi đã có dữ liệu, hay phải tạo phòng mới để giữ lịch sử? | Nhà-Phòng và báo cáo lịch sử | Trung bình |
| 11 | `ThuChi` có khóa sổ theo tháng và dùng bút toán điều chỉnh thay cho sửa/xóa không? | Audit thu chi | Trung bình |
| 12 | App sẽ chạy chỉ trên localhost, trong LAN hay có truy cập Internet? | Auth, HTTPS, backup và bảo vệ ảnh CCCD | Cao |
| 13 | Có gửi nhắc nợ tự động không, gửi cho chủ nhà bằng Telegram Bot hay gửi khách thuê qua ZNS/SMS? | Giai đoạn sau; hiện chỉ có màn nhắc nợ | Thấp |
| 14 | Có thêm index theo kết quả benchmark dữ liệu lớn không? | Tối ưu query | Thấp |

---

## Quyết định chốt cho Phase 1 ngày 12/07/2026

- `Hủy` chỉ dùng trước ngày bắt đầu và khi hợp đồng chưa có hóa đơn, chỉ số gắn hợp đồng, giao dịch cọc, thanh toán hoặc khoản phát sinh. Hợp đồng đã đến ngày bắt đầu phải đi qua flow `Trả phòng`; không có đường `Kết thúc` trực tiếp bỏ qua quyết toán.
- `HopDong.TienThueThoaThuan` là giá gốc riêng của hợp đồng. Thay đổi giá thuê giữa hợp đồng được lưu theo scope `HopDong`; `Phong.GiaThueMacDinh` chỉ là giá gợi ý cho hợp đồng mới và không mang lịch sử giá hợp đồng cũ sang hợp đồng mới.
  - Triển khai REVIEW-003 dùng `LichSuThayDoiGia.LoaiDoiTuong = 'HopDong'`, `DoiTuongId = HopDong.Id`. Sửa thông tin hợp đồng không đổi giá gốc; tăng/giảm giá giữa kỳ phải đi qua màn lịch sử giá thuê của hợp đồng.
- Nếu kỳ trả phòng chưa có hóa đơn thì luôn preview/tạo hóa đơn kỳ cuối, kể cả trả đúng ngày cuối tháng. Nếu đã có hóa đơn đủ tháng nhưng trả giữa tháng và số ngày không khớp, phải xóa/hủy rồi lập lại đúng trước khi trả phòng; Phase 1 chưa dùng credit note.
- Khi chuyển phòng, khoản phát sinh chưa xử lý có `NgayPhatSinh <= NgayChuyenDi` thuộc hóa đơn hợp đồng/phòng cũ; hóa đơn ghép chỉ là cách trình bày, không đổi scope dữ liệu. Nếu chuyển ngày cuối tháng, hợp đồng mới bắt đầu ngày đầu tháng sau, dịch vụ cố định tháng cũ tính trên hóa đơn phòng cũ và không tạo hóa đơn 0 ngày cho phòng mới trong tháng cũ; nợ cũ chưa settlement sẽ được cơ chế hóa đơn đầu tiên của hợp đồng mới mang sang.
- Hóa đơn chưa có thanh toán/settlement được phép xóa vật lý trong transaction; khoản phát sinh phải được trả về `ChuaXuLy` và liên kết hóa đơn ghép phải được tháo an toàn. Hóa đơn đã có thanh toán, `KetChuyenNo`, `TruCoc` hoặc đang mang `TienNoKyTruoc` không được xóa.
  - Triển khai REVIEW-011 khóa dòng `HoaDon` trước khi kiểm tra/xóa; kiểm tra trực tiếp mọi dòng `ThanhToan` và `GiaoDichCoc` thay vì chỉ tin số tổng hợp. Khoản phát sinh chỉ được hoàn tác nếu còn `DaDuaVaoHoaDon`; trạng thái đã xử lý khác làm thao tác xóa bị chặn và rollback.
- Phase 1 không cho thu vượt số còn phải trả của hóa đơn. Tiền ứng trước sau này phải có ledger/số dư riêng, không biểu diễn bằng `HoaDon.SoTienDaThu > TongCong`.
  - Triển khai REVIEW-004 khóa dòng `HoaDon` bằng `SELECT ... FOR UPDATE` trước mọi lần thu/phân bổ settlement; `ThanhToan` và số tổng hợp trên hóa đơn tiếp tục được ghi trong cùng transaction.
- Giao dịch cọc thủ công không được backdate trước ngày bắt đầu hoặc sau ngày hiện tại. `TruNo` phải tham chiếu hóa đơn cùng hợp đồng; `DieuChinh` chỉ do flow nội bộ tạo. Thu/hoàn thủ công lưu phương thức `TienMat`/`ChuyenKhoan`; mọi thay đổi số dư phải khóa theo hợp đồng và không làm số dư âm.
  - Triển khai REVIEW-005 dùng dòng `HopDong` làm khóa serialize ledger; UI thủ công chỉ cho `ThuThemCoc`, `HoanCoc`, `TruNo`, trong đó thu/hoàn bắt buộc phương thức và `TruNo` bắt buộc chọn hóa đơn còn nợ cùng hợp đồng.
- Dịch vụ `TheoNguoi`: người có mặt bất kỳ ngày nào trong kỳ được tính đủ một suất tháng; cần lưu hiệu lực thành viên và không xóa cứng lịch sử.
- Ngày đến hạn kỳ N lấy `HopDong.NgayThanhToanHangThang` trong tháng N+1; nếu tháng ngắn hơn thì dùng ngày cuối tháng.
- Snapshot hóa đơn tối thiểu gồm nhà, mã/tên phòng, khách đại diện, CCCD, tên/đơn vị dịch vụ và mô tả khoản phát sinh.
- Hợp đồng tương lai dùng trạng thái `ChoHieuLuc`; chỉ chuyển `DangHieuLuc` khi đến ngày bắt đầu và không chồng kỳ.
  - Triển khai REVIEW-006 khóa dòng `Phong` trong transaction trước khi tạo/chuyển/kích hoạt hợp đồng và kiểm tra giao nhau trên toàn bộ khoảng `[NgayBatDau, NgayKetThuc]`, không suy từ trạng thái phòng. Hợp đồng tương lai không đổi phòng sang `DangThue`; các màn hợp đồng, dashboard, phòng và kiểm tra dữ liệu kích hoạt hợp đồng đến hạn qua cùng service guard.
  - Triển khai REVIEW-007 không tin `PhongId`, `TrangThai` hoặc giá gốc từ request Edit. Bản gốc được khóa/tải lại trong transaction; hợp đồng đã có dữ liệu nghiệp vụ chỉ cho sửa `GhiChu`. Danh sách khách được giữ nguyên khi đã phát sinh dữ liệu; lịch sử hiệu lực nhân khẩu vẫn thuộc REVIEW-008.
- Trước khi có quyết định triển khai khác, ứng dụng chỉ chạy localhost; Phase 1 không mở rộng auth/multi-user.

Các dòng tương ứng trong bảng `Mục Chưa Chốt` phía trên được giữ lại làm dấu vết câu hỏi của phiên review; nội dung tại mục này là quyết định mới nhất và có hiệu lực.

---

## 1. Tổng Quan

| Mục | Nội dung |
|---|---|
| Mục đích | Thay Excel quản lý nhà trọ bằng ứng dụng nội bộ |
| Người dùng | Một chủ nhà/quản lý, chưa cần đăng nhập/phân quyền |
| Tech stack | .NET 8 MVC, Dapper, MySqlConnector, ClosedXML, Bootstrap 5, MySQL |
| Ngôn ngữ giao diện | Tiếng Việt |
| Database chuẩn | `Database/schema.sql` |
| GitHub repo | `https://github.com/dangthuy83/QuanLyNhaTro2026.git` |
| Quyết định mới nhất | Khi code và schema lệch nhau, lấy `Database/schema.sql` làm nguồn chuẩn, trừ khi có yêu cầu migration schema rõ ràng |
| Review nghiệp vụ/kỹ thuật | `PROJECT_REVIEW.md` là backlog phân tích rủi ro cần đọc trước khi sửa module hóa đơn, chỉ số, cọc, công nợ |

## 1.1. Quy Ước GitHub

- Remote chuẩn của dự án là `origin = https://github.com/dangthuy83/QuanLyNhaTro2026.git`.
- Sau khi hoàn tất một thay đổi có ý nghĩa và đã build/test xong, nên commit rồi push lên `origin/main`.
- Nếu chỉ đang thử nghiệm cục bộ, giữ thay đổi ở workspace cho tới khi chốt.

---

## 2. Kiến Trúc

```
QuanLyNhaTro/
├── Controllers/
├── Models/          # Entity khớp schema + ViewModel
├── Repositories/    # Dapper query, insert, update thuần dữ liệu
├── Services/        # Nghiệp vụ: hóa đơn, chuyển phòng, trả phòng...
├── Views/
├── Database/
│   └── schema.sql
└── wwwroot/
    └── uploads/     # Ảnh CCCD khách thuê
```

Nguyên tắc phân tầng:

- Controller nhận request, gọi service/repository, trả view; không đặt nghiệp vụ tính tiền ở controller.
- Service chứa nghiệp vụ, transaction và kiểm tra trạng thái.
- Repository chỉ thao tác dữ liệu; không tự quyết định nghiệp vụ.
- Model/entity phải khớp `Database/schema.sql`. Không thêm property/cột ảo vào SQL nếu schema không có.

---

## 3. Schema Hiện Hành

Schema có 19 bảng:

| Bảng | Vai trò |
|---|---|
| `Nha` | Nhà trọ |
| `Phong` | Phòng trọ thuộc một nhà |
| `KhachThue` | Hồ sơ khách thuê, độc lập với phòng |
| `DichVu` | Danh mục dịch vụ |
| `PhongDichVu` | Dịch vụ và đơn giá riêng theo phòng |
| `HopDong` | Hợp đồng thuê |
| `HopDongKhachThue` | Liên kết nhiều-nhiều hợp đồng và khách thuê |
| `HopDongDichVu` | Dịch vụ hợp đồng đăng ký, có hiệu lực theo kỳ sử dụng |
| `ChiSoDienNuoc` | Chỉ số theo hợp đồng/phòng, dịch vụ, tháng, năm |
| `ChiSoNgoaiHopDong` | Audit chỉ số phát sinh khi phòng trống/sửa phòng, không tính vào hóa đơn khách |
| `HoaDon` | Hóa đơn theo hợp đồng và kỳ |
| `ChiTietHoaDon` | Dòng dịch vụ của hóa đơn |
| `ThanhToan` | Lịch sử thu tiền |
| `GiaoDichCoc` | Ledger tiền cọc theo hợp đồng |
| `KhoanPhatSinhHopDong` | Khoản một lần gắn với hợp đồng như đền bù hư hỏng, mất chìa khóa, phụ thu |
| `ThuChi` | Thu chi ngoài tiền phòng |
| `LichSuThayDoiGia` | Lịch sử thay đổi giá |
| `LichSuHinhThucDichVu` | Lịch sử hình thức tính dịch vụ theo kỳ sử dụng |
| `ChiSoDauChuyenDoiDichVu` | Chỉ số đầu theo phòng khi chuyển từ cố định sang theo chỉ số |

Các cột dễ nhầm:

- `ChiSoDienNuoc` dùng `HopDongId` nullable để tách nhiều đoạn chỉ số cùng phòng/dịch vụ/tháng khi khách cũ trả phòng, phòng trống/sửa chữa phát sinh điện/nước, rồi khách mới vào trong cùng kỳ. `HopDongId IS NULL` chỉ dùng cho mốc theo phòng/tạm trước khi có hợp đồng hoặc fallback dữ liệu cũ.
- `ChiSoDienNuoc` dùng `LoaiGhiNhan`: `BinhThuong` yêu cầu `ChiSoCuoi >= ChiSoDau`; `Reset` dùng thêm `ChiSoTruocReset` và `ChiSoSauReset` để tính sản lượng khi đồng hồ reset/hỏng/thay/quay vòng.
- `ChiSoNgoaiHopDong` không liên kết `HoaDon`; bảng này chỉ ghi nhận sản lượng ngoài hợp đồng và mốc bàn giao công tơ.
- `DichVu` có `DonGiaMacDinh` chỉ để gợi ý khi gán dịch vụ cho phòng; hóa đơn vẫn tính theo `PhongDichVu.DonGia` hoặc lịch sử giá áp dụng.
- `DichVu.CachTinhCoDinh` chỉ áp dụng khi `LoaiTinhPhi = CoDinh`; giá trị hiện hỗ trợ `TheoPhong` và `TheoNguoi`.
- `DichVu.BatBuocKhiThue` xác định dịch vụ không được bỏ khỏi hợp đồng; dữ liệu mẫu đặt Điện là bắt buộc, Nước là tùy chọn.
- `HopDongDichVu.KyKetThuc` là mốc loại trừ; dịch vụ có hiệu lực khi `KyBatDau <= Ky < KyKetThuc`, hoặc không có `KyKetThuc`.
- `DichVu` không có `Ten`, `DonVi`, `HienThi`, `GhiChu`.
- `KhachThue` có `NgayCapCCCD`, `NgheNghiep`, `LoaiXe`, `BienSoXe`, `AnhCCCDMatTruoc` và `AnhCCCDMatSau`; không có một cột `AnhCCCD` chung.
- `PhongDichVu` có `DangApDung`; không có `ApDung` hoặc `NgayTao`.
- `HoaDon` không có `NgayTao`.
- `HopDongKhachThue` không có `NgayTao`.

---

## 4. Nguyên Tắc Nghiệp Vụ

### 4.1 Thu tiền trả sau

- Tháng N+1 thu tiền sử dụng của tháng N.
- Kỳ hóa đơn xác định bằng `HoaDon.Thang` và `HoaDon.Nam`.
- Không dùng `NgayLap` để suy ra kỳ sử dụng, vì ngày lập thường nằm ở tháng sau.

### 4.2 Snapshot giá

- Khi lập hóa đơn, giá phòng được ghi cứng vào `HoaDon.TienPhong`.
- Giá dịch vụ được ghi cứng vào `ChiTietHoaDon.DonGia`.
- Báo cáo cũ đọc từ `HoaDon` và `ChiTietHoaDon`, không join ngược về bảng giá hiện tại để tính lại.

### 4.3 Thay đổi giá theo kỳ

- Thay đổi giá áp dụng từ đầu kỳ `ThangApDung`/`NamApDung`.
- Các service tính tiền phải tra lịch sử giá bằng kỳ đang lập.
- `HoaDonService`, `ChuyenPhongService`, `TraPhongService` hiện đã tra giá áp dụng thay vì đọc thẳng giá hiện tại.
- Nếu kỳ hóa đơn nằm trước lần thay đổi giá đầu tiên, dùng `GiaCu` của lần thay đổi đầu tiên; không được rơi về giá hiện tại.
- Mỗi đối tượng chỉ có một thay đổi giá trong cùng kỳ; ghi/xóa lịch sử giá và cập nhật giá hiện tại phải cùng transaction.
- Không được thêm hoặc xóa mốc lịch sử giá nếu đã tồn tại hóa đơn của đúng scope từ kỳ áp dụng trở đi. Giá thuê kiểm tra theo `HopDong`; giá dịch vụ kiểm tra các hợp đồng đăng ký đúng `PhongDichVu` trong kỳ hóa đơn.
- Mọi thay đổi chuỗi giá phải khóa dòng đối tượng và các mốc lịch sử trong transaction. Hai thao tác đồng thời cùng kỳ chỉ một thao tác được thành công; lỗi ở bất kỳ bước nào phải rollback cả insert/delete, sửa `GiaCu` của mốc sau và đồng bộ giá hiện tại.
- Mốc giá tương lai không được cập nhật ngay `PhongDichVu.DonGia`. Trường này chỉ phản ánh giá hiệu lực của tháng hiện tại; màn hình và tính hóa đơn từng kỳ phải resolve từ lịch sử giá.
- `HopDong.TienThueThoaThuan` luôn là giá gốc của hợp đồng và không bị ghi ngược bởi lịch sử thay đổi giá.

### 4.4 Dịch vụ theo chỉ số

- Phân nhánh theo `DichVu.LoaiTinhPhi`, không hard-code theo tên dịch vụ.
- Nếu `LoaiTinhPhi == "TheoChiSo"`, sản lượng phải tính bằng `ChiSoConsumptionCalculator` từ `ChiSoDienNuoc`.
- Không dùng số lượng cố định từ `PhongDichVu`.
- `ChiTietHoaDon.ChiSoDienNuocId` phải được lưu khi dòng dịch vụ dùng chỉ số.
- Khi preview/chốt hóa đơn hàng loạt, thiếu chỉ số của dịch vụ `TheoChiSo` là lỗi chặn chốt hóa đơn để tránh bỏ sót tiền dịch vụ.
- Lưu nhiều chỉ số trong một lần nhập là một batch atomic: một dòng lỗi thì rollback toàn batch. `NgayDoc` bắt buộc thuộc đúng tháng/năm và, với chỉ số có `HopDongId`, phải nằm trong khoảng hiệu lực hợp đồng.
- Chỉ số đã được `ChiTietHoaDon.ChiSoDienNuocId` tham chiếu không được sửa/xóa trực tiếp. Phase 1 yêu cầu xóa/reissue hóa đơn hợp lệ trước; chưa triển khai correction record riêng.

### 4.4.1 Đơn giá mặc định của dịch vụ

- `DichVu.DonGiaMacDinh` là giá gợi ý khi tạo phòng/gán dịch vụ cho phòng.
- `PhongDichVu.DonGia` vẫn là đơn giá thực tế áp dụng cho phòng và là nguồn tính hóa đơn.
- Sửa `DonGiaMacDinh` không tự cập nhật các phòng đã gán dịch vụ.
- Nếu cần cập nhật giá hàng loạt cho các phòng cũ, phải làm thao tác riêng có xác nhận và ghi lịch sử giá.

### 4.4.2 Cách tính số lượng dịch vụ cố định

- Không đổi ý nghĩa `DichVu.LoaiTinhPhi`: chỉ gồm `CoDinh` và `TheoChiSo`.
- `DichVu.CachTinhCoDinh` xác định số lượng cho dịch vụ cố định:
  - `TheoPhong`: `SoLuong = 1`, phù hợp Internet hoặc khoản thu theo phòng/hợp đồng.
  - `TheoNguoi`: `SoLuong = COUNT(DISTINCT KhachThueId)` có giai đoạn cư trú giao với kỳ, phù hợp nước không dùng công tơ, vệ sinh, máy giặt, bảo trì nếu chủ nhà thu theo người.
- Nếu `LoaiTinhPhi = TheoChiSo`, `CachTinhCoDinh` không ảnh hưởng.
- Nếu dịch vụ `CoDinh + TheoNguoi` nhưng hợp đồng chưa gắn khách thuê, preview/chốt hóa đơn phải báo lỗi và không tạo hóa đơn để tránh tính `SoLuong = 0`.
- Từ REVIEW-008, số người được resolve theo các giai đoạn `HopDongKhachThue` giao với kỳ; người rời sau khi ở một ngày vẫn được tính đủ một suất, còn kỳ không giao thì không tính.

### 4.4.2.1 Lịch sử cư trú và dịch vụ TheoNgười

- `KhachThue` là hồ sơ cá nhân lâu dài. Khách rời rồi quay lại phải tái sử dụng hồ sơ cũ; không tự động merge hồ sơ trùng.
- `HopDongKhachThue` là từng giai đoạn cư trú, lưu `NgayBatDau` thực tế, `NgayKetThucDuKien`, `NgayKetThuc` thực tế nullable và `LaDaiDien`.
- Một khách có thể có nhiều giai đoạn không chồng nhau trong cùng hợp đồng. Không xóa cứng giai đoạn đã ghi.
- Dịch vụ `CoDinh + TheoNguoi` đếm `COUNT(DISTINCT KhachThueId)` có giai đoạn giao với bất kỳ ngày nào trong kỳ; mỗi khách được tính đủ một suất tháng. Chỉ dùng ngày thực tế, không dùng ngày dự kiến để tính tiền.
- Mỗi thời điểm tối đa một đại diện hiệu lực; đại diện phải thuộc một giai đoạn đang cư trú. Mọi thay đổi cư trú được serialize dưới khóa hợp đồng.
- Chuyển phòng đóng giai đoạn cũ vào ngày chuyển và mở giai đoạn mới từ hôm sau. Trả phòng đóng mọi giai đoạn đang mở vào ngày trả. Hủy hợp đồng tương lai giữ nguyên các dòng để audit.
- Tạo hợp đồng và thêm người ở tìm hồ sơ server-side theo tên/SĐT/CCCD/biển số, debounce và giới hạn 20 kết quả; không tải toàn bộ danh sách khách.
- CCCD trùng bị chặn ở service/UI và hướng người vận hành về hồ sơ cũ. Cleanup/merge và unique DB cứng tiếp tục thuộc REVIEW-014.

### 4.4.3 Dịch vụ đăng ký theo hợp đồng

- `PhongDichVu` là cấu hình dịch vụ có thể cung cấp và đơn giá theo phòng; `HopDongDichVu` là danh sách thực tế từng hợp đồng đăng ký.
- Khi tạo phòng, tất cả dịch vụ trong danh mục được chọn sẵn. Khi tạo hợp đồng hoặc chuyển phòng, tất cả dịch vụ đang áp dụng của phòng được chọn sẵn nhưng dịch vụ tùy chọn có thể bỏ.
- Dịch vụ có `BatBuocKhiThue = 1` phải được chọn nếu đang áp dụng tại phòng.
- Thêm/bỏ dịch vụ giữa hợp đồng áp dụng từ một kỳ sử dụng; không sửa lại hóa đơn đã lập.
- Hóa đơn, nhập chỉ số theo hợp đồng, trả phòng, chuyển phòng và dashboard kiểm tra dữ liệu phải lấy dịch vụ từ `HopDongDichVu` theo kỳ. Nhập chỉ số ngoài hợp đồng vẫn theo phòng và không tạo tiền hóa đơn.
- Tắt `PhongDichVu.DangApDung` chỉ ảnh hưởng cấu hình cho hợp đồng mới; không tự dừng dịch vụ của hợp đồng đã đăng ký.

### 4.4.4 Thay đổi hình thức dịch vụ theo kỳ

- Dịch vụ chưa từng có dòng `PhongDichVu` được sửa trực tiếp `LoaiTinhPhi`/`CachTinhCoDinh`.
- Dịch vụ đã gắn phòng chỉ được thay đổi qua `LichSuHinhThucDichVu`, với `KyApDung` là ngày đầu kỳ sử dụng và lý do bắt buộc.
- Kỳ trước lần thay đổi đầu tiên dùng snapshot `LoaiTinhPhiCu`/`CachTinhCoDinhCu`; từ kỳ áp dụng dùng bản ghi mới gần nhất. Không dùng `LichSuThayDoiGia` cho nghiệp vụ này.
- Chỉ ghi thay đổi khi mọi hợp đồng liên quan đã có hóa đơn kỳ liền trước và chưa có hóa đơn từ kỳ áp dụng trở đi.
- Chuyển `TheoChiSo -> CoDinh` còn yêu cầu hóa đơn kỳ cuối theo chỉ số có `ChiTietHoaDon.ChiSoDienNuocId`.
- Chuyển `CoDinh -> TheoChiSo` bắt buộc lưu `ChiSoDauChuyenDoiDichVu` cho toàn bộ phòng đã gắn dịch vụ; form nhập chỉ số kỳ chuyển đổi lấy đúng mốc này và không cho sửa tay.
- Hóa đơn đã chốt không tính lại: `ChiTietHoaDon` tiếp tục giữ snapshot số lượng, đơn giá, thành tiền và chỉ số đã dùng.
- Hóa đơn, chỉ số theo hợp đồng, chỉ số ngoài hợp đồng, trả/chuyển phòng và dashboard kiểm tra dữ liệu phải resolve hình thức bằng kỳ sử dụng/ngày ghi nhận.

### 4.5 Nợ kỳ trước

- `HoaDon.TienNoKyTruoc` là snapshot tại thời điểm lập hóa đơn.
- Không tính động lại nợ kỳ trước khi xem báo cáo cũ.
- Nếu hợp đồng mới sinh từ chuyển phòng, hóa đơn đầu tiên của hợp đồng mới có thể mang nợ còn lại từ hợp đồng cũ.
- Khi một hóa đơn mới mang nợ cũ dương vào `TienNoKyTruoc`, hệ thống phải ghi `ThanhToan.HinhThuc = KetChuyenNo` để tất toán các hóa đơn cũ tương ứng, tránh báo cáo công nợ double-count.
- Không xóa trực tiếp hóa đơn đang mang `TienNoKyTruoc > 0` bằng flow xóa đơn giản, vì hóa đơn đó đang giữ khoản nợ đã kết chuyển.

### 4.6 Thanh toán

Trạng thái hóa đơn:

```
SoTienDaThu == 0             -> ChuaThu
0 < SoTienDaThu < TongCong   -> ThuMotPhan
SoTienDaThu >= TongCong      -> DaThu
```

`ThanhToan` và cập nhật `HoaDon.SoTienDaThu` phải nằm trong cùng một transaction.

`ThanhToan.HinhThuc` có thể dùng thêm các hình thức phi tiền mặt để xử lý nghiệp vụ công nợ:

- `KetChuyenNo`: tất toán hóa đơn cũ khi nợ được snapshot sang hóa đơn/hợp đồng mới, tránh báo cáo công nợ bị double-count.
- `TruCoc`: tất toán hóa đơn bằng tiền cọc khi trả phòng.

Các dòng này không phải thu tiền mặt mới; chúng là bút toán xử lý công nợ.
Màn chi tiết hóa đơn phải hiển thị `KetChuyenNo` và `TruCoc` bằng nhãn riêng/cảnh báo riêng để tránh hiểu nhầm là khoản tiền mặt/chuyển khoản mới thu.

### 4.7 Ngày vào, ngày ra, ngày chuyển phòng

Quy ước đã chốt:

- `HopDong.NgayBatDau` là ngày đầu tiên tính tiền phòng.
- Khi tạo hợp đồng, giá thuê thỏa thuận và tiền cọc mặc định cùng lấy từ `Phong.GiaThueMacDinh`; người dùng được sửa trước khi lưu.
- Tạo hợp đồng thành công tự chuyển `Phong.TrangThai` sang `DangThue` trong cùng transaction; người dùng không chỉnh trạng thái phòng thủ công.
- `HopDong.NgayKetThuc` / ngày trả phòng là ngày cuối cùng khách sử dụng phòng cũ, vẫn tính tiền ngày đó.
- Khi chuyển phòng, `NgayChuyenDi` là ngày cuối ở phòng cũ; `NgayBatDauMoi = NgayChuyenDi + 1 ngày`.
- Số ngày ở trong một kỳ hóa đơn là phần giao giữa kỳ tháng `[ngày 1, ngày 1 tháng sau)` và khoảng ở thực tế `[ngày bắt đầu, ngày kết thúc + 1)`.
- Không tính số ngày bằng cách lấy thẳng `.Day` của ngày trả/chuyển nếu hợp đồng bắt đầu giữa tháng.
- Dịch vụ `CoDinh` thu trọn một lần trong kỳ có hóa đơn; không pro-rata theo ngày. Riêng chuyển phòng trong cùng tháng, hóa đơn phòng cũ bỏ `CoDinh`, hóa đơn phòng mới tính `CoDinh` để tránh thu hai lần.
- Dịch vụ `TheoChiSo` luôn tính theo `ChiSoDienNuoc` đúng hợp đồng/phòng/kỳ và lưu `ChiSoDienNuocId`.

Ví dụ:

- Tháng 6/2026 có 30 ngày, khách vào 10/06 và ở hết tháng: `SoNgayO = 21`.
- Trả phòng ngày 10/06, hợp đồng đã ở từ trước tháng 6: `SoNgayO = 10`.
- Chuyển phòng ngày 10/06: phòng cũ tính 10 ngày, phòng mới bắt đầu 11/06 và tính 20 ngày.
- Chuyển phòng ngày 30/06: phòng mới không phát sinh ngày ở trong kỳ 6/2026; không dùng flow sinh 2 hóa đơn giữa tháng cho kỳ này.
- Tháng 2/2028 có 29 ngày, khách vào 15/02: `SoNgayO = 15`.

### 4.8 Chỉ số điện/nước và reset đồng hồ

Quy tắc đã chốt:

- `LoaiGhiNhan = BinhThuong`: dùng khi đồng hồ chạy liên tục, không đổi/reset trong kỳ. Bắt buộc `ChiSoCuoi >= ChiSoDau`.
- Khi bắt đầu đưa dữ liệu vào hệ thống và phòng/dịch vụ chưa có chỉ số kỳ trước, `ChiSoDau` được nhập thủ công theo số hiện có trên đồng hồ; không mặc định nghiệp vụ là 0.
- Khi đã có chỉ số kỳ trước, `ChiSoDau` của kỳ mới tự lấy từ `ChiSoCuoi` gần nhất trước đó và không nhập tay để giữ chuỗi audit.
- Khi nhập chỉ số từ màn hợp đồng, dòng `ChiSoDienNuoc` phải gắn `HopDongId`; cùng phòng/dịch vụ/tháng có thể có nhiều dòng nếu thuộc các hợp đồng khác nhau.
- `NgayDoc` phải là ngày đọc/bàn giao thực tế, đặc biệt khi trả phòng hoặc khách mới vào cùng tháng. Đây là mốc để nối đúng chỉ số cuối khách cũ sang chỉ số đầu khách mới.
- Nếu phòng trống/sửa phòng/chủ nhà dùng điện nước sau khi khách cũ trả phòng, ghi vào `ChiSoNgoaiHopDong`; `DenChiSo` mới nhất của bảng này được dùng làm mốc `ChiSoDau` cho kỳ/hợp đồng sau nếu mới hơn chỉ số kỳ trước.
- Không ghi `ChiSoNgoaiHopDong` cho ngày đang thuộc một hợp đồng của phòng; phần tiêu thụ trong ngày khách còn hợp đồng phải nằm trong `ChiSoDienNuoc` của hợp đồng đó.
- Khi lập hóa đơn/trả phòng/chuyển phòng, thiếu chỉ số của dịch vụ `TheoChiSo` là lỗi chặn, không âm thầm bỏ qua dòng dịch vụ.
- `LoaiGhiNhan = Reset`: dùng chung cho đồng hồ reset về 0, hỏng phải thay, thay đồng hồ, hoặc quay vòng số. Chưa tách riêng `HongDongHo`/`ThayDongHo`/`QuayVong` vì công thức tính tiền giống nhau; phân biệt bằng `LyDoDieuChinh`.
- `ChiSoTruocReset`: chỉ số cuối cùng của đồng hồ cũ/trước khi quay về số thấp hơn.
- `ChiSoSauReset`: chỉ số bắt đầu sau reset/thay đồng hồ; mặc định nghiệp vụ là 0 nếu bỏ trống.
- Reset bắt buộc có `LyDoDieuChinh` để dễ audit.
- Các service lập hóa đơn phải dùng `ChiSoConsumptionCalculator`, không tự lấy `ChiSoCuoi - ChiSoDau`.

Công thức sản lượng:

```text
BinhThuong = ChiSoCuoi - ChiSoDau
Reset      = (ChiSoTruocReset - ChiSoDau) + (ChiSoCuoi - ChiSoSauReset)
```

Ví dụ:

- Bình thường: đầu 120, cuối 180 -> sản lượng 60.
- Reset về 0: đầu 980, trước reset 999, sau reset 0, cuối 35 -> sản lượng `(999 - 980) + (35 - 0) = 54`.
- Thay đồng hồ mới bắt đầu ở 50: đầu 980, trước reset 1000, sau reset 50, cuối 83 -> sản lượng `(1000 - 980) + (83 - 50) = 53`.
- Quay vòng công tơ 9999 về 0: đầu 9980, trước reset 9999, sau reset 0, cuối 12 -> sản lượng 31.
- Hỏng đồng hồ không đọc được số thật: hiện chưa cho nhập sản lượng ước tính tự do; cần chốt một chỉ số trước reset được chủ nhà chấp nhận hoặc sẽ thiết kế loại `UocTinh/DieuChinh` riêng sau.

### 4.9 Nhắc nợ

- Giai đoạn 1 chỉ nhắc chủ nhà/quản lý bằng màn danh sách hóa đơn còn nợ/quá hạn trong app.
- Chưa sinh nội dung tin nhắn mẫu, chưa ghi log đã nhắc, chưa gửi tự động cho khách thuê.
- Giai đoạn 2/3 như copy nội dung, ghi nhận đã nhắc, Telegram/ZNS/SMS sẽ cân nhắc sau.

### 4.10 Khoản phát sinh theo hợp đồng

- `KhoanPhatSinhHopDong` dùng cho khoản một lần gắn với hợp đồng: đền bù hư hỏng tài sản, mất chìa khóa/thẻ xe, phạt vi phạm, phụ thu khác.
- Không nhét các khoản này vào `ThuChi`, vì `ThuChi` là sổ thu/chi vận hành và không tự tạo công nợ khách thuê.
- Không tạo dịch vụ giả cho hư hỏng; `DichVu` chỉ dành cho khoản dịch vụ vận hành định kỳ hoặc theo chỉ số.
- Khi lập hóa đơn, các khoản `ChuaXuLy` có ngày phát sinh tới cuối kỳ được đưa vào `HoaDon.TongTienPhatSinh` và liên kết bằng `KhoanPhatSinhHopDong.HoaDonId`.
- Khi khoản đã đưa vào hóa đơn, công nợ/thu tiền đi theo hóa đơn để tránh double-count.
- Khi trả phòng mà còn khoản phát sinh chưa đưa vào hóa đơn, hệ thống cộng khoản đó vào tổng nợ xử lý cọc. Cọc trừ nợ hóa đơn trước, phần còn lại mới trừ các khoản phát sinh chưa vào hóa đơn.

---

## 5. Công Thức Module Chính

### Lập hóa đơn thường

```
TienPhong     = Gia phòng áp dụng theo kỳ
TongTienDV    = Sum(ChiTietHoaDon.ThanhTien)
TongTienPhatSinh = Sum(KhoanPhatSinhHopDong.SoTien chua xu ly duoc dua vao hoa don)
TheoChiSo     = ChiSoConsumptionCalculator.Calculate(ChiSoDienNuoc) * DonGia
CoDinh + TheoPhong = 1 * DonGia
CoDinh + TheoNguoi = COUNT(DISTINCT KhachThueId có giai đoạn giao kỳ) * DonGia
TienNoKyTruoc = snapshot nợ kỳ trước
TongCong      = TienPhong + TongTienDV + TongTienPhatSinh + TienNoKyTruoc
```

### Hóa đơn không trọn tháng

```
SoNgayTrongThang = số ngày thực của tháng
SoNgayO          = số ngày giao giữa kỳ hóa đơn và khoảng ở thực tế
TienPhong        = ROUND(GiaPhong / SoNgayTrongThang * SoNgayO)
```

### Chuyển phòng giữa tháng

Sinh 2 hóa đơn liên kết qua `HoaDonGhepId`:

- Hóa đơn phòng cũ: tính tiền phòng pro-rata, chỉ tính dịch vụ `TheoChiSo` theo `ChiSoDienNuoc`, bỏ qua dịch vụ `CoDinh`.
- Hóa đơn phòng mới: tính tiền phòng pro-rata, tính tất cả dịch vụ của phòng mới, đưa nợ còn lại của hợp đồng cũ vào `TienNoKyTruoc`.
- Chênh lệch cọc: `HopDongMoi.TienCoc - HopDongCu.TienCoc`, theo dõi bằng `DaXuLyChenhLechCoc`.
- Nếu `NgayChuyenDi` là ngày cuối tháng, phòng mới không có ngày ở trong kỳ cũ; không sinh hóa đơn phòng mới cho kỳ đó bằng flow chuyển phòng giữa tháng.

### Trả phòng

- Trả cuối tháng: không sinh hóa đơn mới.
- Trả giữa tháng hoặc kỳ không trọn tháng và chưa có hóa đơn tháng đó: sinh hóa đơn pro-rata.
- Trả giữa tháng hoặc kỳ không trọn tháng nhưng đã có hóa đơn tháng đó: không sinh hóa đơn mới.
- Hóa đơn trả phòng tính dịch vụ `TheoChiSo` theo chỉ số, dịch vụ `CoDinh` thu trọn một lần nếu hóa đơn tháng cuối được sinh.
- `TienHoanCoc = SoDuCocThucTe - TongNoConLai`; nếu âm thì khách còn nợ thêm.
- `TongNoConLai` khi trả phòng gồm nợ hóa đơn còn lại và khoản phát sinh chưa xử lý tới ngày trả phòng.

### Ledger cọc

`GiaoDichCoc` là ledger tiền cọc theo hợp đồng. `HopDong.TienCoc` là số cọc thỏa thuận, còn số cọc thực đang giữ phải xem từ tổng ledger.

Quy ước `GiaoDichCoc.SoTien` là delta có dấu:

- `ThuCoc`, `ThuThemCoc`: số dương, làm tăng số dư cọc.
- `HoanCoc`, `TruNo`: số âm, làm giảm số dư cọc.
- `DieuChinh`: số dương hoặc âm, dùng cho chuyển cọc giữa hợp đồng hoặc điều chỉnh chênh lệch.

`SoDuSauGiaoDich` là snapshot sau từng dòng để audit nhanh.

Khi tạo hợp đồng mới, hệ thống ghi `ThuCoc` nếu `TienCoc > 0`.
Khi chuyển phòng, hệ thống ghi `DieuChinh` âm ở hợp đồng cũ và `DieuChinh` dương ở hợp đồng mới để chuyển số dư cọc thực tế. Nếu số dư cọc nhận sang khác `HopDongMoi.TienCoc`, `DaXuLyChenhLechCoc = false` cho đến khi thu thêm/hoàn/điều chỉnh đủ.
UI xử lý chênh lệch nằm ở màn ledger cọc của hợp đồng mới. Hệ thống tự tính `HopDong.TienCoc - SoDuCoc`; nếu dương thì ghi `ThuThemCoc`, nếu âm thì ghi `HoanCoc`, sau đó cập nhật `DaXuLyChenhLechCoc`.
Khi trả phòng, hệ thống dùng cọc trừ nợ bằng ledger `TruNo` và `ThanhToan.HinhThuc = TruCoc`; phần còn lại nếu có được ghi `HoanCoc`.

---

## 6. Trạng Thái Triển Khai

### Đã hoàn thành

- REVIEW-006/007: chống chồng khoảng hợp đồng bằng khóa dòng phòng + overlap query, trạng thái `ChoHieuLuc`, kích hoạt đến hạn có guard, và khóa sửa lịch sử hợp đồng sau phát sinh dữ liệu.
- REVIEW-010: chuyển phòng khóa/recheck hợp đồng và hai phòng trong transaction, đưa khoản phát sinh đến ngày chuyển vào hóa đơn cũ, hỗ trợ chuyển cuối tháng không sinh hóa đơn phòng mới ở kỳ cũ.
- REVIEW-011: xóa hóa đơn chưa settlement dưới khóa transaction, trả khoản phát sinh về `ChuaXuLy`, tháo liên kết hóa đơn ghép và chặn mọi payment/ledger cọc/nợ kết chuyển.
- REVIEW-009: lưu chỉ số qua `ChiSoService` transaction toàn batch, khóa bản gốc/scope, validate ngày đọc và chặn sửa/xóa chỉ số đã dùng trên hóa đơn.

- Core MVC: phòng, khách thuê, hợp đồng, dịch vụ, chỉ số, hóa đơn.
- Dashboard.
- Thu chi.
- Xuất Excel phiếu thu, thu chi, công nợ.
- Báo cáo công nợ typed ViewModel.
- Chuyển phòng.
- Thay đổi giá.
- Trả phòng.
- Chốt quy ước ngày vào/ngày ra/chuyển phòng và áp dụng vào `HoaDonService`, `ChuyenPhongService`, `TraPhongService`.
- Chặn chỉ số điện/nước âm ở controller, service tính hóa đơn và schema chuẩn.
- Hỗ trợ `Reset` cho đồng hồ reset/hỏng/thay/quay vòng bằng `ChiSoTruocReset`, `ChiSoSauReset`, `LyDoDieuChinh`.
- Transaction cho lập/xóa hóa đơn và tạo/sửa hợp đồng.
- Dịch vụ được đăng ký riêng theo hợp đồng/kỳ qua `HopDongDichVu`; Điện mẫu là bắt buộc, Nước là tùy chọn.
- Tạo phòng mặc định chọn toàn bộ dịch vụ; sửa phòng bật/tắt cấu hình nhưng đổi giá phải qua lịch sử giá theo kỳ.
- Xóa phòng chỉ xóa cứng phòng chưa có dữ liệu nghiệp vụ; phòng đã có hợp đồng/chỉ số/thu chi bị chặn bằng thông báo nghiệp vụ.
- Baseline database ngày 10/07/2026 tạo trực tiếp từ `Database/schema.sql`; các update cũ nằm trong `Database/updates/archive_pre_20260710`.
- UI quan ly `Nha`; form tao/sua `Phong` bat buoc chon `NhaId` tu bang `Nha`.
- Da chay app voi MySQL that va smoke test flow Nha -> Phong -> Khach thue -> Hop dong -> Hoa don -> Thu tien.
- Da test voi MySQL that flow gan dich vu theo phong, nhap chi so binh thuong, nhap chi so reset, lap hoa don co dich vu va xac nhan `ChiTietHoaDon.ChiSoDienNuocId`.
- Da test voi MySQL that flow chuyen phong va tra phong giua thang co ca dich vu theo chi so va dich vu co dinh.
- UI nhap chi so ho tro nhap truc tiep theo `PhongId` + ky, de phong moi co the nhap chi so truoc khi thuc hien chuyen phong.
- UI nhap chi so hang loat theo ky cho cac phong dang thue co dich vu `TheoChiSo`.
- UI nhập chỉ số cho phép nhập `ChiSoDau` ở kỳ đầu chưa có dữ liệu cũ; các kỳ sau tự nối từ chỉ số cuối kỳ trước.
- UI ghi nhận chỉ số ngoài hợp đồng cho điện/nước phát sinh khi phòng trống/sửa phòng; các dòng này chỉ dùng làm audit và mốc bàn giao, không tính vào hóa đơn khách thuê.
- Chỉ số điện/nước hỗ trợ nhiều đoạn trong cùng phòng/dịch vụ/tháng bằng `ChiSoDienNuoc.HopDongId`; flow trả phòng/chuyển phòng chặn thiếu chỉ số thay vì bỏ qua dịch vụ theo chỉ số.
- UI vận hành chỉ số nhiều đoạn đã có link ghi chỉ số ngoài hợp đồng sau trả phòng, gợi ý `TuChiSo` từ mốc gần nhất và hiển thị nguồn mốc `ChiSoDau` khi nhập chỉ số.
- Dịch vụ có `DonGiaMacDinh` để tự điền khi gán dịch vụ cho phòng, nhưng hóa đơn vẫn dùng `PhongDichVu.DonGia`.
- Dịch vụ cố định có `CachTinhCoDinh`: `TheoPhong` tính 1 lần, `TheoNguoi` tính theo số khách gắn trong `HopDongKhachThue` và chặn chốt nếu hợp đồng chưa có khách.
- Màn `Phong/GanDichVuHangLoat` dùng để gán/bật lại cùng một dịch vụ cho nhiều phòng; nếu phòng đã có dịch vụ thì giữ nguyên đơn giá để tránh bỏ qua lịch sử giá theo kỳ.
- Màn `KiemTraDuLieu/Index` là dashboard read-only để rà dữ liệu trước vận hành/chốt hóa đơn: khách gắn hợp đồng, dịch vụ phòng, đơn giá, chỉ số theo kỳ và trạng thái preview.
- Màn kiểm tra dữ liệu không tự tính lại nghiệp vụ hóa đơn ở view/controller; trạng thái chốt phải dựa trên `HoaDonService.TinhHoaDonDuKienAsync`.
- UI khoản phát sinh theo hợp đồng cho phép ghi nhận đền bù hư hỏng/mất chìa khóa/phụ thu, đưa vào hóa đơn hoặc xử lý khi trả phòng/trừ cọc.
- Preview chốt hóa đơn hàng loạt theo kỳ: hiển thị hợp đồng đang hiệu lực, trạng thái dữ liệu, nợ kỳ trước, tổng dự kiến, hỗ trợ filter theo Nhà/từ khóa/trạng thái dòng và chỉ cho chốt các dòng sẵn sàng theo bộ lọc.
- Thêm ledger cọc `GiaoDichCoc`, ghi nhận thu cọc ban đầu, chuyển cọc khi chuyển phòng, trừ nợ/hoàn cọc khi trả phòng.
- Xử lý nợ chuyển kỳ/chuyển hợp đồng bằng dòng `ThanhToan` phi tiền mặt để tránh double-count công nợ.
- UI xử lý chênh lệch cọc khi chuyển phòng ngay trên màn ledger cọc.
- Thu tiền nhanh ngay trên danh sách hóa đơn, dùng lại `HoaDonService.ThuTienAsync`, chặn thu vượt số còn lại và quay về đúng kỳ đang xem.
- In phiếu thu HTML bằng `window.print()`, dùng dữ liệu snapshot hóa đơn và hiển thị riêng bút toán phi tiền mặt.
- Màn nhắc nợ tối thiểu cho chủ nhà/quản lý: lọc hóa đơn còn nợ/quá hạn, ưu tiên nợ quá hạn và đi nhanh tới hóa đơn/phiếu thu.
- Đồng bộ code theo `Database/schema.sql`.
- `dotnet build --no-restore` thành công với 0 warning, 0 error.

### Cần kiểm tra tiếp

- Test thủ công các flow nang cao con lai: xu ly coc/cong no va cac bien the thanh toan no.
- Rà lại `Database/schema.sql` vì file schema cũng đang có dấu hiệu hiển thị lỗi encoding trong terminal, dù tên bảng/cột vẫn đọc được.

### Tính năng còn thiếu

- Gửi/copy nhắc nợ nâng cao: mẫu tin nhắn, log đã nhắc, Telegram/ZNS/SMS.

---

## 7. Quy Ước Đặt Tên

| Loại | Quy ước | Ví dụ |
|---|---|---|
| Bảng DB | PascalCase, không dấu | `HopDong` |
| Cột DB | PascalCase, không dấu | `TienThueThoaThuan` |
| Model | Trùng tên bảng | `class HoaDon` |
| ViewModel | Hậu tố `ViewModel` | `TraPhongViewModel` |
| Repository | Hậu tố `Repository` | `HoaDonRepository` |
| Service | Hậu tố `Service` | `ChuyenPhongService` |
| Controller | Hậu tố `Controller` | `PhongController` |

---

## 8. Quyết Định Bị Đảo

| Quyết định cũ | Quyết định mới | Lý do | Phiên |
|---|---|---|---|
| `LichSuThayDoiGia` dùng `NgayApDung DATE` | Dùng `ThangApDung` và `NamApDung` | Mô hình trả sau dễ lệch tháng nếu dùng ngày | 1 |
| Khi code và schema lệch, có thể sửa theo code hiện có | Lấy `Database/schema.sql` làm chuẩn | Tránh schema trôi, giảm lỗi runtime SQL | 9 |

---

Cập nhật lần cuối: Phiên 58 - 13/07/2026. REVIEW-012 đã triển khai; dry-run giá database vận hành sạch, build, schema/service/concurrency/rollback smoke và Browser QA màn lịch sử giá đều pass. Không cần migration vì không đổi schema và không có dữ liệu giá lệch cần sửa.
