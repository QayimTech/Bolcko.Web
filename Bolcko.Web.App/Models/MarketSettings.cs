namespace Bolcko.Web.App.Models
{
    public class MarketSettings
    {
        public string Country { get; set; } = string.Empty;
        public string Currency { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public bool IsOnlineOnly { get; set; }
    }
}
