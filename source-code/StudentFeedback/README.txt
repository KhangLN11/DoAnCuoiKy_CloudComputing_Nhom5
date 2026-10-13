StudentFeedback REST API (ASP.NET Core 9 / EF Core 9)

Chỉ dành cho project đã có 5 Models + ApplicationDbContext và đã chạy migration.
Giải nén đúng vào thư mục source-code/StudentFeedback (không phải thư mục gốc repository).
Các file mới:
- Controllers/CategoriesController.cs
- Controllers/ReflectionsController.cs
- Controllers/ResponsesController.cs
- Contracts/ApiRequests.cs

Tại Terminal VS Code từ gốc repository:
  dotnet build source-code/StudentFeedback/StudentFeedback.csproj
  $env:ASPNETCORE_ENVIRONMENT = "Development"
  dotnet run --project source-code/StudentFeedback --no-launch-profile --urls http://localhost:5050

Dùng trình duyệt mở:
  http://localhost:5050/api/categories
  http://localhost:5050/api/reflections
  http://localhost:5050/api/responses

API có GET (danh sách/id), POST, PUT /{id}, DELETE /{id}.
GET categories trả [] khi chưa có bản ghi: đó là thành công.
Payload tạo category: {"name":"Học vụ","description":"Phản ánh liên quan học vụ"}
Payload tạo reflection: {"title":"Phản ánh mẫu","content":"Nội dung mẫu","userId":1,"categoryId":1}
Payload tạo response: {"content":"Đã tiếp nhận","reflectionId":1,"userId":1}
Trước khi tạo reflection/response phải có dữ liệu user thực trong DB.

LƯU Ý BẢO MẬT: Các endpoint thay đổi dữ liệu hiện chưa có authentication/authorization. Chỉ chạy local để phát triển, không public lên Internet. Khi tích hợp phải phối hợp với thành viên phụ trách Authentication: lấy UserId từ người dùng đăng nhập và chặn quyền của các thao tác POST/PUT/DELETE phù hợp. Khóa cấu trúc DTO/API với nhóm trước khi merge.
