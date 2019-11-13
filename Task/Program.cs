using HtmlAgilityPack;
using Newtonsoft.Json;
using OpenGov.Models;
using OpenGov.Scrapers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Net.Mime;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Web;

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

            foreach (Search search in config.Searches)
            {
                var clients = config.Clients.Where(c => search.ClientId == "*" || c.Id == search.ClientId);

                if (!clients.Any())
                {
                    Console.WriteLine(search.ClientId + " is invalid client for search " + search.Name);
                    continue;
                }

                foreach (var client in clients)
                {
                    List<string> loadedMeetings = new List<string>();

                    if (!Directory.Exists(Path.Combine(client.Name, search.Phrase)))
                        Directory.CreateDirectory(Path.Combine(client.Name, search.Phrase));

                    string seenMeetingsPath = Path.Combine(client.Name, search.Phrase, "meetings.txt");

                    if (File.Exists(seenMeetingsPath))
                    {
                        loadedMeetings = (await File.ReadAllTextAsync(seenMeetingsPath)).Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).ToList();
                    }

                    HashSet<string> seenMeetings = new HashSet<string>(loadedMeetings);

                    IScraper scraper = null;

                    if (!string.IsNullOrEmpty(client.OpenGovId))
                        scraper = new OpenGov.Scrapers.OpenGov(client.OpenGovId);
                    else if (!string.IsNullOrEmpty(client.JupiterUrl))
                        scraper = new Jupiter(client.JupiterUrl);
                    else if (!string.IsNullOrEmpty(client.SRUUrl))
                        scraper = new SRU(new Uri(client.SRUUrl));
                    else if (!string.IsNullOrEmpty(client.ACOSUrl))
                        scraper = new ACOS(new Uri(client.ACOSUrl));
                    else if (!string.IsNullOrEmpty(client.ElementsUrl))
                        scraper = new Elements(new Uri(client.ElementsUrl));

                    try
                    {
                        IEnumerable<Meeting> newMeetings = await scraper.FindMeetings(search.Phrase.ToLower(), seenMeetings);

                        if (newMeetings.Any())
                        {
                            seenMeetings.UnionWith(newMeetings.Select(meeting => meeting.Url.ToString()));

                            MailMessage email = new MailMessage(config.Smtp.Sender, client.NotifyEmail);
                            email.Subject = "Nye møter for " + search.Name + " i " + client.Name;

                            StringBuilder body = new StringBuilder();
                            body.AppendFormat("<h3>Nye møter har dukket opp på kalenderen for {0} i {1}:</h3>\r\n\r\n<table>", search.Name, client.Name);

                            foreach (var meeting in newMeetings.OrderByDescending(m => m.Date))
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

                            using (var file = new StreamWriter(new FileStream(seenMeetingsPath, FileMode.Create, FileAccess.Write)))
                            {
                                foreach (string meetingUrl in seenMeetings)
                                    await file.WriteLineAsync(meetingUrl);
                            }
                        }
                    }
                    catch (Exception ex)
                    {

                    }
                }
            }
        }
    }
}