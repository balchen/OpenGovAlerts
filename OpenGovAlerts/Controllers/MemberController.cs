using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenGov.Models;
using OpenGovAlerts.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace OpenGovAlerts.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Produces("application/json")]
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

            DateTime matchesSince = DateTime.UtcNow.Subtract(TimeSpan.FromDays(7));

            result.Observers = await db.Observers.Include(o => o.CreatedSearches).ToListAsync();
            result.RecentMatches = await db.Matches
                .Include(m => m.Search).ThenInclude(s => s.CreatedBy)
                .Include(m => m.AgendaItem).ThenInclude(a => a.Meeting).ThenInclude(m => m.Source)
                .Where(m => m.TimeFound > matchesSince && result.Observers.Contains(m.Search.CreatedBy))
                .Take(10)
                .ToListAsync();

            return Ok(result);
        }

        public async Task<ActionResult> GetObserver(int id)
        {
            var result = new ViewObserverModel();
            result.Observer = await db.Observers.Include(o => o.CreatedSearches).FirstOrDefaultAsync(o => o.Id == id);

            return Ok(result);
        }

        public async Task<ActionResult> AddObserver(ViewObserverModel model)
        {
            var newObserver = db.Observers.Add(model.Observer);

            await db.SaveChangesAsync();

            return Ok(newObserver.Entity);
        }

        public async Task<ActionResult> GetSearch(int id)
        {
            var result = new ViewSearchModel();
            result.Search = await db.Searches
                .Include(s => s.Sources).ThenInclude(ss => ss.Source)
                .FirstOrDefaultAsync(o => o.Id == id);

            result.RecentMatches = await db.Matches
                .Include(m => m.Search)
                .Include(m => m.AgendaItem).ThenInclude(a => a.Meeting).ThenInclude(m => m.Source)
                .Where(m => m.TimeFound > DateTime.UtcNow.Subtract(TimeSpan.FromDays(7)) && m.Search.Id == id)
                .OrderByDescending(m => m.AgendaItem.Meeting.Date)
                .Take(10)
                .ToListAsync();

            result.Sources = (await db.Sources.ToListAsync()).Select(s => new ViewSearchSource { Source = s, Selected = result.Search.Sources.Any(ss => ss.Source.Id == s.Id) }).ToList();

            result.Search.Sources = null;

            return Ok(result);
        }

        public async Task<ActionResult> GetMatches(int searchId)
        {
            var result = new ViewMatchesModel();

            result.Matches = await db.Matches
                .Include(m => m.Search)
                .Include(m => m.AgendaItem).ThenInclude(a => a.Meeting).ThenInclude(m => m.Source)
                .Where(m => m.Search.Id == searchId)
                .ToListAsync();

            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult> UpdateSearch(int id, [FromBody] ViewSearchModel updated)
        {
            var search = await db.Searches.Include(s => s.Sources).FirstOrDefaultAsync(s => s.Id == id);

            search.Name = updated.Search.Name;
            search.Phrase = updated.Search.Phrase;

            foreach (ViewSearchSource searchSource in updated.Sources)
            {
                SearchSource sourceAlreadySelected = search.Sources.FirstOrDefault(ss => ss.SourceId == searchSource.Source.Id);

                if (searchSource.Selected)
                {
                    if (sourceAlreadySelected == null)
                    {
                        await db.SearchSources.AddAsync(new SearchSource { SearchId = search.Id, SourceId = searchSource.Source.Id });
                    }
                }
                else
                {
                    if (sourceAlreadySelected != null)
                    {
                        db.SearchSources.Remove(sourceAlreadySelected);
                    }
                }
            }

            await db.SaveChangesAsync();

            return await GetSearch(id);
        }
    }
}