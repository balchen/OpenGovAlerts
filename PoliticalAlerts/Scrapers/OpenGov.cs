using HtmlAgilityPack;
using PoliticalAlerts.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace PoliticalAlerts.Scrapers
{
    public class OpenGov : IScraper
    {
        private readonly string clientId;
        private readonly HttpClient http;

        public OpenGov(string clientId)
        {
            this.clientId = clientId;
            this.http = new HttpClient();
            http.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/87.0.4280.88 Safari/537.36");
        }

        public async Task<IEnumerable<Meeting>> GetNewMeetings(ISet<string> seenAgendaItems)
        {
            List<Meeting> meetings = await FindNew(seenAgendaItems);

            return meetings;
        }

        private async Task<List<Meeting>> FindNew(ISet<string> seenAgendaItems)
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

                    Meeting newMeeting = await GetMeetingDetails(seenAgendaItems, meetingUrl, meetingId, meetingDate, clientId);

                    if (newMeeting != null)
                        newMeetings.Add(newMeeting);
                }
            }

            return newMeetings;
        }

        private async Task<Meeting> GetMeetingDetails(ISet<string> seenAgendaItems, string meetingUrl, string meetingId, DateTime meetingDate, string clientId)
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

                        if (!seenAgendaItems.Contains(url))
                        {
                            Uri detailsUrl = new Uri(string.Format("http://opengov.cloudapp.net/Meetings/{0}/Meetings/LoadAgendaItemDetail/{1}", clientId, id));

                            string detailsHtml = await http.GetStringAsync(detailsUrl);

                            HtmlDocument detailsDoc = new HtmlDocument();
                            detailsDoc.LoadHtml(detailsHtml);

                            var caseNumber = detailsDoc.DocumentNode.SelectSingleNode("//div[@class='detailsList'][descendant::div[@class='detailHeader']/span[contains(., 'Saksnr')]]/div[@class='detailContent']/p")?.InnerText;

                            meeting.AgendaItems.Add(new AgendaItem
                            {
                                Meeting = meeting,
                                ExternalId = id,
                                Title = title,
                                CaseNumber = caseNumber,
                                Url = new Uri(url)
                            });
                        }
                    }
                }
            }

            if (meeting.AgendaItems.Count > 0)
                return meeting;
            else
                return null;
        }

        public async Task<IEnumerable<Document>> GetDocuments(AgendaItem item)
        {
            (IEnumerable<Document> documents, string caseNumber) = await GetAgendaItemDetails(item.ExternalId);

            return documents;
        }

        public async Task<IEnumerable<JournalEntry>> GetCaseJournal(string caseNumber)
        {
            Uri url = new Uri(string.Format("https://opengov.360online.com/Cases/{0}?q={1}", this.clientId, caseNumber));

            string html = await http.GetStringAsync(url);

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);

            var linkNodes = doc.DocumentNode.SelectNodes("//a[descendant::div[@class='caseTypeLink searchPageCaseheader']]");

            if (linkNodes == null || linkNodes.Count != 1)
                return null;

            var linkNode = linkNodes[0];

            var caseUrl = new Uri(url, linkNode.Attributes["href"].Value);
            var caseId = linkNode.Attributes["href"].Value.Split('/').Last();

            html = await http.GetStringAsync(caseUrl);

            doc = new HtmlDocument();
            doc.LoadHtml(html);

            List<JournalEntry> entries = new List<JournalEntry>();

            foreach (var journalNode in doc.DocumentNode.SelectNodes("//div[@class='caseDocumentList']/ul/li/a[@class='accordion']"))
            {
                string title = HttpUtility.HtmlDecode(journalNode.SelectSingleNode("descendant::div[@class='accordionTitle']").InnerText.Trim());
                var detailsNode = journalNode.NextSibling.NextSibling;
                string id = detailsNode.Attributes["id"].Value;
                string type = detailsNode.SelectSingleNode("descendant::div[@class='documentDetails']//div[@class='detailsList' and descendant::span='Dokumenttype']//div[@class='documentDetailContent']").InnerText;
                Uri entryUrl = new Uri(string.Format("https://opengov.360online.com/Cases/stavanger/Case/Details/{0}?documentID={1}", caseId, id));
                string from = HttpUtility.HtmlDecode(detailsNode.SelectSingleNode("descendant::div[@class='documentDetails']//div[@class='detailsList' and descendant::span='Avsender']//div[@class='documentDetailContent']")?.InnerText.Trim());
                string to = HttpUtility.HtmlDecode(detailsNode.SelectSingleNode("descendant::div[@class='documentDetails']//div[@class='detailsList' and descendant::span='Mottaker']//div[@class='documentDetailContent']")?.InnerText.Trim());

                JournalType journalType = JournalType.Unclassified;

                switch (type.ToLower())
                {
                    case "vedtak":
                        journalType = JournalType.Decision;
                        break;
                    case "dokument inn":
                    case "e-post inn":
                        journalType = JournalType.Inbound;
                        break;
                    case "internt dokument med oppfølging":
                        journalType = JournalType.Inbound;
                        break;
                    case "saksframlegg/innstilling":
                        journalType = JournalType.Proposal;
                        break;
                    case "dokument ut":
                    case "e-post ut":
                        journalType = JournalType.Outbound;
                        break;
                }

                var entry = new JournalEntry
                {
                    Title = title,
                    Type = type,
                    ParsedType = journalType,
                    Url = entryUrl,
                    From = from,
                    To = to,
                    Documents = new List<Document>()
                };

                entries.Add(entry);

                foreach (var documentNode in detailsNode.SelectNodes("descendant::li[@class='fileLink ']/a"))
                {
                    Uri docUrl = new Uri(url, documentNode.Attributes["href"].Value);
                    string docTitle = HttpUtility.HtmlDecode(documentNode.SelectSingleNode("descendant::div[@class='fileNameDetail']").InnerText.Trim());
                    string docType = documentNode.SelectSingleNode("descendant::div[@class='fileDocumentCategory']").InnerText.Trim();

                    entry.Documents.Add(new Document
                    {
                        Url = docUrl,
                        Title = docTitle,
                        Type = docType
                    });
                }
            }

            return entries;
        }

        private async Task<(IEnumerable<Document>, string)> GetAgendaItemDetails(string agendaItemId)
        {
            Uri url = new Uri(string.Format("http://opengov.cloudapp.net/Meetings/{0}/Meetings/LoadAgendaItemDetail/{1}", this.clientId, agendaItemId));

            HttpClient http = new HttpClient();

            string html = await http.GetStringAsync(url);

            HtmlDocument details = new HtmlDocument();
            details.LoadHtml(html);

            List<Document> documents = new List<Document>();

            foreach (var documentLink in details.DocumentNode.SelectNodes("//a"))
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

            var caseNumberNode = details.DocumentNode.SelectSingleNode("descendant::div[@class='detailsSection']/div[@class='details']/div[@class='detailsList' and descendant::div[contains(., 'Saksnr')]]/div[@class='detailContent']");

            string caseNumber = caseNumberNode?.InnerText;

            return (documents, caseNumber);
        }
    }
}