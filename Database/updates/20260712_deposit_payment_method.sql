-- REVIEW-005: lưu phương thức cho giao dịch thu/hoàn cọc thủ công.
-- Chạy đúng một lần trên database đã tồn tại từ baseline trước thay đổi này.

ALTER TABLE GiaoDichCoc
    ADD COLUMN PhuongThuc VARCHAR(20) NULL AFTER HoaDonId,
    ADD CONSTRAINT CK_GiaoDichCoc_PhuongThuc
        CHECK (PhuongThuc IS NULL OR PhuongThuc IN ('TienMat', 'ChuyenKhoan'));
