using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Minio;
using StudentFeedback.Data;
using StudentFeedback.Services;

var builder = WebApplication.CreateBuilder(args);

// MVC
builder.Services.AddControllersWithViews();

// CSV Services
builder.Services.AddScoped<StudentCsvPreviewService>();
builder.Services.AddSingleton<StudentCsvTemporaryStorageService>();

// Database
var connectionString = builder.Configuration
    .GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Chưa cấu hình DefaultConnection.");

// MariaDB 10.4.32 (XAMPP)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(
        connectionString,
        new MariaDbServerVersion(new Version(10, 4, 32))));

// MinIO
var minioEndpoint = builder.Configuration["Minio:Endpoint"]
    ?? throw new InvalidOperationException(
        "Thiếu cấu hình Minio:Endpoint.");

var minioAccessKey = builder.Configuration["Minio:AccessKey"]
    ?? throw new InvalidOperationException(
        "Thiếu cấu hình Minio:AccessKey.");

var minioSecretKey = builder.Configuration["Minio:SecretKey"]
    ?? throw new InvalidOperationException(
        "Thiếu cấu hình Minio:SecretKey.");

var minioUseSSL = builder.Configuration.GetValue<bool>(
    "Minio:UseSSL");

builder.Services.AddSingleton<IMinioClient>(_ =>
    new MinioClient()
        .WithEndpoint(minioEndpoint)
        .WithCredentials(minioAccessKey, minioSecretKey)
        .WithSSL(minioUseSSL)
        .Build());

builder.Services.AddScoped<MinioStorageService>();

// Cookie Authentication
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";

        options.Cookie.Name = "StudentFeedback.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy =
            CookieSecurePolicy.SameAsRequest;

        options.ExpireTimeSpan = TimeSpan.FromHours(2);
        options.SlidingExpiration = true;
    });

// Authorization
builder.Services.AddAuthorization();

var app = builder.Build();

// HTTP pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();