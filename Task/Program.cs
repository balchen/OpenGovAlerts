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

                List<KeyValuePair<string, string>> loadedMeetings = new List<KeyValuePair<string, string>>();

                if (File.Exists(search.Phrase + "-meetings.txt"))
                {
                    loadedMeetings = (await File.ReadAllTextAsync(search.Phrase + "-meetings.txt")).Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).Select(m => new KeyValuePair<string, string>(m, m)).ToList();
                }

                Dictionary<string, string> seenMeetings = new Dictionary<string, string>(loadedMeetings);

                IList<Meeting> newMeetings = null;

                if (!string.IsNullOrEmpty(client.OpenGovId))
                    newMeetings = await GetOpenGovMeetings(search, client, seenMeetings);
                else if (!string.IsNullOrEmpty(client.JupiterUrl))
                    newMeetings = await GetJupiterMeetings(search, client, seenMeetings);

                if (newMeetings.Count > 0)
                {
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

        private async static Task<IList<Meeting>> GetJupiterMeetings(Search search, Client client, Dictionary<string, string> seenMeetings)
        {
            HttpClient http = new HttpClient();

            Uri url = new Uri(client.JupiterUrl);

            string html = await http.GetStringAsync(url);

            HtmlDocument calendar = new HtmlDocument();
            calendar.LoadHtml(html);

            List<Meeting> newMeetings = new List<Meeting>();

            foreach (var meetingLink in calendar.DocumentNode.SelectNodes("//div[@id='motekalender_table']/table//a"))
            {
                string meetingUrl = meetingLink.GetAttributeValue("href", null);
                string meetingTitle = meetingLink.GetAttributeValue("title", null);

                Uri meetingUri = new Uri(url, meetingUrl);

                if (seenMeetings.ContainsKey(meetingUri.ToString()))
                    continue;

                string meetingHtml = await http.GetStringAsync(meetingUri);

                HtmlDocument meetingInfo = new HtmlDocument();
                meetingInfo.LoadHtml(meetingHtml);

                string title = meetingInfo.DocumentNode.SelectSingleNode("//h3").ChildNodes[0].InnerText;
                var titleParts = title.Split(',');

                string body = titleParts[0];
                DateTime time;
                string timePart = titleParts[1].Trim();
                if (!DateTime.TryParseExact(timePart, "dd.MM.yyyy hh:mm", CultureInfo.CurrentCulture, DateTimeStyles.None, out time))
                {
                    string dateOnly = timePart.Split(' ')[0];

                    if (!DateTime.TryParseExact(dateOnly, "dd.MM.yyyy", CultureInfo.CurrentCulture, DateTimeStyles.None, out time))
                    {
                        time = new DateTime();
                    }
                }

                foreach (var heading in meetingInfo.DocumentNode.SelectNodes("//h4"))
                {
                    if (heading.InnerText == "Saker til behandling")
                    {
                        var agenda = heading.NextSibling.NextSibling;

                        foreach (var agendaItem in agenda.SelectNodes("descendant::td[@style='text-align: left;']"))
                        {
                            string agendaItemTitle = agendaItem.InnerText;

                            if (agendaItemTitle.ToLower().Contains(search.Phrase.ToLower()))
                            {
                                newMeetings.Add(new Meeting
                                {
                                    Date = time,
                                    Name = body,
                                    Topic = agendaItemTitle,
                                    Url = meetingUri
                                });
                            }
                        }
                    }
                }

                seenMeetings[meetingUri.ToString()] = meetingUri.ToString();
            }

            return newMeetings;
        }

        private static async Task<IList<Meeting>> GetOpenGovMeetings(Search search, Client client, Dictionary<string, string> seenMeetings)
        {
            HttpClient http = new HttpClient();

            Uri url = new Uri(string.Format("http://opengov.cloudapp.net/Meetings/{0}/AgendaItems/Search?q={1}", client.OpenGovId, search.Phrase));

            string html = await http.GetStringAsync(url);

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);

            List<Meeting> newMeetings = new List<Meeting>();

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
                        DateTime date = DateTime.ParseExact(HttpUtility.HtmlDecode(meeting.SelectSingleNode("descendant::div[@class='meetingDate']/span").InnerText), "dd.MM.yyyy", CultureInfo.CurrentCulture);
                        string topic = HttpUtility.HtmlDecode(meeting.SelectSingleNode("descendant::div[@class='serachMeetingResult']/p").InnerText);

                        newMeetings.Add(new Meeting
                        {
                            ClientId = client.OpenGovId,
                            Name = name,
                            Topic = topic,
                            Url = meetingUri,
                            Date = date
                        });

                        string agendaItemId = HttpUtility.ParseQueryString(meetingUri.Query).Get("agendaItemId");
                        await DownloadOpenGovDocuments(client.OpenGovId, agendaItemId, Path.Combine(Environment.CurrentDirectory, search.Phrase, date.ToString("yyyy-MM-dd") + " " + name));
                    }
                }
            }

            return newMeetings;
        }

        private async static Task<IEnumerable<Document>> DownloadOpenGovDocuments(string clientId, string agendaItemId, string path)
        {
            IEnumerable<Document> documents = await GetAgendaItemDocumentUrls(clientId, agendaItemId);

            HttpClient http = new HttpClient();

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            foreach (var document in documents)
            {
                using (var output = new FileStream(Path.Combine(path, document.Name + ".pdf"), FileMode.OpenOrCreate, FileAccess.Write))
                {
                    Stream input = await http.GetStreamAsync(document.Url);
                    await input.CopyToAsync(output);
                }
            }

            return documents;
        }

        private async static Task<IEnumerable<Document>> GetAgendaItemDocumentUrls(string clientId, string agendaItemId)
        {
            Uri url = new Uri(string.Format("http://opengov.cloudapp.net/Meetings/{0}/Meetings/LoadAgendaItemDetail/{1}", clientId, agendaItemId));

            HttpClient http = new HttpClient();

            string html = await http.GetStringAsync(url);

            HtmlDocument agendaItemDocuments = new HtmlDocument();
            agendaItemDocuments.LoadHtml(html);

            List<Document> documents = new List<Document>();

            foreach (var documentLink in agendaItemDocuments.DocumentNode.SelectNodes("//a"))
            {
                Document document = new Document();
                document.Url = new Uri(url, documentLink.Attributes["href"].Value);
                document.Name = documentLink.SelectSingleNode("descendant::div[@class='fileNameDetail']").InnerText;
                document.Type = documentLink.SelectSingleNode("descendant::div[@class='fileDocumentCategory']").InnerText;

                documents.Add(document);
            }

            return documents;
        }
    }
}
