using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PoliticalAlerts.Models;
using PoliticalAlerts.Scrapers;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace PoliticalAlertsTests
{
    [TestClass]
    public class eInnsynScraper
    {
        [TestMethod]
        public async Task FindNew()
        {
            IScraper scraper = new PoliticalAlerts.Scrapers.eInnsyn("https://einnsyn.no/api/result", "http://data.einnsyn.no/virksomhet/96bcff13-d0d4-43fd-8ea4-f9e975cdbed0");

            var meetings = await scraper.GetNewMeetings(new HashSet<string>());

            Assert.IsNotNull(meetings);

            var meetingsList = new List<Meeting>(meetings);

            Assert.IsTrue(meetingsList.Count > 0);

            var meeting = meetingsList[0];

            //var documents = new List<Document>(await scraper.GetDocuments(meeting));

            //Assert.IsTrue(documents.Count > 0);
        }
    }
}
