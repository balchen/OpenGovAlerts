﻿using System;
using System.ComponentModel.DataAnnotations;

namespace PoliticalAlerts.Models
{
    public class Document
    {
        [Key]
        public int Id { get; set; }
        public AgendaItem AgendaItem { get; set; }
        public string Title { get; set; }
        public string Type { get; set; }
        public Uri Url { get; set; }
        public string Text { get; set; }
        public JournalEntry JournalEntry { get; set; }
    }
}