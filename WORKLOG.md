# WORKLOG.md - Nhật Ký Làm Việc

## Dự án: Quản lý nhà trọ (ASP.NET Core MVC)

File này ghi lại tiến trình theo thời gian: đã làm gì, lỗi nào đã sửa, trạng thái build/test hiện tại. Phiên mới nên đọc `DECISIONS.md` trước, rồi đọc file này.

---

## Trạng Thái Hiện Tại

| Mục | Trạng thái |
|---|---|
| Giai đoạn | Phase 4: đang xử lý rủi ro nghiệp vụ lõi - ledger cọc đã có bản tối thiểu |
| Build | `dotnet build --no-restore` thành công với 0 warning, 0 error ở phiên mới nhất. `dotnet test --no-restore` kết thúc mã 0 nhưng không có output test đáng kể |
| Restore | Đã restore NuGet thành công sau khi trỏ cache vào thư mục workspace |
| Database | Đã chạy app với MySQL thật; ledger cọc/công nợ, edge cases kết chuyển nợ, thu tiền nhanh, nhập chỉ số kỳ đầu/hàng loạt/ngoài hợp đồng, preview chốt hóa đơn hàng loạt có filter vận hành, in phiếu thu HTML và nhắc nợ tối thiểu đã smoke test. Phiên 36 đã apply schema runtime và smoke test DB flow chỉ số nhiều đoạn cùng phòng/tháng. Phiên 38 đã smoke test UI/MVC form flow khách cũ trả phòng -> chỉ số ngoài hợp đồng -> khách mới cùng tháng -> preview/chốt hóa đơn. Phiên 39 đã apply migration giá dịch vụ mặc định/khoản phát sinh hợp đồng trên DB runtime. Phiên 40 thêm migration `Database/updates/20260707_fixed_service_quantity_method.sql`; DB hiện hữu cần chạy file này một lần trước khi dùng code mới. Phiên 43 thêm màn `KiemTraDuLieu/Index` read-only để rà dữ liệu trước vận hành/chốt hóa đơn. Phiên 44 chuẩn hóa tiếng Việt có dấu trên các view vận hành chính. |
| GitHub repo | `https://github.com/dangthuy83/QuanLyNhaTro2026.git` |
| Quyết định quan trọng | `Database/schema.sql` là nguồn chuẩn; đã chốt quy ước ngày vào/ngày ra/chuyển phòng; đã chặn chỉ số âm; đã gom reset/hỏng/thay/quay vòng đồng hồ vào `LoaiGhiNhan = Reset`; `DichVu.DonGiaMacDinh` chỉ là giá gợi ý, hóa đơn vẫn dùng `PhongDichVu.DonGia`; `DichVu.CachTinhCoDinh` cho dịch vụ cố định dùng `TheoPhong` hoặc `TheoNguoi`; khoản phát sinh theo hợp đồng được đưa vào hóa đơn hoặc xử lý khi trả phòng/trừ cọc |

### Việc cần làm tiếp

| # | Việc | Ghi chú | Ưu tiên |
|---|---|---|---|
| 1 | Rà dữ liệu test sau smoke test | Dữ liệu có tiền tố `TEST_Codex_*`, `TEST_P*`, `TEST_METER_*`, `TEST_KHACH_*`, `TEST_MOVE_*`, `TEST_RETURN_*`, `TEST_LEDGER_*`, `TEST_DEBT_EDGE_*`, `TEST_QUICKPAY_*`, `TEST_BULK_METER_*`, `TEST_BULK_INVOICE_PREVIEW_*`, `TEST_UI_CONTRACT_SCOPE_*` | Thấp |
| 2 | Theo dõi edge case công nợ trên dữ liệu vận hành thật | Smoke test nhiều hóa đơn nợ, trả phòng có nợ cũ và chặn xóa hóa đơn mang nợ kỳ trước đã pass | Trung bình |
| 3 | Rà lại `Database/schema.sql` encoding | File schema hiển thị mojibake trong terminal; cần chuẩn hóa nếu muốn đọc comment tiếng Việt | Trung bình |
| 4 | Chốt nhãn `LoaiDoiTuong` của `LichSuThayDoiGia` | Code hiện dùng `Phong` và `DichVu`; cần thống nhất với comment/schema | Trung bình |
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

- Thêm `Database/updates/20260701_contract_scoped_meter_readings.sql`.
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
- Thêm migration `Database/updates/20260706_default_service_price_and_contract_charges.sql`.
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
- DB đang tồn tại ở môi trường khác cần chạy một lần file `Database/updates/20260706_default_service_price_and_contract_charges.sql`.

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
- Thêm migration `Database/updates/20260707_fixed_service_quantity_method.sql`.
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
- DB đang tồn tại cần chạy một lần file `Database/updates/20260707_fixed_service_quantity_method.sql`, sau đó rà lại các dịch vụ cố định thực tế và đổi sang `TheoNguoi` nếu cần.
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
