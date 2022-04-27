using System;
using System.ComponentModel.DataAnnotations;

namespace PoliticalAlerts.Models
{
    public class ConsultationMatch
    {
        [Key]
        public int Id { get; set; }
        public ConsultationSearch Search { get; set;}
        public JournalEntry JournalEntry { get; set; }
        public DateTime TimeFound { get; set; }
        public DateTime? TimeNotified { get; set; }
        public string Excerpt { get; set; }
    }
}