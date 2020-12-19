using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenGov.Models;
using OpenGov.Scrapers;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OpenGovTests
{
    [TestClass]
    public class OpenGovScraper
    {
        [TestMethod]
        public async Task FindNew()
        {
            IScraper scraper = new OpenGov.Scrapers.OpenGov("STAVANGER");

            var meetings = await scraper.FindMeetings(null, new HashSet<string>());

            Assert.IsNotNull(meetings);

            var meetingsList = new List<Meeting>(meetings);

            Assert.IsTrue(meetingsList.Count > 0);

            var meeting = meetingsList[0];

            var documents = new List<Document>(await scraper.GetDocuments(meeting));

            Assert.IsTrue(documents.Count > 0);
        }

        [TestMethod]
        public async Task FindPhrase()
        {
            IScraper scraper = new OpenGov.Scrapers.OpenGov("STAVANGER");

            var meetings = await scraper.FindMeetings("sykkel", new HashSet<string>());

            Assert.IsNotNull(meetings);

            var meetingsList = new List<Meeting>(meetings);

            Assert.IsTrue(meetingsList.Count > 0);

            var meeting = meetingsList[0];

            var documents = new List<Document>(await scraper.GetDocuments(meeting));

            Assert.IsTrue(documents.Count > 0);
        }
    }
}
