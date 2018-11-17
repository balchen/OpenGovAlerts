using System;
using System.Collections.Generic;

namespace OpenGov.Models
{
    public class Meeting
    {
        public string Phrase { get; set; }
        public string ClientId { get; set; }
        public string BoardId { get; set; }
        public string MeetingId { get; set; }
        public string AgendaItemId { get; set; }
        public DateTime Date { get; set; }
        public string Name { get; set; }
        public string Topic { get; set; }
        public Uri Url { get; set; }

        public IList<Document> Documents { get; set; }
    }
}