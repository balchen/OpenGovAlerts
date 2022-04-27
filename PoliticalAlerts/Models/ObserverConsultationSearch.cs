using System;

namespace PoliticalAlerts.Models
{
    public class ObserverConsultationSearch
    {
        public int ObserverId { get; set; }
        public Observer Observer { get; set; }
        public int ConsultationSearchId { get; set; }
        public ConsultationSearch ConsultationSearch { get; set; }
        public DateTime Activated { get; set; }
    }
}
