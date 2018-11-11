using OpenGov.Models;
using System.Collections.Generic;

namespace OpenGovAlerts
{
    public class Config
    {
        public SmtpConfig Smtp { get; set; }
        public List<Client> Clients { get; set; }
        public List<Search> Searches { get; set; }
    }
}
