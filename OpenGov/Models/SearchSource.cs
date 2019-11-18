namespace OpenGov.Models
{
    public class SearchSource
    {
        public int SearchId { get; set; }
        public Search Search { get; set; }

        public int SourceId { get; set; }
        public Source Source { get; set; }
    }
}
