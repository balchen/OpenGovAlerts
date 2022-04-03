using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenGov.Models;
using OpenGov.Scrapers;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OpenGovTests
{
    [TestClass]
    public class ElementsScraper
    {
        [TestMethod]
        public async Task FindNew()
        {
            IScraper scraper = new OpenGov.Scrapers.Elements("971045698");

            var meetings = await scraper.FindMeetings(null, new HashSet<string>());

            Assert.IsNotNull(meetings);

            var meetingsList = new List<Meeting>(meetings);

            Assert.IsTrue(meetingsList.Count > 0);

            var meeting = meetingsList[0];

            var documents = new List<Document>(await scraper.GetDocuments(meeting));

            Assert.IsTrue(documents.Count > 0);
        }
    }
}
