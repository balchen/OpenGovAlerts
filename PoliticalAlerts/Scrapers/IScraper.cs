using PoliticalAlerts.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PoliticalAlerts.Scrapers
{
    public interface IScraper
    {
        Task<IEnumerable<Meeting>> GetNewMeetings(ISet<string> seenMeetings);
        Task<IEnumerable<Document>> GetDocuments(AgendaItem item);
        Task<IEnumerable<Document>> GetCaseDocuments(string caseNumber);
    }
}