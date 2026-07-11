# Database updates

Baseline hiện hành được tạo trực tiếp từ `Database/schema.sql` sau đợt chuẩn hóa ngày 10/07/2026.

- Database mới: chỉ chạy toàn bộ `Database/schema.sql`.
- Không chạy các file trong `archive_pre_20260710` trên database tạo từ baseline hiện hành.
- Thư mục archive chỉ giữ lịch sử nâng cấp các database thử nghiệm cũ trước baseline.
- Khi hệ thống đã có dữ liệu thật, mỗi thay đổi schema sau baseline phải có file apply-once mới đặt trực tiếp trong `Database/updates/` và đồng thời cập nhật `Database/schema.sql`.
