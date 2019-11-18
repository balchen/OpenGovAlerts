using System;

namespace OpenGov.Models
{
    public class Match
    {
        public int Id { get; set; }
        public Search Search { get; set;}
        public Meeting Meeting { get; set; }
        public DateTime TimeFound { get; set; }
        public DateTime? TimeNotified { get; set; }
        public string Excerpt { get; set; }
    }
}
