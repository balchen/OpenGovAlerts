using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using HtmlAgilityPack;
using OpenGov.Models;

namespace OpenGov.Scrapers
{
    public class Elements : IScraper
    {
        public Uri baseUrl;

        public Elements(Uri baseUrl)
        {
            this.baseUrl = baseUrl;
        }

        public async Task<IEnumerable<Meeting>> GetNewMeetings(ISet<string> seenAgendaItems)
        {
            HttpClient http = new HttpClient();
            string html = await http.GetStringAsync(baseUrl);

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);

            List<Meeting> newMeetings = new List<Meeting>();

            var calendarTable = doc.DocumentNode.SelectSingleNode("//table[@class='calendar-table']");

            if (calendarTable == null)
            {
                throw new ArgumentException("Invalid HTML document for " + baseUrl.ToString() + "no calendar-table found");
            }

            var meetingLinks = calendarTable.SelectNodes("descendant::a[@class='calendar-link']");

            if (meetingLinks != null)
            {
                foreach (var meetingLink in meetingLinks)
                {
                    var meetingUrl = new Uri(baseUrl, HttpUtility.HtmlDecode(meetingLink.Attributes["href"].Value));

                    var meeting = await GetMeeting(seenAgendaItems, http, meetingUrl);

                    if (meeting != null)
                        newMeetings.Add(meeting);
                }
            }

            return newMeetings;
        }

        private async Task<Meeting> GetMeeting(ISet<string> seenAgendaItems, HttpClient http, Uri url)
        {
            string html = await http.GetStringAsync(url);

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);

            var meetingTable = doc.DocumentNode.SelectSingleNode("//table[@id='innsynListTables']");

            if (meetingTable == null)
                throw new ArgumentException("No innsynListTables found at " + url.ToString());

            string meetingId = HttpUtility.ParseQueryString(url.Query).Get("MeetingId");
            DateTime date = DateTime.ParseExact(meetingTable.SelectSingleNode("descendant::td[@headers='utvDate']/span").InnerText.Trim(), "yyyyMMdd", CultureInfo.CurrentCulture);
            string boardName = meetingTable.SelectSingleNode("descendant::td[@headers='DmbName']").InnerText;

            var meeting = new Meeting { Date = date, BoardName = boardName, ExternalId = meetingId, Url = url };

            List<AgendaItem> items = new List<AgendaItem>();

            var caseTable = doc.DocumentNode.SelectSingleNode("//table[@id='innsynListTableSakskart']");

            if (caseTable == null)
                throw new ArgumentException("No innsynListTableSakskart found at " + url.ToString());

            var caseRows = caseTable.SelectNodes("descendant::tbody/tr");

            if (caseRows != null)
            {
                foreach (var caseRow in caseRows)
                {
                    var caseLink = caseRow.SelectSingleNode("descendant::td[@headers='utvCasesTypeBehCasesTittel']/a");
                    string title = caseLink.InnerText.Trim();
                    Uri caseUrl = new Uri(url, HttpUtility.HtmlDecode(caseLink.Attributes["href"].Value));

                    var docsLink = caseRow.SelectSingleNode("descendant::td[@headers='utvCasesTypeJPDetaljer']/a[@class='registryentry-link']");
                    Uri docsUrl = docsLink == null ? null : new Uri(url, HttpUtility.HtmlDecode(docsLink.Attributes["href"].Value));

                    string agendaItemUrl = url.ToString() + "#" + docsUrl;

                    if (seenAgendaItems.Contains(agendaItemUrl))
                        continue;

                    AgendaItem item = new AgendaItem();
                    item.Title = title;
                    item.Url = new Uri(agendaItemUrl);
                    item.DocumentsUrl = docsUrl;

                    items.Add(item);
                }
            }

            meeting.AgendaItems = items;

            if (items.Count > 0)
                return meeting;
            else
                return null;
        }

        public async Task<IEnumerable<Document>> GetDocuments(AgendaItem item)
        {
            if (item.DocumentsUrl == null)
                return new List<Document>();

            HttpClient http = new HttpClient();
            string html = await http.GetStringAsync(item.DocumentsUrl);

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);

            List<Document> documents = new List<Document>();

            foreach (var docNode in doc.DocumentNode.SelectNodes("//table[@class='listTableClass document-load-target']/tbody/tr"))
            {
                var docLinkNode = docNode.SelectSingleNode("descendant::a");

                Uri docUrl = docLinkNode == null ? null : new Uri(item.DocumentsUrl, HttpUtility.HtmlDecode(docLinkNode.Attributes["href"].Value));
                string title = HttpUtility.HtmlDecode(docNode.SelectSingleNode("descendant::td[@headers='jpDocumentDocumentDescriptionTitle']").InnerText.Trim());

                documents.Add(new Document
                {
                    Title = title,
                    Url = docUrl
                });
            }

            return documents;
        }
    }
}