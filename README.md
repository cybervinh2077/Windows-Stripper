# Windows Stripper

Công cụ đưa Windows (và Office nếu có) về trạng thái **chưa kích hoạt** — hoạt động với cả bản quyền chính hãng lẫn bản đã crack. Dùng các công cụ có sẵn của Microsoft (`slmgr.vbs`, `ospp.vbs`), không bẻ khoá.

> ⚠️ Chỉ dùng trên máy của bạn hoặc máy bạn được phép quản trị (vd: dọn license trước khi bán/chuyển nhượng máy).

## Tính năng

- Hiển thị thông tin máy: tên người dùng, tên máy, phiên bản Windows.
- Hiển thị trạng thái bản quyền Windows + **đánh giá nguồn gốc** (chính hãng / nghi crack KMS) kèm log thông tin thô để tự đối chiếu.
- Phát hiện và hiển thị trạng thái bản quyền **Office** (qua `ospp.vbs`).
- Nút **Xoá bản quyền**: gỡ product key Windows (`/upk`, `/cpky`) + Office (`/unpkey`), bật lại `sppsvc`, khôi phục hiển thị watermark.
- Nút **Test (giả lập)**: liệt kê mọi bước sẽ làm mà **không thực thi**.
- **Cập nhật OTA** qua GitHub Releases.

## Hai bản build

| File | Mô tả |
|------|-------|
| `WindowsStriper.exe` | Bản thật — thực thi xoá bản quyền |
| `WindowsStriper-TEST.exe` | Bản TEST — mọi thao tác chỉ giả lập, an toàn |

## Build từ mã nguồn

Yêu cầu: Windows có sẵn .NET Framework (csc.exe). Chạy:

```powershell
# Tạo icon từ fpt.png (nếu cần)
./make-icon.ps1
# Biên dịch cả 2 bản
./build.ps1
```

## Phát hành bản cập nhật (OTA)

1. Tăng `AppVersion` trong `Program.cs` rồi build lại.
2. Tạo **Release** mới trên GitHub với tag = phiên bản (vd `v1.0.1`).
3. Upload `WindowsStriper.exe` (và `WindowsStriper-TEST.exe`) làm asset của release.

Tool tự đọc release mới nhất, so phiên bản, tải về và tự thay thế.
