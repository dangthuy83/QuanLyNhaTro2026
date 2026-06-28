# WORKLOG.md - Nhật Ký Làm Việc

## Dự án: Quản lý nhà trọ (ASP.NET Core MVC)

File này ghi lại tiến trình theo thời gian: đã làm gì, lỗi nào đã sửa, trạng thái build/test hiện tại. Phiên mới nên đọc `DECISIONS.md` trước, rồi đọc file này.

---

## Trạng Thái Hiện Tại

| Mục | Trạng thái |
|---|---|
| Giai đoạn | Phase 4: đang xử lý rủi ro nghiệp vụ lõi - ledger cọc đã có bản tối thiểu |
| Build | `dotnet build --no-restore` thành công, 0 warning, 0 error |
| Restore | Đã restore NuGet thành công sau khi trỏ cache vào thư mục workspace |
| Database | Đã chạy app với MySQL thật ở các phiên trước; phiên này thêm schema `GiaoDichCoc` và migration SQL cho DB hiện hữu |
| GitHub repo | `https://github.com/dangthuy83/QuanLyNhaTro2026.git` |
| Quyết định quan trọng | `Database/schema.sql` là nguồn chuẩn; đã chốt quy ước ngày vào/ngày ra/chuyển phòng; đã chặn chỉ số âm; đã gom reset/hỏng/thay/quay vòng đồng hồ vào `LoaiGhiNhan = Reset` |

### Việc cần làm tiếp

| # | Việc | Ghi chú | Ưu tiên |
|---|---|---|---|
| 1 | Smoke test ledger cọc với MySQL thật sau khi apply migration | Cần test tạo hợp đồng, chuyển phòng, trả phòng, ghi nhận thu thêm/hoàn cọc | Cao |
| 2 | Rà dữ liệu test sau smoke test | Dữ liệu có tiền tố `TEST_Codex_*`, `TEST_P*`, `TEST_METER_*`, `TEST_KHACH_*`, `TEST_MOVE_*`, `TEST_RETURN_*` | Thấp |
| 3 | Rà lại `Database/schema.sql` encoding | File schema hiển thị mojibake trong terminal; cần chuẩn hóa nếu muốn đọc comment tiếng Việt | Trung bình |
| 4 | Chốt nhãn `LoaiDoiTuong` của `LichSuThayDoiGia` | Code hiện dùng `Phong` và `DichVu`; cần thống nhất với comment/schema | Trung bình |
| 5 | Rà UI ledger cọc sau vận hành thực tế | Theo dõi thêm nhu cầu lọc/in phiếu sau khi dùng thật | Thấp |
| 6 | In phiếu thu HTML | `window.print()` và CSS print | Trung bình |
| 7 | Xử lý các rủi ro trong `PROJECT_REVIEW.md` | Tiếp theo: rà báo cáo công nợ sau ledger, UI nhập chỉ số hàng loạt/preview chốt hóa đơn | Cao |
| 8 | Nâng cấp UI bằng Syncfusion | Làm sau nghiệp vụ lõi; xem `PROJECT_REVIEW.md` mục 8 | Trung bình |

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
- Thêm `Database/updates/20260628_add_giao_dich_coc.sql` cho DB hiện hữu.
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
