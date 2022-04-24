using PoliticalAlerts.Models;
using System;
using System.Collections.Generic;
using System.IO;

namespace PoliticalAlerts.Notifiers
{
    public class RazorFormatter
    {
        public Tuple<Stream, string> Format(IEnumerable<Match> matches, Observer observer, string template)
        {
            return null;
        }
    }
}