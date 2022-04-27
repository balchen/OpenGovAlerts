using Microsoft.VisualStudio.TestTools.UnitTesting;
using PoliticalAlerts.Models;
using PoliticalAlerts.Scrapers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PoliticalAlertsTests
{
    [TestClass]
    public class ACOSScraper
    {
        [TestMethod]
        public async Task FindNew()
        {
            PoliticalAlerts.Scrapers.ACOS scraper = new PoliticalAlerts.Scrapers.ACOS(new Uri("http://nyttinnsyn.sola.kommune.no/wfinnsyn.ashx"));

            var meetings = await scraper.GetNewMeetings(new HashSet<string>());

            Assert.IsNotNull(meetings);

            var meetingsList = new List<Meeting>(meetings);

            Assert.IsTrue(meetingsList.Count > 0);

            var meeting = meetingsList[0];

            var documents = new List<Document>(await scraper.GetDocuments(meeting.AgendaItems[0]));

            Assert.IsTrue(documents.Count > 0);
        }

        [TestMethod]
        public async Task GetCaseDocuments()
        {
            var caseNumber = "2020000951";

            PoliticalAlerts.Scrapers.ACOS scraper = new PoliticalAlerts.Scrapers.ACOS(new Uri("https://innsyn.onacos.no/eigersund/mote/wfinnsyn.ashx"));

            var entries = new List<JournalEntry>(await scraper.GetCaseJournal(caseNumber));

            Assert.IsTrue(entries.Count > 0);
            Assert.IsTrue(entries[0].Documents.Count > 0);
        }
    }
}
