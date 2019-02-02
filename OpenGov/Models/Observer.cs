namespace OpenGov.Models
{
    public class Observer
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public SmtpConfig SmtpConfig { get; set; }
        public string Email { get; set; }
    }
}
