using System;

namespace PoliticalAlerts.Models
{
    public class SeenJournalEntry
    {
        public int ConsultationSearchId { get; set; }
        public ConsultationSearch ConsultationSearch { get; set; }

        public int JournalEntryId { get; set; }
        public JournalEntry JournalEntry { get; set; }

        public DateTime DateSeen { get; set; }
    }
}