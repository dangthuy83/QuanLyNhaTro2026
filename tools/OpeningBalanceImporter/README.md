# OpeningBalanceImporter

Công cụ dùng một lần cho REVIEW-028. Công cụ chỉ nhận JSON; file nguồn có thể là PDF/XLSX/ZIP
đã được duyệt. SHA-256 được tính trực tiếp từ file nguồn, so với `dotMoSo.sha256`, rồi dùng làm
khóa chống replay trong `DotMoSo`.

## An toàn

- `validate` mở transaction `READ ONLY`, không ghi DB.
- `apply` luôn chạy lại validate trước, yêu cầu `--confirm-sha`, rồi ghi toàn bộ đợt trong một
  transaction `SERIALIZABLE`. Crash trước commit không để lại row; retry sau commit bị unique
  SHA-256 chặn.
- Chỉ `MoSoService` ghi dữ liệu. CLI không chứa SQL ghi.
- Công cụ không đọc `appsettings.json`; truyền connection string qua biến môi trường
  `QUANLYNHATRO_CONNECTION`. Log chỉ có SHA-256 và tổng số dòng, không in CCCD, điện thoại hay
  connection string.
- Rehearsal có thể đặt `QUANLYNHATRO_REHEARSAL=1` và dùng
  `--simulate-failure-after-contracts N` để ném lỗi trước commit, chứng minh rollback toàn đợt.
  Cờ này bị chặn nếu không có biến môi trường rehearsal.

## Lệnh

```powershell
$env:QUANLYNHATRO_CONNECTION='<connection string>'
dotnet run --project tools/OpeningBalanceImporter -- validate --input <batch.json> --source <evidence.zip>
dotnet run --project tools/OpeningBalanceImporter -- apply --input <batch.json> --source <evidence.zip> --confirm-sha <SHA256>
```

Migration 12 phải có trước khi validate/apply. Không dùng file trong `samples/` cho production.

## Trường bắt buộc

- `dotMoSo`: `ngayChot`, `tenNguon`, SHA-256 file nguồn, `nguoiDuyet`, ghi chú không chứa PII.
- `hopDong[]`: mã nguồn riêng, phòng, ngày thực tế, giá thuê, cọc thỏa thuận, ngày thanh toán.
- `cuTru[]`: khách đã có trong DB, khoảng cư trú thực tế, cờ đại diện và mã chứng từ; đúng một
  đại diện phải hiệu lực tại cutover.
- `dichVu[]`: ID cấu hình `PhongDichVu` đang áp dụng và mã chứng từ riêng.
- `soDuCocThucTe` cùng `soDuCocNguonThamChieu`: kể cả số dư bằng 0 vẫn phải có chứng từ.
- `congNo[]`: số tiền dương, kỳ nguồn, mã chứng từ và mã nguồn riêng.
- `chiSo[]`: mỗi dịch vụ theo chỉ số phải có đúng một mốc, giá trị không âm và mã chứng từ.

Không tự suy diễn trường thiếu. Khách, phòng, dịch vụ phải tồn tại trước trong DB đã duyệt.
