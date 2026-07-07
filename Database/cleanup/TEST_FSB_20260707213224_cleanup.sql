-- Cleanup smoke data for fixed per-person service billing
-- Generated for test tag: TEST_FSB_20260707213224
--
-- Safe Updates note:
--   DELETE statements include key-column predicates such as Id >= 0 or Id IN (...)
--   so this script can run in MySQL Workbench with Safe Updates enabled.

CREATE TEMPORARY TABLE tmp_cleanup_nha_ids (Id INT PRIMARY KEY);
INSERT INTO tmp_cleanup_nha_ids (Id) VALUES
    (19);

CREATE TEMPORARY TABLE tmp_cleanup_dichvu_ids (Id INT PRIMARY KEY);
INSERT INTO tmp_cleanup_dichvu_ids (Id) VALUES
    (15);

CREATE TEMPORARY TABLE tmp_cleanup_phong_ids (Id INT PRIMARY KEY);
INSERT INTO tmp_cleanup_phong_ids (Id) VALUES
    (22),
    (23);

CREATE TEMPORARY TABLE tmp_cleanup_hopdong_ids (Id INT PRIMARY KEY);
INSERT INTO tmp_cleanup_hopdong_ids (Id) VALUES
    (24),
    (25);

CREATE TEMPORARY TABLE tmp_cleanup_khach_ids (Id INT PRIMARY KEY);
INSERT INTO tmp_cleanup_khach_ids (Id) VALUES
    (20),
    (21);

CREATE TEMPORARY TABLE tmp_cleanup_hoadon_ids (Id INT PRIMARY KEY);
INSERT IGNORE INTO tmp_cleanup_hoadon_ids (Id)
SELECT Id FROM HoaDon WHERE HopDongId IN (SELECT Id FROM tmp_cleanup_hopdong_ids);

SELECT 'Before cleanup - HoaDon' AS Bang, COUNT(*) AS SoDong
FROM HoaDon WHERE Id IN (SELECT Id FROM tmp_cleanup_hoadon_ids)
UNION ALL
SELECT 'Before cleanup - HopDong', COUNT(*) FROM HopDong WHERE Id IN (SELECT Id FROM tmp_cleanup_hopdong_ids)
UNION ALL
SELECT 'Before cleanup - PhongDichVu', COUNT(*) FROM PhongDichVu
WHERE PhongId IN (SELECT Id FROM tmp_cleanup_phong_ids)
  AND DichVuId IN (SELECT Id FROM tmp_cleanup_dichvu_ids);

DELETE FROM ThanhToan
WHERE Id >= 0
  AND HoaDonId IN (SELECT Id FROM tmp_cleanup_hoadon_ids);

DELETE FROM ChiTietHoaDon
WHERE Id >= 0
  AND (
      HoaDonId IN (SELECT Id FROM tmp_cleanup_hoadon_ids)
      OR DichVuId IN (SELECT Id FROM tmp_cleanup_dichvu_ids)
  );

DELETE FROM KhoanPhatSinhHopDong
WHERE Id >= 0
  AND (
      HopDongId IN (SELECT Id FROM tmp_cleanup_hopdong_ids)
      OR HoaDonId IN (SELECT Id FROM tmp_cleanup_hoadon_ids)
  );

DELETE FROM GiaoDichCoc
WHERE Id >= 0
  AND (
      HopDongId IN (SELECT Id FROM tmp_cleanup_hopdong_ids)
      OR HoaDonId IN (SELECT Id FROM tmp_cleanup_hoadon_ids)
  );

DELETE FROM HoaDon
WHERE Id >= 0
  AND Id IN (SELECT Id FROM tmp_cleanup_hoadon_ids);

DELETE FROM ChiSoDienNuoc
WHERE Id >= 0
  AND (
      HopDongId IN (SELECT Id FROM tmp_cleanup_hopdong_ids)
      OR (PhongId IN (SELECT Id FROM tmp_cleanup_phong_ids)
          AND DichVuId IN (SELECT Id FROM tmp_cleanup_dichvu_ids))
  );

DELETE FROM ChiSoNgoaiHopDong
WHERE Id >= 0
  AND PhongId IN (SELECT Id FROM tmp_cleanup_phong_ids)
  AND DichVuId IN (SELECT Id FROM tmp_cleanup_dichvu_ids);

DELETE FROM PhongDichVu
WHERE Id >= 0
  AND PhongId IN (SELECT Id FROM tmp_cleanup_phong_ids)
  AND DichVuId IN (SELECT Id FROM tmp_cleanup_dichvu_ids);

DELETE FROM HopDongKhachThue
WHERE Id >= 0
  AND (
      HopDongId IN (SELECT Id FROM tmp_cleanup_hopdong_ids)
      OR KhachThueId IN (SELECT Id FROM tmp_cleanup_khach_ids)
  );

DELETE FROM HopDong
WHERE Id >= 0
  AND Id IN (SELECT Id FROM tmp_cleanup_hopdong_ids);

DELETE FROM KhachThue
WHERE Id >= 0
  AND Id IN (SELECT Id FROM tmp_cleanup_khach_ids);

DELETE FROM Phong
WHERE Id >= 0
  AND Id IN (SELECT Id FROM tmp_cleanup_phong_ids);

DELETE FROM DichVu
WHERE Id >= 0
  AND Id IN (SELECT Id FROM tmp_cleanup_dichvu_ids);

DELETE FROM Nha
WHERE Id >= 0
  AND Id IN (SELECT Id FROM tmp_cleanup_nha_ids);

SELECT 'Remaining - Nha' AS Bang, COUNT(*) AS SoDong FROM Nha WHERE Id IN (SELECT Id FROM tmp_cleanup_nha_ids)
UNION ALL
SELECT 'Remaining - DichVu', COUNT(*) FROM DichVu WHERE Id IN (SELECT Id FROM tmp_cleanup_dichvu_ids)
UNION ALL
SELECT 'Remaining - Phong', COUNT(*) FROM Phong WHERE Id IN (SELECT Id FROM tmp_cleanup_phong_ids)
UNION ALL
SELECT 'Remaining - HopDong', COUNT(*) FROM HopDong WHERE Id IN (SELECT Id FROM tmp_cleanup_hopdong_ids)
UNION ALL
SELECT 'Remaining - KhachThue', COUNT(*) FROM KhachThue WHERE Id IN (SELECT Id FROM tmp_cleanup_khach_ids);