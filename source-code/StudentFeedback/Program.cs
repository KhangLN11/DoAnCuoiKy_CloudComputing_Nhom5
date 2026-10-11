
using Microsoft.EntityFrameworkCore;
using StudentFeedback.Data;

var builder = WebApplication.CreateBuilder(args);

// Đăng ký MVC
builder.Services.AddControllersWithViews();

// Đọc chuỗi kết nối MySQL
var connectionString = builder.Configuration
    .GetConnectionString("DefaultConnection");

// Chỉ đăng ký DbContext khi đã có chuỗi kết nối
if (!string.IsNullOrWhiteSpace(connectionString))
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseMySql(
            connectionString,
            new MySqlServerVersion(new Version(8, 0, 46))));
}

var app = builder.Build();

// Cấu hình xử lý lỗi
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Cấu hình HTTP
app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();

// Định tuyến MVC
app.MapStaticAssets();
app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
