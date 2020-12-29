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

        public async Task<IEnumerable<Meeting>> GetNewMeetings(ISet<string> seenMeetings)
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

            foreach (var meetingLink in calendarTable.SelectNodes("descendant::a[@class='calendar-link']"))
            {
                var meetingUrl = new Uri(baseUrl, HttpUtility.HtmlDecode(meetingLink.Attributes["href"].Value));

                if (seenMeetings.Contains(meetingUrl.ToString()))
                    continue;

                var meeting = await GetMeeting(http, meetingUrl);
                newMeetings.Add(meeting);
            }

            return newMeetings;
        }

        private async Task<Meeting> GetMeeting(HttpClient http, Uri url)
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

                    AgendaItem item = new AgendaItem();
                    item.Title = title;
                    item.Url = url;
                    item.DocumentsUrl = docsUrl;

                    items.Add(item);
                }
            }

            meeting.AgendaItems = items;

            return meeting;
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