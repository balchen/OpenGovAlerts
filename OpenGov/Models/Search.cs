using System;
using System.Collections.Generic;

namespace OpenGov.Models
{
    public class Search
    {
        public int Id { get; set; }
        public Observer Observer { get; set; }
        public string Name { get; set; }
        public string Phrase { get; set; }
        public DateTime Start { get; set; }
        public IList<Meeting> SeenMeetings { get; set; }
        public IList<Match> Matches { get; set; }
    }
}