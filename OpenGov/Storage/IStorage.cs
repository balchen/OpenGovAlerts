using OpenGov.Models;
using System.Threading.Tasks;

namespace OpenGov.Storage
{
    public interface IStorage
    {
        Task AddDocument(Meeting meeting, Document document);
    }
}