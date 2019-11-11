using System.Collections.Generic;

namespace OpenGov.Models
{
    public class Observer
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public IList<SmtpConfig> SmtpConfig { get; set; }
        public IList<TaskManagerConfig> TaskManager { get; set; }
        public IList<StorageConfig> Storage { get; set; }
    }
}
