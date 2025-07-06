using CarbonZero.Models;
using CarbonZero.Data;
using System.Linq;

namespace CarbonZero.Data
{
    public static class SeedSuperAdmin
    {
        public static void Initialize(ApplicationDbContext context)
        {
            if (!context.Users.Any(u => u.Username == "superadmin"))
            {
                var superadmin = new User
                {
                    Username = "superadmin",
                    Password = "SuperSecretPassword123!",
                    IdNo = "0000",
                    PowerCapacityKW = 0
                };
                context.Users.Add(superadmin);
                context.SaveChanges();
            }
        }
    }
}
