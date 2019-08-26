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
        }
    }
}
