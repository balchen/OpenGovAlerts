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
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace OpenGovAlerts
{
    class Program
    {
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
                var client = config.Clients.FirstOrDefault(c => c.Id == search.ClientId);

                if (client == null)
                {
                    Console.WriteLine(search.ClientId + " is invalid client for search " + search.Name);
                    continue;
                }

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

                IEnumerable<Meeting> newMeetings = await scraper.FindMeetings(search.Phrase.ToLower(), seenMeetings);

                if (newMeetings.Any())
                {
                    seenMeetings.UnionWith(newMeetings.Select(meeting => meeting.Url.ToString()));

                    MailMessage email = new MailMessage("nord-jaren@syklistene.no", "nord-jaren@syklistene.no");
                    email.Subject = "Nye møter for " + search.Name + " i " + client.Name;

                    StringBuilder body = new StringBuilder();
                    body.AppendFormat("<h3>Nye møter har dukket opp på kalenderen for {0} i {1}:</h3>\r\n\r\n<table>", search.Name, client.Name);

                    foreach (var meeting in newMeetings.OrderByDescending(m => m.Date))
                    {
                        body.AppendFormat("<tr><td><a href=\"{1}\">{2}</a></td><td><a href=\"{1}\">{0}</a></td><td><a href=\"{1}\">{3}</a></td></tr>\r\n", meeting.Name, meeting.Url, meeting.Date.ToString("dd.MM.yyyy"), meeting.Topic);
                    }

                    body.Append("</table>");

                    email.Body = body.ToString();
                    email.IsBodyHtml = true;
                    email.BodyEncoding = Encoding.UTF8;
                    email.BodyTransferEncoding = System.Net.Mime.TransferEncoding.Base64;

                    SmtpClient smtp = new SmtpClient("mail.syklistene.no", 587);
                    smtp.UseDefaultCredentials = false;
                    smtp.Credentials = new NetworkCredential("nord-jaren@syklistene.no", "Hverdagssykling123");
                    smtp.EnableSsl = true;

                    NEVER_EAT_POISON_Disable_CertificateValidation();

                    await smtp.SendMailAsync(email);

                    await File.WriteAllLinesAsync(seenMeetingsPath, seenMeetings);
                }
            }
        }
    }
}
