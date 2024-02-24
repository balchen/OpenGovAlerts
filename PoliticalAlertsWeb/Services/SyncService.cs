using Hangfire;
using PoliticalAlerts.Scrapers;
using PoliticalAlerts.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using PoliticalAlertsWeb.Models;
using System.Threading.Tasks;
using System.Net.Http;
using System.IO;
using Microsoft.EntityFrameworkCore;
using PoliticalAlerts.Notifiers;
using PoliticalAlerts.Storage;
using System.Net.Http.Headers;
using IronPdf;
using Hangfire.Storage;
using PoliticalAlerts.TaskManagers;
using Microsoft.Extensions.Logging;

namespace PoliticalAlertsService
{
    public class SyncService
    {
        private readonly AlertsDbContext db;
        private readonly ILogger<SyncService> logger;

        public SyncService(AlertsDbContext db, ILogger<SyncService> logger)
        {
            this.db = db;
            this.logger = logger;
        }

        public async Task Synchronize()
        {
            try
            {
                await FetchNewMeetings().ConfigureAwait(false);
                await MatchSearches().ConfigureAwait(false);
                await UpdateJournals().ConfigureAwait(false);
                await MatchConsultations().ConfigureAwait(false);
                await NotifyObservers().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError("Synchronization failed", ex);
            }
        }

        public void SynchronizeSync()
        {
            Synchronize().GetAwaiter().GetResult();
        }

        private async Task MatchConsultations()
        {
            logger.LogInformation("Searching for consultations...");

            foreach (ConsultationSearch search in await db.ConsultationSearches.Include(s => s.Sources).Include(s => s.SeenJournalEntries).ThenInclude(sm => sm.JournalEntry).ToListAsync().ConfigureAwait(false))
            {
                logger.LogInformation("Finding matches for search " + search.Name);
                foreach (JournalEntry journal in await db.JournalEntries.Include(j => j.Documents)
                    .Where(j => !db.SeenJournalEntries.Any(sj => sj.JournalEntryId == j.Id && sj.ConsultationSearchId == search.Id)).ToListAsync().ConfigureAwait(false))
                {
                    logger.LogDebug("Search {0} has now seen new journal entry {1}", search.Name, journal.Url.ToString());

                    db.SeenJournalEntries.Add(new SeenJournalEntry { JournalEntry = journal, ConsultationSearch = search, DateSeen = DateTime.UtcNow });

                    string excerpt;

                    logger.LogDebug("Matching search {0} with journal entry {1}", search.Name, journal.Url.ToString());

                    if (journal.ParsedType == JournalType.Outbound && Match(search.Phrase, journal.Title, out excerpt))
                    {
                        logger.LogDebug("Search {0} has found a new match journal entry {1}", search.Name, journal.Url.ToString());
                        db.ConsultationMatches.Add(new ConsultationMatch { JournalEntry = journal, Search = search, TimeFound = DateTime.UtcNow, Excerpt = excerpt });
                    }
                }
            }

            await db.SaveChangesAsync().ConfigureAwait(false);
        }

