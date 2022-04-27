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
        private const string GET_CASE_URL_TEMPLATE = "https://prod01.elementscloud.no/publikum/api/Cases/{0}";
        private const string GET_JOURNAL_ENTRY_DETAILS_URL_TEMPLATE = "https://prod01.elementscloud.no/publikum/api/RegistryEntries/{0}";
        private const string CASE_URL_TEMPLATE = "https://prod01.elementscloud.no/publikum/{0}/Case/{1}";
        private const string JOURNAL_ENTRY_URL_TEMPLATE = "https://prod01.elementscloud.no/publikum/{0}/RegistryEntry/{1}";

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

                    try
                    {
                        json = await http.GetStringAsync(foundAgendaItem.DocumentsUrl); // Will also return more agenda item details, such as case number

                        JObject agendaItemDetails = JObject.Parse(json);

                        foundAgendaItem.CaseNumber = (string)agendaItemDetails["Case"]["Id"];

                        var documents = new List<Document>();

                        foreach (JObject document in agendaItemDetails["RegistryEntry"]["Documents"])
                        {
                            documents.Add(new Document
                            {
                                Title = (string)document["DocumentDescription"]["DocumentTitle"],
                                AgendaItem = foundAgendaItem,
                                Url = new Uri(string.Format(DOCUMENT_URL_TEMPLATE, (string)agendaItemDetails["RegistryEntry"]["Database"], (string)agendaItemDetails["RegistryEntry"]["Id"], (int)document["DocumentDescription"]["Id"]))
                            });
                        }

                        foundAgendaItem.Documents = documents;
                    }
                    catch (Exception ex)
                    {
                    }

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

        public async Task<IEnumerable<JournalEntry>> GetCaseJournal(string caseNumber)
        {
            Uri url = new Uri(string.Format(GET_CASE_URL_TEMPLATE, caseNumber));

            List<JournalEntry> entries = new List<JournalEntry>();

            try
            {
                string json = await http.GetStringAsync(url); // Will also return more agenda item details, such as case number

                JObject caseDetails = JObject.Parse(json);

                foreach (JObject journalEntry in caseDetails["RegistryEntries"])
                {
                    string entryType = (string)journalEntry["RegistryEntryTypeId"];
                    JournalType parsedType = JournalType.Unclassified;

                    switch (entryType)
                    {
                        case "U":
                            parsedType = JournalType.Outbound;
                            break;
                        case "I":
                            parsedType = JournalType.Inbound;
                            break;
                        case "N":
                            parsedType = JournalType.Internal;
                            break;
                    }

                    string entryId = (string)journalEntry["Id"];

                    var entry = new JournalEntry
                    {
                        Date = (DateTime)journalEntry["RegistryDate"],
                        ExternalId = entryId,
                        From = parsedType == JournalType.Inbound ? (string)journalEntry["SenderRecipient"] : null,
                        To = parsedType == JournalType.Outbound ? (string)journalEntry["SenderRecipient"] : null,
                        ParsedType = parsedType,
                        Type = entryType,
                        Title = (string)journalEntry["Title"],
                        Url = new Uri(string.Format(JOURNAL_ENTRY_URL_TEMPLATE, TenantId, entryId))
                    };

                    Uri entryDetailsUrl = new Uri(string.Format(GET_JOURNAL_ENTRY_DETAILS_URL_TEMPLATE, entryId));

                    var documents = new List<Document>();

                    try
                    {
                        json = await http.GetStringAsync(entryDetailsUrl); // Will also return more agenda item details, such as case number

                        JObject journalEntryDetails = JObject.Parse(json);

                        foreach (JObject document in journalEntryDetails["Documents"])
                        {
                            documents.Add(new Document
                            {
                                Title = (string)document["DocumentDescription"]["DocumentTitle"],
                                Url = new Uri(string.Format(DOCUMENT_URL_TEMPLATE, (string)journalEntryDetails["Database"], (string)journalEntryDetails["Id"], (int)document["DocumentDescription"]["Id"]))
                            });

                            entry.Documents = documents;
                        }
                    }
                    catch (Exception ex)
                    {
                    }

                    entries.Add(entry);
                }
            }
            catch (Exception ex)
            {
            }

            return entries;
        }
    }
}