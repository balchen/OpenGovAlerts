using System;
using System.Collections.Generic;

namespace OpenGov.Models
{
    public class AgendaItem
    {
        public int Id { get; set; }
        public DateTime Retrieved { get; set; }

        public Meeting Meeting { get; set; }
        public string ExternalId { get; set; }
        public string Number { get; set; }
        public string Title { get; set; }
        public Uri Url { get; set; }

        public Uri DocumentsUrl { get; set; }

        public IList<Match> Matches { get; set; }
        public IList<Document> Documents { get; set; }
    }
}
