using HtmlAgilityPack;
using OpenGov.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace OpenGov.Scrapers
{
    public class OpenGov : IScraper
    {
        private string clientId;

        public OpenGov(string clientId)
        {
            this.clientId = clientId;
        }

        public async Task<IEnumerable<Meeting>> GetNewMeetings(ISet<string> seenAgendaItems)
        {
            HttpClient http = new HttpClient();

            List<Meeting> meetings = await FindNew(seenAgendaItems, http);

            return meetings;
        }

        private async Task<List<Meeting>> FindNew(ISet<string> seenAgendaItems, HttpClient http)
        {
            Uri url = new Uri(string.Format("http://opengov.cloudapp.net/Meetings/{0}", clientId));

            string html = await http.GetStringAsync(url);

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);

            List<Meeting> newMeetings = new List<Meeting>();

            var meetings = doc.DocumentNode.SelectNodes("//div[@class='findMeetings findMeetingsMainPage']/descendant::div[@class='meetingList']/ul/li/a");

            if (meetings != null)
            {
                foreach (var meeting in meetings)
                {
                    var meetingUrl = meeting.Attributes["href"].Value;
                    Uri meetingUri = new Uri(url, meetingUrl);
                    var meetingId = meetingUri.Segments[meetingUri.Segments.Length - 1];
                    meetingUrl = meetingUri.ToString();

                    if (seenAgendaItems.Contains(meetingUrl))
                        continue;

                    DateTime meetingDate = DateTime.ParseExact(HttpUtility.HtmlDecode(meeting.SelectSingleNode("descendant::div[@class='meetingDate']/span").InnerText), "dd.MM.yyyy", CultureInfo.CurrentCulture);

                    newMeetings.Add(await GetMeetingDetails(meetingUrl, meetingId, meetingDate, clientId, http));
                }
            }

            return newMeetings;
        }

        private async Task<Meeting> GetMeetingDetails(string meetingUrl, string meetingId, DateTime meetingDate, string clientId, HttpClient http)
        {
            string html = await http.GetStringAsync(meetingUrl);

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);

            string boardName = HttpUtility.HtmlDecode(doc.DocumentNode.SelectSingleNode("//div[@class='meetingsDetailsDiv']/div[@class='details']/div[@class='detailsList']/div[@class='detailContent']").InnerText).Trim();

            var agendaItems = doc.DocumentNode.SelectNodes("//div[@class='meetingAgendaList']/ul/li/a");

            Meeting meeting = new Meeting
            {
                Url = new Uri(meetingUrl),
                ExternalId = meetingId,
                Date = meetingDate,
                BoardName = boardName,
                AgendaItems = new List<AgendaItem>()
            };

            if (agendaItems != null)
            {
                foreach (var agendaItem in agendaItems)
                {
                    var panel = agendaItem.ParentNode.SelectSingleNode("descendant::div[@class='panel']");

                    if (panel != null)
                    {
                        string id = panel.Attributes["id"].Value;
                        string url = string.Format("http://opengov.cloudapp.net/Meetings/{0}/Meetings/Details/{2}?agendaItemId={1}", clientId, id, meetingId);
                        string title = HttpUtility.HtmlDecode(agendaItem.SelectSingleNode("descendant::div[@class='accordionTitle']").InnerText).Trim();

                        meeting.AgendaItems.Add(new AgendaItem
                        {
                            Meeting = meeting,
                            ExternalId = id,
                            Title = title,
                            Url = new Uri(url)
                        });
                    }
                }
            }

            return meeting;
        }

        public async Task<IEnumerable<Document>> GetDocuments(AgendaItem item)
        {
            return await GetAgendaItemDocumentUrls(clientId, item.ExternalId);
        }

        private async Task<IEnumerable<Document>> GetAgendaItemDocumentUrls(string clientId, string agendaItemId)
        {
            Uri url = new Uri(string.Format("http://opengov.cloudapp.net/Meetings/{0}/Meetings/LoadAgendaItemDetail/{1}", clientId, agendaItemId));

            HttpClient http = new HttpClient();

            string html = await http.GetStringAsync(url);

            HtmlDocument agendaItemDocuments = new HtmlDocument();
            agendaItemDocuments.LoadHtml(html);

            List<Document> documents = new List<Document>();

            foreach (var documentLink in agendaItemDocuments.DocumentNode.SelectNodes("//a"))
            {
                Document document = new Document();
                document.Url = new Uri(url, documentLink.Attributes["href"].Value);
                var fileNameNode = documentLink.SelectSingleNode("descendant::div[@class='fileNameDetail']");

                if (fileNameNode != null)
                {
                    document.Title = fileNameNode.InnerText;
                    document.Type = documentLink.SelectSingleNode("descendant::div[@class='fileDocumentCategory']")?.InnerText;

                    documents.Add(document);
                }
            }

            return documents;
        }
    }
}