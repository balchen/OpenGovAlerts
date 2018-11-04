using HtmlAgilityPack;
using OpenGovAlerts.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace OpenGovAlerts.OpenGov
{
    public class OpenGov
    {
        private string clientId;

        public OpenGov(string clientId)
        {
            this.clientId = clientId;
        }

        public async Task<IEnumerable<Meeting>> FindMeetings(string phrase)
        {
            Uri url = new Uri(string.Format("http://opengov.cloudapp.net/Meetings/{0}/AgendaItems/Search?q={1}", clientId, phrase));

            HttpClient http = new HttpClient();

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

                    string name = HttpUtility.HtmlDecode(meeting.SelectSingleNode("descendant::div[@class='meetingName']/span").InnerText);
                    string date = HttpUtility.HtmlDecode(meeting.SelectSingleNode("descendant::div[@class='meetingDate']/span").InnerText);
                    string topic = HttpUtility.HtmlDecode(meeting.SelectSingleNode("descendant::div[@class='serachMeetingResult']/p").InnerText);

                    newMeetings.Add(new Meeting
                    {
                        ClientId = clientId,
                        Name = name,
                        Topic = topic,
                        Url = meetingUri,
                        Date = DateTime.ParseExact(date, "dd.MM.yyyy", CultureInfo.CurrentCulture)
                    });
                }
            }

            return newMeetings;
        }
    }
}