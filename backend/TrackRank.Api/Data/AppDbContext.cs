using Microsoft.EntityFrameworkCore;
using TrackRank.Api.Models;

namespace TrackRank.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Athlete> Athletes => Set<Athlete>();
    public DbSet<Meet> Meets => Set<Meet>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Result> Results => Set<Result>();
    public DbSet<ImportHistory> ImportHistories => Set<ImportHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Athlete>(entity =>
        {
            entity.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.LastName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Gender).HasMaxLength(20).IsRequired();
        });

        modelBuilder.Entity<Meet>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Location).HasMaxLength(200);
        });

        modelBuilder.Entity<Event>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.Property(x => x.EventType).HasMaxLength(20).IsRequired();
        });

        modelBuilder.Entity<Result>(entity =>
        {
            entity.Property(x => x.Performance).HasColumnType("numeric(10,3)");
            entity.Property(x => x.Wind).HasColumnType("numeric(5,2)");
            entity.Property(x => x.SourceType).HasMaxLength(30).IsRequired();
        });

        modelBuilder.Entity<ImportHistory>(entity =>
        {
            entity.Property(x => x.FileName).HasMaxLength(255).IsRequired();
        });
    }
}