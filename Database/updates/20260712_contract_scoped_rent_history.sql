-- REVIEW-003: chuyển lịch sử giá thuê từ scope Phong sang scope HopDong.
-- Chạy đúng một lần trên database đã tồn tại từ baseline trước thay đổi này.
-- Dòng Phong cũ được giữ với nhãn PhongLegacy để bảo toàn dấu vết audit,
-- nhưng không còn được service tính tiền sử dụng.
SET SQL_SAFE_UPDATES = 0;
START TRANSACTION;

INSERT INTO LichSuThayDoiGia
    (LoaiDoiTuong, DoiTuongId, GiaCu, GiaMoi, ThangApDung, NamApDung, LyDo, NgayTao)
SELECT
    'HopDong',
    hd.Id,
    ls.GiaCu,
    ls.GiaMoi,
    ls.ThangApDung,
    ls.NamApDung,
    CONCAT(COALESCE(ls.LyDo, ''),
        CASE WHEN ls.LyDo IS NULL OR ls.LyDo = '' THEN '' ELSE ' | ' END,
        'Migrated from room-scoped rent history #', ls.Id),
    ls.NgayTao
FROM LichSuThayDoiGia ls
JOIN HopDong hd
  ON hd.PhongId = ls.DoiTuongId
 AND hd.NgayBatDau <= LAST_DAY(STR_TO_DATE(CONCAT(ls.NamApDung, '-', ls.ThangApDung, '-01'), '%Y-%m-%d'))
 AND COALESCE(hd.NgayTraPhongThucTe, hd.NgayKetThuc, '9999-12-31') >=
     STR_TO_DATE(CONCAT(ls.NamApDung, '-', ls.ThangApDung, '-01'), '%Y-%m-%d')
WHERE ls.LoaiDoiTuong = 'Phong'
  AND NOT EXISTS (
      SELECT 1
      FROM LichSuThayDoiGia existing
      WHERE existing.LoaiDoiTuong = 'HopDong'
        AND existing.DoiTuongId = hd.Id
        AND existing.ThangApDung = ls.ThangApDung
        AND existing.NamApDung = ls.NamApDung
  );

UPDATE LichSuThayDoiGia
SET LoaiDoiTuong = 'PhongLegacy'
WHERE LoaiDoiTuong = 'Phong';

COMMIT;
SET SQL_SAFE_UPDATES = 1;