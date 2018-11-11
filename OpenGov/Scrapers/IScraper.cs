using OpenGov.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OpenGov.Scrapers
{
    public interface IScraper
    {
        Task<IEnumerable<Meeting>> FindMeetings(string phrase, ISet<string> seenMeetings);
        Task<IEnumerable<Document>> DownloadDocuments(Meeting meeting, string path);
    }
}
