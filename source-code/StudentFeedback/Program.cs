
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Minio;
using StudentFeedback.Data;
using StudentFeedback.Services;
using StudentFeedback.Swagger;

var builder = WebApplication.CreateBuilder(args);

// =====================================================
// 1. MVC + REST API
// =====================================================

builder.Services.AddControllersWithViews();

// =====================================================
// 2. Swagger
// =====================================================

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.OperationFilter<
        SwaggerAntiforgeryOperationFilter>();
});

// =====================================================
// 3. Antiforgery
// =====================================================

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "RequestVerificationToken";
});

// =====================================================
// 4. CSV Services
// =====================================================

builder.Services.AddScoped<StudentCsvPreviewService>();

builder.Services.AddSingleton<
    StudentCsvTemporaryStorageService>();

// =====================================================
// 5. MariaDB
// =====================================================

var connectionString = builder.Configuration
    .GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Chưa cấu hình DefaultConnection.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(
        connectionString,
        new MariaDbServerVersion(
            new Version(10, 4, 32))));

// =====================================================
// 6. MinIO
// =====================================================

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
        .WithCredentials(
            minioAccessKey,
            minioSecretKey)
        .WithSSL(minioUseSSL)
        .Build());

builder.Services.AddScoped<MinioStorageService>();

// =====================================================
// 7. Cookie Authentication
// =====================================================

builder.Services
    .AddAuthentication(
        CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";

        options.AccessDeniedPath =
            "/Account/AccessDenied";

        options.Cookie.Name = "StudentFeedback.Auth";

        options.Cookie.HttpOnly = true;

        options.Cookie.SameSite = SameSiteMode.Lax;

        options.Cookie.SecurePolicy =
            CookieSecurePolicy.SameAsRequest;

        options.ExpireTimeSpan =
            TimeSpan.FromHours(2);

        options.SlidingExpiration = true;

        options.Events.OnRedirectToLogin = context =>
        {
            if (context.Request.Path.StartsWithSegments(
                "/api"))
            {
                context.Response.StatusCode =
                    StatusCodes.Status401Unauthorized;

                return Task.CompletedTask;
            }

            context.Response.Redirect(
                context.RedirectUri);

            return Task.CompletedTask;
        };

        options.Events.OnRedirectToAccessDenied = context =>
        {
            if (context.Request.Path.StartsWithSegments(
                "/api"))
            {
                context.Response.StatusCode =
                    StatusCodes.Status403Forbidden;

                return Task.CompletedTask;
            }

            context.Response.Redirect(
                context.RedirectUri);

            return Task.CompletedTask;
        };
    });

// =====================================================
// 8. Authorization
// =====================================================

builder.Services.AddAuthorization();

// =====================================================
// 9. Build
// =====================================================

var app = builder.Build();

// =====================================================
// 10. Exception Handling
// =====================================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// =====================================================
// 11. Swagger - Development Only
// =====================================================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint(
            "/swagger/v1/swagger.json",
            "StudentFeedback REST API v1");
    });
}

// =====================================================
// 12. Middleware
// =====================================================

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();

// =====================================================
// 13. Routes
// =====================================================

app.MapStaticAssets();

app.MapControllers();

// Trang chủ chuyển hướng theo vai trò.
app.MapGet("/", (HttpContext context) =>
{
    if (context.User.Identity?.IsAuthenticated == true)
    {
        if (context.User.IsInRole("Admin"))
        {
            return Results.Redirect("/Admin/Index");
        }

        if (context.User.IsInRole("Student"))
        {
            return Results.Redirect("/Student/Index");
        }
    }

    return Results.Redirect("/Account/Login");
})
.ExcludeFromDescription();

// MVC route.
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
