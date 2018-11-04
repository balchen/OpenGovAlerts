using OpenGovAlerts.Models;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace OpenGovAlerts.Notifiers
{
    public class Smtp
    {
        private string host;
        private int port;
        private bool useSsl = false;
        private string username;
        private string password;

        public Smtp(string host, int port, string username, string password, bool useSsl)
        {
            this.host = host;
            this.port = port;
            this.username = username;
            this.password = password;
            this.useSsl = useSsl;
        }

        public async void Notify(IEnumerable<Meeting> meetings, string phraseName, string clientName)
        {
            MailMessage email = new MailMessage("nord-jaren@syklistene.no", "nord-jaren@syklistene.no");
            email.Subject = "Nye møter for " + phraseName + " i " + clientName;

            StringBuilder body = new StringBuilder();
            body.AppendFormat("<h3>Nye møter har dukket opp på kalenderen for {0} i {1}:</h3>\r\n\r\n<table>", phraseName, clientName);

            foreach (var meeting in meetings.OrderByDescending(m => m.Date))
            {
                body.AppendFormat("<tr><td><a href=\"{1}\">{2}</a></td><td><a href=\"{1}\">{0}</a></td></tr>\r\n", meeting.Name, meeting.Url, meeting.Date.ToString("dd.MM.yyyy"));
            }

            body.Append("</table>");

            email.Body = body.ToString();
            email.IsBodyHtml = true;

            SmtpClient smtp = new SmtpClient("mail.syklistene.no", 587);
            smtp.UseDefaultCredentials = false;
            smtp.Credentials = new NetworkCredential("nord-jaren@syklistene.no", "Hverdagssykling123");

            await smtp.SendMailAsync(email);
        }
    }
}