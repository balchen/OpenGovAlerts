namespace PoliticalAlerts.Models
{
    public class ConsultationSearchSource
    {
        public int ConsultationSearchId { get; set; }
        public ConsultationSearch ConsultationSearch { get; set; }

        public int SourceId { get; set; }
        public Source Source { get; set; }
    }
}
