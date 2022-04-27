using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PoliticalAlerts.Models;

namespace PoliticalAlerts.Scrapers
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

        public async Task<IEnumerable<Meeting>> GetNewMeetings(ISet<string> seenAgendaItems)
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

                if (searchMeeting.source.type[0] == "Moetemappe")
                {
                    await Task.Delay(10000);
                    Meeting meeting = new Meeting { ExternalId = id, BoardId = boardId, BoardName = boardName, Date = date };

                    IList<AgendaItem> agendaItems = await GetAgendaItems(meeting);

                    agendaItems = agendaItems.Where(a => !seenAgendaItems.Contains(a.Url.ToString())).ToList();

                    if (agendaItems.Count > 0)
                    {
                        meeting.AgendaItems = agendaItems;
                        meetings.Add(meeting);
                    }
                }
            }

            return meetings;
        }

        private async Task<IList<AgendaItem>> GetAgendaItems(Meeting meeting)
        {
            Uri url = new Uri(string.Format(meetingUrl, meeting.ExternalId));

            List<AgendaItem> agendaItems = new List<AgendaItem>();

            string jsonResult = await http.GetStringAsync(url);

            JArray agendaItemResult = JArray.Parse(jsonResult);

            foreach (var item in agendaItemResult.Children())
            {
                if ((string)item["@type"][0] == "http://www.arkivverket.no/standarder/noark5/arkivstruktur/Møtesaksregistrering")
                {
                    string agendaItemId = (string)item["@id"];
                    string title = (string)item["http://www.arkivverket.no/standarder/noark5/arkivstruktur/offentligTittel_SENSITIV"][0]["@value"];

                    agendaItems.Add(new AgendaItem { Title = title, Url = new Uri(url, "#" + agendaItemId), ExternalId = agendaItemId });
                }
            }

            return agendaItems;
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
