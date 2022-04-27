using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PoliticalAlerts.Models
{
    public class ConsultationSearch
    {
        [Key]
        public int Id { get; set; }
        public Observer CreatedBy { get; set; }
        public DateTime Start { get; set; }
        public string Name { get; set; }
        public string Phrase { get; set; }
        public IList<ConsultationSearchSource> Sources { get; set; }
        public IList<ObserverConsultationSearch> Subscribers { get; set; }
        public IList<SeenJournalEntry> SeenJournalEntries { get; set; }
        public IList<ConsultationMatch> Matches { get; set; }

    }
}