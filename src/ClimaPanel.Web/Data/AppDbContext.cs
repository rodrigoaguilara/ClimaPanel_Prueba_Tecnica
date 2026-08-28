using ClimaPanel.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace ClimaPanel.Web.Data;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<FavoriteCity> FavoriteCities => Set<FavoriteCity>();
    // se configura persistencia de alertas
    public DbSet<WeatherAlert> WeatherAlerts => Set<WeatherAlert>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FavoriteCity>(entity =>
        {
            entity.ToTable("FavoriteCities");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.UserId).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(150).IsRequired();
            entity.Property(x => x.Country).HasMaxLength(100).IsRequired();
            entity.Property(x => x.CountryCode).HasMaxLength(2).IsRequired();
            entity.Property(x => x.Timezone).HasMaxLength(100).IsRequired();
            entity.HasIndex(x => x.UserId);
            //se modifica la restirccion para garantizar la integridad
            //entity.HasIndex(x => new { x.UserId, x.LocationId });
            entity.HasIndex(x => new { x.UserId, x.LocationId })
                .IsUnique();
        });

        // se configura persistencia de alertas
        modelBuilder.Entity<WeatherAlert>(entity =>
        {
            entity.ToTable("WeatherAlerts");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Metric).IsRequired();
            entity.Property(x => x.Operator).IsRequired();
            entity.Property(x => x.Threshold).IsRequired();
            entity.Property(x => x.IsEnabled).IsRequired();
            entity.Property(x => x.IsTriggered).IsRequired();
            entity.Property(x => x.CreatedAtUtc).IsRequired();

            entity.HasIndex(x => x.FavoriteId);

            entity.HasOne<FavoriteCity>()
                .WithMany()
                .HasForeignKey(x => x.FavoriteId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
