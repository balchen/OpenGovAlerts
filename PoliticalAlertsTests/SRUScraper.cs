﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using PoliticalAlerts.Models;
using PoliticalAlerts.Scrapers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PoliticalAlertsTests
{
    [TestClass]
    public class SRUScraper
    {
        [TestMethod]
        public async Task FindNew()
        {
            PoliticalAlerts.Scrapers.SRU scraper = new PoliticalAlerts.Scrapers.SRU(new Uri("https://sru23.porsgrunn.kommune.no"));

            var meetings = await scraper.GetNewMeetings(new HashSet<string>());

            Assert.IsNotNull(meetings);

            var meetingsList = new List<Meeting>(meetings);

            Assert.IsTrue(meetingsList.Count > 0);
            Assert.IsTrue(meetingsList.Any(m => m.AgendaItems.Any(a => a.CaseNumber != null)), "No case numbers found");

            var meeting = meetingsList[0];

            var documents = new List<Document>(await scraper.GetDocuments(meeting.AgendaItems[0]));

            Assert.IsTrue(documents.Count > 0);
        }

        [TestMethod]
        public async Task GetCaseDocuments()
        {
            var caseNumber = "16/01297";

            PoliticalAlerts.Scrapers.SRU scraper = new PoliticalAlerts.Scrapers.SRU(new Uri("https://sru23.porsgrunn.kommune.no"));

            var entries = new List<JournalEntry>(await scraper.GetCaseJournal(caseNumber));

            Assert.IsTrue(entries.Count > 0);
            Assert.IsTrue(entries[0].Documents.Count > 0);
        }
    }
}