using OpenGov.Models;
using System;
using System.Threading.Tasks;

namespace OpenGov.Storage
{
    public interface IStorage
    {
        Task<Uri> AddDocument(Meeting meeting, Document document, string path = "");
    }
}