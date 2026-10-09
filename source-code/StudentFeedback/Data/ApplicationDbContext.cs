using Microsoft.EntityFrameworkCore;
using StudentFeedback.Models;

namespace StudentFeedback.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Reflection> Reflections => Set<Reflection>();
    public DbSet<Response> Responses => Set<Response>();
    public DbSet<Attachment> Attachments => Set<Attachment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email).IsUnique();

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