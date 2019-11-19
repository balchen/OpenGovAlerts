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
                ISet<string> seenMeetings = new HashSet<string>(await db.Meetings.Where(m => m.Source == source).Select(m => m.Url.ToString()).ToListAsync().ConfigureAwait(false));
                IScraper scraper = CreateScraper(source.Url);

                IEnumerable<Meeting> meetings = await scraper.FindMeetings(null, seenMeetings).ConfigureAwait(false);

                foreach (Meeting meeting in meetings)
                {
                    meeting.Source = source;
                    await db.Meetings.AddAsync(meeting).ConfigureAwait(false);

                    IEnumerable<Document> documents = await scraper.GetDocuments(meeting).ConfigureAwait(false);

                    foreach (Document document in documents)
                    {
                        document.Meeting = meeting;
                        await GetText(document).ConfigureAwait(false);
                        await db.Documents.AddAsync(document).ConfigureAwait(false);
                        await db.SaveChangesAsync().ConfigureAwait(false);
                    }
                }
            }
        }

        private async Task MatchSearches()
        {
            foreach (Search search in await db.Searches.Include(s => s.Sources).Include(s => s.Observer).Include(s => s.SeenMeetings).ThenInclude(sm => sm.Meeting).ToListAsync().ConfigureAwait(false))
            {
                foreach (Meeting meeting in await db.Meetings.Include(m => m.Documents).Where(m => (search.Sources.Count == 0 || search.Sources.Any(ss => ss.SourceId == m.Source.Id)) && !search.SeenMeetings.Any(sm => sm.MeetingId == m.Id)).ToListAsync().ConfigureAwait(false))
                {
                    await db.SeenMeetings.AddAsync(new SeenMeeting { Meeting = meeting, Search = search, DateSeen = DateTime.UtcNow }).ConfigureAwait(false);

                    if (Match(search, meeting))
                    {
                        await db.Matches.AddAsync(new Match { Meeting = meeting, Search = search, TimeFound = DateTime.UtcNow }).ConfigureAwait(false);
                    }
                }
            }

            await db.SaveChangesAsync().ConfigureAwait(false);
        }

        private async Task NotifyObservers()
        {
            foreach (var toNotify in await db.Matches.Include(m => m.Search).ThenInclude(s => s.Observer).Include(m => m.Meeting).ThenInclude(m => m.Source).Where(m => m.TimeNotified == null).GroupBy(m => m.Search.Observer).ToListAsync().ConfigureAwait(false))
            {
                //await UploadToStorage(toNotify.Key, toNotify).ConfigureAwait(false);
                //await AddToTaskManager(toNotify.Key, toNotify).ConfigureAwait(false);

                Smtp smtp = new Smtp();
                await smtp.Notify(toNotify, toNotify.Key).ConfigureAwait(false);

                foreach (Match match in toNotify)
                {
                    match.TimeNotified = DateTime.UtcNow;
                }

                await db.SaveChangesAsync().ConfigureAwait(false);
            }
        }

        private async Task AddToTaskManager(Observer key, IEnumerable<Match> toNotify)
        {
            throw new NotImplementedException();
        }

        private async Task UploadToStorage(Observer observer, IEnumerable<Match> matches)
        {
            foreach (StorageConfig storageConfig in observer.Storage)
            {
                IStorage storageProvider = GetStorageProvider(storageConfig.Url);

                foreach (Match match in matches)
                {
                    foreach (Document document in match.Meeting.Documents)
                    {
                        string path = Path.Combine(match.Meeting.Source.Name, match.Search.Name, match.Meeting.Date.ToString("yyyy-MM-dd") + "-" + match.Meeting.BoardName);
                        Uri documentUrl = await storageProvider.AddDocument(match.Meeting, document, path).ConfigureAwait(false);
                    }
                }
            }
        }

        private bool Match(Search search, Meeting meeting)
        {
            if (meeting.Title.Contains(search.Phrase, StringComparison.CurrentCultureIgnoreCase))
                return true;

            foreach (Document document in meeting.Documents)
            {
                if (document.Text != null && document.Text.Contains(search.Phrase, StringComparison.CurrentCultureIgnoreCase))
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
                    return new Elements(new Uri(parameters));
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
    }
}