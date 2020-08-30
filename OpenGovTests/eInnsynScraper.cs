using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using OpenGov.Models;
using OpenGov.Scrapers;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OpenGovTests
{
    [TestClass]
    public class eInnsynScraper
    {
        [TestMethod]
        public async Task FindNew()
        {
            string query = JsonConvert.SerializeObject(new { size = 50, aggregations = new { contentTypes = "type", virksomheter = "arkivskaperTransitive" }, appliedFilters = new[] { new { fieldName = "type", fieldValue = new string[] { "Moetemappe" }, type = "termQueryFilter" }, new { fieldName = "type", fieldValue = new string[] { "JournalpostForMøte" }, type = "notQueryFilter" }, new { fieldName = "arkivskaperTransitive", fieldValue = new string[] { "http://data.oslo.kommune.no/virksomhet/osloKommune" }, type = "postQueryFilter" } } });

            IScraper scraper = new OpenGov.Scrapers.eInnsyn("https://einnsyn.no/api/result", query);

            var meetings = await scraper.FindMeetings(null, new HashSet<string>());

            Assert.IsNotNull(meetings);

            var meetingsList = new List<Meeting>(meetings);

            Assert.IsTrue(meetingsList.Count > 0);

            var meeting = meetingsList[0];

            //var documents = new List<Document>(await scraper.GetDocuments(meeting));

            //Assert.IsTrue(documents.Count > 0);
        }
    }
}
