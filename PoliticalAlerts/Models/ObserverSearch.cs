using System;

namespace PoliticalAlerts.Models
{
    public class ObserverSearch
    {
        public int ObserverId { get; set; }
        public Observer Observer { get; set; }
        public int SearchId { get; set; }
        public Search Search { get; set; }
        public DateTime Activated { get; set; }
    }
}
