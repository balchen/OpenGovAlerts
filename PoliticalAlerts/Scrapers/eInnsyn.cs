using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PoliticalAlerts.Models;
using static System.Net.WebRequestMethods;

namespace PoliticalAlerts.Scrapers
{
    public class eInnsyn : IScraper
    {
        private const string eInnsynUrl = "https://einnsyn.no/api/v2";
        private const string meetingUrl = eInnsynUrl + "/motemappe?iri={0}";
        private const string caseUrl = eInnsynUrl + "/motesaksregistrering?iri={0}";

        private string baseUrl, organizationId;
        private HttpClient http = new HttpClient();

        public eInnsyn(string baseUrl, string organizationId)
        {
            this.baseUrl = baseUrl;
            this.organizationId = organizationId;
        }

        public async Task<IEnumerable<Meeting>> GetNewMeetings(ISet<string> seenAgendaItems)
        {
            Uri url = new Uri(baseUrl);

            var baseQuery = new
            {
                size = 50,
                aggregations = new { contentTypes = "type", virksomheter = "arkivskaperTransitive" },
                appliedFilters = new object[] {
                    new { fieldName = "type", fieldValue = new string[] { "Moetemappe" }, type = "termQueryFilter" },
                    new { fieldName = "type", fieldValue = new string[] { "JournalpostForMøte" }, type = "notQueryFilter" },
                    new { fieldName = "arkivskaperTransitive", fieldValue = new string[] { organizationId }, type = "postQueryFilter" },
                    new { fieldName = "moetedato", from = DateTime.UtcNow.ToString("o"), to = DateTime.UtcNow.AddDays(30).ToString("o"), type = "rangeQueryFilter" }
                }
            };

            string finalQuery = JsonConvert.SerializeObject(baseQuery);

            HttpResponseMessage response = await http.PostAsync(url, new StringContent(finalQuery, Encoding.UTF8, "application/json"));

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

            dynamic meetingResult = JObject.Parse(jsonResult);

            foreach (var item in meetingResult["møtesaksregistreringer"])
            {
                string agendaItemId = (string)item.id;
                string title = (string)item.tittel;

                agendaItems.Add(new AgendaItem { Title = title, Url = new Uri(agendaItemId), ExternalId = agendaItemId });
            }

            return agendaItems;
        }

        public Task<IEnumerable<Document>> GetDocuments(AgendaItem item)
        {
            return Task.FromResult<IEnumerable<Document>>(new List<Document>());

            //Uri url = new Uri(string.Format(caseUrl, item.ExternalId));

            //string jsonResult = await http.GetStringAsync(url);

            //dynamic caseResult = JObject.Parse(jsonResult);

            //foreach (var @case in caseResult["møtesaksregistreringer"])
            //{
            //    string agendaItemId = (string)item.Id;
            //    string title = (string)item.Title;

            //    agendaItems.Add(new AgendaItem { Title = title, Url = new Uri(agendaItemId), ExternalId = agendaItemId });
            //}

            //return agendaItems;
        }

        public Task<IEnumerable<JournalEntry>> GetCaseJournal(string caseNumber)
        {
            return Task.FromResult<IEnumerable<JournalEntry>>(new List<JournalEntry>());
        }
    }
}
