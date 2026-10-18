
using Microsoft.EntityFrameworkCore;
using StudentFeedback.Models;

namespace StudentFeedback.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Reflection> Reflections => Set<Reflection>();

    public DbSet<Response> Responses => Set<Response>();

    public DbSet<Attachment> Attachments => Set<Attachment>();

    // N1.1: Thông báo dành cho sinh viên.
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Email không được trùng.
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        // Mã sinh viên không được trùng.
        modelBuilder.Entity<User>()
            .HasIndex(u => u.StudentCode)
            .IsUnique();

        // Quan hệ Reflection - User
        modelBuilder.Entity<Reflection>()
            .HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Quan hệ Reflection - Category
        modelBuilder.Entity<Reflection>()
            .HasOne(r => r.Category)
            .WithMany()
            .HasForeignKey(r => r.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Quan hệ Response - Reflection
        modelBuilder.Entity<Response>()
            .HasOne(r => r.Reflection)
            .WithMany()
            .HasForeignKey(r => r.ReflectionId)
            .OnDelete(DeleteBehavior.Restrict);

        // Quan hệ Response - User
        modelBuilder.Entity<Response>()
            .HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Quan hệ Attachment - Reflection
        modelBuilder.Entity<Attachment>()
            .HasOne(a => a.Reflection)
            .WithMany()
            .HasForeignKey(a => a.ReflectionId)
            .OnDelete(DeleteBehavior.Restrict);

        // ========================================
        // N1.1 - CẤU HÌNH BẢNG NOTIFICATIONS
        // ========================================

        // Mỗi thông báo thuộc một tài khoản.
        modelBuilder.Entity<Notification>()
            .HasOne(n => n.User)
            .WithMany()
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Mỗi thông báo liên quan đến một phản ánh.
        modelBuilder.Entity<Notification>()
            .HasOne(n => n.Reflection)
            .WithMany()
            .HasForeignKey(n => n.ReflectionId)
            .OnDelete(DeleteBehavior.Restrict);

        // Tối ưu danh sách thông báo của từng sinh viên.
        modelBuilder.Entity<Notification>()
            .HasIndex(n => new
            {
                n.UserId,
                n.CreatedAt
            });

        // Tối ưu việc đếm thông báo chưa đọc.
        modelBuilder.Entity<Notification>()
            .HasIndex(n => new
            {
                n.UserId,
                n.IsRead
            });
    }
}
