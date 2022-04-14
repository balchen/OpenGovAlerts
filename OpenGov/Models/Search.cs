using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OpenGov.Models
{
    public class Search
    {
        [Key]
        public int Id { get; set; }
        public Observer CreatedBy { get; set; }
        public string Name { get; set; }
        public string Phrase { get; set; }
        public DateTime Start { get; set; }
        public IList<ObserverSearch> Subscribers { get; set; }
        public IList<SearchSource> Sources { get; set; }
        public IList<SeenAgendaItem> SeenAgendaItems { get; set; }
        public IList<Match> Matches { get; set; }
    }
}