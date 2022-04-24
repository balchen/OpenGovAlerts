using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PoliticalAlerts.Models
{
    public class Source
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
        public IList<Meeting> Meetings { get; set; }
    }
}