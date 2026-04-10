using Microsoft.EntityFrameworkCore;
using UserManagement.Models.Entities;
using UserManagement.Models.Enums;

namespace UserManagement.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base (options) { }
        
        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique()
                .HasDatabaseName("IX_Users_Email");

            modelBuilder.Entity<User>()
                .Property(u => u.Status)
                .HasConversion<string>()
                .HasDefaultValue(UserStatus.Unverified);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.EmailConfirmationToken)
                .HasDatabaseName("IX_Users_EmailConfirmationToken");
        }
    }
}
