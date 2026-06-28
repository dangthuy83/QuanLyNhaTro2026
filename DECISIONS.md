# DECISIONS.md - Quyết Định Và Nguyên Tắc Ổn Định

## Dự án: Quản lý nhà trọ (ASP.NET Core MVC)

File này ghi các quyết định đã chốt. Mỗi phiên mới nên đọc file này trước khi sửa code. Nếu đảo quyết định đã chốt, ghi lý do vào `WORKLOG.md`.

---

## Mục Chưa Chốt

| # | Vấn đề | Ngữ cảnh | Ưu tiên |
|---|---|---|---|
| 1 | Khi trả phòng còn nợ, có cần cờ riêng đánh dấu "đã trừ nợ vào cọc" trong `HopDong` không, hay lưu vào `GhiChu` là đủ? | Trả phòng, hoàn cọc, công nợ | Trung bình |
| 2 | Có thêm index cho các cột hay dùng trong `WHERE`/`JOIN` không? Ví dụ `HopDong.PhongId`, `HoaDon.HopDongId`, `ThanhToan.HoaDonId`, `ChiSoDienNuoc.PhongId`. | Tối ưu khi dữ liệu lớn | Thấp |
| 3 | Thông báo nhắc nợ chỉ gửi cho chủ nhà bằng Telegram Bot, hay gửi thêm khách thuê qua ZNS/SMS? | Module thông báo | Thấp |
| 5 | Có cần in phiếu thu HTML bằng `window.print()` không, bên cạnh xuất Excel? | Thu tiền, hóa đơn | Trung bình |
| 6 | Cần thống nhất nhãn `LoaiDoiTuong` trong `LichSuThayDoiGia`: code hiện dùng `Phong` và `DichVu`, còn comment trong schema cũ từng mô tả theo `HopDong`/`PhongDichVu`. | Lịch sử giá | Trung bình |

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

Schema có 14 bảng:

| Bảng | Vai trò |
|---|---|
| `Nha` | Nhà trọ |
| `Phong` | Phòng trọ thuộc một nhà |
| `KhachThue` | Hồ sơ khách thuê, độc lập với phòng |
| `DichVu` | Danh mục dịch vụ |
| `PhongDichVu` | Dịch vụ và đơn giá riêng theo phòng |
| `HopDong` | Hợp đồng thuê |
| `HopDongKhachThue` | Liên kết nhiều-nhiều hợp đồng và khách thuê |
| `ChiSoDienNuoc` | Chỉ số theo phòng, dịch vụ, tháng, năm |
| `HoaDon` | Hóa đơn theo hợp đồng và kỳ |
| `ChiTietHoaDon` | Dòng dịch vụ của hóa đơn |
| `ThanhToan` | Lịch sử thu tiền |
| `GiaoDichCoc` | Ledger tiền cọc theo hợp đồng |
| `ThuChi` | Thu chi ngoài tiền phòng |
| `LichSuThayDoiGia` | Lịch sử thay đổi giá |

Các cột dễ nhầm:

- `ChiSoDienNuoc` dùng `PhongId`, không dùng `HopDongId`.
- `ChiSoDienNuoc` dùng `LoaiGhiNhan`: `BinhThuong` yêu cầu `ChiSoCuoi >= ChiSoDau`; `Reset` dùng thêm `ChiSoTruocReset` và `ChiSoSauReset` để tính sản lượng khi đồng hồ reset/hỏng/thay/quay vòng.
- `DichVu` chỉ có `TenDichVu`, `LoaiTinhPhi`, `DonViTinh`; không có `Ten`, `DonVi`, `HienThi`, `GhiChu`.
- `KhachThue` có `AnhCCCDMatTruoc` và `AnhCCCDMatSau`; không có một cột `AnhCCCD` chung.
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

### 4.4 Dịch vụ theo chỉ số

- Phân nhánh theo `DichVu.LoaiTinhPhi`, không hard-code theo tên dịch vụ.
- Nếu `LoaiTinhPhi == "TheoChiSo"`, sản lượng phải tính bằng `ChiSoConsumptionCalculator` từ `ChiSoDienNuoc`.
- Không dùng số lượng cố định từ `PhongDichVu`.
- `ChiTietHoaDon.ChiSoDienNuocId` phải được lưu khi dòng dịch vụ dùng chỉ số.

### 4.5 Nợ kỳ trước

- `HoaDon.TienNoKyTruoc` là snapshot tại thời điểm lập hóa đơn.
- Không tính động lại nợ kỳ trước khi xem báo cáo cũ.
- Nếu hợp đồng mới sinh từ chuyển phòng, hóa đơn đầu tiên của hợp đồng mới có thể mang nợ còn lại từ hợp đồng cũ.

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

### 4.7 Ngày vào, ngày ra, ngày chuyển phòng

