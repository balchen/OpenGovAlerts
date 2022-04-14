using System.ComponentModel.DataAnnotations;

namespace OpenGov.Models
{
    public class TaskManagerConfig
    {
        [Key]
        public int Id { get; set; }
        public string Url { get; set; }
    }
}