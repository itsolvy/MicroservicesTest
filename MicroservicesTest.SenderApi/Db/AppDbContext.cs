using Microsoft.EntityFrameworkCore;

namespace MicroservicesTest.SenderApi.Db
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Order> Orders { get; set; }
    }

}
