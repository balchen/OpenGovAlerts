using PoliticalAlerts.Models;
using System.Collections.Generic;

namespace PoliticalAlertsTask
{
    public class Config
    {
        public SmtpConfig Smtp { get; set; }
        public List<Client> Clients { get; set; }
        public List<Search> Searches { get; set; }
    }
}