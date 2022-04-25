using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using PoliticalAlerts.Models;

namespace PoliticalAlerts.Scrapers
{
    public class SRU : IScraper
    {
        private Uri baseUrl;
        private HttpClient http;

        public SRU(Uri baseUrl)
        {
            this.baseUrl = baseUrl;
            http = new HttpClient();
            http.BaseAddress = baseUrl;
        }

        public Task<IEnumerable<Document>> GetCaseDocuments(string caseNumber)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<Document>> GetDocuments(AgendaItem agendaItem)
        {
            string agendaItemJson = await http.GetStringAsync(string.Format("/api/utvalg/{0}/moter/{1}/behandlinger/", agendaItem.Meeting.BoardId, agendaItem.Meeting.ExternalId));

            List<Document> documents = new List<Document>();

            foreach (dynamic item in JArray.Parse(agendaItemJson))
            {
                if (item.Id == agendaItem.ExternalId && item.Dokumenter != null)
                {
                    foreach (dynamic itemDocument in item.Dokumenter)
                    {
                        Document document = new Document
                        {
                            Title = itemDocument.Tittel,
                            Type = itemDocument.Dokumenttilknytning,
                            Url = new Uri(baseUrl, string.Format("/api/utvalg/{0}/moter/{1}/dokumenter/{2}", agendaItem.Meeting.BoardId, agendaItem.Meeting.ExternalId, itemDocument.Rekkefolge))
                        };

                        documents.Add(document);
                    }
                }
            }

            return documents;
        }

        public async Task<IEnumerable<Meeting>> GetNewMeetings(ISet<string> seenAgendaItems)
        {
            List<Meeting> meetings = new List<Meeting>();

            string boardsJson = await http.GetStringAsync("/api/utvalg/");

            foreach (dynamic board in JArray.Parse(boardsJson))
            {
                string boardMeetingsJson = await http.GetStringAsync(string.Format("/api/utvalg/{0}/moter/", board.Id));

                foreach (dynamic boardMeeting in JArray.Parse(boardMeetingsJson))
                {
                    string meetingUrl = string.Format("{2}/app#se:mote/moteid:{1}/utvalgid:{0}", board.Id, boardMeeting.Id, baseUrl.ToString());

                    if (boardMeeting.Behandlinger != null)
                    {
                        Meeting meeting = new Meeting
                        {
                            BoardId = board.Id,
                            BoardName = board.Name,
                            Date = boardMeeting.Start,
                            ExternalId = boardMeeting.Id,
                            Url = new Uri(meetingUrl),
                            AgendaItems = new List<AgendaItem>()
                        };

                        foreach (dynamic item in boardMeeting.Behandlinger)
                        {
                            string title = item.Tittel;
                            string agendaItemUrl = meetingUrl + "#" + item.Id;

                            if (seenAgendaItems.Contains(agendaItemUrl))
                                continue;

                            AgendaItem agendaItem = new AgendaItem
                            {
                                Meeting = meeting,
                                ExternalId = item.Id,
                                Title = item.Tittel,
                                Url = new Uri(agendaItemUrl)
                            };

                            meeting.AgendaItems.Add(agendaItem);
                        }

                        if (meeting.AgendaItems.Count > 0)
                            meetings.Add(meeting);
                    }
                }
            }

            return meetings;
        }
    }
}
