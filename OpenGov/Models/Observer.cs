using System.Collections.Generic;

namespace OpenGov.Models
{
    public class Observer
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string[] Emails { get; set; }
        public string SmtpSender { get; set; }
        public string SmtpPassword { get; set; }
        public string SmtpServer { get; set; }
        public int SmtpPort { get; set; }
        public bool SmtpUseSsl { get; set; }

        public IList<Search> Searches { get; set; }
        public IList<TaskManagerConfig> TaskManager { get; set; }
        public IList<StorageConfig> Storage { get; set; }
    }
}
