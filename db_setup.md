# Hướng dẫn tạo và cập nhật Database (Entity Framework Core)

Tài liệu này chứa các lệnh cần thiết để quản lý database cho dự án `ChatApp_Backend`.

## 1. Yêu cầu hệ thống
Đảm bảo bạn đã cài đặt công cụ `dotnet-ef`. Nếu chưa, hãy chạy lệnh sau:
```bash
dotnet tool install --global dotnet-ef
```

## 2. Di chuyển vào thư mục Backend
Mở terminal và di chuyển vào thư mục gốc của backend:
```bash
cd d:\Projects\DotNet\chat_app\ChatApp_Backend
```

## 3. Các lệnh chính

### A. Tạo/Cập nhật Database (Dựa trên migrations có sẵn)
Chạy lệnh này để tạo database lần đầu hoặc cập nhật các thay đổi mới nhất từ các file Migration:
```bash
dotnet ef database update --project Infrastructure --startup-project API
```

### B. Thêm Migration mới (Khi bạn thay đổi Model/DbContext)
Nếu bạn chỉnh sửa các class trong thư mục `Core/Entities` hoặc chỉnh sửa `ChatAppDbContext.cs`, hãy chạy lệnh này để tạo file migration mới:
```bash
dotnet ef migrations add <Tên_Migration> --project Infrastructure --startup-project API
```
*Ví dụ: `dotnet ef migrations add AddUserStatus --project Infrastructure --startup-project API`*

### C. Xóa Migration cuối cùng (Chưa update vào DB)
Nếu bạn vừa tạo migration nhưng muốn thay đổi trước khi chạy lệnh update:
```bash
dotnet ef migrations remove --project Infrastructure --startup-project API
```

## 4. Lưu ý
- Dự án sử dụng `Infrastructure` làm nơi lưu trữ dữ liệu và migration (`--project Infrastructure`).
- Dự án sử dụng `API` làm điểm khởi đầu để đọc cấu hình chuỗi kết nối (`--startup-project API`).

### D. Chạy dự án
```bash
dotnet watch run --project API
```