        private async Task UpdateJournals()
        {
            logger.LogInformation("Updating journals...");

            foreach (AgendaItem item in await db.AgendaItems.Include(a => a.Meeting).ThenInclude(m => m.Source).Where(a => a.CaseNumber != null && a.MonitorConsultations).ToListAsync().ConfigureAwait(false))
            {
                IScraper scraper = CreateScraper(item.Meeting.Source.Url);

                logger.LogInformation("Scraping journal entries from " + item.Meeting.Source.Name + " for agenda item " + item.Title);

                var newJournalEntries = await scraper.GetCaseJournal(item.CaseNumber);

                var existingDocuments = await db.Documents.Where(d => d.AgendaItem == item).ToListAsync().ConfigureAwait(false);

                logger.LogDebug("Found {0] new journal entries from {1} for agenda item {2}", newJournalEntries.Count(), item.Meeting.Source.Name, item.Title);

                foreach (var newJournalEntry in newJournalEntries)
                {
                    logger.LogDebug("Processing journal entry {0} from {1} for agenda item {2}", newJournalEntry.Url.ToString(), item.Meeting.Source.Name, item.Title);
                    var dbJournalEntry = await db.JournalEntries.FirstOrDefaultAsync(j => j.Url == newJournalEntry.Url);

                    if (dbJournalEntry == null)
                    {
                        dbJournalEntry = db.JournalEntries.Add(new JournalEntry
                        {
                            AgendaItem = item,
                            Title = newJournalEntry.Title,
                            Url = newJournalEntry.Url,
                            Type = newJournalEntry.Type,
                            ParsedType = newJournalEntry.ParsedType,
                            ExternalId = newJournalEntry.ExternalId
                        }).Entity;
                    }

                    foreach (var newDocument in newJournalEntry.Documents)
                    {
                        if (!existingDocuments.Any(e => e.Url == newDocument.Url))
                        {
                            db.Documents.Add(new Document { 
                                AgendaItem = item,
                                JournalEntry = dbJournalEntry,
                                Title = newDocument.Title,
                                Text = newDocument.Text,
                                Type = newDocument.Type,
                                Url = newDocument.Url
                            });
                        }
                    }
                }
            }

            await db.SaveChangesAsync();
        }

        private async Task FetchNewMeetings()
        {
            foreach (Source source in await db.Sources.ToListAsync().ConfigureAwait(false))
            {
                ISet<string> seenAgendaItems = new HashSet<string>(await db.AgendaItems.Include(a => a.Meeting).Where(a => a.Meeting.Source == source).Select(a => a.Url.ToString()).ToListAsync().ConfigureAwait(false));
                IScraper scraper = CreateScraper(source.Url);

                IEnumerable<Meeting> meetings = null;

                try
                {
                    logger.LogInformation("Getting meetings from source " + source.Name);
                    meetings = await scraper.GetNewMeetings(seenAgendaItems).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    logger.LogError("Failed to get new meetings from " + source.Name, ex);
                    continue;
                }

                foreach (Meeting meeting in meetings)
                {
                    meeting.Source = source;

                    foreach (AgendaItem item in meeting.AgendaItems)
                        item.Retrieved = DateTime.UtcNow;

                    db.Meetings.Add(meeting);
                    await db.SaveChangesAsync().ConfigureAwait(false);

                    foreach (AgendaItem item in meeting.AgendaItems)
                    {
                        IEnumerable<Document> documents = await scraper.GetDocuments(item).ConfigureAwait(false);

                        foreach (Document document in documents)
                        {
                            document.AgendaItem = item;
                            //await GetText(document).ConfigureAwait(false);
                            db.Documents.Add(document);
                            await db.SaveChangesAsync().ConfigureAwait(false);
                        }
                    }
                }
            }
        }

        private async Task MatchSearches()
        {
            foreach (Search search in await db.Searches.Include(s => s.Sources).Include(s => s.SeenAgendaItems).ThenInclude(sm => sm.AgendaItem).ToListAsync().ConfigureAwait(false))
            {
                logger.LogInformation("Finding matches for search " + search.Name);
                foreach (AgendaItem item in await db.AgendaItems.Include(a => a.Meeting).Include(a => a.Documents)
                    .Where(a => !db.SeenAgendaItems.Any(sa => sa.AgendaItemId == a.Id && sa.SearchId == search.Id)).ToListAsync().ConfigureAwait(false))
                {
                    db.SeenAgendaItems.Add(new SeenAgendaItem { AgendaItem = item, Search = search, DateSeen = DateTime.UtcNow });

                    string excerpt;

                    if (Match(search, item, out excerpt))
                    {
                        db.Matches.Add(new Match { AgendaItem = item, Search = search, TimeFound = DateTime.UtcNow, Excerpt = excerpt });
                    }
                }
            }

            await db.SaveChangesAsync().ConfigureAwait(false);
        }

