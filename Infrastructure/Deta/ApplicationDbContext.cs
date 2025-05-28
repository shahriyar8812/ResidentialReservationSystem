using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
            System.Console.WriteLine("ApplicationDbContext: Initialized.");
        }

        public DbSet<Complex> Complexes { get; set; }
        public DbSet<Unit> Units { get; set; }
        public DbSet<UnitImage> UnitImages { get; set; }
        public DbSet<Feature> Features { get; set; }
        public DbSet<UnitFeature> UnitFeatures { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Reservation> Reservations { get; set; }
        public DbSet<Rate> Rates { get; set; }
        public DbSet<CheckInRule> CheckInRules { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            System.Console.WriteLine("ApplicationDbContext: Configuring model...");

            modelBuilder.Entity<Rate>()
                .Property(r => r.PricePerNight)
                .HasColumnType("decimal(18,2)");
            System.Console.WriteLine("ApplicationDbContext: Configured decimal type for Rate.PricePerNight.");

            modelBuilder.Entity<Reservation>()
                .Property(r => r.TotalPrice)
                .HasColumnType("decimal(18,2)");
            System.Console.WriteLine("ApplicationDbContext: Configured decimal type for Reservation.TotalPrice.");

            modelBuilder.Entity<UnitFeature>()
                .HasKey(uf => new { uf.UnitId, uf.FeatureId });
            System.Console.WriteLine("ApplicationDbContext: Configured composite key for UnitFeature.");

            modelBuilder.Entity<UnitFeature>()
                .HasOne(uf => uf.Unit)
                .WithMany(u => u.UnitFeatures)
                .HasForeignKey(uf => uf.UnitId);
            System.Console.WriteLine("ApplicationDbContext: Configured UnitFeature to Unit relationship.");

            modelBuilder.Entity<UnitFeature>()
                .HasOne(uf => uf.Feature)
                .WithMany(f => f.UnitFeatures)
                .HasForeignKey(uf => uf.FeatureId);
            System.Console.WriteLine("ApplicationDbContext: Configured UnitFeature to Feature relationship.");

            modelBuilder.Entity<CheckInRule>()
                .HasOne(r => r.Unit)
                .WithOne()
                .HasForeignKey<CheckInRule>(r => r.UnitId)
                .OnDelete(DeleteBehavior.Cascade);
            System.Console.WriteLine("ApplicationDbContext: Configured CheckInRule to Unit relationship with cascade delete.");

            modelBuilder.Entity<Feature>().HasData(
                new Feature { Id = 1, Name = "Wi-Fi" },
                new Feature { Id = 2, Name = "Parking" },
                new Feature { Id = 3, Name = "Pool" },
                new Feature { Id = 4, Name = "Air Conditioning" }
            );
            System.Console.WriteLine("ApplicationDbContext: Seeded Features data.");
        }
    }
}