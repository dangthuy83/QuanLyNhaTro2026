# Database updates

REVIEW-014 apply-once:

- `20260713_tenant_identity_photo_lifecycle.sql`: báo cáo nhóm CCCD/SĐT/đường dẫn ảnh trùng trước khi thêm generated `CCCDNormalized` và unique CCCD sau `TRIM`. Script dừng nếu còn nhóm CCCD trùng, không merge/sửa/xóa hồ sơ hoặc file. Dry-run vận hành phải bổ sung đối chiếu filesystem để báo ảnh thiếu và file mồ côi trước khi apply.

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
- `20260713_invoice_identity_snapshot.sql`: REVIEW-013, đóng băng nhà/phòng/khách đại diện, tên-đơn vị dịch vụ và mô tả-số tiền khoản phát sinh trên hóa đơn. Phải lưu dry-run đầu file; script dừng nếu hóa đơn thiếu phạm vi, không có đúng một đại diện trong kỳ, mất dịch vụ hoặc khoản phát sinh liên kết có mô tả rỗng.
