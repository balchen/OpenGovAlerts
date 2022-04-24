using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PoliticalAlertsWeb.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace PoliticalAlertsWebTests
{
    public class Environment
    {
        private IServiceProvider provider = null;

        private DbContextOptions<AlertsDbContext> options;

        public Environment()
        {
            var services = new ServiceCollection();

            options = GetDbContextOptions();

            services.AddTransient<AlertsDbContext, AlertsDbContext>(sp => new AlertsDbContext(options)); // use Transient instances so we can test our database actions on a new instance of the context (but with the same underlying in-memory database)

            provider = services.BuildServiceProvider();
        }

        public AlertsDbContext Db
        {
            get
            {
                return provider.GetService<AlertsDbContext>();
            }
        }

        private static DbContextOptions<AlertsDbContext> GetDbContextOptions()
        {
            var services = new ServiceCollection();

            services.AddEntityFrameworkInMemoryDatabase();

            var provider = services.BuildServiceProvider();

            // Create a new options instance telling the context to use an
            // InMemory database and the new service provider.
            var builder = new DbContextOptionsBuilder<AlertsDbContext>();
            builder.UseInMemoryDatabase("alertsdb")
                   .UseInternalServiceProvider(provider);

            return builder.Options;
        }
    }
}