using Microsoft.VisualStudio.TestTools.UnitTesting;
using PoliticalAlerts.Models;
using PoliticalAlerts.Scrapers;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace PoliticalAlertsTests
{
    [TestClass]
    public class ElementsScraper
    {
        [TestMethod]
        public async Task FindNew()
        {
            IScraper scraper = new PoliticalAlerts.Scrapers.Elements("971045698");

            var meetings = await scraper.GetNewMeetings(new HashSet<string>());

            Assert.IsNotNull(meetings);

            var meetingsList = new List<Meeting>(meetings);

            Assert.IsTrue(meetingsList.Count > 0);

            var meeting = meetingsList[0];

            var documents = new List<Document>(await scraper.GetDocuments(meeting.AgendaItems[0]));

            Assert.IsTrue(documents.Count > 0);
        }
    }
}
