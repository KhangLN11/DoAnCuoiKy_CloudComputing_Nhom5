
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

    // Token thiết lập mật khẩu lần đầu.
    public DbSet<PasswordSetupToken> PasswordSetupTokens =>
        Set<PasswordSetupToken>();

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

        // Cấu hình bảng PasswordSetupTokens.
        modelBuilder.Entity<PasswordSetupToken>(entity =>
        {
            entity.HasKey(t => t.Id);

            // Không cho phép hai token có cùng mã băm.
            entity.HasIndex(t => t.TokenHash)
                .IsUnique();

            // Hỗ trợ tìm token theo sinh viên.
            entity.HasIndex(t => t.UserId);

            entity.HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Reflection>()
            .HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Reflection>()
            .HasOne(r => r.Category)
            .WithMany()
            .HasForeignKey(r => r.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Response>()
            .HasOne(r => r.Reflection)
            .WithMany()
            .HasForeignKey(r => r.ReflectionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Response>()
            .HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Attachment>()
            .HasOne(a => a.Reflection)
            .WithMany()
            .HasForeignKey(a => a.ReflectionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
