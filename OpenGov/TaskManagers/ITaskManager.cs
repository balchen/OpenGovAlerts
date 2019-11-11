using OpenGov.Models;
using System;
using System.Threading.Tasks;

namespace OpenGov.TaskManagers
{
    public interface ITaskManager
    {
        Task<Uri> AddTask(Meeting meeting);
    }
}
