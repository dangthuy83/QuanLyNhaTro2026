-- Cleanup smoke data for fixed per-person service billing
-- Generated for test tag: TEST_FSB_20260707213224
--
-- MySQL Workbench Safe Updates note:
--   This version avoids DELETE ... IN (SELECT ... FROM temp table).
--   DELETE statements use direct constant IDs or indexed foreign-key values.
--
-- Smoke IDs:
--   Nha: 19
--   DichVu: 15
--   Phong: 22, 23
--   HopDong: 24, 25
--   KhachThue: 20, 21
--
-- The smoke test only called invoice preview, so it should not have created HoaDon rows.

SELECT 'Before cleanup - HoaDon' AS Bang, COUNT(*) AS SoDong
FROM HoaDon
WHERE HopDongId IN (24, 25)
UNION ALL
SELECT 'Before cleanup - HopDong', COUNT(*)
FROM HopDong
WHERE Id IN (24, 25)
UNION ALL
SELECT 'Before cleanup - PhongDichVu', COUNT(*)
FROM PhongDichVu
WHERE PhongId IN (22, 23)
  AND DichVuId = 15
UNION ALL
SELECT 'Before cleanup - HopDongKhachThue', COUNT(*)
FROM HopDongKhachThue
WHERE HopDongId IN (24, 25);

-- If the HoaDon count above is not 0, stop here and review before deleting HopDong.
-- The expected value for this smoke test is 0 because only preview was executed.

START TRANSACTION;

DELETE FROM PhongDichVu
WHERE PhongId IN (22, 23)
  AND DichVuId = 15;

DELETE FROM HopDongKhachThue
WHERE HopDongId IN (24, 25);

DELETE FROM HopDong
WHERE Id IN (24, 25);

DELETE FROM KhachThue
WHERE Id IN (20, 21);

DELETE FROM Phong
WHERE Id IN (22, 23);

DELETE FROM DichVu
WHERE Id = 15;

DELETE FROM Nha
WHERE Id = 19;

COMMIT;

SELECT 'Remaining - Nha' AS Bang, COUNT(*) AS SoDong
FROM Nha
WHERE Id = 19
UNION ALL
SELECT 'Remaining - DichVu', COUNT(*)
FROM DichVu
WHERE Id = 15
UNION ALL
SELECT 'Remaining - Phong', COUNT(*)
FROM Phong
WHERE Id IN (22, 23)
UNION ALL
SELECT 'Remaining - HopDong', COUNT(*)
FROM HopDong
WHERE Id IN (24, 25)
UNION ALL
SELECT 'Remaining - KhachThue', COUNT(*)
FROM KhachThue
WHERE Id IN (20, 21)
UNION ALL
SELECT 'Remaining - PhongDichVu', COUNT(*)
FROM PhongDichVu
WHERE PhongId IN (22, 23)
  AND DichVuId = 15
UNION ALL
SELECT 'Remaining - HopDongKhachThue', COUNT(*)
FROM HopDongKhachThue
WHERE HopDongId IN (24, 25);
