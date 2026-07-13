# Database updates

Baseline hiện hành được tạo trực tiếp từ `Database/schema.sql` sau đợt chuẩn hóa ngày 10/07/2026.

- Database mới: chỉ chạy toàn bộ `Database/schema.sql`.
- Không chạy các file trong `archive_pre_20260710` trên database tạo từ baseline hiện hành.
- Thư mục archive chỉ giữ lịch sử nâng cấp các database thử nghiệm cũ trước baseline.
- Database đã có dữ liệu thật: mỗi thay đổi schema sau baseline phải có file apply-once mới trực tiếp trong `Database/updates/` và đồng thời cập nhật `Database/schema.sql`.

Apply-once hiện hành:

- `20260712_contract_scoped_rent_history.sql`: chuyển lịch sử giá thuê scope `Phong` sang hợp đồng hiệu lực tại kỳ áp dụng; giữ dòng nguồn dưới nhãn `PhongLegacy` để audit.
- `20260712_deposit_payment_method.sql`: thêm `GiaoDichCoc.PhuongThuc` cho giao dịch thu/hoàn cọc thủ công.
- `20260712_contract_overlap_future_status.sql`: thêm CHECK khoảng ngày/trạng thái `ChoHieuLuc` và index kiểm tra chồng kỳ; phải chạy dry-run đầu file trước.
- `20260713_tenant_residency_history.sql`: REVIEW-008, thêm ngày bắt đầu/dự kiến/kết thúc thực tế cho từng giai đoạn cư trú, backfill từ hợp đồng và thay unique cũ bằng unique theo giai đoạn. Phải lưu kết quả dry-run đầu file trước khi apply; script không merge/xóa hồ sơ khách và không thêm unique cứng cho CCCD.
