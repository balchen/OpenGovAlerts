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

namespace OpenGovAlerts.Services
{
    public class SyncService
    {
        private AlertsDbContext db;

        public SyncService(AlertsDbContext db)
        {
            this.db = db;
        }

        public async void Synchronize(bool requeue = false)
        {
            await FetchNewMeetings();
            await MatchSearches();
            await NotifyObservers();

            if (requeue)
                ScheduleSynchronization();
        }

        private async Task FetchNewMeetings()
        {
            foreach (Source source in db.Sources)
            {
                ISet<string> seenMeetings = new HashSet<string>(db.Meetings.Where(m => m.Source == source).Select(m => m.Url.ToString()));
                IScraper scraper = CreateScraper(source.Url);

                IEnumerable<Meeting> meetings = await scraper.FindMeetings(null, seenMeetings);

                foreach (Meeting meeting in meetings)
                {
                    await db.Meetings.AddAsync(meeting);

                    IEnumerable<Document> documents = await scraper.GetDocuments(meeting);

                    foreach (Document document in documents)
                    {
                        await GetText(document);
                        await db.Documents.AddAsync(document);
                        meeting.Documents.Add(document);
                    }
                }

                await db.Meetings.AddRangeAsync(meetings);

                await db.SaveChangesAsync();
            }
        }

        private async Task MatchSearches()
        {
            foreach (Search search in db.Searches.Include(s => s.Observer).Include(s => s.SeenMeetings))
            {
                foreach (Meeting meeting in db.Meetings.Include(m => m.Documents).Where(m => (search.Sources.Count == 0 || search.Sources.Contains(m.Source)) && !search.SeenMeetings.Contains(m)))
                {
                    search.SeenMeetings.Add(meeting);

                    if (Match(search, meeting))
                    {
                        await db.Matches.AddAsync(new Match { Meeting = meeting, Search = search, TimeFound = DateTime.UtcNow });
                    }
                }
            }

            await db.SaveChangesAsync();
        }

        private async Task NotifyObservers()
        {
            foreach (var toNotify in await db.Matches.Include(m => m.Search).ThenInclude(s => s.Observer).Include(m => m.Meeting).ThenInclude(m => m.Source).Where(m => m.TimeNotified == null).GroupBy(m => m.Search.Observer).ToListAsync())
            {
                await UploadToStorage(toNotify.Key, toNotify);
                AddToTaskManager(toNotify.Key, toNotify);

                Smtp smtp = new Smtp();
                await smtp.Notify(toNotify, toNotify.Key);

                foreach (Match match in toNotify)
                {
                    match.TimeNotified = DateTime.UtcNow;
                }

                await db.SaveChangesAsync();
            }
        }

        private void AddToTaskManager(Observer key, IEnumerable<Match> toNotify)
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
                        Uri documentUrl = await storageProvider.AddDocument(match.Meeting, document, path);
                    }
                }
            }
        }

        private IStorage GetStorageProvider(string url)
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

        private bool Match(Search search, Meeting meeting)
        {
            if (meeting.Title.Contains(search.Phrase, StringComparison.CurrentCultureIgnoreCase))
                return true;

            foreach (Document document in meeting.Documents)
            {
                if (document.Text.Contains(search.Phrase, StringComparison.CurrentCultureIgnoreCase))
                    return true;
            }

            return false;
        }

        private async Task GetText(Document document)
        {
            try
            {
                HttpClient http = new HttpClient();

                var response = await http.GetAsync(document.Url);

                if (response.IsSuccessStatusCode)
                {
                    if (response.Content.Headers.ContentType.MediaType == "application/pdf")
                    {
                        var ocr = new IronOcr.AutoOcr();
                        var result = ocr.ReadPdf(await response.Content.ReadAsStreamAsync());
                        document.Text = result.Text;
                    }
                }
            }
            catch (Exception ex)
            {

            }
        }

        public void ScheduleSynchronization()
        {
            BackgroundJob.Schedule(() => Synchronize(true), new DateTimeOffset(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, 15, 00, 00, TimeSpan.FromSeconds(0)));
        }

        private IScraper CreateScraper(string sourceUrl)
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
                default:
                    throw new ArgumentException("Invalid source URL " + sourceUrl);
            }
        }
    }
}