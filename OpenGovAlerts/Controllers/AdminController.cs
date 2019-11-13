using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenGovAlerts.Models;

namespace OpenGovAlerts.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private AlertsDbContext db;

        public AdminController(AlertsDbContext db)
        {
            this.db = db;
        }

        public async Task<ActionResult> GetSources()
        {
            return Ok(await db.Sources.ToListAsync());
        }

        public async Task<ActionResult> GetObservers()
        {
            return Ok(await db.Observers.ToListAsync());
        }

        public async Task<ActionResult> GetMeetings(int sourceId)
        {
            return Ok(await db.Matches.ToListAsync());
        }

    }
}