using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenGov.Models;

namespace OpenGov.Scrapers
{
    public class eInnsyn : IScraper
    {
        private const string eInnsynUrl = "https://einnsyn.no/api/";
        private const string meetingUrl = eInnsynUrl + "mappe?uri={0}";
        private const string caseUrl = eInnsynUrl + "registrering?uri={0}";

        private string baseUrl, baseQuery;
        private HttpClient http = new HttpClient();

        public eInnsyn(string baseUrl, string baseQuery)
        {
            this.baseUrl = baseUrl;
            this.baseQuery = baseQuery;
        }

        public async Task<IEnumerable<Meeting>> GetNewMeetings(ISet<string> seenMeetings)
        {
            Uri url = new Uri(baseUrl);

            HttpResponseMessage response = await http.PostAsync(url, new StringContent(baseQuery, Encoding.UTF8, "application/json"));

            string jsonResult = await response.Content.ReadAsStringAsync();

            dynamic searchResult = JObject.Parse(jsonResult);

            List<Meeting> meetings = new List<Meeting>();

            foreach (var searchMeeting in searchResult.searchHits)
            {
                string id = searchMeeting.id;
                DateTime date = searchMeeting.source.moetedato;
                string boardId = searchMeeting.source.utvalg;

                string boardName;

                if (searchMeeting.source.arkivskaperSorteringNavn is JArray)
                    boardName = searchMeeting.source.arkivskaperSorteringNavn[0];
                else
                    boardName = searchMeeting.source.arkivskaperSorteringNavn;

                if (searchMeeting.source.type[0] == "Moetemappe" && !seenMeetings.Contains(id))
                {
                    await Task.Delay(10000);
                    IEnumerable<Meeting> agendaItems = await GetAgendaItems(id, boardId, boardName, date);
                    meetings.AddRange(agendaItems);
                }
            }

            return meetings;
        }

        private async Task<IEnumerable<Meeting>> GetAgendaItems(string meetingId, string boardId, string boardName, DateTime date)
        {
            Uri url = new Uri(string.Format(meetingUrl, meetingId));

            List<Meeting> agendaItems = new List<Meeting>();

            string jsonResult = await http.GetStringAsync(url);

            JArray agendaItemResult = JArray.Parse(jsonResult);

            foreach (var item in agendaItemResult.Children())
            {
                if ((string)item["@type"][0] == "http://www.arkivverket.no/standarder/noark5/arkivstruktur/Møtesaksregistrering")
                {
                    string title = (string)item["http://www.arkivverket.no/standarder/noark5/arkivstruktur/offentligTittel_SENSITIV"][0]["@value"];

                    agendaItems.Add(new Meeting { BoardId = boardId, BoardName = boardName, Date = date, Title = title, Url = new Uri(meetingId), MeetingId = meetingId, AgendaItemId = (string)item["@id"] });
                }
            }

            return agendaItems;
        }

        public async Task<IEnumerable<Document>> GetDocuments(Meeting meeting)
        {
            return new List<Document>();
        }
    }
}
