namespace PoliticalAlerts.Models
{
    public class UserObserver
    {
        public int UserId { get; set; }
        public User User { get; set; }
        public int ObserverId { get; set; }
        public Observer Observer { get; set; }
    }
}