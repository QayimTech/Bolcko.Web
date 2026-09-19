using Blocko.Services.Interfaces.Logistics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Blocko.Services.Implementations.Logistics
{
    public class GeoDistancePricingService : IGeoDistancePricingService
    {
        private readonly List<(double Lat, double Lng, string NameAr, string NameEn)> _fulfillmentHubs = new()
        {
            (31.8756, 35.9861, "مستودعات عمّان اللوجستية المركزية - مدينة سحاب الصناعية", "Amman Central Hub - Sahab"),
            (31.9539, 35.8450, "مركز توزيع غرب عمّان - بيادر وادي السير", "West Amman Hub - Bayader"),
            (32.0911, 36.1264, "محطة خلاطات وتوريد الزرقاء - الهاشمية والمنطقة الحرة", "Zarqa Batching Hub - Hashemiyeh"),
            (32.5568, 35.9520, "مجمع إربد والشمال اللوجستي - مدينة الحسن الصناعية", "Irbid North Hub - Al-Hassan"),
            (29.5320, 35.0078, "بوابة العقبة والميناء اللوجستية", "Aqaba Port Logistics Gateway"),
            (32.3421, 36.2081, "مستودع المفرق والبادية الشمالية", "Mafraq Regional Hub"),
            (31.1853, 35.7048, "محطة توزيع إقليم الجنوب - الكرك", "Karak & South Regional Hub")
        };

        public GeoDistanceCalculationResult CalculateFreightFee(double latitude, double longitude, decimal cartSubtotal, bool hasHeavyBulkItems, string? city = null)
        {
            // Validate coordinates: default to central Amman if invalid
            if (latitude < 29.0 || latitude > 33.5 || longitude < 34.5 || longitude > 39.5)
            {
                latitude = 31.9539;
                longitude = 35.9106;
            }

            var nearestHub = GetNearestHub(latitude, longitude);
            double distanceKm = CalculateHaversineDistanceKm(nearestHub.Lat, nearestHub.Lng, latitude, longitude);
            
            // Minimum distance buffer of 2.0 km
            distanceKm = Math.Max(2.0, Math.Round(distanceKm, 1));

            decimal shippingFee;
            bool isFreeShipping = false;
            string recommendedVehicle;
            string formulaDesc;

            if (hasHeavyBulkItems)
            {
                // Heavy / Bulk materials formula:
                // Base 12.00 JOD for first 15 km, then 0.45 JOD per additional km
                decimal baseHeavyFee = 12.00m;
                decimal additionalKmRate = 0.45m;
                double extraKm = Math.Max(0, distanceKm - 15.0);

                shippingFee = Math.Round(baseHeavyFee + ((decimal)extraKm * additionalKmRate), 2);
                recommendedVehicle = "شاحنة نقل ثقيل (تريلا مسطحة 30 طن) مع ونش تفريغ هيدروليكي";
                formulaDesc = $"تسعير مواد ثقيلة: 12 د.أ لأول 15 كم + 0.45 د.أ لكل كم إضافي (المسافة: {distanceKm} كم من {nearestHub.NameAr})";
            }
            else
            {
                // Standard Materials / Retail formula:
                // Base 2.50 JOD for first 10 km, then 0.20 JOD per additional km
                // Free shipping if cart >= 150 JOD and distance <= 25 km
                if (cartSubtotal >= 150.0m && distanceKm <= 25.0)
                {
                    shippingFee = 0.00m;
                    isFreeShipping = true;
                    recommendedVehicle = "فان شحن سريع (Express Freight Van)";
                    formulaDesc = $"توصيل مجاني مطبق (الطلبية تتجاوز 150 د.أ وضمن نطاق 25 كم من {nearestHub.NameAr})";
                }
                else
                {
                    decimal baseFee = 2.50m;
                    decimal additionalKmRate = 0.20m;
                    double extraKm = Math.Max(0, distanceKm - 10.0);

                    shippingFee = Math.Round(baseFee + ((decimal)extraKm * additionalKmRate), 2);
                    recommendedVehicle = "مركبة نقل بضائع متوسطة (3.5 طن)";
                    formulaDesc = $"تسعير ديناميكي: 2.5 د.أ لأول 10 كم + 0.20 د.أ لكل كم إضافي (المسافة: {distanceKm} كم من {nearestHub.NameAr})";
                }
            }

            // Estimate transit time: roughly 35 km/h average commercial freight speed + 15 min loading buffer
            int transitMinutes = (int)Math.Round(15 + (distanceKm / 35.0 * 60.0));

            return new GeoDistanceCalculationResult
            {
                DistanceKm = distanceKm,
                ShippingFeeJod = shippingFee,
                NearestHubNameAr = nearestHub.NameAr,
                NearestHubNameEn = nearestHub.NameEn,
                EstimatedTransitMinutes = transitMinutes,
                RecommendedVehicleType = recommendedVehicle,
                IsHeavyBulkTier = hasHeavyBulkItems,
                IsFreeShippingApplied = isFreeShipping,
                PricingFormulaDescription = formulaDesc
            };
        }

        public (double Lat, double Lng, string NameAr, string NameEn) GetNearestHub(double latitude, double longitude)
        {
            return _fulfillmentHubs
                .OrderBy(h => CalculateHaversineDistanceKm(h.Lat, h.Lng, latitude, longitude))
                .First();
        }

        public double CalculateHaversineDistanceKm(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371.0; // Earth's radius in kilometers
            double dLat = ToRadians(lat2 - lat1);
            double dLon = ToRadians(lon2 - lon1);

            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private static double ToRadians(double degrees)
        {
            return degrees * (Math.PI / 180.0);
        }
    }
}
