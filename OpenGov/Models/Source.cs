using System;
using System.Collections.Generic;

namespace OpenGov.Models
{
    public class Source
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
        public IList<Meeting> Meetings { get; set; }
    }
}