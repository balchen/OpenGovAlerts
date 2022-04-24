using PoliticalAlerts.Models;
using System;
using System.Threading.Tasks;

namespace PoliticalAlerts.TaskManagers
{
    public interface ITaskManager
    {
        Task<Uri> AddTask(AgendaItem agendaItem);
    }
}
