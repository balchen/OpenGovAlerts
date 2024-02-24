using Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SpaServices.ReactDevelopmentServer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using PoliticalAlertsWeb.Models;
using PoliticalAlertsWeb.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Linq;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using PoliticalAlertsWeb.Settings;

namespace PoliticalAlertsWeb
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var authSettingsSection = Configuration.GetSection("Authentication");
            services.Configure<AuthenticationSettings>(authSettingsSection);
            var authSettings = authSettingsSection.Get<AuthenticationSettings>();

            var key = Encoding.ASCII.GetBytes(authSettings.Secret);

            services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(x =>
            {
                x.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var db = context.HttpContext.RequestServices.GetRequiredService<AlertsDbContext>();
                        var userId = int.Parse(context.Principal.Identity.Name ?? string.Empty);
                        var roleId = int.Parse(context.Principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value ?? "0");

                        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
                        if (user == null || roleId != (int)user.Role)
                        {
                            // return unauthorized if user no longer exists
                            context.Fail("Unauthorized");
                        }
                    }
                };
                x.RequireHttpsMetadata = false;
                x.SaveToken = true;
                x.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true
                };
            });

            services.AddMvc()
                .AddMvcOptions(options => { 
                    options.EnableEndpointRouting = false; 
                });

            services.AddDbContext<AlertsDbContext>(options =>
                options.UseSqlServer(Configuration["ConnectionString"]));

            services.AddTransient<SyncService>();

            services.AddHangfire(x => x.UseSqlServerStorage(Configuration["ConnectionString"]));

            services.AddHangfireServer(options =>
            {
                options.WorkerCount = 1;
            });

            // In production, the React files will be served from this directory
            services.AddSpaStaticFiles(configuration =>
            {
                configuration.RootPath = "ClientApp/build";
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, AlertsDbContext db, SyncService sync)
        {
            bool isDevelopment = env.EnvironmentName.ToLower() == "development";

            if (isDevelopment)
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseStaticFiles();
            app.UseSpaStaticFiles();

            app.UseHangfireDashboard("/hangfire", new DashboardOptions
            {
                StatsPollingInterval = 60,
                Authorization = new[] { new HangfireDashboardAuthorizationFilter() }
            });

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller}/{action=Index}/{id?}");
            });

            app.UseSpa(spa =>
            {
                spa.Options.SourcePath = "ClientApp";

                if (isDevelopment)
                {
                    spa.UseReactDevelopmentServer(npmScript: "start");
                }
            });

            db.UpdateSchema().GetAwaiter().GetResult();

            sync.ScheduleSynchronization();
        }
    }
}