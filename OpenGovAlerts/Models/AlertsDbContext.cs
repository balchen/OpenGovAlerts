using Microsoft.EntityFrameworkCore;
using OpenGov.Models;

namespace OpenGovAlerts.Models
{
    public class AlertsDbContext : DbContext
    {
        public DbSet<Meeting> Meetings { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(@"Server=.\;Database=EFCoreWebDemo;Trusted_Connection=True;MultipleActiveResultSets=true");
        }
    }
}
