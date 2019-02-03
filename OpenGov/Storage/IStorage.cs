using OpenGov.Models;
using System.Threading.Tasks;

namespace OpenGov.Storage
{
    public interface IStorage
    {
        Task<string> AddDocument(Meeting meeting, Document document);
    }
}