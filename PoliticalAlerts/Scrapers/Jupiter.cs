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

            foreach (var meetingLink in calendar.DocumentNode.SelectNodes("//div[@id='motekalender_table']/table//a"))
            {
                string meetingUrl = meetingLink.GetAttributeValue("href", null);
                string meetingTitle = meetingLink.GetAttributeValue("title", null);

                Uri meetingUri = new Uri(url, meetingUrl);

                meetingUrl = meetingUri.ToString();

                string meetingHtml = await http.GetStringAsync(meetingUri);

                HtmlDocument meetingInfo = new HtmlDocument();
                meetingInfo.LoadHtml(meetingHtml);

                string title = meetingInfo.DocumentNode.SelectSingleNode("//h3").ChildNodes[0].InnerText;
                var titleParts = title.Split(',');

                string body = titleParts[0];
                DateTime time;
                string timePart = titleParts[1].Trim();
                if (!DateTime.TryParseExact(timePart, "dd.MM.yyyy hh:mm", CultureInfo.CurrentCulture, DateTimeStyles.None, out time))
                {
                    string dateOnly = timePart.Split(' ')[0];

                    if (!DateTime.TryParseExact(dateOnly, "dd.MM.yyyy", CultureInfo.CurrentCulture, DateTimeStyles.None, out time))
                    {
                        time = new DateTime();
                    }
                }

                Meeting meeting = new Meeting { Url = meetingUri, Date = time, BoardName = body, AgendaItems = new List<AgendaItem>() };

                foreach (var heading in meetingInfo.DocumentNode.SelectNodes("//h4"))
                {
                    if (heading.InnerText == "Saker til behandling")
                    {
                        var agenda = heading.NextSibling;

                        foreach (var agendaItem in agenda.SelectNodes("descendant::tbody/tr"))
                        {
                            string number = agendaItem.ChildNodes[0].InnerText;
                            string agendaItemTitle = agendaItem.ChildNodes[1].InnerText;
                            string agendaItemUrl = meetingUrl + "#" + HttpUtility.UrlEncode(number);

                            if (seenAgendaItems.Contains(agendaItemUrl))
                                continue;

                            meeting.AgendaItems.Add(new AgendaItem
                            {
                                Number = number,
                                Title = agendaItemTitle,
                                Url = new Uri(agendaItemUrl)
                            });
                        }
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
    }
}