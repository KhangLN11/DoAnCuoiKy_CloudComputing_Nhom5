
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
    }
}
