using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OpenGov.Models;

namespace OpenGovAlerts.Models
{
    public class AlertsDbContext : DbContext
    {
        private string connectionString;

        public AlertsDbContext(IConfiguration configuration)
        {
            this.connectionString = configuration["ConnectionString"];
        }

        public DbSet<Meeting> Meetings { get; set; }
        public DbSet<Source> Sources { get; set; }
        public DbSet<Document> Documents { get; set; }
        public DbSet<Observer> Observer { get; set; }
        public DbSet<Search> Searches { get; set; }
        public DbSet<Match> Matches { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(connectionString);
        }

        public async virtual void UpdateSchema()
        {
            await Database.MigrateAsync();
        }
    }
}
