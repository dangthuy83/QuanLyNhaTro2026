# PROJECT_REVIEW.md - Review nghiep vu va kien truc

Ngay lap: 27/06/2026

Vai tro review: Solution Architect + Business Analyst cho ung dung quan ly nha tro thay the Excel.

---

## 1. Ket luan tong quan

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
- Chua xu ly bien dong nhan khau theo ngay trong cung hop dong; can them moc ngay rieng vao `HopDongKhachThue` neu sau nay can tinh theo ngay.

Da xu ly trong phien 41:

- Them man `Phong/GanDichVuHangLoat` de gan/cap nhat cung mot dich vu cho nhieu phong, giam thao tac sau khi cau hinh danh muc dich vu theo nguoi.
- Man nay cap nhat truc tiep `PhongDichVu.DonGia`, giu dung quy tac hoa don lay gia theo phong thay vi `DichVu.DonGiaMacDinh`.
- Man nay canh bao hop dong hieu luc chua co khach khi dich vu tinh theo nguoi, va hien thanh tien du kien theo so khach x don gia de review truoc khi luu.

Da smoke test trong phien 42:

- Tao du lieu `TEST_FSB_20260707213224`, gan dich vu `CoDinh + TheoNguoi` cho 2 phong dang thue qua repository hang loat.
- Xac nhan `PhongDichVu.DonGia = 120000`, preview ky 07/2026 tinh phong co 2 khach thanh `SoLuong = 2`, `ThanhTien = 240000`.
- Xac nhan hop dong chua gan khach bi bao loi/chặn preview de tranh tinh `SoLuong = 0`.
- Da sinh file cleanup safe-mode-friendly: `Database/cleanup/TEST_FSB_20260707213224_cleanup.sql`.

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
