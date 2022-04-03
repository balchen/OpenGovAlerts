using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using OpenGov.Models;

namespace OpenGov.Scrapers
{
    public class Elements : IScraper
    {
        public string TenantId { get; private set; }
        public Uri baseUrl;

        private HttpClient http = new HttpClient();

        private const string MEETING_URL_TEMPLATE = "https://prod01.elementscloud.no/publikum/{0}/DmbMeeting/{1}";
        private const string AGENDA_ITEMS_URL_TEMPLATE = "https://prod01.elementscloud.no/publikum/api/DmbHandlings/GetByMeetingId/{0}";
        private const string AGENDA_ITEM_URL_TEMPLATE = "https://prod01.elementscloud.no/publikum/api/DmbHandlings/{0}";
        private const string DOCUMENT_URL_TEMPLATE = "https://prod01.elementscloud.no/publikum/Documents/ShowDocument/{0}/{1}/{2}";

        public Elements(string tenantId)
        {
            TenantId = tenantId;
            this.baseUrl = new Uri(string.Format("https://prod01.elementscloud.no/publikum/{0}/Dmb", TenantId));

            http.DefaultRequestHeaders.Referrer = baseUrl;
            http.DefaultRequestHeaders.Add("Tenant", TenantId);
        }

        public async Task<IEnumerable<Meeting>> FindMeetings(string phrase, ISet<string> seenMeetings)
        {
            string json = await http.GetStringAsync(string.Format("https://prod01.elementscloud.no/publikum/api/PredefinedQuery/DmbMeetings?year={0}", DateTime.Now.Year));

            List<Meeting> newMeetings = new List<Meeting>();

            JArray meetings = JArray.Parse(json);

            string searchPhrase = phrase?.ToLower();

            foreach (JObject meeting in meetings)
            {
                string id = (string)meeting["MO_ID"];
                string meetingUrl = string.Format(MEETING_URL_TEMPLATE, TenantId, id);

                if (seenMeetings.Contains(meetingUrl.ToString()))
                    continue;

                string agendaItemsUrl = string.Format(AGENDA_ITEMS_URL_TEMPLATE, id);

                json = await http.GetStringAsync(agendaItemsUrl);

                JArray agendaItems = JArray.Parse(json);

                foreach (JObject agendaItem in agendaItems)
                {
                    Meeting foundMeeting = new Meeting();
                    foundMeeting.Id = (int)agendaItem["Id"];
                    foundMeeting.Title = (string)agendaItem["Title"];
                    foundMeeting.Url = new Uri(meetingUrl);
                    foundMeeting.BoardName = (string)agendaItem["Dmb"]["Name"];
                    foundMeeting.Date = (DateTime)agendaItem["Meeting"]["StartDate"];
                    foundMeeting.DocumentsUrl = new Uri(string.Format(AGENDA_ITEM_URL_TEMPLATE, foundMeeting.Id));

                    foundMeeting.Documents = new List<Document>(await GetDocuments(foundMeeting));

                    if (string.IsNullOrEmpty(searchPhrase) || foundMeeting.Title.ToLower().Contains(searchPhrase) || foundMeeting.Documents.Any(d => d.Title.ToLower().Contains(searchPhrase)))
                        newMeetings.Add(foundMeeting);
                }
            }

            return newMeetings;
        }

        public async Task<IEnumerable<Document>> GetDocuments(Meeting meeting)
        {
            if (meeting.DocumentsUrl == null)
                return new List<Document>();

            try
            {
                string json = await http.GetStringAsync(meeting.DocumentsUrl);

                JObject agendaItemDetails = JObject.Parse(json);
                var documents = new List<Document>();

                foreach (JObject document in agendaItemDetails["RegistryEntry"]["Documents"])
                {
                    documents.Add(new Document
                    {
                        Id = (int)document["DocumentDescription"]["Id"],
                        Title = (string)document["DocumentDescription"]["DocumentTitle"],
                        Meeting = meeting,
                        Url = new Uri(string.Format(DOCUMENT_URL_TEMPLATE, (string)agendaItemDetails["RegistryEntry"]["Database"], (string)agendaItemDetails["RegistryEntry"]["Id"], (int)document["DocumentDescription"]["Id"]))
                    });
                }

                return documents;
            }
            catch (Exception ex)
            {
                return new List<Document>();
            }
        }
    }
}