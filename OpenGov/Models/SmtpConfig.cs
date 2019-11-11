namespace OpenGov.Models
{
    public class SmtpConfig
    {
        public Observer Observer { get; set; }
        public string Sender { get; set; }
        public string Password { get; set; }
        public string Server { get; set; }
        public int Port { get; set; }
        public bool UseSsl { get; internal set; }
    }
}