        private async Task NotifyObservers()
        {
            foreach (var toNotify in (await db.Matches
                .Include(m => m.Search).ThenInclude(s => s.Subscribers).ThenInclude(s => s.Observer)
                .Include(m => m.AgendaItem).ThenInclude(a => a.Meeting).ThenInclude(m => m.Source)
                .Where(m => m.TimeNotified == null)
                .ToListAsync().ConfigureAwait(false))
                .SelectMany(m => m.Search.Subscribers.Select(s => new { Match = m, Search = m.Search, Subscriber = s.Observer }))
                .GroupBy(m => m.Subscriber))
            {
                logger.LogInformation("Notifying " + toNotify.Key.Name);

                var matches = toNotify.Select(m => m.Match);

                Smtp smtp = new Smtp();
                await smtp.Notify(matches, toNotify.Key).ConfigureAwait(false);

                foreach (Match match in matches)
                {
                    match.TimeNotified = DateTime.UtcNow;
                }

                await db.SaveChangesAsync().ConfigureAwait(false);

                //await UploadToStorage(toNotify.Key, matches).ConfigureAwait(false);
                //await AddToTaskManager(toNotify.Key, matches).ConfigureAwait(false);
            }

            foreach (var toNotify in (await db.ConsultationMatches
                .Include(m => m.Search).ThenInclude(s => s.Subscribers).ThenInclude(s => s.Observer)
                .Include(m => m.JournalEntry).ThenInclude(a => a.AgendaItem)
                .Where(m => m.TimeNotified == null)
                .ToListAsync().ConfigureAwait(false))
                .SelectMany(m => m.Search.Subscribers.Select(s => new { Match = m, Search = m.Search, Subscriber = s.Observer }))
                .GroupBy(m => m.Subscriber))
            {
                logger.LogInformation("Notifying " + toNotify.Key.Name);

                var matches = toNotify.Select(m => m.Match);

                Smtp smtp = new Smtp();
                await smtp.Notify(matches, toNotify.Key).ConfigureAwait(false);

                foreach (ConsultationMatch match in matches)
                {
                    match.TimeNotified = DateTime.UtcNow;
                }

                await db.SaveChangesAsync().ConfigureAwait(false);

                //await UploadToStorage(toNotify.Key, matches).ConfigureAwait(false);
                //await AddToTaskManager(toNotify.Key, matches).ConfigureAwait(false);
            }
        }

        private async Task AddToTaskManager(Observer observer, IEnumerable<Match> matches)
        {
            foreach (TaskManagerConfig config in observer.TaskManager)
            {
                ITaskManager taskManager = GetTaskManager(config.Url);

                foreach (Match match in matches)
                {
                    await taskManager.AddTask(match.AgendaItem);
                }
            }
        }

        private async Task UploadToStorage(Observer observer, IEnumerable<Match> matches)
        {
            foreach (StorageConfig storageConfig in observer.Storage)
            {
                IStorage storageProvider = GetStorageProvider(storageConfig.Url);

                foreach (Match match in matches)
                {
                    foreach (Document document in match.AgendaItem.Documents)
                    {
                        string path = Path.Combine(match.AgendaItem.Meeting.Source.Name, match.Search.Name, match.AgendaItem.Meeting.Date.ToString("yyyy-MM-dd") + "-" + match.AgendaItem.Meeting.BoardName);
                        Uri documentUrl = await storageProvider.AddDocument(match.AgendaItem, document, path).ConfigureAwait(false);
                    }
                }
            }
        }

