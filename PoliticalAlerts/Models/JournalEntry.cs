using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PoliticalAlerts.Models
{
    public class JournalEntry
    {
        [Key]
        public int Id { get; set; }
        public string Title { get; set; }
        public Uri Url { get; set; }
        public string ExternalId { get; set; }
        public string Type { get; set; }
        public JournalType ParsedType { get; set; }
        public AgendaItem AgendaItem { get; set; }
        public IList<Document> Documents { get; set; }
        public DateTime? Date { get; set; }
        public string From { get; set; }
        public string To { get; set; }
    }
}