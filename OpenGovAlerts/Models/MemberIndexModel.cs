using OpenGov.Models;
using System.Collections.Generic;

namespace OpenGovAlerts.Models
{
    public class MemberIndexModel
    {
        public List<Observer> Observers { get; internal set; }
        public List<Match> RecentMatches { get; internal set; }
    }
}
