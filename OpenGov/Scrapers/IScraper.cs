using OpenGov.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OpenGov.Scrapers
{
    public interface IScraper
    {
        Task<IEnumerable<Meeting>> GetNewMeetings(ISet<string> seenMeetings);
        Task<IEnumerable<Document>> GetDocuments(AgendaItem item);
    }
}
