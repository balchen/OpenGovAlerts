using Microsoft.VisualStudio.TestTools.UnitTesting;
using PoliticalAlerts.Models;
using PoliticalAlerts.Scrapers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PoliticalAlertsTests
{
    [TestClass]
    public class JupiterScraper
    {
        [TestMethod]
        public async Task FindNew()
        {
            Jupiter scraper = new PoliticalAlerts.Scrapers.Jupiter("https://einnsyn.mrfylke.no/");

            var meetings = await scraper.GetNewMeetings(new HashSet<string>());

            Assert.IsNotNull(meetings);

            var meetingsList = new List<Meeting>(meetings);

            Assert.IsTrue(meetingsList.Count > 0, "No meetings found");

            var meeting = meetingsList[0];

            Assert.IsTrue(meeting.AgendaItems.Count > 0, "No agenda items found");

            Assert.IsTrue(meeting.AgendaItems.Any(a => a.Documents.Count > 0), "No documents founds");
        }

        [TestMethod]
        public async Task GetCaseDocuments()
        {
            var caseNumber = "16/01297";

            Jupiter scraper = new PoliticalAlerts.Scrapers.Jupiter("https://einnsyn.mrfylke.no/");
            
            var entries = new List<JournalEntry>(await scraper.GetCaseJournal(caseNumber));

            Assert.IsTrue(entries.Count > 0);
            Assert.IsTrue(entries[0].Documents.Count > 0);
        }
    }
}