using System;
using System.ComponentModel.DataAnnotations;

namespace PoliticalAlerts.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Username { get; set; }
        public byte[] PasswordSalt { get; set; }
        public byte[] PasswordHash { get; set; }
        public DateTime Created { get; set; }
        public DateTime LastLogin { get; set; }
        public int Role { get; set; }
    }
}