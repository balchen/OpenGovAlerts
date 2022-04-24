using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using HtmlAgilityPack;
using PoliticalAlerts.Models;

namespace PoliticalAlerts.Scrapers
{
    public class ACOS : IScraper
    {
        private Uri url;
        private HttpClient http;

        public ACOS(Uri url)
        {
            this.url = url;
            http = new HttpClient();
        }

        public async Task<IEnumerable<Meeting>> GetNewMeetings(ISet<string> seenAgendaItems)
        {
            Uri calendarUrl = new Uri(url, "?response=moteplan");

            HttpClient client = new HttpClient();
            string calendarHtml = await client.GetStringAsync(calendarUrl);

            HtmlDocument calendarDoc = new HtmlDocument();
            calendarDoc.LoadHtml(calendarHtml);

            var calendarTable = calendarDoc.DocumentNode.SelectSingleNode("//table");

            int year = int.Parse(calendarTable.SelectSingleNode("caption").InnerText.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)[1]);

            List<Meeting> newMeetings = new List<Meeting>();

            foreach (var boardRow in calendarTable.SelectNodes("tbody/tr"))
            {
                string boardName = boardRow.SelectSingleNode("th").InnerText;
                string boardId = HttpUtility.ParseQueryString(new Uri(calendarUrl, HttpUtility.HtmlDecode(boardRow.SelectSingleNode("th/a").Attributes["href"].Value)).Query).Get("utvalg");

                int month = 0;
                foreach (var monthCell in boardRow.SelectNodes("td"))
                {
                    month++;

                    var meetingLinks = monthCell.SelectNodes("a");

                    if (meetingLinks != null)
                    {
                        foreach (var meetingLink in meetingLinks)
                        {
                            string meetingCell = meetingLink.InnerText.Trim();

                            if (meetingCell != "")
                            {
                                int day = int.Parse(meetingCell);
                                Uri meetingUrl = new Uri(calendarUrl, HttpUtility.HtmlDecode(meetingLink.Attributes["href"].Value));
                                DateTime date = new DateTime(year, month, day);
                                string meetingId = HttpUtility.ParseQueryString(meetingUrl.Query).Get("moteid");

                                Meeting meeting = new Meeting { ExternalId = meetingId, BoardId = boardId, BoardName = boardName, Date = date, Url = meetingUrl };

                                var items = await GetAgendaItems(meetingUrl);

                                if (items == null)
                                    continue;

                                foreach (var item in items)
                                    item.Meeting = meeting;

                                meeting.AgendaItems = items.Where(i => !seenAgendaItems.Contains(i.Url.ToString())).ToList();

                                if (meeting != null && meeting.AgendaItems != null && meeting.AgendaItems.Count > 0)
                                    newMeetings.Add(meeting);
                            }
                        }
                    }
                }
            }

            return newMeetings;
        }

        private async Task<IList<AgendaItem>> GetAgendaItems(Uri meetingUrl)
        {
            HtmlDocument meetingDoc = new HtmlDocument();

            meetingDoc.LoadHtml(await http.GetStringAsync(meetingUrl));

            var agendaItemNodes = meetingDoc.DocumentNode.SelectNodes("//div[@id='div_sok_resultstable']/ul/li");

            if (agendaItemNodes == null)
                return null;

            List<AgendaItem> items = new List<AgendaItem>();

            foreach (var agendaItem in agendaItemNodes)
            {
                string agendaItemId = null;
                string title = null;

                foreach (var agendaDetails in agendaItem.SelectNodes("div[@class='det']/h3/a"))
                {
                    if (agendaDetails.Attributes["id"] != null)
                        agendaItemId = agendaDetails.Attributes["id"].Value;

                    if (agendaDetails.Attributes["class"] != null && agendaDetails.Attributes["class"].Value == "content-link")
                        title = agendaDetails.InnerText;
                }

                if (agendaItemId != null && title != null)
                {
                    Uri url = new Uri(meetingUrl, "#" + agendaItemId);
                    items.Add(new AgendaItem { ExternalId = agendaItemId, Title = title, Url = url });
                }
            }

            return items;
        }

        public async Task<IEnumerable<Document>> GetDocuments(AgendaItem item)
        {
            string meetingUrl = url + "?response=mote&moteid=" + item.Meeting.ExternalId;

            HtmlDocument meetingDoc = new HtmlDocument();

            meetingDoc.LoadHtml(await http.GetStringAsync(meetingUrl));

            List<Document> documents = new List<Document>();

            var agendaItem = meetingDoc.DocumentNode.SelectSingleNode("//div[@id='div_sok_resultstable']//li[//a[@id='" + item.ExternalId + "']]");

            foreach (var documentsHeading in agendaItem.SelectNodes("descendant::h3"))
            {
                if (documentsHeading.NextSibling.Name == "ul")
                {
                    foreach (var documentNode in documentsHeading.NextSibling.SelectNodes("descendant::a"))
                    {
                        documents.Add(new Document
                        {
                            Title = documentNode.InnerText,
                            Type = documentNode.NextSibling.InnerText,
                            Url = new Uri(new Uri(meetingUrl), HttpUtility.HtmlDecode(documentNode.Attributes["href"].Value))
                        });
                    }
                }
            }

            return documents;
        }
    }
}