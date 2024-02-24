namespace PoliticalAlertsWeb.Models
{
    public class AuthenticateResponse
    {
        public object Id { get; internal set; }
        public object Email { get; internal set; }
        public string Token { get; internal set; }
        public object Role { get; internal set; }
    }
}
