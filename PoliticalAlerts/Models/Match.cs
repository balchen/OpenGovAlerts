using System;
using System.ComponentModel.DataAnnotations;

namespace PoliticalAlerts.Models
{
    public class Match
    {
        [Key]
        public int Id { get; set; }
        public Search Search { get; set;}
        public AgendaItem AgendaItem { get; set; }
        public DateTime TimeFound { get; set; }
        public DateTime? TimeNotified { get; set; }
        public string Excerpt { get; set; }
    }
}