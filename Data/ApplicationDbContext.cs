using Microsoft.EntityFrameworkCore;
using CarbonZero.Models;

namespace CarbonZero.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<SessionHistory> SessionHistories { get; set; }
    }
}
