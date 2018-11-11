using System;

namespace OpenGov.Models
{
    public class Meeting
    {
        public string ClientId { get; set; }
        public DateTime Date { get; set; }
        public string Name { get; set; }
        public string Topic { get; set; }
        public Uri Url { get; set; }
        public string AgendaItemId { get; set; }
    }
}