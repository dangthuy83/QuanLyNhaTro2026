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

### 5.1. Nhap chi so hang loat

Mot bang theo ky:

```text
Phong | Dien cu | Dien moi | Tieu thu | Nuoc cu | Nuoc moi | Tieu thu | Ghi chu
```

Tinh tieu thu ngay khi nhap. Dong nao chi so moi nho hon chi so cu thi to do va hien lua chon "Dong ho reset".

### 5.2. Preview chot hoa don hang loat

Truoc khi chot:

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
Chot tat ca hop le
Bo qua dong loi
Xem chi tiet
```

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

1. Lam UI nhap chi so hang loat va preview chot hoa don.
2. In phieu thu HTML neu can in nhanh tu trinh duyet.

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

1. `ChiSo/Index` va man nhap chi so hang loat: inline edit, validate dong loi, to mau canh bao.
2. Preview chot hoa don hang loat: grid tong hop tien phong, dich vu, no cu, tong cong, trang thai du lieu.
3. `HoaDon/Index`: da co thu nhanh Bootstrap toi thieu; chi doi sang Syncfusion Grid khi can sort/filter/export nang cao hon.
4. `BaoCao/CongNo`: da co filter Bootstrap toi thieu; chi doi sang Syncfusion Grid khi can sort/filter/export nang cao hon.
5. Dashboard: them Chart/KPI nhe neu can, sau khi cac man thao tac chinh da tot.

Khong lam truoc khi:

- Sua xong validate chi so va dong ho reset.
- Them transaction cho cac flow tai chinh chinh.
- Co quyet dinh ve ledger coc/cong no.
