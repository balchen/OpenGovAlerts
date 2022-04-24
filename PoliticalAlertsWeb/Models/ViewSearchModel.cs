using PoliticalAlerts.Models;
using System.Collections.Generic;

namespace PoliticalAlertsWeb.Models
{
    public class ViewSearchModel
    {
        public Search Search { get; set; }
        public IList<ViewSearchSource> Sources { get; set; }
        public IList<Match> RecentMatches { get; set; }
    }

    public class ViewSearchSource
    {
        public Source Source { get; set; }
        public bool Selected { get; set; }
    }
}
