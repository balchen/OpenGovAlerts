using System.ComponentModel.DataAnnotations;

namespace OpenGov.Models
{
    public class StorageConfig
    {
        [Key]
        public int Id { get; set; }
        public string Url { get; set; }
    }
}
