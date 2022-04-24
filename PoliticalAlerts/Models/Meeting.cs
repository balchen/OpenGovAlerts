using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PoliticalAlerts.Models
{
    public class Meeting
    {
        [Key]
        public int Id { get; set; }
        public Source Source { get; set; }
        public string ExternalId { get; set; }
        public string BoardId { get; set; }
        public string BoardName { get; set; }
        public DateTime Date { get; set; }
        public Uri Url { get; set; }

        public IList<Match> Matches { get; set; }
        public IList<AgendaItem> AgendaItems { get; set; }
    }
}