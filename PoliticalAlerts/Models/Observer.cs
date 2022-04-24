using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PoliticalAlerts.Models
{
    public class Observer
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public string[] Emails { get; set; }
        public string SmtpSender { get; set; }
        public string SmtpPassword { get; set; }
        public string SmtpServer { get; set; }
        public int SmtpPort { get; set; }
        public bool SmtpUseSsl { get; set; }

        public IList<Search> CreatedSearches { get; set; }
        public IList<ObserverSearch> SubscribedSearches { get; set; }
        public IList<TaskManagerConfig> TaskManager { get; set; }
        public IList<StorageConfig> Storage { get; set; }
    }
}