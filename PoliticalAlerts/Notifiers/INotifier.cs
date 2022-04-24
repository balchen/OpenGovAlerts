using PoliticalAlerts.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PoliticalAlerts.Notifiers
{
    public interface INotifier
    {
        Task Notify(IEnumerable<Match> matches, Observer observer);
    }
}