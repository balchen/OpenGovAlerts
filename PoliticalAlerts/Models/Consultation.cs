using System;
using System.ComponentModel.DataAnnotations;

namespace PoliticalAlerts.Models
{
    public class Consultation
    {
        [Key]
        public int Id { get; set; }
        public AgendaItem AgendaItem { get; set; }
        public JournalEntry JournalEntry { get; set; }
        public DateTime Discovered { get; set; }
    }
}