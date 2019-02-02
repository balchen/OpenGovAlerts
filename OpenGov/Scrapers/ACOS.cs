using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using HtmlAgilityPack;
using OpenGov.Models;

namespace OpenGov.Scrapers
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

        public async Task<IEnumerable<Meeting>> FindMeetings(string phrase, ISet<string> seenMeetings)
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

                    string meetingCell = monthCell.InnerText.Trim();

                    if (meetingCell != "")
                    {
                        int day = int.Parse(meetingCell);
                        Uri meetingUrl = new Uri(calendarUrl, HttpUtility.HtmlDecode(monthCell.SelectSingleNode("a").Attributes["href"].Value));
                        DateTime date = new DateTime(year, month, day);

                        if (seenMeetings.Contains(meetingUrl.ToString()))
                            continue;

                        var meeting = await FindMeeting(phrase, meetingUrl, date, boardName, boardId);

                        if (meeting != null)
                            newMeetings.Add(meeting);
                    }
                }
            }

            return newMeetings;
        }

        private async Task<Meeting> FindMeeting(string phrase, Uri meetingUrl, DateTime date, string boardName, string boardId)
        {
            string meetingId = HttpUtility.ParseQueryString(meetingUrl.Query).Get("moteid");

            HtmlDocument meetingDoc = new HtmlDocument();

            meetingDoc.LoadHtml(await http.GetStringAsync(meetingUrl));

            var agendaItemNodes = meetingDoc.DocumentNode.SelectNodes("//div[@id='div_sok_resultstable']/ul/li");

            if (agendaItemNodes == null)
                return null;

            foreach (var agendaItem in agendaItemNodes)
            {
                string agendaItemId = null;
                string title = null;

                foreach (var agendaDetails in agendaItem.SelectNodes("div[@class='det']/h2/a"))
                {
                    if (agendaDetails.Attributes["id"] != null)
                        agendaItemId = agendaDetails.Attributes["id"].Value;

                    if (agendaDetails.Attributes["class"] != null && agendaDetails.Attributes["class"].Value == "content-link")
                        title = agendaDetails.InnerText;
                }

                if (agendaItemId != null && title != null)
                {
                    if (title.ToLower().Contains(phrase))
                    {
                        return new Meeting
                        {
                            AgendaItemId = agendaItemId,
                            BoardId = boardId,
                            BoardName = boardName,
                            Date = date,
                            Title = title.RemoveWhitespace(),
                            MeetingId = meetingId,
                            Url = meetingUrl
                        };
                    }
                }
            }

            return null;
        }

        public async Task<IEnumerable<Document>> GetDocuments(Meeting meeting)
        {
            string meetingUrl = url + "?response=mote&moteid=133";

            HtmlDocument meetingDoc = new HtmlDocument();

            meetingDoc.LoadHtml(await http.GetStringAsync(meetingUrl));

            List<Document> documents = new List<Document>();

            var agendaItem = meetingDoc.DocumentNode.SelectSingleNode("//div[@id='div_sok_resultstable']//li[/a[@id='" + meeting.AgendaItemId + "']]");

            foreach (var documentsHeading in agendaItem.SelectNodes("descendant::h3"))
            {
                if (documentsHeading.NextSibling.Name == "ul")
                {
                    foreach (var documentNode in documentsHeading.NextSibling.SelectNodes("descendant::a"))
                    {
                        documents.Add(new Document
                        {
                            Name = documentNode.InnerText,
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