using Microsoft.EntityFrameworkCore;

namespace UpGoDown.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<LevelAttempt> LevelAttempts => Set<LevelAttempt>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUser>(e =>
        {
            e.HasIndex(x => x.Login).IsUnique();
            e.Property(x => x.Login).HasMaxLength(64);
            e.Property(x => x.Name).HasMaxLength(128);
            e.Property(x => x.Role).HasMaxLength(32).HasDefaultValue(UserRoles.Student);
        });

        modelBuilder.Entity<LevelAttempt>(e =>
        {
            e.HasIndex(x => new { x.UserId, x.LevelId, x.CreatedAt });
            e.HasOne(x => x.User).WithMany(u => u.Attempts).HasForeignKey(x => x.UserId);
        });
    }
}
