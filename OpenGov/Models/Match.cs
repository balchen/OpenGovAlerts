using System;

namespace OpenGov.Models
{
    public class Match
    {
        public Search Search { get; set;}
        public Meeting Meeting { get; set; }
        public DateTime TimeFound { get; set; }
        public DateTime? TimeNotified { get; set; }
    }
}
