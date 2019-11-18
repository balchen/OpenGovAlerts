using System;
using System.Collections.Generic;

namespace OpenGov.Models
{
    public class Meeting
    {
        public int Id { get; set; }
        public Source Source { get; set; }
        public string BoardId { get; set; }
        public string BoardName { get; set; }
        public string MeetingId { get; set; }
        public string AgendaItemId { get; set; }
        public DateTime Date { get; set; }
        public string Title { get; set; }
        public Uri Url { get; set; }
        public Uri DocumentsUrl { get; set; }

        public IList<Match> Matches { get; set; }
        public IList<Document> Documents { get; set; }
    }
}