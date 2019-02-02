using System;

namespace OpenGov.Models
{
    public class Document
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public Uri Url { get; set; }
        public string Text { get; set; }
    }
}