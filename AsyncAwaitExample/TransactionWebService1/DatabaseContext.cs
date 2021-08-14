using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace TransactionWebService
{
    public class DatabaseContext : DbContext
    {
        public DatabaseContext(string connectionString)
            :base(Create1(connectionString))
        {
            
        }
        
        private static DbContextOptions Create1(string connectionString)
        {
            var optionsBuilder = new DbContextOptionsBuilder<DatabaseContext>();
            optionsBuilder.UseSqlServer(connectionString);

            return optionsBuilder.Options;
        }
        
        public DbSet<DataPoint> DataPoints { get; set; }
    }
}