Quy ước đã chốt:

- `HopDong.NgayBatDau` là ngày đầu tiên tính tiền phòng.
- `HopDong.NgayKetThuc` / ngày trả phòng là ngày cuối cùng khách sử dụng phòng cũ, vẫn tính tiền ngày đó.
- Khi chuyển phòng, `NgayChuyenDi` là ngày cuối ở phòng cũ; `NgayBatDauMoi = NgayChuyenDi + 1 ngày`.
- Số ngày ở trong một kỳ hóa đơn là phần giao giữa kỳ tháng `[ngày 1, ngày 1 tháng sau)` và khoảng ở thực tế `[ngày bắt đầu, ngày kết thúc + 1)`.
- Không tính số ngày bằng cách lấy thẳng `.Day` của ngày trả/chuyển nếu hợp đồng bắt đầu giữa tháng.
- Dịch vụ `CoDinh` thu trọn một lần trong kỳ có hóa đơn; không pro-rata theo ngày. Riêng chuyển phòng trong cùng tháng, hóa đơn phòng cũ bỏ `CoDinh`, hóa đơn phòng mới tính `CoDinh` để tránh thu hai lần.
- Dịch vụ `TheoChiSo` luôn tính theo `ChiSoDienNuoc` của phòng/kỳ và lưu `ChiSoDienNuocId`.

Ví dụ:

- Tháng 6/2026 có 30 ngày, khách vào 10/06 và ở hết tháng: `SoNgayO = 21`.
- Trả phòng ngày 10/06, hợp đồng đã ở từ trước tháng 6: `SoNgayO = 10`.
- Chuyển phòng ngày 10/06: phòng cũ tính 10 ngày, phòng mới bắt đầu 11/06 và tính 20 ngày.
- Chuyển phòng ngày 30/06: phòng mới không phát sinh ngày ở trong kỳ 6/2026; không dùng flow sinh 2 hóa đơn giữa tháng cho kỳ này.
- Tháng 2/2028 có 29 ngày, khách vào 15/02: `SoNgayO = 15`.

### 4.8 Chỉ số điện/nước và reset đồng hồ

Quy tắc đã chốt:

- `LoaiGhiNhan = BinhThuong`: dùng khi đồng hồ chạy liên tục, không đổi/reset trong kỳ. Bắt buộc `ChiSoCuoi >= ChiSoDau`.
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

---

## 5. Công Thức Module Chính

### Lập hóa đơn thường

```
TienPhong     = Gia phòng áp dụng theo kỳ
TongTienDV    = Sum(ChiTietHoaDon.ThanhTien)
TheoChiSo     = ChiSoConsumptionCalculator.Calculate(ChiSoDienNuoc) * DonGia
CoDinh        = 1 * DonGia
TienNoKyTruoc = snapshot nợ kỳ trước
TongCong      = TienPhong + TongTienDV + TienNoKyTruoc
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
- UI quan ly `Nha`; form tao/sua `Phong` bat buoc chon `NhaId` tu bang `Nha`.
- Da chay app voi MySQL that va smoke test flow Nha -> Phong -> Khach thue -> Hop dong -> Hoa don -> Thu tien.
- Da test voi MySQL that flow gan dich vu theo phong, nhap chi so binh thuong, nhap chi so reset, lap hoa don co dich vu va xac nhan `ChiTietHoaDon.ChiSoDienNuocId`.
- Da test voi MySQL that flow chuyen phong va tra phong giua thang co ca dich vu theo chi so va dich vu co dinh.
- UI nhap chi so ho tro nhap truc tiep theo `PhongId` + ky, de phong moi co the nhap chi so truoc khi thuc hien chuyen phong.
- Thêm ledger cọc `GiaoDichCoc`, ghi nhận thu cọc ban đầu, chuyển cọc khi chuyển phòng, trừ nợ/hoàn cọc khi trả phòng.
- Xử lý nợ chuyển kỳ/chuyển hợp đồng bằng dòng `ThanhToan` phi tiền mặt để tránh double-count công nợ.
- UI xử lý chênh lệch cọc khi chuyển phòng ngay trên màn ledger cọc.
- Đồng bộ code theo `Database/schema.sql`.
- `dotnet build --no-restore` thành công với 0 warning, 0 error.

### Cần kiểm tra tiếp

- Test thủ công các flow nang cao con lai: xu ly coc/cong no va cac bien the thanh toan no.
- Rà lại `Database/schema.sql` vì file schema cũng đang có dấu hiệu hiển thị lỗi encoding trong terminal, dù tên bảng/cột vẫn đọc được.

### Tính năng còn thiếu

- In phiếu thu HTML.
- Thông báo nhắc nợ.

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

Cập nhật lần cuối: Phiên 20 - 28/06/2026
