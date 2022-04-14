using Hangfire;
using OpenGov.Scrapers;
using OpenGov.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using OpenGovAlerts.Models;
using System.Threading.Tasks;
using System.Net.Http;
using System.IO;
using Microsoft.EntityFrameworkCore;
using OpenGov.Notifiers;
using OpenGov.Storage;
using System.Net.Http.Headers;
using IronPdf;
using Hangfire.Storage;
using OpenGov.TaskManagers;

namespace OpenGovAlerts.Services
{
    public class SyncService
    {
        private AlertsDbContext db;

        public SyncService(AlertsDbContext db)
        {
            this.db = db;
        }

        public async Task Synchronize()
        {
            await FetchNewMeetings().ConfigureAwait(false);
            await MatchSearches().ConfigureAwait(false);
            await NotifyObservers().ConfigureAwait(false);
        }

        public void SynchronizeSync()
        {
            Synchronize().GetAwaiter().GetResult();
        }

        private async Task FetchNewMeetings()
        {
            foreach (Source source in await db.Sources.ToListAsync().ConfigureAwait(false))
            {
                ISet<string> seenAgendaItems = new HashSet<string>(await db.AgendaItems.Include(a => a.Meeting).Where(a => a.Meeting.Source == source).Select(a => a.Url.ToString()).ToListAsync().ConfigureAwait(false));
                IScraper scraper = CreateScraper(source.Url);

                IEnumerable<Meeting> meetings = await scraper.GetNewMeetings(seenAgendaItems).ConfigureAwait(false);

                foreach (Meeting meeting in meetings)
                {
                    meeting.Source = source;
                    db.Meetings.Add(meeting);
                    await db.SaveChangesAsync().ConfigureAwait(false);

                    foreach (AgendaItem item in meeting.AgendaItems)
                    {
                        IEnumerable<Document> documents = await scraper.GetDocuments(item).ConfigureAwait(false);

                        foreach (Document document in documents)
                        {
                            document.AgendaItem = item;
                            await GetText(document).ConfigureAwait(false);
                            db.Documents.Add(document);
                            await db.SaveChangesAsync().ConfigureAwait(false);
                        }
                    }
                }
            }
        }

        private async Task MatchSearches()
        {
            foreach (Search search in await db.Searches.Include(s => s.Sources).Include(s => s.CreatedBy).Include(s => s.SeenAgendaItems).ThenInclude(sm => sm.AgendaItem).ToListAsync().ConfigureAwait(false))
            {
                foreach (Meeting meeting in await db.Meetings.Include(m => m.AgendaItems).ThenInclude(a => a.Documents).Where(m => (search.Sources.Count == 0 || search.Sources.Any(ss => ss.SourceId == m.Source.Id)) && !search.SeenAgendaItems.Any(sm => sm.AgendaItemId == m.Id)).ToListAsync().ConfigureAwait(false))
                {
                    foreach (AgendaItem item in meeting.AgendaItems)
                    {
                        db.SeenAgendaItems.Add(new SeenAgendaItem { AgendaItem = item, Search = search, DateSeen = DateTime.UtcNow });

                        if (Match(search, item))
                        {
                            db.Matches.Add(new Match { AgendaItem = item, Search = search, TimeFound = DateTime.UtcNow });
                        }
                    }
                }
            }

            await db.SaveChangesAsync().ConfigureAwait(false);
        }

        private async Task NotifyObservers()
        {
            foreach (var toNotify in await db.Matches
                .Include(m => m.Search).ThenInclude(s => s.Subscribers).ThenInclude(s => s.Observer)
                .Include(m => m.AgendaItem).ThenInclude(a => a.Meeting).ThenInclude(m => m.Source)
                .SelectMany(m => m.Search.Subscribers.Select(s => new { Match = m, Search = m.Search, Subscriber = s.Observer }))
                .Where(m => m.Match.TimeNotified == null)
                .GroupBy(m => m.Subscriber)
                .ToListAsync().ConfigureAwait(false))
            {
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

        private bool Match(Search search, AgendaItem item)
        {
            if (item.Title.Contains(search.Phrase, StringComparison.CurrentCultureIgnoreCase))
                return true;

            foreach (Document document in item.Documents)
            {
                if (document.Title != null && document.Title.Contains(search.Phrase, StringComparison.CurrentCultureIgnoreCase))
                    return true;
            }

            return false;
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
            catch (Exception ex)
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

            RecurringJob.AddOrUpdate(() => SynchronizeSync(), Cron.Daily(0, 30), TimeZoneInfo.Local);
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
                    return new OpenGov.Scrapers.OpenGov(parameters);
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
                    return new OpenGov.Storage.Dropbox(uri.UserInfo, uri.PathAndQuery);
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