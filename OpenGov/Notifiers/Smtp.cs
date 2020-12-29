using OpenGov.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace OpenGov.Notifiers
{
    public class Smtp: INotifier
    {
        public Smtp()
        {
        }

        public async Task Notify(IEnumerable<Match> matches, Observer observer)
        {
            StringBuilder body = new StringBuilder("<html><head></head><body>");

            foreach (var searches in matches.GroupBy(m => m.Search))
            {
                body.AppendFormat("<h3>Nye møter har dukket opp på kalenderen for {0}</h3>\r\n\r\n<table>", searches.Key.Name);

                foreach (var source in searches.GroupBy(m => m.AgendaItem.Meeting.Source))
                {
                    body.AppendFormat("<tr><td colspan=\"3\"><h4>{0}</h4></td></tr>\r\n", source.Key.Name);

                    foreach (var match in source.OrderBy(m => m.AgendaItem.Meeting.Date))
                    {
                        body.AppendFormat("<tr><td><a href=\"{1}\">{2}</a></td><td><a href=\"{1}\">{0}</a></td><td><a href=\"{1}\">{4}</a></td></tr>\r\n", match.AgendaItem.Meeting.BoardName, match.AgendaItem.Url, match.AgendaItem.Meeting.Date.ToString("dd.MM.yyyy"), match.AgendaItem.Meeting.Source.Name, match.AgendaItem.Title);
                    }
                }

                body.Append("</table>");
            }

            body.Append("</body></html>");

            string bodyString = body.ToString();

            MailMessage email = new MailMessage();
            email.From = new MailAddress(observer.SmtpSender);
            foreach (string to in observer.Emails)
                email.To.Add(new MailAddress(to, observer.Name));
            email.Subject = "Varsel om nye møter til " + observer.Name;

            email.Body = bodyString;
            email.IsBodyHtml = true;

            SmtpClient smtp = new SmtpClient(observer.SmtpServer, observer.SmtpPort);
            smtp.UseDefaultCredentials = false;
            smtp.Credentials = new NetworkCredential(observer.SmtpSender, observer.SmtpPassword);

            if (observer.SmtpUseSsl)
            {
                smtp.EnableSsl = true;

                NEVER_EAT_POISON_Disable_CertificateValidation();
            }

            await smtp.SendMailAsync(email);
        }

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
    }
}