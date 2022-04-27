using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using PoliticalAlerts.Models;
using PoliticalAlertsWeb.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PoliticalAlertsWebTests
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

            Observer slf = new Observer { Name = "Rogaland syklistforening", Emails = new string[] { "post@rogalandsyklistforening.no" }, SmtpServer = "outlook.office365.com", SmtpPassword = "", SmtpSender = "nord-jaren@syklistforeningen.no", SmtpPort = 587, SmtpUseSsl = true };

            db.Observers.Add(slf);

            var search = db.Searches.Add(new Search { CreatedBy = slf, Name = "Vei", Phrase = "vei", Start = DateTime.UtcNow }).Entity;

            db.SearchSources.Add(new SearchSource { Source = stavanger, Search = search });
            db.SearchSources.Add(new SearchSource { Source = sandnes, Search = search });
            db.ObserverSearches.Add(new ObserverSearch { Observer = slf, Search = search, Activated = DateTime.UtcNow });

            await db.SaveChangesAsync();

            var logger = new Mock<ILogger<PoliticalAlertsWeb.Services.SyncService>>();

            PoliticalAlertsWeb.Services.SyncService service = new PoliticalAlertsWeb.Services.SyncService(db, logger.Object);
            await service.Synchronize();

            Assert.IsTrue(db.Meetings.Any(), "No meetings in DB");
            Assert.IsTrue(db.Matches.Any(), "No matches in DB");

            stavanger = db.Sources.Include(s => s.Meetings).FirstOrDefault(s => s.Name == "Stavanger kommune");
            Assert.IsTrue(stavanger.Meetings.Any(), "No meetings for Stavanger kommune in DB");

            sandnes = db.Sources.Include(s => s.Meetings).FirstOrDefault(s => s.Name == "Sandnes kommune");
            Assert.IsTrue(sandnes.Meetings.Any(), "No seen meeting for Sandnes kommune in DB");
        }

        [TestMethod]
        public async Task UpdateJournal()
        {
            Environment e = new Environment();

            AlertsDbContext db = e.Db;

            var stavanger = db.Sources.Add(new Source { Name = "Stavanger kommune", Url = "opengov:stavanger" }).Entity;

            Observer slf = new Observer { Name = "Rogaland syklistforening", Emails = new string[] { "post@rogalandsyklistforening.no" }, SmtpServer = "outlook.office365.com", SmtpPassword = "", SmtpSender = "nord-jaren@syklistforeningen.no", SmtpPort = 587, SmtpUseSsl = true };

            db.Observers.Add(slf);

            var consultationSearch = db.ConsultationSearches.Add(new ConsultationSearch { CreatedBy = slf, Name = "Vei", Phrase = "ettersyn", Start = DateTime.UtcNow }).Entity;

            db.ConsultationSearchSources.Add(new ConsultationSearchSource { Source = stavanger, ConsultationSearch = consultationSearch });
            db.ObserverConsultationSearches.Add(new ObserverConsultationSearch { Observer = slf, ConsultationSearch = consultationSearch, Activated = DateTime.UtcNow });

            var meeting = db.Meetings.Add(new Meeting
            {
                Source = stavanger,
                BoardName = "Test",
                BoardId = "test",
                Date = DateTime.Today,
                ExternalId = "1234",
                Url = new Uri("https://meeting.no")
            }).Entity;

            var agendaItem = db.AgendaItems.Add(new AgendaItem
            {
                Meeting = meeting,
                Title = "Agenda item",
                Retrieved = DateTime.UtcNow,
                CaseNumber = "16/01297",
                MonitorConsultations = true
            }).Entity;

            await db.SaveChangesAsync();

            var logger = new Mock<ILogger<PoliticalAlertsWeb.Services.SyncService>>();

            PoliticalAlertsWeb.Services.SyncService service = new PoliticalAlertsWeb.Services.SyncService(db, logger.Object);
            await service.Synchronize();

            Assert.IsTrue(db.ConsultationMatches.Any(), "No consultation matches");
        }
    }
}