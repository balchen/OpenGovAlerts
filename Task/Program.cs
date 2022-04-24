using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using PoliticalAlerts.Models;
using PoliticalAlerts.Scrapers;
using PoliticalAlertsWeb.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace PoliticalAlertsTask
{
    class Program
    {
        static readonly char[] invalidFileNameChars = Path.GetInvalidFileNameChars();

        [Obsolete("Do not use this in Production code!!!", false)]
        static void NEVER_EAT_POISON_Disable_CertificateValidation()
        {
            // Disabling certificate validation can expose you to a man-in-the-middle attack
            // which may allow your encrypted message to be read by an attacker
            // https://stackoverflow.com/a/14907718/740639
            ServicePointManager.ServerCertificateValidationCallback =
                delegate (
                    object s,
                    X509Certificate certificate,
                    X509Chain chain,
                    SslPolicyErrors sslPolicyErrors
                )
                {
                    return true;
                };
        }

        static async Task Main(string[] args)
        {
            var config = JsonConvert.DeserializeObject<Config>(await File.ReadAllTextAsync("config.json"));

            if (args.Length == 2 && args[0] == "migrate")
            {
                await Migrate(config, args[1]);
            }
            else
            {
                await Run(config);
            }
        }

        static async Task Migrate(Config config, string connectionString)
        {
            AlertsDbContext db = new AlertsDbContext(new DbContextOptionsBuilder().UseSqlServer(connectionString).Options);

            await db.UpdateSchema();

            db.SeenAgendaItems.RemoveRange(db.SeenAgendaItems);
            db.Matches.RemoveRange(db.Matches);
            db.AgendaItems.RemoveRange(db.AgendaItems);
            db.ObserverSearches.RemoveRange(db.ObserverSearches);
            db.SearchSources.RemoveRange(db.SearchSources);
            db.Searches.RemoveRange(db.Searches);
            db.Observers.RemoveRange(db.Observers);
            db.Sources.RemoveRange(db.Sources);

            await db.SaveChangesAsync();

            Dictionary<string, Source> sourcesByUrl = new Dictionary<string, Source>();
            Dictionary<string, Source> sourcesById = new Dictionary<string, Source>();
            Dictionary<string, Observer> observers = new Dictionary<string, Observer>();
            Dictionary<string, string> sourceObservers = new Dictionary<string, string>();

            foreach (Client client in config.Clients)
            {
                string sourceUrl = "";

                if (!string.IsNullOrEmpty(client.OpenGovId))
                    sourceUrl = "opengov:" + client.OpenGovId;
                else if (!string.IsNullOrEmpty(client.JupiterUrl))
                    sourceUrl = "jupiter:" + client.JupiterUrl;
                else if (!string.IsNullOrEmpty(client.SRUUrl))
                    sourceUrl = "sru:" + client.SRUUrl;
                else if (!string.IsNullOrEmpty(client.ACOSUrl))
                    sourceUrl = "acos:" + client.ACOSUrl;
                else if (!string.IsNullOrEmpty(client.ElementsTenantId))
                    sourceUrl = "elements:" + client.ElementsTenantId;

                if (!sourcesByUrl.ContainsKey(sourceUrl))
                {
                    Source source = new Source
                    {
                        Name = client.Name,
                        Url = sourceUrl
                    };

                    source = db.Add(source).Entity;

                    sourcesByUrl.Add(sourceUrl, source);
                    sourcesById.Add(client.Id, source);
                }
                else
                {
                    Source source = sourcesByUrl[sourceUrl];
                    sourcesById.Add(client.Id, source);
                }

                if (!observers.ContainsKey(client.NotifyEmail))
                {
                    observers.Add(client.NotifyEmail, db.Add(new Observer { 
                        Emails = new string[] { client.NotifyEmail },  
                        SmtpServer = config.Smtp.Server,
                        SmtpPort = config.Smtp.Port,
                        SmtpSender = config.Smtp.Sender,
                        SmtpPassword = config.Smtp.Password,
                        SmtpUseSsl = config.Smtp.UseSsl
                    }).Entity);
                }

                sourceObservers.Add(client.Id, client.NotifyEmail);
            }

            await db.SaveChangesAsync();

            foreach (Search search in config.Searches)
            {
                var dbSearch = db.Searches.Add(new PoliticalAlerts.Models.Search {
                    Name = search.Name,
                    Phrase = search.Phrase,
                    Start = DateTime.UtcNow
                }).Entity;

                await db.SaveChangesAsync();
                
                Observer observer;
                IEnumerable<string> sources;

                if (search.ClientId == "*")
                {
                    sources = sourcesById.Keys;
                }
                else
                {
                    sources = search.ClientId.Split(',');
                }

                foreach (string sourceId in sources)
                {
                    Source source = sourcesById[sourceId];

                    db.SearchSources.Add(new SearchSource
                    {
                        Search = dbSearch,
                        Source = source
                    });

                    await db.SaveChangesAsync();

                    observer = observers[sourceObservers[sourceId]];

                    if (!db.ObserverSearches.Any(os => os.SearchId == dbSearch.Id && os.ObserverId == observer.Id))
                    {
                        db.ObserverSearches.Add(new ObserverSearch
                        {
                            Search = dbSearch,
                            Observer = observers[sourceObservers[sourceId]],
                            Activated = DateTime.UtcNow
                        });

                        await db.SaveChangesAsync();
                    }
                }
            }
        }

        static async Task Run(Config config)
        {
            foreach (Client client in config.Clients)
            {
                IScraper scraper = null;

                if (!string.IsNullOrEmpty(client.OpenGovId))
                    scraper = new OpenGov(client.OpenGovId);
                else if (!string.IsNullOrEmpty(client.JupiterUrl))
                    scraper = new Jupiter(client.JupiterUrl);
                else if (!string.IsNullOrEmpty(client.SRUUrl))
                    scraper = new SRU(new Uri(client.SRUUrl));
                else if (!string.IsNullOrEmpty(client.ACOSUrl))
                    scraper = new ACOS(new Uri(client.ACOSUrl));
                else if (!string.IsNullOrEmpty(client.ElementsTenantId))
                    scraper = new Elements(client.ElementsTenantId);

                try
                {
                    Console.WriteLine("Getting new meetings for " + client.Name);
                    IEnumerable<Meeting> newMeetings = await scraper.GetNewMeetings(new HashSet<string>());

                    if (newMeetings.Any())
                    {
                        Console.WriteLine(newMeetings.Count() + " new meetings found for " + client.Name);
                        foreach (Search search in config.Searches)
                        {
                            var clientIds = search.ClientId.Split(',');
                            if (search.ClientId == "*" || clientIds.Any(id => id == client.Id))
                            {
                                Console.WriteLine("Searching new meetings from " + client.Name + " for '" + search.Phrase + "'");
                                List<string> loadedAgendaItems = new List<string>();

                                if (!Directory.Exists(Path.Combine(client.Name, search.Phrase)))
                                    Directory.CreateDirectory(Path.Combine(client.Name, search.Phrase));

                                string seenAgendaItemsPath = Path.Combine(client.Name, search.Phrase, "meetings.txt");

                                if (File.Exists(seenAgendaItemsPath))
                                {
                                    loadedAgendaItems = (await File.ReadAllTextAsync(seenAgendaItemsPath)).Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).ToList();
                                }

                                HashSet<string> seenAgendaItems = new HashSet<string>(loadedAgendaItems);

                                string searchPhrase = search.Phrase.ToLower();

                                List<AgendaItem> foundAgendaItems = new List<AgendaItem>();

                                foreach (Meeting newMeeting in newMeetings)
                                {
                                    var newAgendaItemsForThisSearch = newMeeting.AgendaItems.Where(m => !loadedAgendaItems.Any(l => l.Equals(m.Url.ToString())));

                                    foreach (AgendaItem newAgendaItem in newAgendaItemsForThisSearch)
                                    {
                                        if (newAgendaItem.Title.ToLower().Contains(searchPhrase))
                                        {
                                            Console.WriteLine("Found meeting from " + client.Name + " for '" + search.Phrase + "': " + newMeeting.Url.ToString());
                                            foundAgendaItems.Add(newAgendaItem);
                                            continue;
                                        }

                                        if (newAgendaItem.Documents != null)
                                        {
                                            foreach (Document document in newAgendaItem.Documents)
                                            {
                                                if (!string.IsNullOrEmpty(document.Title) && document.Title.ToLower().Contains(searchPhrase))
                                                {
                                                    Console.WriteLine("Found meeting from " + client.Name + " for '" + search.Phrase + "': " + newMeeting.Url.ToString());
                                                    foundAgendaItems.Add(newAgendaItem);
                                                    break;
                                                }
                                            }
                                        }
                                    }

                                    seenAgendaItems.UnionWith(newAgendaItemsForThisSearch.Select(agendaItem => agendaItem.Url.ToString()));
                                }

                                foundAgendaItems = foundAgendaItems.Where(a => DateTime.Now.Subtract(a.Meeting.Date).TotalDays < 30).ToList();

                                if (foundAgendaItems.Any())
                                {
                                    MailMessage email = new MailMessage(config.Smtp.Sender, client.NotifyEmail);
                                    email.SubjectEncoding = Encoding.Unicode;
                                    email.Subject = "Nye møter for " + search.Name + " i " + client.Name;

                                    StringBuilder body = new StringBuilder();
                                    body.AppendFormat("<h3>Nye møter har dukket opp på kalenderen for {0} i {1}:</h3>\r\n\r\n<table>", search.Name, client.Name);

                                    foreach (var agendaItem in foundAgendaItems.OrderByDescending(a => a.Meeting.Date))
                                    {
                                        body.AppendFormat("<tr><td><a href=\"{1}\">{2}</a></td><td><a href=\"{1}\">{0}</a></td><td><a href=\"{1}\">{3}</a></td></tr>\r\n", agendaItem.Meeting.BoardName, agendaItem.Url, agendaItem.Meeting.Date.ToString("dd.MM.yyyy"), agendaItem.Title);
                                    }

                                    body.Append("</table>");

                                    email.Body = body.ToString();
                                    email.IsBodyHtml = true;
                                    email.BodyEncoding = Encoding.UTF8;
                                    email.BodyTransferEncoding = TransferEncoding.Base64;

                                    SmtpClient smtp = new SmtpClient(config.Smtp.Server, config.Smtp.Port);
                                    smtp.UseDefaultCredentials = false;
                                    smtp.Credentials = new NetworkCredential(config.Smtp.Sender, config.Smtp.Password);
                                    smtp.EnableSsl = true;

                                    NEVER_EAT_POISON_Disable_CertificateValidation();

                                    await smtp.SendMailAsync(email);
                                }

                                using (var file = new StreamWriter(new FileStream(seenAgendaItemsPath, FileMode.Create, FileAccess.Write)))
                                {
                                    foreach (string meetingUrl in seenAgendaItems)
                                        await file.WriteLineAsync(meetingUrl);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error while processing meetings: " + ex.Message + "\n" + ex.StackTrace);
                }
            }
        }
    }
}