using System.ComponentModel.DataAnnotations;

namespace PoliticalAlerts.Models
{
    public class Storage
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
    }
}