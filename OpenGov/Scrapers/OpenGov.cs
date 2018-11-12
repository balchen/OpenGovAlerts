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
    public class OpenGov: IScraper
    {
        private string clientId;

        public OpenGov(string clientId)
        {
            this.clientId = clientId;
        }

        public async Task<IEnumerable<Meeting>> FindMeetings(string phrase, ISet<string> seenMeetings)
        {
            HttpClient http = new HttpClient();

            Uri url = new Uri(string.Format("http://opengov.cloudapp.net/Meetings/{0}/AgendaItems/Search?q={1}", clientId, phrase));

            string html = await http.GetStringAsync(url);

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);

            List<Meeting> newMeetings = new List<Meeting>();

            var meetings = doc.DocumentNode.SelectNodes("//div[@class='meetingList searchResultsList']/ul/li/a");

            if (meetings != null)
            {
                foreach (var meeting in meetings)
                {
                    var meetingUrl = meeting.Attributes["href"].Value;
                    Uri meetingUri = new Uri(url, meetingUrl);
                    meetingUrl = meetingUri.ToString();

                    if (seenMeetings.Contains(meetingUrl))
                        continue;

                    string name = HttpUtility.HtmlDecode(meeting.SelectSingleNode("descendant::div[@class='meetingName']/span").InnerText);
                    DateTime date = DateTime.ParseExact(HttpUtility.HtmlDecode(meeting.SelectSingleNode("descendant::div[@class='meetingDate']/span").InnerText), "dd.MM.yyyy", CultureInfo.CurrentCulture);
                    string topic = HttpUtility.HtmlDecode(meeting.SelectSingleNode("descendant::div[@class='serachMeetingResult']/p").InnerText);
                    string agendaItemId = HttpUtility.ParseQueryString(meetingUri.Query).Get("agendaItemId");

                    newMeetings.Add(new Meeting
                    {
                        Phrase = phrase,
                        ClientId = clientId,
                        Name = name,
                        Topic = topic,
                        Url = meetingUri,
                        Date = date,
                        AgendaItemId = agendaItemId
                    });
                }
            }

            return newMeetings;
        }

        public async Task<IEnumerable<Document>> GetDocuments(Meeting meeting)
        {
            return await GetAgendaItemDocumentUrls(clientId, meeting.AgendaItemId);
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
                    document.Name = fileNameNode.InnerText;
                    document.Type = documentLink.SelectSingleNode("descendant::div[@class='fileDocumentCategory']").InnerText;

                    documents.Add(document);
                }
            }

            return documents;
        }
    }
}