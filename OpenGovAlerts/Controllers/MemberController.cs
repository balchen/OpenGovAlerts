using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenGovAlerts.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace OpenGovAlerts.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MemberController: ControllerBase
    {
        private AlertsDbContext db;

        public MemberController(AlertsDbContext db)
        {
            this.db = db;
        }

        public async Task<ActionResult> GetIndex()
        {
            MemberIndexModel result = new MemberIndexModel();

            result.Observers = await db.Observers.Include(o => o.Searches).ToListAsync();
            result.RecentMatches = await db.Matches
                .Include(m => m.Search).ThenInclude(s => s.Observer)
                .Include(m => m.Meeting).ThenInclude(m => m.Source)
                .Where(m => m.TimeFound > DateTime.UtcNow.Subtract(TimeSpan.FromDays(7)) && result.Observers.Contains(m.Search.Observer)).ToListAsync();

            return Ok(result);
        }
    }
}
