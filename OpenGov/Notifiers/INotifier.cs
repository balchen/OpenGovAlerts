using OpenGov.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OpenGov.Notifiers
{
    public interface INotifier
    {
        Task Notify(IEnumerable<Match> matches, Observer observer);
    }
}