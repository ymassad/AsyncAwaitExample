using Microsoft.EntityFrameworkCore;

namespace TransactionWebService1
{
    public class DatabaseContext : DbContext
    {
        public DatabaseContext(string connectionString)
            :base(Create(connectionString))
        {
            
        }
        
        private static DbContextOptions Create(string connectionString)
        {
            var optionsBuilder = new DbContextOptionsBuilder<DatabaseContext>();
            optionsBuilder.UseSqlServer(connectionString);

            return optionsBuilder.Options;
        }
        
        public DbSet<DataPoint> DataPoints { get; set; }
    }
}
