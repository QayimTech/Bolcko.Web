namespace Blocko.Services.Interfaces.Logistics
{
    public class GeoDistanceCalculationResult
    {
        public double DistanceKm { get; set; }
        public decimal ShippingFeeJod { get; set; }
        public string NearestHubNameAr { get; set; } = string.Empty;
        public string NearestHubNameEn { get; set; } = string.Empty;
        public int EstimatedTransitMinutes { get; set; }
        public string RecommendedVehicleType { get; set; } = string.Empty;
        public bool IsHeavyBulkTier { get; set; }
        public bool IsFreeShippingApplied { get; set; }
        public string PricingFormulaDescription { get; set; } = string.Empty;
    }

    public interface IGeoDistancePricingService
    {
        GeoDistanceCalculationResult CalculateFreightFee(double latitude, double longitude, decimal cartSubtotal, bool hasHeavyBulkItems, string? city = null);
        (double Lat, double Lng, string NameAr, string NameEn) GetNearestHub(double latitude, double longitude);
        double CalculateHaversineDistanceKm(double lat1, double lon1, double lat2, double lon2);
    }
}
