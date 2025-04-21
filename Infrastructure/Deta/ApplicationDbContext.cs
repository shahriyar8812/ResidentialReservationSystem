using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Deta
{
    using Domain.Entities;
    using Microsoft.EntityFrameworkCore;

    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // مجموعه جداولی که در پایگاه داده داریم
        public DbSet<Complex> Complexes { get; set; }
        public DbSet<Unit> Units { get; set; }
        public DbSet<UnitImage> UnitImages { get; set; }
        public DbSet<Feature> Features { get; set; }
        public DbSet<UnitFeature> UnitFeatures { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Reservation> Reservations { get; set; }
        public DbSet<Rate> Rates { get; set; }
        public DbSet<CheckInRule> CheckInRules { get; set; }
    }

}