        private bool Match(Search search, AgendaItem item, out string excerpt)
        {
            int pos = -1;
            excerpt = null;

            while ((pos = item.Title.IndexOf(search.Phrase, pos + 1, StringComparison.CurrentCultureIgnoreCase)) > -1)
            {
                excerpt += GetExcerpt(item.Title, pos);
            }

            foreach (Document document in item.Documents)
            {
                if (document.Title != null && (pos = document.Title.IndexOf(search.Phrase, StringComparison.CurrentCultureIgnoreCase)) > -1)
                {
                    excerpt += GetExcerpt(document.Title, pos);
                }
            }

            return excerpt != null;
        }

        private bool Match(string phrase, string source, out string excerpt)
        {
            int pos = -1;
            excerpt = null;

            while ((pos = source.IndexOf(phrase, pos + 1, StringComparison.CurrentCultureIgnoreCase)) > -1)
            {
                excerpt += GetExcerpt(source, pos);
            }

            return excerpt != null;
        }

        private string GetExcerpt(string title, int pos)
        {
            int start = Math.Max(0, pos - 10);
            int length = Math.Min(20, title.Length - start);

            return (start > 0 ? " ..." : "") + title.Substring(start, length) + (start + length < title.Length ? "... " : "");
        }

        public static async Task GetText(Document document)
        {
            try
            {
                HttpClient http = new HttpClient();

                var response = await http.GetAsync(document.Url).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    MediaTypeHeaderValue contentType = response.Content.Headers.ContentType;
                    KeyValuePair<string, IEnumerable<string>> rawHeaderValue = new KeyValuePair<string, IEnumerable<string>>();

                    if (contentType == null)
                    {
                        rawHeaderValue = response.Content.Headers.FirstOrDefault(h => h.Key == "Content-Type");
                    }

                    if (contentType?.MediaType == "application/pdf" || rawHeaderValue.Value.Any(v => v.ToLower().Contains("pdf")))
                    {
                        PdfDocument pdf = new PdfDocument(await response.Content.ReadAsStreamAsync().ConfigureAwait(false));
                        document.Text = pdf.ExtractAllText();
                    }
                }
            }
            catch (Exception)
            {

            }
        }

        public void ScheduleSynchronization()
        {
            using (var connection = JobStorage.Current.GetConnection())
            {
                foreach (var recurringJob in StorageConnectionExtensions.GetRecurringJobs(connection))
                {
                    RecurringJob.RemoveIfExists(recurringJob.Id);
                }
            }

            RecurringJob.AddOrUpdate(() => SynchronizeSync(), Cron.Hourly(), TimeZoneInfo.Local);
        }

        public static IScraper CreateScraper(string sourceUrl)
        {
            string type = sourceUrl.Split(":")[0];
            string parameters = sourceUrl.Substring(type.Length + 1);

            switch (type)
            {
                case "acos":
                    return new ACOS(new Uri(parameters));
                case "jupiter":
                    return new Jupiter(parameters);
                case "opengov":
                    return new OpenGov(parameters);
                case "sru":
                    return new SRU(new Uri(parameters));
                case "elements":
                    return new Elements(parameters);
                default:
                    throw new ArgumentException("Invalid source URL " + sourceUrl);
            }
        }

        public static IStorage GetStorageProvider(string url)
        {
            Uri uri = new Uri(url);

            switch (uri.Scheme)
            {
                case "dropbox":
                    return new PoliticalAlerts.Storage.Dropbox(uri.UserInfo, uri.PathAndQuery);
                case "file":
                    return new LocalDisk(uri.PathAndQuery);
                default:
                    throw new ArgumentException("Unknown storage provider URL " + url);
            }
        }

        public static ITaskManager GetTaskManager(string url)
        {
            Uri uri = new Uri(url);

            switch (uri.Scheme)
            {
                case "trello":
                    string[] boardAndListId = uri.AbsolutePath.Split('/');
                    return new Trello(uri.Host, uri.UserInfo, boardAndListId[0], boardAndListId[1]);
                default:
                    throw new ArgumentException("Unknown task manager URL " + url);
            }
        }
    }
}