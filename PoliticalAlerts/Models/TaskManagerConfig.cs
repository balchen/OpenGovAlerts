using System.ComponentModel.DataAnnotations;

namespace PoliticalAlerts.Models
{
    public class TaskManagerConfig
    {
        [Key]
        public int Id { get; set; }
        public string Url { get; set; }
    }
}