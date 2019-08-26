using HtmlAgilityPack;
using OpenGov.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;

namespace OpenGov.Scrapers
{
    public class Jupiter: IScraper
    {
        private string jupiterUrl;

        public Jupiter(string jupiterUrl)
        {
            this.jupiterUrl = jupiterUrl;
        }

        public async Task<IEnumerable<Meeting>> FindMeetings(string phrase, ISet<string> seenMeetings)
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

                if (seenMeetings.Contains(meetingUrl))
                    continue;

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

                foreach (var heading in meetingInfo.DocumentNode.SelectNodes("//h4"))
                {
                    if (heading.InnerText == "Saker til behandling")
                    {
                        var agenda = heading.NextSibling.NextSibling;

                        foreach (var agendaItem in agenda.SelectNodes("descendant::td[@style='text-align: left;']"))
                        {
                            string agendaItemTitle = agendaItem.InnerText;

                            if (string.IsNullOrEmpty(phrase) || agendaItemTitle.ToLower().Contains(phrase))
                            {
                                newMeetings.Add(new Meeting
                                {
                                    Date = time,
                                    BoardName = body,
                                    Title = agendaItemTitle,
                                    Url = meetingUri
                                });
                            }
                        }
                    }
                }
            }

            return newMeetings;
        }

        public async Task<IEnumerable<Document>> GetDocuments(Meeting meeting)
        {
            return new List<Document>();
        }
    }
}