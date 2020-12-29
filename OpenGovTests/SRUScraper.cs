using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenGov.Models;
using OpenGov.Scrapers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OpenGovTests
{
    [TestClass]
    public class SRUScraper
    {
        [TestMethod]
        public async Task FindNew()
        {
            IScraper scraper = new OpenGov.Scrapers.SRU(new Uri("https://sru23.porsgrunn.kommune.no"));

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
