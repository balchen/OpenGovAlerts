using HtmlAgilityPack;
using PoliticalAlerts.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace PoliticalAlerts.Scrapers
{
    public class Jupiter : IScraper
    {
        private string jupiterUrl;

        public Jupiter(string jupiterUrl)
        {
            this.jupiterUrl = jupiterUrl;
        }

        public async Task<IEnumerable<Meeting>> GetNewMeetings(ISet<string> seenAgendaItems)
        {
            HttpClient http = new HttpClient();

            Uri url = new Uri(jupiterUrl);

            string html = await http.GetStringAsync(url);

            HtmlDocument calendar = new HtmlDocument();
            calendar.LoadHtml(html);

            List<Meeting> newMeetings = new List<Meeting>();

            foreach (var meetingLink in calendar.DocumentNode.SelectNodes("//table//a"))
            {
                string meetingUrl = meetingLink.GetAttributeValue("href", null);

                Uri meetingUri = new Uri(url, meetingUrl);

                meetingUrl = meetingUri.ToString();

                string meetingHtml = await http.GetStringAsync(meetingUri);

                HtmlDocument meetingInfo = new HtmlDocument();
                meetingInfo.LoadHtml(meetingHtml);

                string boardName = meetingInfo.DocumentNode.SelectSingleNode("//h3").ChildNodes[0].InnerText;

                string title = meetingInfo.DocumentNode.SelectSingleNode("//p[@class='lead' and contains(., 'Møtedato')]")?.InnerText;

                var titleParts = title.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                DateTime time;
                string timePart = titleParts[2].Trim();
                if (!DateTime.TryParseExact(timePart, "dd.MM.yyyy", CultureInfo.CurrentCulture, DateTimeStyles.None, out time))
                {
                    time = new DateTime();
                }

                Meeting meeting = new Meeting { Url = meetingUri, Date = time, BoardName = boardName, AgendaItems = new List<AgendaItem>() };

                var headings = meetingInfo.DocumentNode.SelectNodes("//h4[contains(., 'Saker til behandling')]");

                if (headings == null)
                    continue;

                foreach (var heading in headings)
                {
                    HtmlNode agenda = heading;
                    while (agenda.Name != "table")
                        agenda = agenda.NextSibling;

                    if (agenda.Name != "table")
                        continue;

                    foreach (var agendaItem in agenda.SelectNodes("descendant::tbody/tr"))
                    {
                        string number = agendaItem.ChildNodes[0].InnerText;
                        string agendaItemTitle = agendaItem.ChildNodes[1].InnerText;
                        string agendaItemUrl = meetingUrl + "#" + HttpUtility.UrlEncode(number);

                        if (seenAgendaItems.Contains(agendaItemUrl))
                            continue;

                        var attachments = agendaItem.SelectNodes("descendant::a");

                        List<Document> documents = new List<Document>();

                        if (attachments != null)
                        {
                            foreach (var attachment in attachments)
                            {
                                if (attachment.Attributes["class"]?.Value == "vedlegg" || attachment.Attributes["class"]?.Value == "andre_behandlinger")
                                {
                                    string attachmentsHtml = attachment.Attributes["data-content"].Value;

                                    HtmlDocument attachmentsDoc = new HtmlDocument();
                                    attachmentsDoc.LoadHtml("<html><body>" + attachmentsHtml + "</body></html>");

                                    var attachmentLinks = attachmentsDoc.DocumentNode.SelectNodes("//a");
                                    if (attachmentLinks != null)
                                    {
                                        foreach (var attachmentLink in attachmentLinks)
                                        {
                                            documents.Add(new Document
                                            {
                                                Title = attachmentLink.InnerText,
                                                Type = "Vedlegg",
                                                Url = new Uri(url, attachmentLink.Attributes["href"].Value)
                                            });
                                        }
                                    }
                                }
                                else
                                {
                                    if (attachment.InnerText == "Framlegg" || attachment.InnerText == "Protokoll")
                                    {
                                        documents.Add(new Document
                                        {
                                            Title = attachment.InnerText,
                                            Type = attachment.InnerText,
                                            Url = new Uri(url, attachment.Attributes["href"].Value)
                                        });
                                    }
                                }
                            }
                        }

                        meeting.AgendaItems.Add(new AgendaItem
                        {
                            Number = number,
                            Title = agendaItemTitle,
                            Url = new Uri(agendaItemUrl),
                            Documents = documents
                        });
                    }
                }

                if (meeting.AgendaItems.Count > 0)
                    newMeetings.Add(meeting);
            }

            return newMeetings;
        }

        public async Task<IEnumerable<Document>> GetDocuments(AgendaItem item)
        {
            return new List<Document>();
        }

        public Task<IEnumerable<JournalEntry>> GetCaseJournal(string caseNumber)
        {
            return Task.FromResult<IEnumerable<JournalEntry>>(new List<JournalEntry>());
        }
    }
}