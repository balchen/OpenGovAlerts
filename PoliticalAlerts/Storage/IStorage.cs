using PoliticalAlerts.Models;
using System;
using System.Threading.Tasks;

namespace PoliticalAlerts.Storage
{
    public interface IStorage
    {
        Task<Uri> AddDocument(AgendaItem item, Document document, string path = "");
    }
}