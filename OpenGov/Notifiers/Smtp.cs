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
    public class Smtp
    {
        public Smtp()
        {
        }

        public async Task Notify(IEnumerable<Match> matches, Observer observer)
        {
            MailMessage email = new MailMessage(observer.SmtpConfig.Sender, observer.Email);
            email.Subject = "Nye møter for " + observer.Name;

            StringBuilder body = new StringBuilder();

            foreach (var searches in matches.GroupBy(m => m.Search))
            {
                body.AppendFormat("<h3>Nye møter har dukket opp på kalenderen for {0}:</h3>\r\n\r\n<table>", searches.Key.Name);

                foreach (var match in searches.OrderByDescending(m => m.Meeting.Date))
                {
                    body.AppendFormat("<tr><td>{3}</td><td><a href=\"{1}\">{2}</a></td><td><a href=\"{1}\">{0}</a></td></tr>\r\n", match.Meeting.BoardName, match.Meeting.Url, match.Meeting.Date.ToString("dd.MM.yyyy"), match.Meeting.Source.Name);
                }

                body.Append("</table>");
            }

            email.Body = body.ToString();
            email.IsBodyHtml = true;

            SmtpClient smtp = new SmtpClient(observer.SmtpConfig.Server, observer.SmtpConfig.Port);
            smtp.UseDefaultCredentials = false;
            smtp.Credentials = new NetworkCredential(observer.SmtpConfig.Sender, observer.SmtpConfig.Password);

            if (observer.SmtpConfig.UseSsl)
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