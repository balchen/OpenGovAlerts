using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenGov.Models;

namespace OpenGov.Scrapers
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

        public async Task<IEnumerable<Document>> GetDocuments(Meeting meeting)
        {
            string agendaItemJson = await http.GetStringAsync(string.Format("/api/utvalg/{0}/moter/{1}/behandlinger/", meeting.BoardId, meeting.MeetingId));

            List<Document> documents = new List<Document>();
            
            foreach (dynamic item in JArray.Parse(agendaItemJson))
            {
                if (item.Id == meeting.AgendaItemId)
                {
                    foreach (dynamic itemDocument in item.Dokumenter)
                    {
                        Document document = new Document {
                            Name = itemDocument.Tittel,
                            Type = itemDocument.Dokumenttilknytning,
                            Url = new Uri(baseUrl, string.Format("/api/utvalg/{0}/moter/{1}/dokumenter/{2}", meeting.BoardId, meeting.MeetingId, itemDocument.Rekkefolge))
                        };

                        documents.Add(document);
                    }
                }
            }

            return documents;
        }

        public async Task<IEnumerable<Meeting>> FindMeetings(string phrase, ISet<string> seenMeetings)
        {
            List<Meeting> meetings = new List<Meeting>();

            string boardsJson = await http.GetStringAsync("/api/utvalg/");

            foreach (dynamic board in JArray.Parse(boardsJson))
            {
                string boardMeetingsJson = await http.GetStringAsync(string.Format("/api/utvalg/{0}/moter/", board.Id));

                foreach (dynamic boardMeeting in JArray.Parse(boardMeetingsJson))
                {
                    string meetingUrl = string.Format("https://prokomresources.prokomcdn.no/plugins/sru-v2/iframe-app/app.html?url={2}&v=1.01#se:mote/moteid:{1}/utvalgid:{0}", board.Id, boardMeeting.Id, baseUrl.ToString());

                    if (seenMeetings.Contains(meetingUrl))
                        continue;

                    foreach (dynamic item in boardMeeting.Behandlinger)
                    {
                        string title = item.Tittel;
                        if (title.ToLower().Contains(phrase.ToLower()))
                        {
                            Meeting meeting = new Meeting();

                            meeting.BoardId = board.Id;
                            meeting.MeetingId = boardMeeting.Id;
                            meeting.AgendaItemId = item.Id;

                            meeting.Phrase = phrase;
                            meeting.Date = boardMeeting.Start;
                            meeting.Name = board.Name;
                            meeting.Topic = item.Tittel;
                            meeting.Url = new Uri(meetingUrl);

                            meetings.Add(meeting);
                        }
                    }
                }
            }

            return meetings;
        }
    }
}
