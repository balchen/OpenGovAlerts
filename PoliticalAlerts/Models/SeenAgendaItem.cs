using System;

namespace PoliticalAlerts.Models
{
    public class SeenAgendaItem
    {
        public int SearchId { get; set; }
        public Search Search { get; set; }

        public int AgendaItemId { get; set; }
        public AgendaItem AgendaItem { get; set; }

        public DateTime DateSeen { get; set; }
    }
}