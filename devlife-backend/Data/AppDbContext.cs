using Microsoft.EntityFrameworkCore;
using DevLife.API.Models;

namespace DevLife.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Score> Scores { get; set; }
    public DbSet<Session> Sessions { get; set; }
    public DbSet<CasinoChallenge> CasinoChallenges { get; set; }
    public DbSet<CasinoGame> CasinoGames { get; set; }
    public DbSet<UserStats> UserStats { get; set; }
    public DbSet<DailyChallenge> DailyChallenges { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.Property(e => e.Username).HasMaxLength(50).IsRequired();
            entity.Property(e => e.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.LastName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.TechStack).HasMaxLength(100).IsRequired();
            entity.Property(e => e.ExperienceLevel).HasConversion<string>();
            entity.Property(e => e.ZodiacSign).HasConversion<string>();

            entity.Property(e => e.BirthDate).HasColumnType("date");
            entity.Property(e => e.CreatedAt).HasColumnType("timestamp with time zone");
        });

        modelBuilder.Entity<Score>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId);
            entity.HasIndex(e => new { e.UserId, e.GameType });
            entity.Property(e => e.CreatedAt).HasColumnType("timestamp with time zone");
        });

        modelBuilder.Entity<Session>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.SessionToken).IsUnique();
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId);
            entity.Property(e => e.ExpiresAt).HasColumnType("timestamp with time zone");
            entity.Property(e => e.CreatedAt).HasColumnType("timestamp with time zone");
        });

        modelBuilder.Entity<CasinoChallenge>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TechStack).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.CodeSnippet1).HasColumnType("text");
            entity.Property(e => e.CodeSnippet2).HasColumnType("text");
            entity.Property(e => e.Explanation).HasColumnType("text");
            entity.Property(e => e.Difficulty).HasConversion<string>();
            entity.Property(e => e.CreatedAt).HasColumnType("timestamp with time zone");
        });

        modelBuilder.Entity<CasinoGame>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId);
            entity.HasOne(e => e.Challenge)
                  .WithMany()
                  .HasForeignKey(e => e.ChallengeId);
            entity.Property(e => e.PlayedAt).HasColumnType("timestamp with time zone");
        });

        modelBuilder.Entity<UserStats>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId);
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.Property(e => e.LastPlayedAt).HasColumnType("timestamp with time zone");
        });

        modelBuilder.Entity<DailyChallenge>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Challenge)
                  .WithMany()
                  .HasForeignKey(e => e.ChallengeId);
            entity.HasIndex(e => e.Date);
            entity.Property(e => e.Date).HasColumnType("date");
        });
    }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.Entity is User user && entry.State == EntityState.Added)
            {
                user.CreatedAt = DateTime.UtcNow;
            }
            if (entry.Entity is Score score && entry.State == EntityState.Added)
            {
                score.CreatedAt = DateTime.UtcNow;
            }
            if (entry.Entity is Session session && entry.State == EntityState.Added)
            {
                session.CreatedAt = DateTime.UtcNow;
            }
            if (entry.Entity is CasinoChallenge challenge && entry.State == EntityState.Added)
            {
                challenge.CreatedAt = DateTime.UtcNow;
            }
            if (entry.Entity is CasinoGame game && entry.State == EntityState.Added)
            {
                game.PlayedAt = DateTime.UtcNow;
            }
            if (entry.Entity is UserStats stats && entry.State == EntityState.Added)
            {
                stats.LastPlayedAt = DateTime.UtcNow;
            }
        }
    }
}