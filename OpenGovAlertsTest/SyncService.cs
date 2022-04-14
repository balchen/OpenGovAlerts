using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenGov.Models;
using OpenGovAlerts.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OpenGovAlertsTests
{
    [TestClass]
    public class SyncService
    {
        [TestMethod]
        public async Task Synchronize()
        {
            Environment e = new Environment();

            AlertsDbContext db = e.Db;

            var stavanger = db.Sources.Add(new Source { Name = "Stavanger kommune", Url = "opengov:stavanger" }).Entity;
            var sandnes = db.Sources.Add(new Source { Name = "Sandnes kommune", Url = "opengov:sandnes" }).Entity;

            Observer slf = new Observer { Name = "Rogaland syklistforening", Emails = new string[] { "post@rogalandsyklistforening.no" }, SmtpServer = "mail.syklistene.no", SmtpPassword = "", SmtpSender = "nord-jaren@syklistene.no", SmtpPort = 587, SmtpUseSsl = true };

            db.Observers.Add(slf);

            var search = db.Searches.Add(new Search { CreatedBy = slf, Name = "Sykkel", Phrase = "sykkel", Start = DateTime.UtcNow }).Entity;

            search.Sources = new List<SearchSource>() { new SearchSource { Source = stavanger, Search = search }, new SearchSource { Source = sandnes, Search = search } };

            await db.SaveChangesAsync();

            OpenGovAlerts.Services.SyncService service = new OpenGovAlerts.Services.SyncService(db);
            await service.Synchronize();

            Assert.IsTrue(db.Meetings.Any(), "No meetings in DB");
            Assert.IsTrue(db.Matches.Any(), "No matches in DB");

            stavanger = db.Sources.Include(s => s.Meetings).FirstOrDefault(s => s.Name == "Stavanger kommune");
            Assert.IsTrue(stavanger.Meetings.Any(), "No meetings for Stavanger kommune in DB");

            sandnes = db.Sources.Include(s => s.Meetings).FirstOrDefault(s => s.Name == "Sandnes kommune");
            Assert.IsTrue(sandnes.Meetings.Any(), "No seen meeting for Sandnes kommune in DB");
        }
    }
}
