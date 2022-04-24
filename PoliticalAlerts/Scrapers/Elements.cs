using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using PoliticalAlerts.Models;

namespace PoliticalAlerts.Scrapers
{
    public class Elements : IScraper
    {
        public string TenantId { get; private set; }
        public Uri baseUrl;

        private HttpClient http = new HttpClient();

        private const string GET_AGENDA_ITEMS_URL_TEMPLATE = "https://prod01.elementscloud.no/publikum/api/DmbHandlings/GetByMeetingId/{0}";
        private const string GET_AGENDA_ITEM_URL_TEMPLATE = "https://prod01.elementscloud.no/publikum/api/DmbHandlings/{0}";
        private const string MEETING_URL_TEMPLATE = "https://prod01.elementscloud.no/publikum/{0}/DmbMeeting/{1}";
        private const string AGENDA_ITEM_URL_TEMPLATE = "https://prod01.elementscloud.no/publikum/{0}/DmbMeeting/{1}#{2}";
        private const string DOCUMENT_URL_TEMPLATE = "https://prod01.elementscloud.no/publikum/Documents/ShowDocument/{0}/{1}/{2}";

        public Elements(string tenantId)
        {
            TenantId = tenantId;
            this.baseUrl = new Uri(string.Format("https://prod01.elementscloud.no/publikum/{0}/Dmb", TenantId));

            http.DefaultRequestHeaders.Referrer = baseUrl;
            http.DefaultRequestHeaders.Add("Tenant", TenantId);
        }

        public async Task<IEnumerable<Meeting>> GetNewMeetings(ISet<string> seenMeetings)
        {
            string json = await http.GetStringAsync(string.Format("https://prod01.elementscloud.no/publikum/api/PredefinedQuery/DmbMeetings?year={0}", DateTime.Now.Year));

            List<Meeting> newMeetings = new List<Meeting>();

            JArray meetings = JArray.Parse(json);

            foreach (JObject meeting in meetings)
            {
                string id = (string)meeting["MO_ID"];
                string meetingUrl = string.Format(MEETING_URL_TEMPLATE, TenantId, id);

                Meeting newMeeting = new Meeting
                {
                    ExternalId = id,
                    AgendaItems = new List<AgendaItem>(),
                    Url = new Uri(meetingUrl),
                    BoardId = (string)meeting["UT_ID"],
                    BoardName = (string)meeting["UT_NAVN"],
                    Date = (DateTime)meeting["MO_START"]
                };

                string agendaItemsUrl = string.Format(GET_AGENDA_ITEMS_URL_TEMPLATE, id);

                json = await http.GetStringAsync(agendaItemsUrl);

                JArray agendaItems = JArray.Parse(json);

                foreach (JObject agendaItem in agendaItems)
                {
                    AgendaItem foundAgendaItem = new AgendaItem();

                    foundAgendaItem.ExternalId = (string)agendaItem["Id"];
                    foundAgendaItem.Title = (string)agendaItem["Title"];
                    foundAgendaItem.Url = new Uri(string.Format(AGENDA_ITEM_URL_TEMPLATE, TenantId, id, foundAgendaItem.ExternalId));
                    foundAgendaItem.DocumentsUrl = new Uri(string.Format(GET_AGENDA_ITEM_URL_TEMPLATE, foundAgendaItem.ExternalId));

                    //foundAgendaItem.Documents = new List<Document>(await GetDocuments(foundAgendaItem));
                    foundAgendaItem.Meeting = newMeeting;

                    newMeeting.AgendaItems.Add(foundAgendaItem);
                }

                newMeetings.Add(newMeeting);
            }

            return newMeetings;
        }

        public async Task<IEnumerable<Document>> GetDocuments(AgendaItem agendaItem)
        {
            if (agendaItem.DocumentsUrl == null)
                return new List<Document>();

            try
            {
                string json = await http.GetStringAsync(agendaItem.DocumentsUrl);

                JObject agendaItemDetails = JObject.Parse(json);
                var documents = new List<Document>();

                foreach (JObject document in agendaItemDetails["RegistryEntry"]["Documents"])
                {
                    documents.Add(new Document
                    {
                        Title = (string)document["DocumentDescription"]["DocumentTitle"],
                        AgendaItem = agendaItem,
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