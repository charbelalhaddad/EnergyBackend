using EnergyBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace EnergyBackend.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<EnergyReading> Readings => Set<EnergyReading>();
        public DbSet<DailyAverage> DailyAverages => Set<DailyAverage>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Speed up queries and enforce one average per day per source
            modelBuilder.Entity<EnergyReading>().HasIndex(r => r.TimestampUtc);

            modelBuilder.Entity<DailyAverage>()
                .HasIndex(a => new { a.Date, a.Source })
                .IsUnique();
        }
    }
}
