using Newtonsoft.Json;
using OpenGov.Models;
using OpenGov.Scrapers;
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

namespace OpenGovAlerts
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

            foreach (Client client in config.Clients)
            {
                IScraper scraper = null;

                if (!string.IsNullOrEmpty(client.OpenGovId))
                    scraper = new OpenGov.Scrapers.OpenGov(client.OpenGovId);
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
                                List<string> loadedMeetings = new List<string>();

                                if (!Directory.Exists(Path.Combine(client.Name, search.Phrase)))
                                    Directory.CreateDirectory(Path.Combine(client.Name, search.Phrase));

                                string seenMeetingsPath = Path.Combine(client.Name, search.Phrase, "meetings.txt");

                                if (File.Exists(seenMeetingsPath))
                                {
                                    loadedMeetings = (await File.ReadAllTextAsync(seenMeetingsPath)).Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).ToList();
                                }

                                HashSet<string> seenMeetings = new HashSet<string>(loadedMeetings);

                                var newMeetingsForThisSearch = newMeetings.Where(m => !loadedMeetings.Any(l => l.Equals(m.Url.ToString())));

                                string searchPhrase = search.Phrase.ToLower();

                                List<Meeting> foundMeetings = new List<Meeting>();

                                foreach (Meeting newMeeting in newMeetingsForThisSearch)
                                {
                                    if (newMeeting.Title.ToLower().Contains(searchPhrase))
                                    {
                                        Console.WriteLine("Found meeting from " + client.Name + " for '" + search.Phrase + "': " + newMeeting.Url.ToString());
                                        foundMeetings.Add(newMeeting);
                                        continue;
                                    }

                                    if (newMeeting.Documents != null)
                                    {
                                        foreach (Document document in newMeeting.Documents)
                                        {
                                            if (!string.IsNullOrEmpty(document.Title) && document.Title.ToLower().Contains(searchPhrase))
                                            {
                                                Console.WriteLine("Found meeting from " + client.Name + " for '" + search.Phrase + "': " + newMeeting.Url.ToString());
                                                foundMeetings.Add(newMeeting);
                                                break;
                                            }
                                        }
                                    }
                                }

                                seenMeetings.UnionWith(newMeetingsForThisSearch.Select(meeting => meeting.Url.ToString()));

                                newMeetingsForThisSearch = foundMeetings.Where(m => DateTime.Now.Subtract(m.Date).TotalDays < 30);

                                if (newMeetingsForThisSearch.Any())
                                {
                                    MailMessage email = new MailMessage(config.Smtp.Sender, client.NotifyEmail);
                                    email.SubjectEncoding = Encoding.Unicode;
                                    email.Subject = "Nye møter for " + search.Name + " i " + client.Name;

                                    StringBuilder body = new StringBuilder();
                                    body.AppendFormat("<h3>Nye møter har dukket opp på kalenderen for {0} i {1}:</h3>\r\n\r\n<table>", search.Name, client.Name);

                                    foreach (var meeting in newMeetingsForThisSearch.OrderByDescending(m => m.Date))
                                    {
                                        body.AppendFormat("<tr><td><a href=\"{1}\">{2}</a></td><td><a href=\"{1}\">{0}</a></td><td><a href=\"{1}\">{3}</a></td></tr>\r\n", meeting.BoardName, meeting.Url, meeting.Date.ToString("dd.MM.yyyy"), meeting.Title);
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

                                using (var file = new StreamWriter(new FileStream(seenMeetingsPath, FileMode.Create, FileAccess.Write)))
                                {
                                    foreach (string meetingUrl in seenMeetings)
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