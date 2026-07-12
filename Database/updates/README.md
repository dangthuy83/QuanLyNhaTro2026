# Database updates

Baseline hiện hành được tạo trực tiếp từ `Database/schema.sql` sau đợt chuẩn hóa ngày 10/07/2026.

- Database mới: chỉ chạy toàn bộ `Database/schema.sql`.
- Không chạy các file trong `archive_pre_20260710` trên database tạo từ baseline hiện hành.
- Thư mục archive chỉ giữ lịch sử nâng cấp các database thử nghiệm cũ trước baseline.
- Khi hệ thống đã có dữ liệu thật, mỗi thay đổi schema sau baseline phải có file apply-once mới đặt trực tiếp trong `Database/updates/` và đồng thời cập nhật `Database/schema.sql`.

Apply-once hiện hành:

- `20260712_contract_scoped_rent_history.sql`: chuyển lịch sử giá thuê scope `Phong` sang hợp đồng đang hiệu lực tại kỳ áp dụng; dòng nguồn được giữ dưới nhãn `PhongLegacy` và không còn tham gia tính tiền.
- `20260712_deposit_payment_method.sql`: thêm `GiaoDichCoc.PhuongThuc` cho giao dịch thu/hoàn cọc thủ công (`TienMat`/`ChuyenKhoan`).
- `20260712_contract_overlap_future_status.sql`: thêm CHECK khoảng ngày/trạng thái `ChoHieuLuc` và index phục vụ kiểm tra chồng kỳ; chạy hai câu dry-run đầu file và chỉ apply khi đều bằng 0.
