using HtmlAgilityPack;
using Newtonsoft.Json;
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
                ) {
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

                Uri url = new Uri(string.Format("http://opengov.cloudapp.net/Meetings/{0}/AgendaItems/Search?q={1}", client.OpenGovId, search.Phrase));

                List<KeyValuePair<string, string>> loadedMeetings = new List<KeyValuePair<string, string>>();

                if (File.Exists(search.Phrase + "-meetings.txt"))
                {
                    loadedMeetings = (await File.ReadAllTextAsync(search.Phrase + "-meetings.txt")).Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).Select(m => new KeyValuePair<string, string>(m, m)).ToList();
                }

                Dictionary<string, string> seenMeetings = new Dictionary<string, string>(loadedMeetings);

                HttpClient http = new HttpClient();

                string html = await http.GetStringAsync(url);

                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(html);

                List<Tuple<Uri, string, DateTime>> newMeetings = new List<Tuple<Uri, string, DateTime>>();

                var meetings = doc.DocumentNode.SelectNodes("//div[@class='meetingList searchResultsList']/ul/li/a");

                if (meetings != null)
                {
                    foreach (var meeting in meetings)
                    {
                        var meetingUrl = meeting.Attributes["href"].Value;
                        Uri meetingUri = new Uri(url, meetingUrl);
                        meetingUrl = meetingUri.ToString();

                        if (!seenMeetings.ContainsKey(meetingUrl))
                        {
                            seenMeetings[meetingUrl] = meetingUrl;

                            string name = HttpUtility.HtmlDecode(meeting.SelectSingleNode("descendant::div[@class='meetingName']/span").InnerText);
                            string date = HttpUtility.HtmlDecode(meeting.SelectSingleNode("descendant::div[@class='meetingDate']/span").InnerText);

                            newMeetings.Add(new Tuple<Uri, string, DateTime>(meetingUri, name, DateTime.ParseExact(date, "dd.MM.yyyy", CultureInfo.CurrentCulture)));
                        }
                    }
                }

                if (newMeetings.Count > 0)
                {
                    MailMessage email = new MailMessage("nord-jaren@syklistene.no", "nord-jaren@syklistene.no");
                    email.Subject = "Nye møter for " + search.Name + " i " + client.Name;

                    StringBuilder body = new StringBuilder();
                    body.AppendFormat("<h3>Nye møter har dukket opp på kalenderen for {0} i {1}:</h3>\r\n\r\n<table>", search.Name, client.Name);

                    foreach (var meeting in newMeetings.OrderByDescending(m => m.Item3))
                    {
                        body.AppendFormat("<tr><td><a href=\"{1}\">{2}</a></td><td><a href=\"{1}\">{0}</a></td></tr>\r\n", meeting.Item2, meeting.Item1, meeting.Item3.ToString("dd.MM.yyyy"));
                    }

                    body.Append("</table>");

                    email.Body = body.ToString();
                    email.IsBodyHtml = true;

                    SmtpClient smtp = new SmtpClient("mail.syklistene.no", 587);
                    smtp.UseDefaultCredentials = false;
                    smtp.Credentials = new NetworkCredential("nord-jaren@syklistene.no", "Hverdagssykling123");
                    smtp.EnableSsl = true;

                    NEVER_EAT_POISON_Disable_CertificateValidation();

                    await smtp.SendMailAsync(email);

                    await File.WriteAllLinesAsync(search.Phrase + "-meetings.txt", seenMeetings.Keys);
                }
            }
        }
    }
}
