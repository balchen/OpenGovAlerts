using System;

namespace OpenGov.Models
{
    public class SeenMeeting
    {
        public int SearchId { get; set; }
        public Search Search { get; set; }

        public int MeetingId { get; set; }
        public Meeting Meeting { get; set; }

        public DateTime DateSeen { get; set; }
    }
}